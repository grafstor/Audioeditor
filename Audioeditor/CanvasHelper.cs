using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Audioeditor;
using Audioeditor.MVVM.Model;
using Audioeditor.MVVM.ViewModel;
using NAudio.Wave;
using Path = System.IO.Path;

public class CanvasHelper
{
    public delegate AudioData LoadAudioDataDelegate(string filepath);

    private readonly Canvas canvas;

    private Point curentMouseDownPoint;
    private Point curentSoundBlockMouseDownPoint;
    private int fieldStartPosition;
    
    private bool isDragging;
    private bool isDraggingLeftSide;
    private bool isDraggingRightSide;

    private bool isPaused;
    private readonly MainWindow mainWindow;
    private Thread playbackThread;
    private SoundBlock selectedSoundBlock;
    private readonly int sideDraggSize = 13;
    private int startCut;
    private int startFrame;
    private int startSoundBlockLength;

    private readonly TextBlock timeRunnerPosition;
    private readonly MainViewModel viewModel;

    public CanvasHelper(MainWindow mainWindow)
    {
        canvas = mainWindow.WorkFieldCanvas;
        viewModel = mainWindow.viewModel;
        this.mainWindow = mainWindow;

        timeRunnerPosition = mainWindow.TimeRunnerPosition;
        LoadAudioData = mainWindow.playHelper.LoadAudioData;

        canvas.MouseDown += OnCanvasMouseDown;
        canvas.MouseUp += OnCanvasMouseUp;
        canvas.MouseMove += OnCanvasMouseMove;
        canvas.Drop += OnCanvasDrop;
        canvas.PreviewMouseWheel += OnCavasMouseWheel;
    }

    public LoadAudioDataDelegate LoadAudioData { get; set; }

    public void startRunner()
    {
        long wp;
        try
        {
            wp = WaveManager.GetPosition();
        }
        catch
        {
            wp = 0;
        }
        startFrame = wp > 0 ? startFrame : viewModel.TimeRunner.Position;
        playbackThread = new Thread(() =>
        {
            viewModel.Runner.View();
            while (WaveManager.isPlaying)
            {
                try
                {
                    var wop = WaveManager.GetPosition();
                    viewModel.Runner.MoveTo(startFrame + (int)wop / 2);
                    Thread.Sleep(100);
                }
                catch
                {
                }
            }
            viewModel.Runner.Hide();
        });
        playbackThread.Start();
    }

    private void SoundBlockMove(Point position)
    {
        var deltaX = position.X - curentMouseDownPoint.X;

        var newSoundBlockLeft = curentSoundBlockMouseDownPoint.X + deltaX;

        if (isDraggingRightSide)
        {
            var newSoundBlockLength = startSoundBlockLength + WFConverter.PixelsToFrames(deltaX);

            if (newSoundBlockLength > selectedSoundBlock.File.Length - selectedSoundBlock.LeftCut)
                newSoundBlockLength = selectedSoundBlock.File.Length - selectedSoundBlock.LeftCut;
            else if (newSoundBlockLength < 0) newSoundBlockLength = 0;

            selectedSoundBlock.Length = newSoundBlockLength;

            deltaX = -deltaX;

            var newCut = startCut + WFConverter.PixelsToFrames(deltaX);

            if (newCut > selectedSoundBlock.File.Length - selectedSoundBlock.LeftCut)
                newCut = selectedSoundBlock.File.Length - selectedSoundBlock.LeftCut;
            else if (newCut < 0) newCut = 0;

            selectedSoundBlock.RightCut = newCut;

            selectedSoundBlock.Width = WFConverter.FramesToPixels(selectedSoundBlock.Length);
        }
        else if (isDraggingLeftSide)
        {
            var newSoundBlockLength = startSoundBlockLength - WFConverter.PixelsToFrames(deltaX);

            if (newSoundBlockLength > selectedSoundBlock.File.Length - selectedSoundBlock.RightCut)
                newSoundBlockLength = selectedSoundBlock.File.Length - selectedSoundBlock.RightCut;
            else if (newSoundBlockLength < 0)
                newSoundBlockLength = 0;
            else
                selectedSoundBlock.Position = WFConverter.PixelToFrame(newSoundBlockLeft);

            selectedSoundBlock.Length = newSoundBlockLength;
            
            var newCut = startCut + WFConverter.PixelsToFrames(deltaX);

            if (newCut > selectedSoundBlock.File.Length - selectedSoundBlock.RightCut)
                newCut = selectedSoundBlock.File.Length - selectedSoundBlock.RightCut;
            else if (newCut < 0) newCut = 0;

            selectedSoundBlock.LeftCut = newCut;

            selectedSoundBlock.Width = WFConverter.FramesToPixels(selectedSoundBlock.Length);
            selectedSoundBlock.Left = WFConverter.FrameToPixel(selectedSoundBlock.Position);
        }
        else if (isDragging && selectedSoundBlock != null)
        {
            selectedSoundBlock.SetPosition(newSoundBlockLeft);

            var track = GetPointTrack(position);
            selectedSoundBlock.Figure.Height = track.Height;
            selectedSoundBlock.TrackId = viewModel.Tracks.IndexOf(track);
            selectedSoundBlock.Top = GetTrackPosition(track);
        }
    }

    private double GetTrackPosition(TrackModel track)
    {
        double sumHeight = 0;
        foreach (var t in viewModel.Tracks)
        {
            if (t == track) return sumHeight;

            sumHeight += t.Height;
        }

        return sumHeight;
    }

    private TrackModel GetPointTrack(Point point)
    {
        double sumHeight = 0;
        foreach (var track in viewModel.Tracks)
        {
            if (point.Y > sumHeight && point.Y < sumHeight + track.Height) return track;

            sumHeight += track.Height;
        }

        if (point.Y <= 0)
            return viewModel.Tracks[0];

        return viewModel.Tracks[viewModel.Tracks.Count - 1];
    }

    private void TimeRunnerMove(Point position)
    {
        if (isDragging && selectedSoundBlock == null)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                viewModel.TimeRunner.MoveTo(position.X);
                timeRunnerPosition.Text = GetTimeRunnerTime(viewModel.TimeRunner.Position);
            }
            else
            {
                var deltaX = position.X - curentMouseDownPoint.X;
                WFConverter.FieldFramePosition = fieldStartPosition - WFConverter.PixelsToFrames(deltaX);
                if (WFConverter.FieldFramePosition < 0) WFConverter.FieldFramePosition = 0;

                UpdateField();
            }
        }
    }

    public void UpdateField()
    {
        foreach (var child in canvas.Children)
            if (child is Rectangle block && block.Tag as SoundBlock != null)
            {
                var soundBlock = block.Tag as SoundBlock;

                soundBlock.Width = WFConverter.FramesToPixels(soundBlock.Length);
                soundBlock.Left = WFConverter.FrameToPixel(soundBlock.Position);
                soundBlock.Top = GetTrackPosition(viewModel.Tracks[soundBlock.TrackId]);
            }

        Canvas.SetLeft(viewModel.TimeRunner.Figure, WFConverter.FrameToPixel(viewModel.TimeRunner.Position));
        Canvas.SetLeft(viewModel.Runner.Figure, WFConverter.FrameToPixel(viewModel.Runner.Position));
    }
    
    public string GetTimeRunnerTime(int position)
    {
        var sampleRate = 44100;
        var totalSeconds = position / sampleRate;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var milliseconds = position % sampleRate / (sampleRate / 1000);

        return $"{minutes:D}:{seconds:D2}.{milliseconds:D3}";
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var canvas = (Canvas)sender;
        var position = e.GetPosition(canvas);
        SoundBlockMove(position);
        TimeRunnerMove(position);
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        var canvas = (Canvas)sender;

        if (isDragging && selectedSoundBlock != null)
        {
            canvas.ReleaseMouseCapture();
            Panel.SetZIndex(selectedSoundBlock.Figure, 0);
            selectedSoundBlock = null;
        }

        foreach (var soundBlock in viewModel.SoundBlocks)
        {
            soundBlock.WaveFigure.InvalidateVisual();
        }

        isDragging = false;
        isDraggingLeftSide = false;
        isDraggingRightSide = false;
        canvas.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        isDragging = true;

        curentMouseDownPoint = e.GetPosition(canvas);

        var tag = (FrameworkElement)(e.Source as Rectangle) ??
                  (FrameworkElement)(e.Source as TextBlock) ?? e.Source as WaveformVisual;

        selectedSoundBlock = tag?.Tag as SoundBlock;


        if (selectedSoundBlock != null)
        {
            curentSoundBlockMouseDownPoint = new Point(selectedSoundBlock.Left, selectedSoundBlock.Top);

            if (curentMouseDownPoint.X > curentSoundBlockMouseDownPoint.X &&
                curentMouseDownPoint.X < curentSoundBlockMouseDownPoint.X + sideDraggSize)
            {
                canvas.Cursor = Cursors.ScrollW;
                isDraggingLeftSide = true;

                startSoundBlockLength = selectedSoundBlock.Length;
                startCut = selectedSoundBlock.LeftCut;
            }

            else if (curentMouseDownPoint.X < curentSoundBlockMouseDownPoint.X + selectedSoundBlock.Width &&
                     curentMouseDownPoint.X > curentSoundBlockMouseDownPoint.X + selectedSoundBlock.Width -
                     sideDraggSize)
            {
                canvas.Cursor = Cursors.ScrollE;
                isDraggingRightSide = true;

                startSoundBlockLength = selectedSoundBlock.Length;
                startCut = selectedSoundBlock.RightCut;
            }

            else
            {
                canvas.Cursor = Cursors.Hand;

                if (selectedSoundBlock != null)
                {
                    canvas.CaptureMouse();
                    Panel.SetZIndex(tag, 1);
                }

                if (!Keyboard.IsKeyDown(Key.LeftCtrl)) DeselectAllBlocks();

                selectedSoundBlock.IsSelected = true;
            }
        }
        else
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                viewModel.TimeRunner.MoveTo(curentMouseDownPoint.X);
                timeRunnerPosition.Text = GetTimeRunnerTime(viewModel.TimeRunner.Position);

                DeselectAllBlocks();
            }
            else
            {
                fieldStartPosition = WFConverter.FieldFramePosition;
                canvas.Cursor = Cursors.Hand;
            }
        }

        e.Handled = true;
    }

    public void DeselectAllBlocks()
    {
        foreach (var child in canvas.Children)
        {
            var rectangle = child as Rectangle;
            var soundBlock = rectangle?.Tag as SoundBlock;
            if (soundBlock != null) soundBlock.IsSelected = false;
        }
    }

    private void OnCavasMouseWheel(object sender, MouseWheelEventArgs e)
    {

        double newScale = e.Delta > 0 ? 1.3 : 1 / 1.3;

        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            int newFFW;
            int newFFP;

            double dTR = viewModel.TimeRunner.Position -
                         (WFConverter.FieldFramePosition + (double)WFConverter.FieldFrameWidth / 2);

            newFFW = Convert.ToInt32(WFConverter.FieldFrameWidth * newScale);

            int dNew = newFFW - WFConverter.FieldFrameWidth;
            newFFP = Convert.ToInt32(WFConverter.FieldFramePosition - (double)dNew / 2 + dTR * 0.7);


            WFConverter.FieldFrameWidth = newFFW;

            if (newFFP < 0)
            {
                newFFP = 0;
            }

            WFConverter.FieldFramePosition = newFFP;
        }
        else
        {
            foreach (var track in viewModel.Tracks)
            {
                float th = track.Height;
                th *= (float)newScale;
                if (th < 80)
                {
                    th = 80;
                }
                else if (th > 300)
                {
                    th = 300;
                }
                
                track.Height = th;
            }    
        }
        UpdateField();
        e.Handled = true;
    }

    private string GetFirstWord(string line)
    {
        var words = line.Split(' ');
        var trackName = words[0];
        words = trackName.Split('.');
        var word = words[0];
        words = word.Split('_');
        word = words[0];
        return word;
    }

    private TrackModel CreateTrack(FileModel newFileModel)
    {
        var trackName = GetFirstWord(newFileModel.Name);

        var track = new TrackModel
        {
            viewModel = viewModel,
            Name = trackName,
            Height = 160f,
            Opacity = 1
        };

        viewModel.Tracks.Add(track);

        UpdateCanvasSize();

        CreateChannel(track);

        return track;
    }

    public void UpdateCanvasSize()
    {
        double sumHeight = viewModel.Tracks.Sum(tr => tr.Height);
        var scrollViewerHeight = ((ScrollViewer)canvas.Parent).ActualHeight;
        var canvasHeight = sumHeight >= scrollViewerHeight ? sumHeight : scrollViewerHeight;
        canvas.Height = canvasHeight;
    }

    private void CreateChannel(TrackModel track)
    {
        var channel = new ChannelModel
        {
            Track = track,
            Name = track.Name,
            Volume = 0
        };

        viewModel.Channels.Add(channel);
    }

    private SoundBlock CreateSoundBlock(FileModel fileModel, Point point)
    {
        double sumHeight = viewModel.Tracks.Sum(tr => tr.Height);

        TrackModel track;

        if (viewModel.Tracks.Count > 0 && point.Y <= sumHeight)
            track = GetPointTrack(point);
        else
            track = CreateTrack(fileModel);

        var newSoundBlock = new SoundBlock();

        newSoundBlock.File = fileModel;
        newSoundBlock.Name = fileModel.Name;

        newSoundBlock.InitFigures();

        newSoundBlock.TrackId = viewModel.Tracks.IndexOf(track);
        newSoundBlock.Height = track.Height;
        newSoundBlock.Top = GetTrackPosition(track);

        newSoundBlock.Width = WFConverter.FramesToPixels(newSoundBlock.File.Length);
        newSoundBlock.Left = point.X;

        newSoundBlock.Length = newSoundBlock.File.Length;
        newSoundBlock.Position = WFConverter.PixelToFrame(point.X);
        newSoundBlock.Opacity = 1;
        newSoundBlock.IsSelected = true;

        viewModel.SoundBlocks.Add(newSoundBlock);

        return newSoundBlock;
    }

    private FileModel CreateFileModel(string filepath)
    {
        AudioData audioData = LoadAudioData(filepath);

        return new FileModel
        {
            Name = Path.GetFileName(filepath),
            Path = filepath,
            AudioData = audioData,
            SampleRate = audioData.AudioFormat.SampleRate,
            BitsPerSample = audioData.AudioFormat.BitsPerSample,
            Channels = audioData.AudioFormat.Channels
        };
    }

    private void AddSoundBlockToCanvas(SoundBlock soundBlock)
    {
        canvas.Children.Add(soundBlock.Figure);
        canvas.Children.Add(soundBlock.TextFigure);
        canvas.Children.Add(soundBlock.WaveFigure);
    }

    private FileModel AddNewFile(string filepath, Point point)
    {
        var newFileModel = CreateFileModel(filepath);
        var newSoundBlock = CreateSoundBlock(newFileModel, point);

        AddSoundBlockToCanvas(newSoundBlock);

        return newFileModel;
    }

    public void OnCanvasDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        var point = e.GetPosition(canvas);

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var filepaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            double lastFileLength = 0;
            foreach (var filepath in filepaths)
            {
                point.X += lastFileLength;
                FileModel newFileModel = AddNewFile(filepath, point);
                lastFileLength = WFConverter.FramesToPixels(newFileModel.Length);
            }
        }
    }
}