using System.Windows;
using Audioeditor.MVVM.Model;

namespace Audioeditor.MVVM.View
{
    public partial class CompressorWindow : Window
    {
        public ChannelModel Channel { get; set; }
        public CompressorWindow()
        {
            InitializeComponent();
        }

        public void Init()
        {
            ThresholdSlider.Value = Channel.Threshold;
            RatioSlider.Value = Channel.Ratio;
        }
        
        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Channel != null)
                Channel.Threshold = (float)e.NewValue;
        }

        private void RatioSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Channel != null)
                Channel.Ratio = (float)e.NewValue;

        }
    }
}