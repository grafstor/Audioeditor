using System;
using System.Linq;
using System.Windows.Controls;
using Audioeditor.MVVM.Model;
using Audioeditor.MVVM.ViewModel;

namespace Audioeditor
{
    public class SoundBlocksHelper
    {
        private readonly Canvas canvas;
        private readonly MainViewModel viewModel;

        public SoundBlocksHelper(MainWindow mainWindow)
        {
            canvas = mainWindow.WorkFieldCanvas;
            viewModel = mainWindow.viewModel;
        }
                
        public void CompressSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                var audioData = selectedBlock.File.AudioData;

                var threshold = 0.5f;
                var ratio = 2.0f;

                for (var i = 0; i < audioData.Length; i++)
                {
                    var LAmplitude = Math.Abs(audioData.LeftChannel[i]);
                    var RAmplitude = Math.Abs(audioData.RightChannel[i]);

                    if (LAmplitude > threshold)
                    {
                        var gainReduction = (LAmplitude - threshold) / (ratio * (LAmplitude - threshold) + threshold);
                        audioData.LeftChannel[i] *= gainReduction;
                        audioData.LeftChannel[i] = Math.Max(-1, Math.Min(audioData.LeftChannel[i], 1));
                    }

                    if (RAmplitude > threshold)
                    {
                        var gainReduction = (RAmplitude - threshold) / (ratio * (RAmplitude - threshold) + threshold);
                        audioData.RightChannel[i] *= gainReduction;
                        audioData.RightChannel[i] = Math.Max(-1, Math.Min(audioData.RightChannel[i], 1));
                    }
                }

                selectedBlock.File.AudioData = audioData;
                selectedBlock.WaveFigure.UpdatePicks();
            }
        }
        
        public void ReverbSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                var audioData = selectedBlock.File.AudioData;

                var decay = 0.5f;
                var delay = 44100;

                var LeftReverbBuffer = new float[audioData.Length];

                for (var i = 0; i < audioData.Length; i++)
                {
                    var decayedSample = audioData.LeftChannel[i] * (1.0f - decay);

                    if (i >= delay)
                        LeftReverbBuffer[i] = audioData.LeftChannel[i - delay] + decayedSample;
                    else
                        LeftReverbBuffer[i] = decayedSample;
                }

                for (var i = 0; i < audioData.Length; i++)
                    audioData.LeftChannel[i] = Math.Max(-1, Math.Min((audioData.LeftChannel[i] + LeftReverbBuffer[i]), 1));
                
                var RightReverbBuffer = new float[audioData.Length];

                for (var i = 0; i < audioData.Length; i++)
                {
                    var decayedSample = audioData.RightChannel[i] * (1.0f - decay);

                    if (i >= delay)
                        RightReverbBuffer[i] = audioData.RightChannel[i - delay] + decayedSample;
                    else
                        RightReverbBuffer[i] = decayedSample;
                }

                for (var i = 0; i < audioData.Length; i++)
                    audioData.RightChannel[i] = Math.Max(-1, Math.Min((audioData.RightChannel[i] + RightReverbBuffer[i]), 1));

                selectedBlock.File.AudioData = audioData;
                selectedBlock.WaveFigure.UpdatePicks();
            }
        }

        public void FadeoutSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                var audioData = selectedBlock.File.AudioData;
                var fadeOutLength = 44100 * 5;

                for (var i = audioData.Length - fadeOutLength; i < audioData.Length; i++)
                {
                    var fadeMultiplier = 1.0f - (i - (audioData.Length - fadeOutLength)) / (float)fadeOutLength;
                    audioData.LeftChannel[i] *= fadeMultiplier;
                    audioData.RightChannel[i] *= fadeMultiplier;
                }

                selectedBlock.File.AudioData = audioData;
                selectedBlock.WaveFigure.UpdatePicks();
            }
        }

        public void FadeinSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                var audioData = selectedBlock.File.AudioData;
                var fadeInLength = 44100 * 5;

                for (var i = 0; i < fadeInLength; i++)
                {
                    var fadeMultiplier = i / (float)fadeInLength;
                    audioData.LeftChannel[i] *= fadeMultiplier;
                    audioData.RightChannel[i] *= fadeMultiplier;
                }

                selectedBlock.File.AudioData = audioData;
                selectedBlock.WaveFigure.UpdatePicks();
            }
        }

        public void DuplicateSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                var newSoundBlock = new SoundBlock();

                newSoundBlock.File = selectedBlock.File;
                newSoundBlock.Name = selectedBlock.File.Name;
                newSoundBlock.InitFigures();
                newSoundBlock.TrackId = selectedBlock.TrackId;
                newSoundBlock.Height = selectedBlock.Height;
                newSoundBlock.Top = selectedBlock.Top;
                newSoundBlock.Width = selectedBlock.Width;
                newSoundBlock.Left = selectedBlock.Left + selectedBlock.Width;
                newSoundBlock.Length = selectedBlock.Length;
                newSoundBlock.Position = selectedBlock.Position + selectedBlock.Length;
                newSoundBlock.LeftCut = selectedBlock.LeftCut;
                newSoundBlock.RightCut = selectedBlock.RightCut;
                newSoundBlock.Opacity = selectedBlock.Opacity;
                newSoundBlock.IsSelected = true;
                selectedBlock.IsSelected = false;

                viewModel.SoundBlocks.Add(newSoundBlock);
                
                canvas.Children.Add(newSoundBlock.Figure);
                canvas.Children.Add(newSoundBlock.TextFigure);
                canvas.Children.Add(newSoundBlock.WaveFigure);
                
            }
        }

        public void RemoveSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                viewModel.SoundBlocks.Remove(selectedBlock);

                canvas.Children.Remove(selectedBlock.Figure);
                canvas.Children.Remove(selectedBlock.TextFigure);
                canvas.Children.Remove(selectedBlock.WaveFigure);
            }
        }

        public void CutSelectedSoundBlocks()
        {
            var selectedBlocks = viewModel.SoundBlocks.Where(block => block.IsSelected).ToList();

            foreach (var selectedBlock in selectedBlocks)
            {
                int trPosition = viewModel.TimeRunner.Position;
                
                var newSoundBlock = new SoundBlock();

                newSoundBlock.File = selectedBlock.File;
                newSoundBlock.Name = selectedBlock.File.Name;
                newSoundBlock.InitFigures();
                newSoundBlock.TrackId = selectedBlock.TrackId;
                newSoundBlock.Height = selectedBlock.Height;
                newSoundBlock.Top = selectedBlock.Top;

                int aTrPosition = trPosition - selectedBlock.Position;
                
                newSoundBlock.Width = selectedBlock.Width - WFConverter.FramesToPixels(aTrPosition);
                newSoundBlock.Left = selectedBlock.Left + WFConverter.FramesToPixels(aTrPosition);
                
                newSoundBlock.Length = selectedBlock.Length - aTrPosition;
                newSoundBlock.Position = trPosition;
                newSoundBlock.LeftCut = selectedBlock.LeftCut + aTrPosition;
                newSoundBlock.RightCut = selectedBlock.RightCut;
                newSoundBlock.Opacity = selectedBlock.Opacity;

                newSoundBlock.IsSelected = true;
                
                selectedBlock.Width = WFConverter.FramesToPixels(aTrPosition);
                selectedBlock.RightCut = selectedBlock.Length - aTrPosition;
                selectedBlock.Length = aTrPosition;
                selectedBlock.IsSelected = true;

                viewModel.SoundBlocks.Add(newSoundBlock);
                
                canvas.Children.Add(newSoundBlock.Figure);
                canvas.Children.Add(newSoundBlock.TextFigure);
                canvas.Children.Add(newSoundBlock.WaveFigure);
            }
        }
    }
}