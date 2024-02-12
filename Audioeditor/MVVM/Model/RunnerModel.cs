using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Audioeditor.MVVM.Model
{
    public class RunnerModel
    {
        public int Position { get; set; }

        public Rectangle Figure { get; set; }

        public void InitFigures()
        {
            Figure = new Rectangle
            {
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FA5959"),
                Width = 1d,
                Height = 735,
                Visibility = Visibility.Collapsed
            };
        }

        public void Hide()
        {
            Application.Current.Dispatcher.Invoke(() => { Figure.Visibility = Visibility.Collapsed; });
        }

        public void View()
        {
            Application.Current.Dispatcher.Invoke(() => { Figure.Visibility = Visibility.Visible; Panel.SetZIndex(Figure, 8); });
        }

        public void MoveTo(int frame)
        {
            Position = frame;
            Application.Current.Dispatcher.Invoke(() => { Canvas.SetLeft(Figure, WFConverter.FrameToPixel(frame)); });
        }
    }
}