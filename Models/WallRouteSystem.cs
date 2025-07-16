using System.Collections.Generic;
using System.Windows.Media;

namespace ColorApp.Models
{
    public class Route
    {
        public int Id { get; set; }
        public int WallIndex { get; set; }
        public int RouteIndex { get; set; }
        public Color? AssignedColor { get; set; }
        public bool IsFixed { get; set; }
        public GraphNode? CorrespondingNode { get; set; }

        public Route(int id, int wallIndex, int routeIndex)
        {
            Id = id;
            WallIndex = wallIndex;
            RouteIndex = routeIndex;
        }

        public string DisplayName => $"W{WallIndex}R{RouteIndex}";
    }

    public class Wall
    {
        public int Index { get; set; }
        public List<Route> Routes { get; set; } = new();

        public Wall(int index, int routeCount)
        {
            Index = index;
            for (int i = 0; i < routeCount; i++)
            {
                Routes.Add(new Route(0, index, i)); // ID will be set later
            }
        }
    }

    public class WallRouteSystem
    {
        public List<Wall> Walls { get; set; } = new();
        private int _nextRouteId = 0;

        public Wall AddWall(int routeCount)
        {
            var wall = new Wall(Walls.Count, routeCount);
            
            // Assign unique IDs to routes
            foreach (var route in wall.Routes)
            {
                route.Id = _nextRouteId++;
            }
            
            Walls.Add(wall);
            return wall;
        }

        public void RemoveLastWall()
        {
            if (Walls.Count > 0)
            {
                Walls.RemoveAt(Walls.Count - 1);
            }
        }

        public void Clear()
        {
            Walls.Clear();
            _nextRouteId = 0;
        }

        public Graph ConvertToGraph()
        {
            var graph = new Graph();
            var routeToNodeMap = new Dictionary<Route, GraphNode>();

            // Create nodes for all routes
            int nodeX = 50;
            int nodeY = 100;
            const int wallSpacing = 150;
            const int routeSpacing = 60;

            foreach (var wall in Walls)
            {
                int routeY = nodeY;
                foreach (var route in wall.Routes)
                {
                    var node = graph.AddNode(new System.Windows.Point(nodeX, routeY));
                    node.AssignedColor = route.AssignedColor;
                    node.IsFixed = route.IsFixed;
                    
                    routeToNodeMap[route] = node;
                    route.CorrespondingNode = node;
                    
                    routeY += routeSpacing;
                }
                nodeX += wallSpacing;
            }

            // Create edges within each wall (complete graph)
            foreach (var wall in Walls)
            {
                for (int i = 0; i < wall.Routes.Count; i++)
                {
                    for (int j = i + 1; j < wall.Routes.Count; j++)
                    {
                        var route1 = wall.Routes[i];
                        var route2 = wall.Routes[j];
                        
                        if (routeToNodeMap.TryGetValue(route1, out var node1) &&
                            routeToNodeMap.TryGetValue(route2, out var node2))
                        {
                            graph.AddEdge(node1, node2);
                        }
                    }
                }
            }

            // Create edges between adjacent walls (complete bipartite connection)
            for (int wallIndex = 0; wallIndex < Walls.Count - 1; wallIndex++)
            {
                var currentWall = Walls[wallIndex];
                var nextWall = Walls[wallIndex + 1];

                foreach (var route1 in currentWall.Routes)
                {
                    foreach (var route2 in nextWall.Routes)
                    {
                        if (routeToNodeMap.TryGetValue(route1, out var node1) &&
                            routeToNodeMap.TryGetValue(route2, out var node2))
                        {
                            graph.AddEdge(node1, node2);
                        }
                    }
                }
            }

            return graph;
        }

        public void SyncFromGraph(Graph graph)
        {
            // Update route colors based on corresponding nodes
            foreach (var wall in Walls)
            {
                foreach (var route in wall.Routes)
                {
                    if (route.CorrespondingNode != null)
                    {
                        route.AssignedColor = route.CorrespondingNode.AssignedColor;
                        route.IsFixed = route.CorrespondingNode.IsFixed;
                    }
                }
            }
        }

        public void AddRoute(Wall wall)
        {
            var newRoute = new Route(_nextRouteId++, wall.Index, wall.Routes.Count);
            wall.Routes.Add(newRoute);
        }

        public void RemoveRoute(Wall wall)
        {
            if (wall.Routes.Count > 0)
            {
                wall.Routes.RemoveAt(wall.Routes.Count - 1);
            }
        }

        public void MoveWallLeft(Wall wall)
        {
            var currentIndex = Walls.FindIndex(w => w == wall);
            if (currentIndex > 0)
            {
                Walls.RemoveAt(currentIndex);
                Walls.Insert(currentIndex - 1, wall);
                
                // Update wall indices
                for (int i = 0; i < Walls.Count; i++)
                {
                    Walls[i].Index = i;
                    foreach (var route in Walls[i].Routes)
                    {
                        route.WallIndex = i;
                    }
                }
            }
        }

        public void MoveWallRight(Wall wall)
        {
            var currentIndex = Walls.FindIndex(w => w == wall);
            if (currentIndex >= 0 && currentIndex < Walls.Count - 1)
            {
                Walls.RemoveAt(currentIndex);
                Walls.Insert(currentIndex + 1, wall);
                
                // Update wall indices
                for (int i = 0; i < Walls.Count; i++)
                {
                    Walls[i].Index = i;
                    foreach (var route in Walls[i].Routes)
                    {
                        route.WallIndex = i;
                    }
                }
            }
        }

        public void RemoveWall(Wall wall)
        {
            var index = Walls.FindIndex(w => w == wall);
            if (index >= 0)
            {
                Walls.RemoveAt(index);
                
                // Update wall indices
                for (int i = 0; i < Walls.Count; i++)
                {
                    Walls[i].Index = i;
                    foreach (var route in Walls[i].Routes)
                    {
                        route.WallIndex = i;
                    }
                }
            }
        }
    }
}
