using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Audioeditor.MVVM.Model;
using NAudio.Wave;

namespace Audioeditor
{
    public static class WaveManager
    {
        public static WaveOut waveOut;
        public static WaveMemoryStream waveMemoryStream;
        public static BufferedWaveProvider bufferedWaveProvider;
        public static int bufferSize = 8192;
        public static int currentPosition = 0;
        public static int startRPostion = 0;
        public static bool isPlaying = false;
        public static bool isPaused = false;
        public static object lockObject = new object();

        public static long GetPosition()
        {
            return waveOut.GetPosition()/2;
        }
            
    }

    public static class WFConverter
    {
        public static int FieldFrameWidth = 30000000;
        public static int FieldFramePosition = 0;
        public static double CanvasWidth = 1;

        public static double FramesToPixels(int frames)
        {
            var framesPerPixel = FieldFrameWidth / CanvasWidth;

            return frames / framesPerPixel;
        }

        public static double FrameToPixel(int frame)
        {
            var frames = frame - FieldFramePosition;

            var framesPerPixel = FieldFrameWidth / CanvasWidth;

            return frames / framesPerPixel;
        }

        public static int PixelToFrame(double pixel)
        {
            var framesPerPixel = FieldFrameWidth / CanvasWidth;

            return FieldFramePosition + Convert.ToInt32(pixel * framesPerPixel);
        }

        public static int PixelsToFrames(double pixels)
        {
            var framesPerPixel = FieldFrameWidth / CanvasWidth;

            return Convert.ToInt32(pixels * framesPerPixel);
        }
    }

    public class WaveformVisual : FrameworkElement
    {
        private float[] buffer32;
        private bool isRunning;
        private int currentPicksWidth;
        private double currentPicksHeight;

        private int lastFFW = 0;

        private double[] y2s;

        public float[] Buffer32
        {
            get => buffer32;
            set
            {
                float[] smallerBuffer = new float[value.Length / 3 + 2];
                int j = 0;
                for (int i = 0; i < value.Length; i += 3)
                {
                    smallerBuffer[j++] = value[i];
                }
                
                buffer32 = smallerBuffer;
                isRunning = false;
                InvalidateVisual();
            }
        }

        public int LeftCut { get; set; }

        public int RightCut { get; set; }

        public double[] Y1s { get; set; }

        public double[] Y2s
        {
            get => y2s;
            set
            {
                y2s = value;
                InvalidateVisual();
            }
        }

        private async Task CountPicksAsync(int width, CancellationToken cancellationToken, bool isForce)
        {
            isRunning = true;

            if (width < 200)
            {
                width = 200;
            }

            if (buffer32 == null || buffer32.Length == 0 || (width == currentPicksWidth && !isForce))
                return;

            double height = Height;

            float maxValue = buffer32.Max();
            float minValue = buffer32.Min();

            if (width <= 0)
                return;

            int sampleCount = buffer32.Length;
            double samplesPerPixel = Math.Max(1, (double)sampleCount / width);

            double[] newy1s = new double[width];
            double[] newy2s = new double[width];
            
            await Task.Run(() =>
            {
                Parallel.For(0, width, new ParallelOptions { CancellationToken = cancellationToken }, x =>
                {
                    if (x % 30 == 0)
                        cancellationToken.ThrowIfCancellationRequested();

                    double startIndex = x * samplesPerPixel;
                    double endIndex = (x + 1) * samplesPerPixel;
                    if (endIndex >= sampleCount)
                        endIndex = sampleCount - 1;

                    double sumSample = 0;
                    double sumSampleM = 0;

                    var numM = 0;
                    var num = 0;

                    for (int i = (int)startIndex; i < endIndex; i++)
                    {
                        float sampleValue = buffer32[i];
                        if (sampleValue < 0)
                        {
                            sumSampleM += sampleValue;
                            numM++;
                        }
                        else
                        {
                            sumSample += sampleValue;
                            num++;
                        }
                    }

                    double averageSample = sumSample / num;
                    double averageSampleM = sumSampleM / numM;

                    double y1 = height / 2 - averageSample / maxValue * height / 2;
                    double y2 = height / 2 + averageSampleM / minValue * height / 2;

                    newy1s[x] = y1;
                    newy2s[x] = y2;
                });
            });

            isRunning = false;

                        
            currentPicksWidth = width;
            currentPicksHeight = Height;
            Y1s = newy1s;
            Y2s = newy2s;
        }

        private CancellationTokenSource countingCancellationTokenSource;

        public async void StartCounting(int width, bool isForce=false)
        {
            if (countingCancellationTokenSource != null)
            {
                countingCancellationTokenSource.Cancel();
                countingCancellationTokenSource.Dispose();
                countingCancellationTokenSource = null;
            }

            countingCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await CountPicksAsync(width, countingCancellationTokenSource.Token, isForce);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (countingCancellationTokenSource != null)
                {
                    countingCancellationTokenSource.Dispose();
                    countingCancellationTokenSource = null;
                }
            }
        }

        public void UpdatePicks()
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var width = (int)ActualWidth;
            
            if (Y1s == null)
            {
                StartCounting(Convert.ToInt32((float)width / 2));
                return;
            }

            if (currentPicksHeight != Height)
            {
                StartCounting(Convert.ToInt32((float)width / 2), true);
            }

            if (width == 0) width = 1;

            int yR = Convert.ToInt32(Y1s.Length * ((RightCut/3) / (double)buffer32.Length));
            int yL = Convert.ToInt32(Y1s.Length * ((LeftCut/3) / (double)buffer32.Length));
            int yLength = Y1s.Length - yR - yL;

            var pixPerY = (double)width / yLength;

            if (pixPerY < 1.9)
            {
                StartCounting(Convert.ToInt32((float)width / 6));
            }
            else if (pixPerY > 6.1)
            {
                StartCounting(Convert.ToInt32((float)width / 2));
            }

            var pen = new Pen((SolidColorBrush)new BrushConverter().ConvertFrom("#8B8EA5"), pixPerY + 0.8);

            double dx;
            SoundBlock sb = Tag as SoundBlock;
            
            int sampleCount = buffer32.Length;

                    
            double samplesPerXs = (double)sampleCount / yLength;
                    
            var pen2 = new Pen((SolidColorBrush)new BrushConverter().ConvertFrom("#8B8EA5"), 3);

            double height = ActualHeight;

            float maxValue = buffer32.Max();
            float minValue = buffer32.Min();
            
            for (var x = 0; x < yLength; x++)
            {
                dx = x * pixPerY;
                if (dx + WFConverter.FrameToPixel(sb.Position) > WFConverter.CanvasWidth)
                    break;
                if (dx + WFConverter.FrameToPixel(sb.Position) < 0)
                    continue;
                if (x + yL >= Y1s.Length)
                {
                    break;
                }

                if (WFConverter.FieldFrameWidth < 50000)
                {
                    double y1 = height / 2 - buffer32[Convert.ToInt32((x + yL-1)*samplesPerXs)] / maxValue * height / 2;
                    double y2 = height / 2 + buffer32[Convert.ToInt32((x + yL )*samplesPerXs)] / minValue * height / 2;
                    double dx2 = (x+1) * pixPerY;
                    
                    drawingContext.DrawLine(pen2, new Point(dx, y1), new Point(dx2, y2));
                }
                else
                {
                    drawingContext.DrawLine(pen, new Point(dx - 0.4, Y1s[x + yL]), new Point(dx - 0.4, Y2s[x + yL]));
                }
            }
        }
    }

    public class WaveMemoryStream : WaveStream
    {
        private readonly MemoryStream _memoryStream;

        public WaveMemoryStream(byte[] bytes)
        {
            _memoryStream = new MemoryStream(bytes);
        }

        public override WaveFormat WaveFormat { get; }

        public override long Length => _memoryStream.Length;

        public override long Position
        {
            get => _memoryStream.Position;
            set => _memoryStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _memoryStream.Read(buffer, offset, count);
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Predicate<object> canExecute;
        private readonly Action<object> execute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}