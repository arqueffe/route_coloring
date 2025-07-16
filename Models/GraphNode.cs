using System;
using System.Windows;
using System.Windows.Media;

namespace ColorApp.Models
{
    public class GraphNode
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public Color? AssignedColor { get; set; }
        public bool IsFixed { get; set; } // For partial coloring
        public double Radius { get; set; } = 20;

        public GraphNode(int id, Point position)
        {
            Id = id;
            Position = position;
        }

        public bool ContainsPoint(Point point)
        {
            var distance = Math.Sqrt(
                Math.Pow(point.X - Position.X, 2) + 
                Math.Pow(point.Y - Position.Y, 2));
            return distance <= Radius;
        }
    }
}
