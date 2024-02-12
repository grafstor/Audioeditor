using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor.MVVM.Model
{
    public class SoundBlock : FrameworkElement
    {
        public int Length { get; set; }
        public int Position { get; set; }
        public int TrackId { get; set; }
        public FileModel File { get; set; }
        public string Name { get; set; }
        public Rectangle Figure { get; set; }
        public TextBlock TextFigure { get; set; }
        public WaveformVisual WaveFigure { get; set; }

        private float opacity;
        public float Opacity
        {
            get => opacity;
            set
            {
                if (opacity != value)
                {
                    opacity = value;
                    Figure.Opacity = value;
                    TextFigure.Opacity = value;
                    WaveFigure.Opacity = value;
                }
            }
        }
        
        private double height;
        public double Height
        {
            get => height;
            set
            {
                height = value;
                Figure.Height = value;
                WaveFigure.Height = value - 40;
                WaveFigure.UpdatePicks();
            }
        }

        private double width;
        public double Width
        {
            get => width;
            set
            {
                UpdateCornerRadius(value);

                width = value < 1 ? 1 : value;

                Figure.Width = width;
                WaveFigure.Width = width == 1 ? 0 : width;
                TextFigure.Width = width - 9 < 0 ? 0 : width - 9;
            }
        }

        private double left;
        public double Left
        {
            get => left;
            set
            {
                left = value;

                Canvas.SetLeft(Figure, value);

                Canvas.SetLeft(TextFigure, value + 9);
                Panel.SetZIndex(TextFigure, 3);

                Canvas.SetLeft(WaveFigure, value + 2);
                Panel.SetZIndex(WaveFigure, 3);
            }
        }

        private double top;
        public double Top
        {
            get => top;
            set
            {
                top = value;
                Canvas.SetTop(Figure, value);
                Canvas.SetTop(TextFigure, value + 3.5);
                Panel.SetZIndex(TextFigure, 3);

                Canvas.SetTop(WaveFigure, value + 30);
                Panel.SetZIndex(WaveFigure, 3);
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                if (value)
                {
                    Figure.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#1C92FF");
                    Figure.StrokeThickness = 1.4;
                    TextFigure.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#1983E6");
                }
                else
                {
                    Figure.StrokeThickness = 0;
                    TextFigure.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#404255");
                }
            }
        }

        private int leftCut;
        public int LeftCut
        {
            get => leftCut;
            set
            {
                leftCut = value;
                WaveFigure.LeftCut = value;
            }
        }

        private int rightCut;
        public int RightCut
        {
            get => rightCut;
            set
            {
                rightCut = value;
                WaveFigure.RightCut = value;
            }
        }

        private void UpdateCornerRadius(double w)
        {
            if (w < 20)
            {
                Figure.RadiusX = 0;
                Figure.RadiusY = 0;
            }
            else if (w < 40)
            {
                Figure.RadiusX = 9.8 * (w - 20) / 20;
                Figure.RadiusY = 9.8 * (w - 20) / 20;
            }
            else
            {
                Figure.RadiusX = 9.8;
                Figure.RadiusY = 9.8;
            }
        }

        public void SetPosition(double pixel)
        {
            var newPosition = WFConverter.PixelToFrame(pixel);
            if (newPosition < 0)
            {
                newPosition = 0;
                pixel = 0;
            }

            Position = newPosition;
            Left = pixel;
        }

        public void InitFigures()
        {
            Figure = new Rectangle
            {
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#13E4E7FF"),
                RadiusX = 9.8,
                RadiusY = 9.8
            };

            Figure.Tag = this;

            TextFigure = new TextBlock
            {
                Text = Name,
                FontSize = 15,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#404255")
            };
            TextFigure.Tag = this;


            WaveFigure = new WaveformVisual();
            WaveFigure.Tag = this;

            WaveFigure.Buffer32 = File.AudioData.LeftChannel;
        }

        public SerializableSoundBlock GetSerializableModel()
        {
            return new SerializableSoundBlock
            {
                TrackId = TrackId,
                File = File,
                Name = Name,
                Height = Height,
                Width = Width,
                Left = Left,
                Top = Top,
                IsSelected = IsSelected,
                LeftCut = LeftCut,
                RightCut = RightCut,
                Position = Position,
                Length = Length,
                Opacity = Opacity,
            };
        }

        public void UpdateFromSerializableModel(SerializableSoundBlock serializableModel)
        {
            TrackId = serializableModel.TrackId;
            File = serializableModel.File;
            Name = serializableModel.Name;
            InitFigures();
            Height = serializableModel.Height;
            Width = serializableModel.Width;
            Left = serializableModel.Left;
            Top = serializableModel.Top;
            IsSelected = serializableModel.IsSelected;
            LeftCut = serializableModel.LeftCut;
            RightCut = serializableModel.RightCut;
            Position = serializableModel.Position;
            Length = serializableModel.Length;
            Opacity = serializableModel.Opacity;
        }
    }
}