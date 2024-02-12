using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor
{
    public static class LOL
    {
        public static TextBlock C;
    }
    
    public partial class MainWindow
    {
        public CanvasHelper canvasHelper;
        public PlayHelper playHelper;
        private readonly FileHelper fileHelper;
        private readonly SoundBlocksHelper soundBlocksHelper;
        private readonly WindowToolbarHelper toolbarHelper;

        public MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            LOL.C = ConsoleTextBlock;
            LOL.C.Text = "Im working";
            
            viewModel = new MainViewModel();
            viewModel.Init(this);

            toolbarHelper = new WindowToolbarHelper();
            playHelper = new PlayHelper(this);
            canvasHelper = new CanvasHelper(this);
            fileHelper = new FileHelper(this);
            soundBlocksHelper = new SoundBlocksHelper(this);

            DataContext = viewModel;

            WorkFieldCanvas.Children.Add(viewModel.TimeRunner.Figure);
            WorkFieldCanvas.Children.Add(viewModel.Runner.Figure);
            TimeRunnerPosition.Text = canvasHelper.GetTimeRunnerTime(viewModel.TimeRunner.Position);

            InitDelegates();
        }

        private void NewMenu_Click(object sender = null, RoutedEventArgs e = null)
        {
            fileHelper.NewMenuClick(sender, e);
        }

        private void OpenMenu_Click(object sender = null, RoutedEventArgs e = null)
        {
            fileHelper.OpenMenuClick(sender, e);
        }

        private void SaveMenu_Click(object sender = null, RoutedEventArgs e = null)
        {
            fileHelper.SaveMenuClick(sender, e);
        }

        private void SaveAsMenu_Click(object sender = null, RoutedEventArgs e = null)
        {
            fileHelper.SaveAsMenuClick(sender, e);
        }

        private void ExportMenu_Click(object sender = null, RoutedEventArgs e = null)
        {
            fileHelper.ExportMenuClick(sender, e);
        }

        private void InitDelegates()
        {
            WorkFieldCanvas.SizeChanged += delegate { WFConverter.CanvasWidth = WorkFieldCanvas.ActualWidth; };

            pauseButton.MouseDown += delegate
            {
                playHelper.PausePlayback();

                pauseButton.Visibility = Visibility.Collapsed;
                playButton.Visibility = Visibility.Visible;
            };
            playButton.MouseDown += delegate
            {
                if (!WaveManager.isPlaying)
                {
                    canvasHelper.startRunner();
                    playHelper.InitializeWaveOut();
                    playHelper.StartPlayback();
                }
                else
                {
                    playHelper.ResumePlayback();
                }

                pauseButton.Visibility = Visibility.Visible;
                playButton.Visibility = Visibility.Collapsed;
            };
            beginButton.MouseDown += delegate
            {
                playHelper.StopPlayback();

                pauseButton.Visibility = Visibility.Collapsed;
                playButton.Visibility = Visibility.Visible;
            };
        }
        
        private void DeselectMenu_Click(object sender, RoutedEventArgs e)
        {
            canvasHelper.DeselectAllBlocks();
        }

        private void CutMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.CutSelectedSoundBlocks();
        }

        private void DoubleMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.DuplicateSelectedSoundBlocks();
        }

        private void CompressMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.CompressSelectedSoundBlocks();
        }

        private void ReverbMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.ReverbSelectedSoundBlocks();
        }

        private void FadeoutMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.FadeoutSelectedSoundBlocks();
        }

        private void FadeinMenu_Click(object sender, RoutedEventArgs e)
        {
            soundBlocksHelper.FadeinSelectedSoundBlocks();
        }
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                soundBlocksHelper.RemoveSelectedSoundBlocks();
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D)
                soundBlocksHelper.DuplicateSelectedSoundBlocks();
            else if (e.Key == Key.S)
            {
                soundBlocksHelper.CutSelectedSoundBlocks();
            }
            else if (e.Key == Key.Space)
            {
                if (!WaveManager.isPlaying)
                {
                    canvasHelper.startRunner();
                    playHelper.InitializeWaveOut();
                    playHelper.StartPlayback();
                }
                else
                {
                    playHelper. ResumePlayback();
                }

                pauseButton.Visibility = Visibility.Visible;
                playButton.Visibility = Visibility.Collapsed;
            }
            e.Handled = true;
        }

        private void CanvasScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasHelper.UpdateCanvasSize();
        }

        private void ListView1ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            CanvasScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private void ListView2ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(TrackCardListView);

            scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer viewer)
                return viewer;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void BorderMouseDown(object sender, MouseButtonEventArgs e)
        {
            toolbarHelper.BorderMouseDown(sender, e);
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            toolbarHelper.MinimizeButtonClick(sender, e);
        }

        private void WindowStateButtonClick(object sender, RoutedEventArgs e)
        {
            toolbarHelper.WindowStateButtonClick(sender, e);
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            toolbarHelper.CloseButtonClick(sender, e);
        }
    }
}