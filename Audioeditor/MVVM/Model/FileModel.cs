using System;

namespace Audioeditor.MVVM.Model
{
    [Serializable]
    public class FileModel
    {
        private AudioData audioData;
        public string Name { get; set; }
        public int Length { get; set; }
        public string Path { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }

        public AudioData AudioData
        {
            get => audioData;
            set
            {
                audioData = value;
                Length = audioData.LeftChannel.Length;
            }
        }
    }
}