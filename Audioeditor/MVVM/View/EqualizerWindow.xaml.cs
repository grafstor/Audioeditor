using System.Windows;
using Audioeditor.MVVM.Model;

namespace Audioeditor.MVVM.View
{
    public partial class EqualizerWindow : Window
    {
        public MainWindow mainWindow;
        public ChannelModel Channel { get; set; }
        public EqualizerWindow()
        {
            InitializeComponent();
        }

        public void Init()
        {
            LowFreqSlider.Value = Channel.LowFreq;
            MidFreqSlider.Value = Channel.MidFreq;
            HighFreqSlider.Value = Channel.HighFreq;
        }
        
        private void LowFreqSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWindow != null)
                Channel.LowFreq = (float)e.NewValue;
        }

        private void MidFreqSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWindow != null)
                Channel.MidFreq = (float)e.NewValue;

        }

        private void HighFreqSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWindow != null) 
                Channel.HighFreq = (float)e.NewValue;
        }
    }
}