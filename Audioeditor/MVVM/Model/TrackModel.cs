using System;
using System.ComponentModel;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor.MVVM.Model
{
    public class TrackModel : INotifyPropertyChanged
    {
        public MainViewModel viewModel;
        public string Name { get; set; }

        private float height;
        public float Height {
            get
            {
                return height;
            }
            set
            {
                height = value;
                int trackId = viewModel.Tracks.IndexOf(this);
                foreach (SoundBlock sb in viewModel.SoundBlocks)
                {
                    if (trackId == sb.TrackId)
                    {
                        sb.Height = value;
                    }
                }
                OnPropertyChanged(nameof(Height));
            }
        }

        private float opacity;
        public float Opacity
        {
            get => opacity;
            set
            {
                if (opacity != value)
                {
                    opacity = value;
                    OnPropertyChanged(nameof(Opacity));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SerializableTrackModel GetSerializableModel()
        {
            return new SerializableTrackModel
            {
                Name = Name,
                Height = Height,
                Opacity = Opacity,
            };
        }

        public void UpdateFromSerializableModel(SerializableTrackModel serializableModel)
        {
            Name = serializableModel.Name;
            Height = serializableModel.Height;
            Opacity = serializableModel.Opacity;
        }
    }
}