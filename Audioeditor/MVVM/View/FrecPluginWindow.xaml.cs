using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Audioeditor.MVVM.Model;
using NAudio.CoreAudioApi;
using NAudio.Dsp;

namespace Audioeditor.MVVM.View
{
    public partial class FrecPluginWindow : Window
    {
        private const int SampleRate = 44100;
        private const int BufferSize = (int)(SampleRate * 2 * 0.032);
        private readonly Canvas canvas;

        private readonly Complex[] fftBuffer;
        private readonly double[] spectrumData;
        private readonly double[] spectrumMaxData;

        private WasapiCapture capture;
        public MainWindow mainWindow;

        private double minFrx;
        private Rectangle rect;
        private Path spectrumMaxPath;
        private Path spectrumPath;

        private int startFrame;

        public bool isPlaying;

        private byte[] audioBytes;
        public ChannelModel Channel { get; set; }

        public FrecPluginWindow()
        {
            canvas = new Canvas();
            InitializeComponent();

            fftBuffer = new Complex[BufferSize];

            spectrumData = new double[BufferSize / 2];
            spectrumMaxData = new double[BufferSize / 2];

            for (var i = 0; i < spectrumMaxData.Length; i++) spectrumMaxData[i] = -120;
        }

        public void AddData(byte[] channelAudioDataBatch)
        {
            audioBytes = channelAudioDataBatch;
        }
        
        public void Start()
        {
            spectrumMaxPath = new Path
            {
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#6c2ee8"),
                StrokeThickness = 1
            };

            canvas.Children.Add(spectrumMaxPath);
            spectrumPath = new Path
            {
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom("#404255"),
                StrokeThickness = 1
            };
            canvas.Children.Add(spectrumPath);
            Content = canvas;
            isPlaying = true;

            startFrame = WaveManager.currentPosition > 0 ? startFrame : mainWindow.viewModel.TimeRunner.Position;
            var playbackThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        int wop = (int)WaveManager.GetPosition();
                        WaveIn_DataAvailable0(
                            wop + WaveManager.startRPostion * 2 - (WaveManager.currentPosition * 2),
                            audioBytes);
                        Thread.Sleep(32);
                    }
                    catch
                    {
                        Thread.Sleep(1);
                    }
                }
            });
            playbackThread.Start();
        }

        private void WaveIn_DataAvailable0(int index, byte[] audioData)
        {
            var buffer = new byte[BufferSize];
            var ii = 0;
            for (var i = index; i < BufferSize + index; i++) buffer[ii++] = audioData[i];


            for (var i = 0; i < buffer.Length / 2; i++)
            {
                var sample = (short)((buffer[2 * i + 1] << 8) | buffer[2 * i]);
                var windowMultiplier =
                    (float)(0.54 -
                            0.46 * Math.Cos(2 * Math.PI * i / (buffer.Length / 2 - 1)));
                fftBuffer[i].X =
                    windowMultiplier * (sample / 32768.0f);
                fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(BufferSize, 2), fftBuffer);


            for (var i = 0; i < BufferSize / 2; i++)
            {
                var frequency = (double)i / BufferSize * 44100;

                var amplitude = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);


                var aWeighting = ApplyAWeighting(frequency);
                spectrumData[i] = 20 * Math.Log10(amplitude) + aWeighting;

                if (spectrumMaxData[i] < spectrumData[i]) spectrumMaxData[i] = spectrumData[i];
            }


            Application.Current.Dispatcher.Invoke(() =>
            {
                DrawSpectrum(spectrumData, spectrumPath);
                DrawSpectrum(spectrumMaxData, spectrumMaxPath);
            });
        }

        private double ApplyAWeighting(double frequency)
        {
            var fSquared = frequency * frequency;
            var fSquaredSquared = fSquared * fSquared;
            var numerator = Math.Pow(12194.217 * frequency, 4);
            var denominator = (fSquared + 20.6 * 20.6) *
                              Math.Sqrt((fSquared + 107.7 * 107.7) * (fSquared + 737.9 * 737.9)) *
                              (fSquared + 12194.217 * 12194.217) * Math.Sqrt(fSquared + 158.5 * 158.5);
            return 2.0 + 20.0 * Math.Log10(numerator / denominator);
        }

        public static double[] SmoothData(double[] data, int windowSize)
        {
            var dataLength = data.Length;
            var smoothedData = new double[dataLength];

            for (var i = 0; i < dataLength; i++)
            {
                var startIndex = Math.Max(0, i - windowSize / 2);
                var endIndex = Math.Min(dataLength - 1, i + windowSize / 2);
                double sum = 0f;

                for (var j = startIndex; j <= endIndex; j++) sum += data[j];

                smoothedData[i] = sum / (endIndex - startIndex + 1);
            }

            return smoothedData;
        }

        private void DrawSpectrum(double[] spectrum, Path path)
        {
            for (int i = 0; i < spectrum.Length; i++)
            {
                if (double.IsNegativeInfinity(spectrum[i]))
                {
                    spectrum[i] = 0;
                }
            }
            spectrum = SmoothData(spectrum, 10);
            path.Data = null;
            var pathGeometry = new PathGeometry();

            var canvasWidth = canvas.ActualWidth;
            var canvasHeight = canvas.ActualHeight;

            double maxAmplitude = 100;
            double minAmplitude = -114;

            var pathFigure = new PathFigure();

            var maxFr = BufferSize / 2 - 475;
            for (var i = 10; i < maxFr; i++)
            {
                var normalizedAmplitude = (spectrum[i] - minAmplitude) / (maxAmplitude - minAmplitude);
                var barHeight = normalizedAmplitude * canvasHeight;

                var frequency = (double)i / maxFr * 22050;
                double x;
                if (frequency == 0)
                {
                    x = 0;
                }
                else
                {
                    if (minFrx == 0) minFrx = Math.Log(frequency, 10);
                    x = (Math.Log(frequency, 10) - minFrx) / (Math.Log(22050, 10) - minFrx) * canvasWidth;
                }

                var lineSegment = new LineSegment();
                lineSegment.Point = new Point(x, (canvasHeight - barHeight));
                pathFigure.Segments.Add(lineSegment);
            }
            
            pathGeometry.Figures.Add(pathFigure);

            path.Data = pathGeometry;
        }
    }
}