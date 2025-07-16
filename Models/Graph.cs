using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ColorApp.Models
{
    public class ColorConstraint
    {
        public Color Color { get; set; }
        public int MaxUsage { get; set; }
        public string Name { get; set; }

        public ColorConstraint(Color color, int maxUsage, string name)
        {
            Color = color;
            MaxUsage = maxUsage;
            Name = name;
        }
    }

    public class Graph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
        public List<ColorConstraint> AvailableColors { get; set; } = new();
        private int _nextNodeId = 0;

        public GraphNode AddNode(System.Windows.Point position)
        {
            var node = new GraphNode(_nextNodeId++, position);
            Nodes.Add(node);
            return node;
        }

        public void RemoveNode(GraphNode node)
        {
            Nodes.Remove(node);
            Edges.RemoveAll(e => e.From == node || e.To == node);
        }

        public GraphEdge? AddEdge(GraphNode from, GraphNode to)
        {
            if (from == to) return null;
            
            var newEdge = new GraphEdge(from, to);
            if (!Edges.Contains(newEdge))
            {
                Edges.Add(newEdge);
                return newEdge;
            }
            return null;
        }

        public void RemoveEdge(GraphEdge edge)
        {
            Edges.Remove(edge);
        }

        public GraphNode? GetNodeAt(System.Windows.Point point)
        {
            return Nodes.FirstOrDefault(n => n.ContainsPoint(point));
        }

        public List<GraphNode> GetNeighbors(GraphNode node)
        {
            var neighbors = new List<GraphNode>();
            foreach (var edge in Edges)
            {
                if (edge.From == node)
                    neighbors.Add(edge.To);
                else if (edge.To == node)
                    neighbors.Add(edge.From);
            }
            return neighbors;
        }
    }
}
