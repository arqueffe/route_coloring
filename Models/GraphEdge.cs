using System;

namespace ColorApp.Models
{
    public class GraphEdge
    {
        public GraphNode From { get; set; }
        public GraphNode To { get; set; }

        public GraphEdge(GraphNode from, GraphNode to)
        {
            From = from;
            To = to;
        }

        public override bool Equals(object? obj)
        {
            if (obj is GraphEdge other)
            {
                return (From.Id == other.From.Id && To.Id == other.To.Id) ||
                       (From.Id == other.To.Id && To.Id == other.From.Id);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Ensure hash is same regardless of direction
            var id1 = Math.Min(From.Id, To.Id);
            var id2 = Math.Max(From.Id, To.Id);
            return HashCode.Combine(id1, id2);
        }
    }
}
