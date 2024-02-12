using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Audioeditor.MVVM.Model;
using Audioeditor.MVVM.View;

namespace Audioeditor.MVVM.ViewModel
{
    [Serializable]
    public class SerializableMainViewModel
    {
        public ObservableCollection<SerializableTrackModel> Tracks { get; set; }
        public ObservableCollection<SerializableChannelModel> Channels { get; set; }
        public ObservableCollection<SerializableSoundBlock> SoundBlocks { get; set; }
        public SerializableTimeRunnerModel TimeRunner { get; set; }
    }
    [Serializable]
    public class SerializableTrackModel
    {
        public string Name { get; set; }
        public float Height { get; set; }
        public float Opacity { get; set; }
    }

    [Serializable]
    public class SerializableChannelModel
    {
        public string Name { get; set; }
        public string VolumeString { get; set; }
        public string IsMutedColor { get; set; }
        public string IsSoloedColor { get; set; }
        public bool IsMuted { get; set; }
        public bool IsSoloed { get; set; }
        public float Volume { get; set; }
        public string NameColor { get; set; }
        public float HighFreq { get; set; }
        public float MidFreq { get; set; } 
        public float LowFreq { get; set; }
        public float Threshold { get; set; } 
        public float Ratio { get; set; } 
        public float Opacity { get; set; }
    }

    [Serializable]
    public class SerializableSoundBlock
    {
        public int TrackId { get; set; }
        public FileModel File { get; set; }
        public string Name { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public bool IsSelected { get; set; }
        public int LeftCut { get; set; }
        public int RightCut { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public float Opacity { get; set; }
    }

    [Serializable]
    public class SerializableTimeRunnerModel
    {
        public int Position { get; set; }
    }

    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items) collection.Add(item);
        }
    }

    public class MainViewModel
    {
        public readonly FrecPluginWindow frecPluginWindow = new FrecPluginWindow();
        private readonly EqualizerWindow equalizerWindow = new EqualizerWindow();
        private readonly CompressorWindow compressorWindow = new CompressorWindow();
        
        public ObservableCollection<TrackModel> Tracks { get; set; }
        public ObservableCollection<ChannelModel> Channels { get; set; }
        public ObservableCollection<SoundBlock> SoundBlocks { get; set; }
        public TimeRunnerModel TimeRunner { get; set; }
        public RunnerModel Runner { get; set; }

        public ICommand SoloChannelCommand { get; set; }
        public ICommand MuteChannelCommand { get; set; }
        public ICommand PluginChannelCommand { get; set; }
        public ICommand OpenPluginCommand { get; set; }
        public ICommand DeletePluginCommand { get; set; }

        public void Init(MainWindow mainWindow)
        {
            
            Tracks = new ObservableCollection<TrackModel>();
            Channels = new ObservableCollection<ChannelModel>();
            SoundBlocks = new ObservableCollection<SoundBlock>();

            TimeRunner = new TimeRunnerModel();
            TimeRunner.InitFigures();

            Runner = new RunnerModel();
            Runner.InitFigures();

            Channels.Add(new ChannelModel
            {
                Name = "Мастер",
                NameColor = "#737483",
                Volume = 0
            });
            
            OpenPluginCommand = new RelayCommand(OpenPlugin_Click);
            SoloChannelCommand = new RelayCommand(SoloChannel_Click);
            MuteChannelCommand = new RelayCommand(MuteChannel_Click);
            PluginChannelCommand = new RelayCommand(PluginChannel_Click);
            DeletePluginCommand = new RelayCommand(DeletePlugin_Click);

            frecPluginWindow.mainWindow = mainWindow;
            frecPluginWindow.Closing += (sender, e) =>
            {
                e.Cancel = true;
                frecPluginWindow.Hide(); 
            };

            equalizerWindow.mainWindow = mainWindow;
            equalizerWindow.Closing += (sender, e) =>
            {
                e.Cancel = true;
                equalizerWindow.Hide(); 
            };
            compressorWindow.Closing += (sender, e) =>
            {
                e.Cancel = true;
                compressorWindow.Hide(); 
            };
        }

        private void SoloChannel_Click(object parameter)
        {
            var channel = (ChannelModel)parameter;

            channel.IsSoloed = !channel.IsSoloed;
            
            UpdateChnnelsVisual();
        }

        private void SetChannellSounBlocksOpacity(ChannelModel channel, float opacity)
        {
            int channelId = Channels.IndexOf(channel);
            foreach (SoundBlock soundblock in SoundBlocks)
            {
                if (channelId == (soundblock.TrackId + 1))
                {
                        soundblock.Opacity = opacity;
                }
            }
        }
        
        private void UpdateChnnelsVisual()
        {
            bool isSomeSoloed =  Channels.Skip(1).Any(c => c.IsSoloed);

            if (Channels[0].IsMuted)
            {
                foreach (var ch in Channels)
                {
                    DisableChannelVisual(ch);
                }
            }
            else
            {
                EnableChannelVisual(Channels[0]);
                foreach (var ch in Channels.Skip(1))
                {
                    if (!ch.IsMuted)
                    {
                        if ((isSomeSoloed && ch.IsSoloed) || !isSomeSoloed)
                        {
                            EnableChannelVisual(ch);
                        }
                        else
                        {
                            DisableChannelVisual(ch);
                        }
                    }
                    else
                    {
                        DisableChannelVisual(ch);

                    }
                }
            }
        }
        
        private void EnableChannelVisual(ChannelModel channel)
        {
            channel.Opacity = 1.0f;
            SetChannellSounBlocksOpacity(channel, 1.0f);
        }
        
        private void DisableChannelVisual(ChannelModel channel)
        {
            channel.Opacity = 0.7f;
            SetChannellSounBlocksOpacity(channel, 0.7f);
        }

        private void MuteChannel_Click(object parameter)
        {
            var channel = (ChannelModel)parameter;

            channel.IsMuted = !channel.IsMuted;

            UpdateChnnelsVisual();
        }

        private void OpenPlugin_Click(object parameter)
        {
            var plugin = (Plugin)parameter;
            if (plugin.Name == "Анализ частот")
            {
                if (!frecPluginWindow.isPlaying)
                    frecPluginWindow.Start();
                frecPluginWindow.Channel = plugin.Channel;
                frecPluginWindow.Show();
                frecPluginWindow.Activate();
            }
            else if (plugin.Name == "Эквализатор")
            {
                equalizerWindow.Channel = plugin.Channel;
                equalizerWindow.Init();
                equalizerWindow.Show();
                equalizerWindow.Activate();

            }
            else if (plugin.Name == "Компрессор")
            {
                compressorWindow.Channel = plugin.Channel;
                compressorWindow.Init();
                compressorWindow.Show();
                compressorWindow.Activate();

            }
        }
        
        private void PluginChannel_Click(object parameter)
        {
            var channel = (ChannelModel)parameter;

            var items = new List<string> { "Анализ частот", "Эквализатор", "Компрессор" };

            var selectionWindow = new SelectionWindow();
            selectionWindow.SelectionMade += SelectionWindow_SelectionMade;
            selectionWindow.SetItems(items);
            selectionWindow.Tag = channel;
            selectionWindow.DataContext = channel;

            var window = new Window
            {
                Content = selectionWindow,
                WindowStyle = WindowStyle.ToolWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                Title = "Выберите плагин",
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
            }
        }
        
        private void SelectionWindow_SelectionMade(object sender, string selectedValue)
        {
            var selectionWindow = (SelectionWindow)sender;

            var channelModel = (ChannelModel)selectionWindow.Tag;

            if (channelModel.Plugins == null) channelModel.Plugins = new ObservableCollection<Plugin>();

            var isPluginExists = channelModel.Plugins.Any(plugin => plugin.Name == selectedValue);


            if (!isPluginExists) channelModel.Plugins.Add(new Plugin { Name = selectedValue, Channel = channelModel });
        }

        private void DeletePlugin_Click(object parameter)
        {
            var plugin = (Plugin)parameter;


            plugin.Channel.Plugins.Remove(plugin);
        }
        
        public SerializableMainViewModel GetSerializableModel()
        {
            return new SerializableMainViewModel
            {
                Tracks = new ObservableCollection<SerializableTrackModel>(Tracks.Select(c =>
                    c.GetSerializableModel())),
                Channels = new ObservableCollection<SerializableChannelModel>(Channels.Select(c =>
                    c.GetSerializableModel())),
                SoundBlocks =
                    new ObservableCollection<SerializableSoundBlock>(
                        SoundBlocks.Select(sb => sb.GetSerializableModel())),
                TimeRunner = TimeRunner.GetSerializableModel()
            };
        }

        public void UpdateFromSerializableModel(SerializableMainViewModel serializableModel)
        {
            Tracks.Clear();
            Tracks.AddRange(serializableModel.Tracks.Select(c =>
            {
                var tracks = new TrackModel();
                tracks.viewModel = this;
                tracks.UpdateFromSerializableModel(c);
                return tracks;
            }));
            Channels.Clear();
            Channels.AddRange(serializableModel.Channels.Select(c =>
            {
                var channel = new ChannelModel();
                channel.UpdateFromSerializableModel(c);
                return channel;
            }));
            SoundBlocks.Clear();
            SoundBlocks.AddRange(serializableModel.SoundBlocks.Select(sb =>
            {
                var soundBlock = new SoundBlock();
                soundBlock.UpdateFromSerializableModel(sb);
                return soundBlock;
            }));
            TimeRunner = new TimeRunnerModel();
            TimeRunner.UpdateFromSerializableModel(serializableModel.TimeRunner);
        }
    }
}