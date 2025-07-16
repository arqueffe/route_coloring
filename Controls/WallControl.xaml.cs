using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ColorApp.Models;
using ColorApp.ViewModels;

namespace ColorApp.Controls
{
    public partial class WallControl : UserControl
    {
        public Wall? Wall { get; private set; }
        public event EventHandler<RouteColorChangedEventArgs>? RouteColorChanged;
        public event EventHandler<WallEventArgs>? WallMoveLeft;
        public event EventHandler<WallEventArgs>? WallMoveRight;
        public event EventHandler<WallEventArgs>? WallDelete;
        public event EventHandler<RouteEventArgs>? RouteAdded;
        public event EventHandler<RouteEventArgs>? RouteRemoved;

        private System.Collections.ObjectModel.ObservableCollection<ColorConstraintViewModel>? _availableColors;

        public WallControl()
        {
            InitializeComponent();
        }

        public void SetWall(Wall wall, System.Collections.ObjectModel.ObservableCollection<ColorConstraintViewModel> availableColors)
        {
            Wall = wall;
            _availableColors = availableColors;
            WallHeaderText.Text = $"Wall {wall.Index}";
            
            RefreshRoutesDisplay();
        }

        private Brush GetRouteColorBrush(Route route)
        {
            if (route.AssignedColor.HasValue)
            {
                return new SolidColorBrush(route.AssignedColor.Value);
            }
            return new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)); // BorderBrush color for dark theme
        }

        private void ShowRouteColorMenu(Route route, Button colorButton, System.Collections.ObjectModel.ObservableCollection<ColorConstraintViewModel> availableColors)
        {
            var contextMenu = new ContextMenu();

            // Add "Clear Color" option
            var clearItem = new MenuItem { Header = "Clear Color" };
            clearItem.Click += (s, e) =>
            {
                route.AssignedColor = null;
                route.IsFixed = false;
                colorButton.Background = GetRouteColorBrush(route);
                OnRouteColorChanged(route);
            };
            contextMenu.Items.Add(clearItem);

            if (availableColors.Count > 0)
            {
                contextMenu.Items.Add(new Separator());

                // Add color options
                foreach (var colorVM in availableColors)
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
                        route.AssignedColor = colorVM.Color;
                        route.IsFixed = true;
                        colorButton.Background = GetRouteColorBrush(route);
                        OnRouteColorChanged(route);
                    };

                    contextMenu.Items.Add(menuItem);
                }
            }

            contextMenu.PlacementTarget = colorButton;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void OnRouteColorChanged(Route route)
        {
            RouteColorChanged?.Invoke(this, new RouteColorChangedEventArgs(route));
        }

        public void UpdateRouteColors()
        {
            if (Wall == null) return;

            var routeCards = RoutesPanel.Children;
            for (int i = 0; i < routeCards.Count && i < Wall.Routes.Count; i++)
            {
                if (routeCards[i] is Border routeCard && 
                    routeCard.Child is StackPanel routePanel && 
                    routePanel.Children.Count > 1)
                {
                    var colorButton = routePanel.Children[1] as Button;
                    if (colorButton != null)
                    {
                        colorButton.Background = GetRouteColorBrush(Wall.Routes[i]);
                    }
                }
            }
        }

        private void RefreshRoutesDisplay()
        {
            if (Wall == null || _availableColors == null) return;
            
            RoutesPanel.Children.Clear();
            
            foreach (var route in Wall.Routes)
            {
                // Modern route card
                var routeCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(31, 41, 55)), // CardBrush equivalent for dark theme
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 4, 0, 0),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81)), // BorderBrush equivalent for dark theme
                    BorderThickness = new Thickness(1)
                };

                var routePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                // Route label with modern styling
                var routeLabel = new TextBlock
                {
                    Text = route.DisplayName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Medium,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(249, 250, 251)), // TextPrimaryBrush equivalent for dark theme
                    Width = 50,
                    Margin = new Thickness(0, 0, 12, 0)
                };
                routePanel.Children.Add(routeLabel);

                // Modern color indicator/button
                var colorButton = new Button
                {
                    Width = 40,
                    Height = 32,
                    Background = GetRouteColorBrush(route),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81)), // BorderBrush equivalent for dark theme
                    BorderThickness = new Thickness(1),
                    Tag = route,
                    Cursor = Cursors.Hand
                };

                // Add modern button template
                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                
                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                border.AppendChild(contentPresenter);
                
                template.VisualTree = border;
                colorButton.Template = template;

                colorButton.Click += (s, e) => ShowRouteColorMenu(route, colorButton, _availableColors);
                routePanel.Children.Add(colorButton);

                routeCard.Child = routePanel;
                RoutesPanel.Children.Add(routeCard);
            }
        }

        // Event handlers for wall controls
        private void MoveLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Wall != null)
                WallMoveLeft?.Invoke(this, new WallEventArgs(Wall));
        }

        private void MoveRightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Wall != null)
                WallMoveRight?.Invoke(this, new WallEventArgs(Wall));
        }

        private void DeleteWallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Wall != null)
                WallDelete?.Invoke(this, new WallEventArgs(Wall));
        }

        private void AddRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Wall != null)
            {
                // Create new route
                var newRoute = new Route(0, Wall.Index, Wall.Routes.Count); // ID will be updated by the system
                Wall.Routes.Add(newRoute);
                
                RefreshRoutesDisplay();
                RouteAdded?.Invoke(this, new RouteEventArgs(Wall, newRoute));
            }
        }

        private void RemoveRouteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Wall != null && Wall.Routes.Count > 0)
            {
                var lastRoute = Wall.Routes.Last();
                Wall.Routes.Remove(lastRoute);
                
                RefreshRoutesDisplay();
                RouteRemoved?.Invoke(this, new RouteEventArgs(Wall, lastRoute));
            }
        }
    }

    public class WallEventArgs : EventArgs
    {
        public Wall Wall { get; }

        public WallEventArgs(Wall wall)
        {
            Wall = wall;
        }
    }

    public class RouteEventArgs : EventArgs
    {
        public Wall Wall { get; }
        public Route Route { get; }

        public RouteEventArgs(Wall wall, Route route)
        {
            Wall = wall;
            Route = route;
        }
    }

    public class RouteColorChangedEventArgs : EventArgs
    {
        public Route Route { get; }

        public RouteColorChangedEventArgs(Route route)
        {
            Route = route;
        }
    }
}
