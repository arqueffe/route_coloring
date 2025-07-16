using System.ComponentModel;
using System.Windows.Media;

namespace ColorApp.ViewModels
{
    public class ColorConstraintViewModel : INotifyPropertyChanged
    {
        private Color _color;
        private string _name = string.Empty;
        private int _maxUsage;
        private Brush _colorBrush = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51)); // BorderBrush color for dark theme

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
                ColorBrush = new SolidColorBrush(value);
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public int MaxUsage
        {
            get => _maxUsage;
            set
            {
                _maxUsage = value;
                OnPropertyChanged(nameof(MaxUsage));
            }
        }

        public Brush ColorBrush
        {
            get => _colorBrush;
            set
            {
                _colorBrush = value;
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
