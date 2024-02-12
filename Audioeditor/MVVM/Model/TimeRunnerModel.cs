using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor.MVVM.Model
{
    public class TimeRunnerModel
    {
        public int Position { get; set; }

        public Rectangle Figure { get; set; }

        public void InitFigures()
        {
            Figure = new Rectangle
            {
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#706E86"),
                Width = 1.4d,
                Height = 735
            };
            Figure.Tag = this;
        }

        public void MoveTo(double pixel)
        {
            Position = WFConverter.PixelToFrame(pixel);
            Canvas.SetLeft(Figure, pixel);
        }

        public SerializableTimeRunnerModel GetSerializableModel()
        {
            return new SerializableTimeRunnerModel
            {
                Position = Position
            };
        }

        public void UpdateFromSerializableModel(SerializableTimeRunnerModel serializableModel)
        {
            Position = serializableModel.Position;
        }
    }
}