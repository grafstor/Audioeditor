using NAudio.Wave;

namespace Audioeditor.MVVM.Model
{
    public class AudioData
    {
        public float[] LeftChannel { get; set; }
        public float[] RightChannel { get; set; }
        
        public int Length
        {
            get
            {
                return LeftChannel.Length;
            }
            set
            {
                
            }
        }
        public WaveFormat AudioFormat { get; set; }
    }
}