using System.Collections.Generic;
using System.Windows.Media;

namespace ColorApp.Models
{
    public class WallRouteExportData
    {
        public List<WallData> Walls { get; set; } = new List<WallData>();
    }

    public class WallData
    {
        public int Index { get; set; }
        public List<RouteData> Routes { get; set; } = new List<RouteData>();
    }

    public class RouteData
    {
        public int Id { get; set; }
        public int WallIndex { get; set; }
        public int RouteIndex { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public ColorData? AssignedColor { get; set; }
        public bool IsFixed { get; set; }
    }

    public class ColorSetupExportData
    {
        public List<ColorConstraintData> Colors { get; set; } = new List<ColorConstraintData>();
    }

    public class ColorConstraintData
    {
        public ColorData Color { get; set; } = new ColorData();
        public string Name { get; set; } = string.Empty;
        public int MaxUsage { get; set; }
    }

    public class ColorData
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = 255;

        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }

        public static ColorData FromColor(Color color)
        {
            return new ColorData
            {
                R = color.R,
                G = color.G,
                B = color.B,
                A = color.A
            };
        }
    }
}
