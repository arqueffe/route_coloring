using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;
using ColorApp.Models;
using ColorApp.ViewModels;

namespace ColorApp.Services
{
    public class ExportImportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void ExportWallRouteSetup(WallRouteSystem wallRouteSystem, string filePath)
        {
            var exportData = new WallRouteExportData();

            foreach (var wall in wallRouteSystem.Walls)
            {
                var wallData = new WallData
                {
                    Index = wall.Index
                };

                foreach (var route in wall.Routes)
                {
                    var routeData = new RouteData
                    {
                        Id = route.Id,
                        WallIndex = route.WallIndex,
                        RouteIndex = route.RouteIndex,
                        DisplayName = route.DisplayName,
                        IsFixed = route.IsFixed,
                        AssignedColor = route.AssignedColor.HasValue ? 
                            ColorData.FromColor(route.AssignedColor.Value) : null
                    };
                    wallData.Routes.Add(routeData);
                }

                exportData.Walls.Add(wallData);
            }

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        public static void ImportWallRouteSetup(string filePath, WallRouteSystem wallRouteSystem, Action onImportComplete)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Import file not found.");

            var json = File.ReadAllText(filePath);
            var importData = JsonSerializer.Deserialize<WallRouteExportData>(json, JsonOptions);

            if (importData == null)
                throw new InvalidOperationException("Invalid import data.");

            // Clear existing walls
            wallRouteSystem.Walls.Clear();

            // Import walls and routes
            foreach (var wallData in importData.Walls)
            {
                var wall = new Wall(wallData.Index, wallData.Routes.Count);
                wall.Routes.Clear(); // Clear the auto-generated routes
                
                foreach (var routeData in wallData.Routes)
                {
                    var route = new Route(routeData.Id, routeData.WallIndex, routeData.RouteIndex)
                    {
                        IsFixed = routeData.IsFixed,
                        AssignedColor = routeData.AssignedColor?.ToColor()
                    };
                    wall.Routes.Add(route);
                }

                wallRouteSystem.Walls.Add(wall);
            }

            onImportComplete?.Invoke();
        }

        public static void ExportColorSetup(IEnumerable<ColorConstraintViewModel> colors, string filePath)
        {
            var exportData = new ColorSetupExportData();

            foreach (var color in colors)
            {
                var colorData = new ColorConstraintData
                {
                    Color = ColorData.FromColor(color.Color),
                    Name = color.Name,
                    MaxUsage = color.MaxUsage
                };
                exportData.Colors.Add(colorData);
            }

            var json = JsonSerializer.Serialize(exportData, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        public static void ImportColorSetup(string filePath, 
            System.Collections.ObjectModel.ObservableCollection<ColorConstraintViewModel> colors, 
            Action onImportComplete)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Import file not found.");

            var json = File.ReadAllText(filePath);
            var importData = JsonSerializer.Deserialize<ColorSetupExportData>(json, JsonOptions);

            if (importData == null)
                throw new InvalidOperationException("Invalid import data.");

            // Clear existing colors
            colors.Clear();

            // Import colors
            foreach (var colorData in importData.Colors)
            {
                var colorVM = new ColorConstraintViewModel
                {
                    Color = colorData.Color.ToColor(),
                    Name = colorData.Name,
                    MaxUsage = colorData.MaxUsage,
                    ColorBrush = new SolidColorBrush(colorData.Color.ToColor())
                };
                colors.Add(colorVM);
            }

            onImportComplete?.Invoke();
        }
    }
}
