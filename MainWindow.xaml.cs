using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ColorApp.Models;
using ColorApp.Services;
using ColorApp.ViewModels;
using ColorApp.Controls;
using Microsoft.Win32;
using IOPath = System.IO.Path;

namespace ColorApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Graph _graph = new();
        private WallRouteSystem _wallRouteSystem = new();
        private GraphColoringSolver _solver = new();
        private GraphNode? _selectedNode;
        private GraphNode? _edgeStartNode;
        private bool _isDragging;
        private Point _lastMousePosition;
        private Dictionary<GraphNode, Ellipse> _nodeVisuals = new();
        private Dictionary<GraphEdge, Line> _edgeVisuals = new();
        private List<WallControl> _wallControls = new();
        private bool _isUpdatingFromWalls = false;

        public ObservableCollection<ColorConstraintViewModel> AvailableColors { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Add some default colors
            AddDefaultColors();
            UpdateStatus();
        }

        private void AddDefaultColors()
        {
            var defaultColors = new[]
            {
                (Colors.Red, "Red"),
                (Colors.Blue, "Blue"),
                (Colors.Green, "Green"),
                (Colors.Yellow, "Yellow"),
                (Colors.Purple, "Purple"),
                (Colors.Orange, "Orange")
            };

            foreach (var (color, name) in defaultColors)
            {
                AvailableColors.Add(new ColorConstraintViewModel
                {
                    Color = color,
                    Name = name,
                    MaxUsage = 1,
                    ColorBrush = new SolidColorBrush(color)
                });
            }

            UpdateGraphColors();
        }

        private void UpdateGraphColors()
        {
            _graph.AvailableColors.Clear();
            foreach (var colorVM in AvailableColors)
            {
                _graph.AvailableColors.Add(new ColorConstraint(colorVM.Color, colorVM.MaxUsage, colorVM.Name));
            }
        }

        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GraphCanvas.Focus();
            var position = e.GetPosition(GraphCanvas);
            var clickedNode = _graph.GetNodeAt(position);

            if (AddNodeMode.IsChecked == true)
            {
                if (clickedNode == null)
                {
                    var node = _graph.AddNode(position);
                    CreateNodeVisual(node);
                    UpdateStatus();
                }
            }
            else if (AddEdgeMode.IsChecked == true)
            {
                if (clickedNode != null)
                {
                    if (_edgeStartNode == null)
                    {
                        _edgeStartNode = clickedNode;
                        HighlightNode(clickedNode, true);
                    }
                    else if (_edgeStartNode != clickedNode)
                    {
                        var edge = _graph.AddEdge(_edgeStartNode, clickedNode);
                        if (edge != null)
                        {
                            CreateEdgeVisual(edge);
                            UpdateStatus();
                        }
                        HighlightNode(_edgeStartNode, false);
                        _edgeStartNode = null;
                    }
                }
            }
            else if (SelectMode.IsChecked == true)
            {
                if (clickedNode != null)
                {
                    SelectNode(clickedNode);
                    _isDragging = true;
                    _lastMousePosition = position;
                    GraphCanvas.CaptureMouse();
                }
                else
                {
                    SelectNode(null);
                }
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(GraphCanvas);
                var deltaX = currentPosition.X - _lastMousePosition.X;
                var deltaY = currentPosition.Y - _lastMousePosition.Y;

                _selectedNode.Position = new Point(
                    _selectedNode.Position.X + deltaX,
                    _selectedNode.Position.Y + deltaY);

                UpdateNodeVisual(_selectedNode);
                UpdateConnectedEdges(_selectedNode);

                _lastMousePosition = currentPosition;
            }
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                GraphCanvas.ReleaseMouseCapture();
            }
        }

        private void GraphCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GraphCanvas);
            var clickedNode = _graph.GetNodeAt(position);

            if (clickedNode != null && AvailableColors.Count > 0)
            {
                ShowColorContextMenu(clickedNode, position);
            }
        }

        private void ShowColorContextMenu(GraphNode node, Point position)
        {
            var contextMenu = new ContextMenu();

            // Add "Clear Color" option
            var clearItem = new MenuItem { Header = "Clear Color" };
            clearItem.Click += (s, e) =>
            {
                node.AssignedColor = null;
                node.IsFixed = false;
                UpdateNodeVisual(node);
            };
            contextMenu.Items.Add(clearItem);

            contextMenu.Items.Add(new Separator());

            // Add color options
            foreach (var colorVM in AvailableColors)
            {
                var menuItem = new MenuItem { Header = colorVM.Name };
                
                // Create a colored rectangle for the menu item
                var rect = new Rectangle
                {
                    Width = 16,
                    Height = 16,
                    Fill = colorVM.ColorBrush,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)), // TextPrimaryBrush color
                    StrokeThickness = 1
                };
                menuItem.Icon = rect;

                menuItem.Click += (s, e) =>
                {
                    node.AssignedColor = colorVM.Color;
                    node.IsFixed = true;
                    UpdateNodeVisual(node);
                };

                contextMenu.Items.Add(menuItem);
            }

            contextMenu.PlacementTarget = GraphCanvas;
            contextMenu.PlacementRectangle = new Rect(position, new Size(0, 0));
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            contextMenu.IsOpen = true;
        }

        private void GraphCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _selectedNode != null)
            {
                RemoveNodeVisual(_selectedNode);
                _graph.RemoveNode(_selectedNode);
                _selectedNode = null;
                UpdateStatus();
            }
            else if (e.Key == Key.Escape)
            {
                if (_edgeStartNode != null)
                {
                    HighlightNode(_edgeStartNode, false);
                    _edgeStartNode = null;
                }
                SelectNode(null);
            }
        }

        private void CreateNodeVisual(GraphNode node)
        {
            var ellipse = new Ellipse
            {
                Width = node.Radius * 2,
                Height = node.Radius * 2,
                Fill = GetNodeFillBrush(node),
                Stroke = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)), // TextPrimaryBrush color
                StrokeThickness = 2
            };

            Canvas.SetLeft(ellipse, node.Position.X - node.Radius);
            Canvas.SetTop(ellipse, node.Position.Y - node.Radius);

            GraphCanvas.Children.Add(ellipse);
            _nodeVisuals[node] = ellipse;

            // Add node ID text
            var text = new TextBlock
            {
                Text = node.Id.ToString(),
                Foreground = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)), // TextPrimaryBrush for dark theme
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(text, node.Position.X - 10);
            Canvas.SetTop(text, node.Position.Y - 10);
            GraphCanvas.Children.Add(text);
        }

        private void CreateEdgeVisual(GraphEdge edge)
        {
            var line = new Line
            {
                X1 = edge.From.Position.X,
                Y1 = edge.From.Position.Y,
                X2 = edge.To.Position.X,
                Y2 = edge.To.Position.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)), // SecondaryBrush color
                StrokeThickness = 2
            };

            GraphCanvas.Children.Insert(0, line); // Add behind nodes
            _edgeVisuals[edge] = line;
        }

        private void UpdateNodeVisual(GraphNode node)
        {
            if (_nodeVisuals.TryGetValue(node, out var ellipse))
            {
                Canvas.SetLeft(ellipse, node.Position.X - node.Radius);
                Canvas.SetTop(ellipse, node.Position.Y - node.Radius);
                ellipse.Fill = GetNodeFillBrush(node);
            }

            // Update text position
            foreach (var child in GraphCanvas.Children.OfType<TextBlock>())
            {
                if (child.Text == node.Id.ToString())
                {
                    Canvas.SetLeft(child, node.Position.X - 10);
                    Canvas.SetTop(child, node.Position.Y - 10);
                    break;
                }
            }
        }

        private void UpdateConnectedEdges(GraphNode node)
        {
            foreach (var edge in _graph.Edges.Where(e => e.From == node || e.To == node))
            {
                if (_edgeVisuals.TryGetValue(edge, out var line))
                {
                    line.X1 = edge.From.Position.X;
                    line.Y1 = edge.From.Position.Y;
                    line.X2 = edge.To.Position.X;
                    line.Y2 = edge.To.Position.Y;
                }
            }
        }

        private Brush GetNodeFillBrush(GraphNode node)
        {
            if (node.AssignedColor.HasValue)
            {
                var brush = new SolidColorBrush(node.AssignedColor.Value);
                if (node.IsFixed)
                {
                    // Add a border pattern for fixed colors
                    return brush;
                }
                return brush;
            }
            return new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)); // BorderBrush color
        }

        private void SelectNode(GraphNode? node)
        {
            // Remove previous selection highlight
            if (_selectedNode != null)
            {
                HighlightNode(_selectedNode, false);
            }

            _selectedNode = node;

            // Add new selection highlight
            if (_selectedNode != null)
            {
                HighlightNode(_selectedNode, true);
            }
        }

        private void HighlightNode(GraphNode node, bool highlight)
        {
            if (_nodeVisuals.TryGetValue(node, out var ellipse))
            {
                ellipse.StrokeThickness = highlight ? 4 : 2;
                ellipse.Stroke = highlight ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB)); // ErrorBrush : TextPrimaryBrush
            }
        }

        private void RemoveNodeVisual(GraphNode node)
        {
            if (_nodeVisuals.TryGetValue(node, out var ellipse))
            {
                GraphCanvas.Children.Remove(ellipse);
                _nodeVisuals.Remove(node);
            }

            // Remove connected edges
            var connectedEdges = _graph.Edges.Where(e => e.From == node || e.To == node).ToList();
            foreach (var edge in connectedEdges)
            {
                if (_edgeVisuals.TryGetValue(edge, out var line))
                {
                    GraphCanvas.Children.Remove(line);
                    _edgeVisuals.Remove(edge);
                }
            }

            // Remove text
            var textToRemove = GraphCanvas.Children.OfType<TextBlock>()
                .FirstOrDefault(t => t.Text == node.Id.ToString());
            if (textToRemove != null)
            {
                GraphCanvas.Children.Remove(textToRemove);
            }
        }

        private void AddColorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MaxUsageBox.Text, out int maxUsage) && maxUsage > 0)
            {
                var color = NewColorPicker.SelectedColor ?? Colors.Black;
                var colorName = $"Color{AvailableColors.Count + 1}";

                AvailableColors.Add(new ColorConstraintViewModel
                {
                    Color = color,
                    Name = colorName,
                    MaxUsage = maxUsage,
                    ColorBrush = new SolidColorBrush(color)
                });

                UpdateGraphColors();
                MaxUsageBox.Text = "1";
            }
        }

        private void RemoveColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ColorConstraintViewModel colorVM)
            {
                AvailableColors.Remove(colorVM);
                UpdateGraphColors();

                // Clear any nodes using this color
                foreach (var node in _graph.Nodes)
                {
                    if (node.AssignedColor.HasValue && ColorsEqual(node.AssignedColor.Value, colorVM.Color))
                    {
                        node.AssignedColor = null;
                        node.IsFixed = false;
                        UpdateNodeVisual(node);
                    }
                }
            }
        }

        private bool ColorsEqual(Color c1, Color c2)
        {
            return c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }

        private void ClearGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            _graph = new Graph();
            _nodeVisuals.Clear();
            _edgeVisuals.Clear();
            _selectedNode = null;
            _edgeStartNode = null;
            GraphCanvas.Children.Clear();
            
            // Also clear walls and routes
            ClearWallsAndRoutes();
            
            UpdateGraphColors();
            UpdateStatus();
        }

        private void SolveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_graph.Nodes.Count == 0)
            {
                StatusText.Text = "No nodes to color!";
                return;
            }

            if (AvailableColors.Count == 0)
            {
                StatusText.Text = "No colors available!";
                return;
            }

            StatusText.Text = "Solving...";
            
            try
            {
                var solution = _solver.SolveGraphColoring(_graph);

                if (solution.IsSatisfiable)
                {
                    // Apply the solution
                    foreach (var kvp in solution.NodeColors)
                    {
                        var node = _graph.Nodes.FirstOrDefault(n => n.Id == kvp.Key);
                        if (node != null && !node.IsFixed)
                        {
                            node.AssignedColor = kvp.Value;
                            UpdateNodeVisual(node);
                        }
                    }
                    StatusText.Text = "Solution found!";
                }
                else
                {
                    StatusText.Text = solution.ErrorMessage ?? "No solution exists.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void ClearSolutionBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _graph.Nodes.Where(n => !n.IsFixed))
            {
                node.AssignedColor = null;
                UpdateNodeVisual(node);
            }
            StatusText.Text = "Solution cleared.";
        }

        private void UpdateStatus()
        {
            NodeCountText.Text = $"Nodes: {_graph.Nodes.Count}";
            EdgeCountText.Text = $"Edges: {_graph.Edges.Count}";
            
            if (StatusText.Text == "Ready" || StatusText.Text.Contains("Nodes:") || StatusText.Text.Contains("Edges:"))
            {
                StatusText.Text = "Ready";
            }
        }

        // Walls & Routes Tab Methods
        private void AddWallBtn_Click(object sender, RoutedEventArgs e)
        {
            var wall = _wallRouteSystem.AddWall(3); // Always add wall with 3 routes
            CreateWallControl(wall);
            UpdateFromWallSystem();
            UpdateStatus();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl.SelectedItem == WallsRoutesTab)
            {
                // Switching to Walls & Routes tab - sync from graph if needed
                SyncWallsFromGraph();
            }
            else if (MainTabControl.SelectedItem == GraphViewTab)
            {
                // Switching to Graph tab - sync from walls if needed
                if (!_isUpdatingFromWalls)
                {
                    UpdateFromWallSystem();
                }
            }
        }

        private void CreateWallControl(Wall wall)
        {
            var wallControl = new WallControl();
            wallControl.SetWall(wall, AvailableColors);
            wallControl.RouteColorChanged += WallControl_RouteColorChanged;
            wallControl.WallMoveLeft += WallControl_WallMoveLeft;
            wallControl.WallMoveRight += WallControl_WallMoveRight;
            wallControl.WallDelete += WallControl_WallDelete;
            wallControl.RouteAdded += WallControl_RouteAdded;
            wallControl.RouteRemoved += WallControl_RouteRemoved;
            
            WallsPanel.Children.Add(wallControl);
            _wallControls.Add(wallControl);
        }

        private void WallControl_RouteColorChanged(object? sender, RouteColorChangedEventArgs e)
        {
            // Update the corresponding node in the graph
            if (e.Route.CorrespondingNode != null)
            {
                e.Route.CorrespondingNode.AssignedColor = e.Route.AssignedColor;
                e.Route.CorrespondingNode.IsFixed = e.Route.IsFixed;
                UpdateNodeVisual(e.Route.CorrespondingNode);
            }
        }

        private void UpdateFromWallSystem()
        {
            if (_wallRouteSystem.Walls.Count == 0) return;

            _isUpdatingFromWalls = true;
            
            // Clear existing graph visuals
            _nodeVisuals.Clear();
            _edgeVisuals.Clear();
            _selectedNode = null;
            _edgeStartNode = null;
            GraphCanvas.Children.Clear();

            // Generate new graph from wall system
            _graph = _wallRouteSystem.ConvertToGraph();
            _graph.AvailableColors.Clear();
            foreach (var colorVM in AvailableColors)
            {
                _graph.AvailableColors.Add(new ColorConstraint(colorVM.Color, colorVM.MaxUsage, colorVM.Name));
            }

            // Create visuals for the new graph
            foreach (var node in _graph.Nodes)
            {
                CreateNodeVisual(node);
            }

            foreach (var edge in _graph.Edges)
            {
                CreateEdgeVisual(edge);
            }

            _isUpdatingFromWalls = false;
            UpdateStatus();
        }

        private void SyncWallsFromGraph()
        {
            _wallRouteSystem.SyncFromGraph(_graph);
            
            // Update wall control displays
            foreach (var wallControl in _wallControls)
            {
                wallControl.UpdateRouteColors();
            }
        }

        private void ClearWallsAndRoutes()
        {
            _wallRouteSystem.Clear();
            _wallControls.Clear();
            WallsPanel.Children.Clear();
        }

        private void WallControl_WallMoveLeft(object? sender, WallEventArgs e)
        {
            _wallRouteSystem.MoveWallLeft(e.Wall);
            RefreshWallControls();
            UpdateFromWallSystem();
        }

        private void WallControl_WallMoveRight(object? sender, WallEventArgs e)
        {
            _wallRouteSystem.MoveWallRight(e.Wall);
            RefreshWallControls();
            UpdateFromWallSystem();
        }

        private void WallControl_WallDelete(object? sender, WallEventArgs e)
        {
            _wallRouteSystem.RemoveWall(e.Wall);
            RefreshWallControls();
            UpdateFromWallSystem();
            UpdateStatus();
        }

        private void WallControl_RouteAdded(object? sender, RouteEventArgs e)
        {
            // Route was already added to the wall, just update the system
            UpdateFromWallSystem();
            UpdateStatus();
        }

        private void WallControl_RouteRemoved(object? sender, RouteEventArgs e)
        {
            // Route was already removed from the wall, just update the system
            UpdateFromWallSystem();
            UpdateStatus();
        }

        private void RefreshWallControls()
        {
            _wallControls.Clear();
            WallsPanel.Children.Clear();
            
            foreach (var wall in _wallRouteSystem.Walls)
            {
                CreateWallControl(wall);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Export/Import Event Handlers
        private void ExportColorsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Color Setup Files (*.colors)|*.colors|All Files (*.*)|*.*",
                    DefaultExt = "colors",
                    Title = "Export Color Setup"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportImportService.ExportColorSetup(AvailableColors, saveFileDialog.FileName);
                    StatusText.Text = $"Color setup exported to {IOPath.GetFileName(saveFileDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting colors: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportColorsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Color Setup Files (*.colors)|*.colors|All Files (*.*)|*.*",
                    Title = "Import Color Setup"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ExportImportService.ImportColorSetup(openFileDialog.FileName, AvailableColors, () =>
                    {
                        UpdateGraphColors();
                        StatusText.Text = $"Color setup imported from {IOPath.GetFileName(openFileDialog.FileName)}";
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing colors: {ex.Message}", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportWallsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Wall Setup Files (*.walls)|*.walls|All Files (*.*)|*.*",
                    DefaultExt = "walls",
                    Title = "Export Wall/Route Setup"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportImportService.ExportWallRouteSetup(_wallRouteSystem, saveFileDialog.FileName);
                    StatusText.Text = $"Wall setup exported to {IOPath.GetFileName(saveFileDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting walls: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportWallsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Wall Setup Files (*.walls)|*.walls|All Files (*.*)|*.*",
                    Title = "Import Wall/Route Setup"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ExportImportService.ImportWallRouteSetup(openFileDialog.FileName, _wallRouteSystem, () =>
                    {
                        // Clear existing wall controls
                        WallsPanel.Children.Clear();
                        _wallControls.Clear();
                        
                        // Recreate wall controls for imported walls
                        foreach (var wall in _wallRouteSystem.Walls)
                        {
                            CreateWallControl(wall);
                        }
                        
                        UpdateFromWallSystem();
                        UpdateStatus();
                        StatusText.Text = $"Wall setup imported from {IOPath.GetFileName(openFileDialog.FileName)}";
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing walls: {ex.Message}", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
