
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using Audioeditor.MVVM.Model;
using Audioeditor.MVVM.ViewModel;
using Microsoft.Win32;
using NAudio.Wave;

namespace Audioeditor
{
    public class FileHelper
    {
        public delegate byte[] ConvertFloatToByteDelegate(float[] fullAudioData);
        public delegate (float[], float[]) GetFullAudioDataDelegate();

        private readonly Canvas canvas;
        private readonly CanvasHelper canvasHelper;

        private readonly TextBlock timeRunnerPosition;
        private readonly MainViewModel viewModel;
        
        public GetFullAudioDataDelegate GetFullAudioData { get; set; }
        public ConvertFloatToByteDelegate ConvertFloatToByte { get; set; }

        public FileHelper(MainWindow mainWindow)
        {
            canvas = mainWindow.WorkFieldCanvas;
            viewModel = mainWindow.viewModel;
            canvasHelper = mainWindow.canvasHelper;

            timeRunnerPosition = mainWindow.TimeRunnerPosition;

            GetFullAudioData = mainWindow.playHelper.GetFullAudioData;
            ConvertFloatToByte = mainWindow.playHelper.ConvertFloatToByte;
        }
        
        public void NewMenuClick(object sender = null, RoutedEventArgs e = null)
        {
            foreach (var block in viewModel.SoundBlocks)
            {
                canvas.Children.Remove(block.Figure);
                canvas.Children.Remove(block.TextFigure);
                canvas.Children.Remove(block.WaveFigure);
            }

            canvas.Children.Remove(viewModel.TimeRunner.Figure);

            viewModel.Tracks.Clear();
            viewModel.Channels.Clear();
            viewModel.SoundBlocks.Clear();
            viewModel.Channels.Add(new ChannelModel
            {
                Name = "Мастер",
                NameColor = "#737483",
                Volume = 0
            });
            viewModel.TimeRunner = new TimeRunnerModel();
            viewModel.TimeRunner.InitFigures();
            canvas.Children.Add(viewModel.TimeRunner.Figure);
            timeRunnerPosition.Text = canvasHelper.GetTimeRunnerTime(viewModel.TimeRunner.Position);
        }

        public void OpenMenuClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "AEditor Files (*.aeditor)|*.aeditor";
            if (openFileDialog.ShowDialog() == true)
                using (var fileStream = File.Open(openFileDialog.FileName, FileMode.Open))
                {
                    DeserializeViewModel(fileStream);
                }
        }

        public void SaveMenuClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "AEditor Files (*.aeditor)|*.aeditor";
            if (saveFileDialog.ShowDialog() == true)
            {
                var selectedFilePath = saveFileDialog.FileName;

                if (File.Exists(selectedFilePath))
                    using (var fileStream = File.Open(selectedFilePath, FileMode.Truncate))
                    {
                        SerializeViewModel(fileStream);
                    }
                else
                    using (var fileStream = File.Open(selectedFilePath, FileMode.Create))
                    {
                        SerializeViewModel(fileStream);
                    }
            }
        }

        public void SaveAsMenuClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "AEditor Files (*.aeditor)|*.aeditor";
            if (saveFileDialog.ShowDialog() == true)
                using (var fileStream = File.Open(saveFileDialog.FileName, FileMode.Create))
                {
                    SerializeViewModel(fileStream);
                }
        }

        private void DeserializeViewModel(FileStream fileStream)
        {
            var binaryFormatter = new BinaryFormatter();
            var serializableMainViewModel =
                (SerializableMainViewModel)binaryFormatter.Deserialize(fileStream);

            NewMenuClick();
            canvas.Children.Remove(viewModel.TimeRunner.Figure);

            viewModel.UpdateFromSerializableModel(serializableMainViewModel);

            viewModel.TimeRunner.InitFigures();

            canvas.Children.Add(viewModel.TimeRunner.Figure);
            timeRunnerPosition.Text = canvasHelper.GetTimeRunnerTime(viewModel.TimeRunner.Position);

            foreach (var soundBlock in viewModel.SoundBlocks)
            {
                canvas.Children.Add(soundBlock.Figure);
                canvas.Children.Add(soundBlock.TextFigure);
                canvas.Children.Add(soundBlock.WaveFigure);
            }

            canvasHelper.UpdateField();
        }

        private void SerializeViewModel(FileStream fileStream)
        {
            var serializableMainViewModel = viewModel.GetSerializableModel();

            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, serializableMainViewModel);
        }

        public void ExportMenuClick(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "WAV Files (*.wav)|*.wav|MP3 Files (*.mp3)|*.mp3";
            if (saveFileDialog.ShowDialog() == true)
            {
                var selectedFilePath = saveFileDialog.FileName;
                var fileExtension = Path.GetExtension(selectedFilePath);
                var fullAudioData = GetFullAudioData();

                var leftAudioBytes = ConvertFloatToByte(fullAudioData.Item1);
                var rightAudioBytes = ConvertFloatToByte(fullAudioData.Item2);
                if (fileExtension == ".wav")
                    ExportToWav(leftAudioBytes, rightAudioBytes, selectedFilePath);
                else if (fileExtension == ".mp3")
                    ExportToMp3(leftAudioBytes, rightAudioBytes, selectedFilePath);
                else
                    MessageBox.Show("Выбранный формат экспорта не поддерживается.", "Ошибка", MessageBoxButton.OK,
                        MessageBoxImage.Error);
            }
        }
        
        private void ExportToWav(byte[] leftAudioBytes, byte[] rightAudioBytes, string filePath)
        {
            using (var
                   waveFileWriter =
                   new WaveFileWriter(filePath,
                       new WaveFormat(44100, 16, 2)))
            {
                waveFileWriter.Write(leftAudioBytes, 0, leftAudioBytes.Length);
                waveFileWriter.Write(rightAudioBytes, 0, rightAudioBytes.Length);
            }
        }
        
        private void ExportToMp3(byte[] leftAudioBytes, byte[] rightAudioBytes, string filePath)
        {
            using (var waveFileWriter = new WaveFileWriter(filePath, new WaveFormat(44100, 16, 2)))
            {
                waveFileWriter.Write(leftAudioBytes, 0, leftAudioBytes.Length);
                waveFileWriter.Write(rightAudioBytes, 0, rightAudioBytes.Length);
            }
        }
    }
}