using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ColorApp.Controls
{
    public class ColorPicker : Button
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color?), typeof(ColorPicker),
                new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        private static readonly Color[] PredefinedColors = 
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Purple, Colors.Orange,
            Colors.Pink, Colors.Brown, Colors.Gray, Colors.Black, Colors.White, Colors.Cyan,
            Colors.Magenta, Colors.Lime, Colors.Navy, Colors.Maroon, Colors.Olive, Colors.Teal
        };

        public Color? SelectedColor
        {
            get { return (Color?)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker),
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public ColorPicker()
        {
            Click += ColorPicker_Click;
            UpdateBackground();
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            var colorPickerWindow = new Window
            {
                Title = "Select Color",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new UniformGrid
            {
                Columns = 6,
                Margin = new Thickness(10)
            };

            foreach (var color in PredefinedColors)
            {
                var colorButton = new Button
                {
                    Background = new SolidColorBrush(color),
                    Margin = new Thickness(2),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };

                colorButton.Click += (s, args) =>
                {
                    SelectedColor = color;
                    colorPickerWindow.DialogResult = true;
                    colorPickerWindow.Close();
                };

                grid.Children.Add(colorButton);
            }

            colorPickerWindow.Content = grid;
            colorPickerWindow.ShowDialog();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker colorPicker)
            {
                colorPicker.UpdateBackground();
            }
        }

        private void UpdateBackground()
        {
            if (SelectedColor.HasValue)
            {
                Background = new SolidColorBrush(SelectedColor.Value);
            }
            else
            {
                Background = Brushes.White;
            }
        }
    }
}
