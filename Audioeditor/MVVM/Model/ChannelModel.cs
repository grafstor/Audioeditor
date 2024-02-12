using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor.MVVM.Model
{
    public class ChannelModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string NameColor { get; set; } = "#ffffff";
        public TrackModel Track { get; set; }
        
        public float[] LeftAudioDataBatch { get; set; }
        public float[] RightAudioDataBatch { get; set; }
        
        public float HighFreq { get; set; } = 1;
        public float MidFreq { get; set; } = 1;
        public float LowFreq { get; set; } = 1;
        
        public float Threshold { get; set; } = 0.5f;
        public float Ratio { get; set; } = 2.0f;

       
        private ObservableCollection<Plugin> plugins;
        public ObservableCollection<Plugin> Plugins
        {
            get => plugins;
            set
            {
                plugins = value;
                OnPropertyChanged(nameof(Plugins));
            }
        }

        private string volumeString;
        public string VolumeString
        {
            get => volumeString;
            set
            {
                if (volumeString != value)
                {
                    volumeString = value;
                    OnPropertyChanged(nameof(VolumeString));
                }
            }
        }

        private string isMutedColor;
        public string IsMutedColor
        {
            get => isMutedColor;
            set
            {
                if (isMutedColor != value)
                {
                    isMutedColor = value;
                    OnPropertyChanged(nameof(IsMutedColor));
                }
            }
        }
        
        private float opacity = 1f;
        public float Opacity
        {
            get => opacity;
            set
            {
                if (opacity != value)
                {
                    opacity = value;
                    if (Track != null)
                    {
                        Track.Opacity = value-0.15f;
                        if (value == 1)
                        {
                            Track.Opacity = 1;
                        } 
                    }
                    OnPropertyChanged(nameof(Opacity));
                }
            }
        }
        
        private string isSoloedColor;
        public string IsSoloedColor
        {
            get => isSoloedColor;
            set
            {
                if (isSoloedColor != value)
                {
                    isSoloedColor = value;
                    OnPropertyChanged(nameof(IsSoloedColor));
                }
            }
        }
        
        private bool isMuted;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                if (isMuted)
                    IsMutedColor = "#EED5D5";
                else
                    IsMutedColor = "Transparent";
            }
        }

        private bool isSoloed;
        public bool IsSoloed
        {
            get => isSoloed;
            set
            {
                isSoloed = value;
                if (isSoloed)
                    IsSoloedColor = "#DAD9E9";
                else
                    IsSoloedColor = "Transparent";
            }
        }

        private float volume;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                if (volume >= 0)
                    VolumeString = $"+{Math.Round(volume)} дб";
                else
                    VolumeString = $"{Math.Round(volume)} дб";
                OnPropertyChanged(nameof(Volume));
            }
        }

        private float amplitude;
        public float Amplitude
        {
            get => amplitude;
            set
            {
                amplitude = value;
                OnPropertyChanged(nameof(Amplitude));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public SerializableChannelModel GetSerializableModel()
        {
            return new SerializableChannelModel
            {
                Name = Name,
                VolumeString = VolumeString,
                IsMutedColor = IsMutedColor,
                IsSoloedColor = IsSoloedColor,
                IsMuted = IsMuted,
                IsSoloed = IsSoloed,
                Volume = Volume,
                NameColor = NameColor,
                HighFreq = HighFreq,
                MidFreq = MidFreq,
                LowFreq = LowFreq,
                Threshold = Threshold,
                Ratio = Ratio,
                Opacity = Opacity,
            };
        }

        public void UpdateFromSerializableModel(SerializableChannelModel serializableModel)
        {
            Name = serializableModel.Name;
            VolumeString = serializableModel.VolumeString;
            IsMutedColor = serializableModel.IsMutedColor;
            IsSoloedColor = serializableModel.IsSoloedColor;
            IsMuted = serializableModel.IsMuted;
            IsSoloed = serializableModel.IsSoloed;
            Volume = serializableModel.Volume;
            NameColor = serializableModel.NameColor;
            HighFreq = serializableModel.HighFreq;
            MidFreq = serializableModel.MidFreq;
            LowFreq = serializableModel.LowFreq;
            Threshold = serializableModel.Threshold;
            Ratio = serializableModel.Ratio;
            Opacity = serializableModel.Opacity;
        }
    }
}