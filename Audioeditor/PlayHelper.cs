using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audioeditor.MVVM.Model;
using Audioeditor.MVVM.ViewModel;
using NAudio.Dsp;
using NAudio.Wave;

namespace Audioeditor
{
    public class PlayHelper
    {
        private readonly MainViewModel viewModel;

        public PlayHelper(MainWindow mainWindow)
        {
            viewModel = mainWindow.viewModel;
        }

        public void InitializeWaveOut()
        {
            WaveManager.waveOut = new WaveOut();
            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);
            WaveManager.bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            WaveManager.bufferedWaveProvider.BufferLength = WaveManager.bufferSize * 200;
            WaveManager.waveOut.Init(WaveManager.bufferedWaveProvider);
            WaveManager.waveOut.Play();
            WaveManager.currentPosition = viewModel.TimeRunner.Position;
            WaveManager.startRPostion = viewModel.TimeRunner.Position;
        }

        public async Task StartPlaybackAsync()
        {
            var amplitudeThread = new Thread(() =>
            {
                int last = 0;
                while (true)
                {
                    lock (WaveManager.lockObject)
                    {
                        if (!WaveManager.isPlaying)
                        {
                            foreach (var channel in viewModel.Channels)
                            {
                                channel.Amplitude = 0;
                            }

                            break;
                        }
                        if (WaveManager.isPaused) continue;
                    }

                    try
                    {
                        int wop = (int)WaveManager.GetPosition();
                        int indB = wop + WaveManager.startRPostion * 2 - (WaveManager.currentPosition * 2);
                        if (indB < last)
                        {
                            last = indB;
                        }

                        foreach (ChannelModel channel in viewModel.Channels)
                        {
                            if (channel.LeftAudioDataBatch != null)
                            {
                                float[] trimmedData = channel.LeftAudioDataBatch.Skip(last).Take(indB - last).ToArray(); //WARR
                                float maxAmplitude = trimmedData.Max();
                                channel.Amplitude = maxAmplitude * 150f;
                            }
                        }

                        last = indB;
                        Thread.Sleep(16);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                        Thread.Sleep(1);
                    }
                }
            });
            amplitudeThread.Start();
            while (true)
            {
                lock (WaveManager.lockObject)
                {
                    if (!WaveManager.isPlaying) break;
                    if (WaveManager.isPaused) continue;
                }

                long wop = WaveManager.GetPosition();
                if (WaveManager.currentPosition * 2 + WaveManager.bufferSize * 2 - 1000 <
                    wop + WaveManager.startRPostion * 2)
                {
                    (float[] leftChannel, float[] rightChannel) audioData = GetAudioDataBatch(WaveManager.currentPosition, WaveManager.bufferSize);

                    float[] fullAudioData = new float[audioData.leftChannel.Length*2];

                    for (int i = 0; i < audioData.leftChannel.Length; i++)
                    {
                        fullAudioData[i*2] = audioData.leftChannel[i];
                        fullAudioData[i*2+1] = audioData.rightChannel[i];
                    }
                    
                    
                    byte[] audioBytes = ConvertFloatToByte(fullAudioData);

                    WaveManager.bufferedWaveProvider.AddSamples(audioBytes, 0, audioBytes.Length);

                    WaveManager.currentPosition += audioData.leftChannel.Length;
                }
            }

            amplitudeThread.Suspend();
            StopPlayback();
        }

        public void StartPlayback()
        {
            lock (WaveManager.lockObject)
            {
                if (WaveManager.isPlaying) return;
                WaveManager.isPlaying = true;
                WaveManager.isPaused = false;
            }

            Task.Run(StartPlaybackAsync);
        }

        public void PausePlayback()
        {
            lock (WaveManager.lockObject)
            {
                if (!WaveManager.isPlaying || WaveManager.isPaused)
                    return;
                WaveManager.waveOut.Pause();
                WaveManager.isPaused = true;
            }
        }

        public void ResumePlayback()
        {
            lock (WaveManager.lockObject)
            {
                if (!WaveManager.isPlaying || !WaveManager.isPaused)
                    return;
                WaveManager.isPaused = false;
                WaveManager.waveOut.Resume();
            }
        }

        public void StopPlayback()
        {
            lock (WaveManager.lockObject)
            {
                if (!WaveManager.isPlaying) return;
                WaveManager.isPlaying = false;
                WaveManager.isPaused = true;
                WaveManager.currentPosition = viewModel.TimeRunner.Position;
                WaveManager.waveOut.Stop();
                WaveManager.bufferedWaveProvider.ClearBuffer();
                WaveManager.waveOut.Dispose();
            }
        }

        public static float AdjustVolume(float audioDataFrame, float volumeFactor)
        {
            float adjustedValue = Math.Sign(audioDataFrame) *
                                  (1 - (float)Math.Pow(1 - Math.Abs(audioDataFrame), volumeFactor));

            return adjustedValue;
        }

        public (float[], float[]) GetAudioDataBatch(int startSample, int batchSize)
        {
            float[] leftAudioDataBatch = new float[batchSize];
            float[] rightAudioDataBatch = new float[batchSize];
            
            ChannelModel masterChannel = viewModel.Channels[0];

            if (masterChannel.IsMuted)
            {
                return (leftAudioDataBatch, rightAudioDataBatch);
            }

            bool isSomeSoloed =  viewModel.SoundBlocks.Any(soundblock => viewModel.Channels[soundblock.TrackId + 1].IsSoloed);
            
            for (int channelId = 1; channelId < viewModel.Channels.Count; channelId++)
            {
                ChannelModel channel = viewModel.Channels[channelId];
                
                float[] leftChannelAudioDataBatch = new float[batchSize];
                float[] rightChannelAudioDataBatch = new float[batchSize];
                
                
                if (!channel.IsMuted)
                {

                    if ((isSomeSoloed && channel.IsSoloed) || !isSomeSoloed)
                    {

                        foreach (SoundBlock soundblock in viewModel.SoundBlocks)
                        {
                            if (channelId == (soundblock.TrackId + 1))
                            {
                                if (soundblock.Position < (startSample + batchSize) &&
                                    (soundblock.Position + soundblock.Length) > startSample)
                                {
                                    int soundblockStartRead = soundblock.LeftCut;
                                    int soundblockEndRead = soundblock.File.AudioData.Length - soundblock.RightCut;

                                    if (startSample > soundblock.Position)
                                    {
                                        soundblockStartRead = (startSample - soundblock.Position) + soundblock.LeftCut;
                                    }

                                    if (soundblock.Position + soundblock.Length > startSample + batchSize)
                                    {
                                        soundblockEndRead = soundblock.File.AudioData.Length - soundblock.RightCut -
                                                            ((soundblock.Position + soundblock.Length) -
                                                             (startSample + batchSize));
                                    }

                                    for (int sbIndex = soundblockStartRead;
                                         sbIndex < soundblockEndRead;
                                         sbIndex++)
                                    {

                                        leftChannelAudioDataBatch[(sbIndex - soundblock.LeftCut + soundblock.Position) - startSample] += soundblock.File.AudioData.LeftChannel[sbIndex];
                                        rightChannelAudioDataBatch[(sbIndex - soundblock.LeftCut + soundblock.Position) - startSample] += soundblock.File.AudioData.RightChannel[sbIndex];
                                    }
                                }
                            }
                        }

                        if (channel.Plugins != null)
                        {
                            foreach (var plugin in channel.Plugins)
                            {
                                if (plugin.Name == "Эквализатор")
                                {
                                    leftChannelAudioDataBatch = EqualizeAudio(leftChannelAudioDataBatch, channel.LowFreq,
                                        channel.MidFreq, channel.HighFreq);
                                    rightChannelAudioDataBatch = EqualizeAudio(rightChannelAudioDataBatch, channel.LowFreq,
                                        channel.MidFreq, channel.HighFreq);
                                }
                                if (plugin.Name == "Компрессор")
                                {
                                    leftChannelAudioDataBatch = CompressAudio(leftChannelAudioDataBatch, channel.Threshold, channel.Ratio);
                                    rightChannelAudioDataBatch = CompressAudio(rightChannelAudioDataBatch, channel.Threshold, channel.Ratio);
                                }
                            }
                        }

                        float volumeFactor = (float)Math.Pow(10, channel.Volume / 20);
                        for (int i = 0; i < leftChannelAudioDataBatch.Length; i++)
                        {
                            leftChannelAudioDataBatch[i] = AdjustVolume(leftChannelAudioDataBatch[i], volumeFactor);
                            rightChannelAudioDataBatch[i] = AdjustVolume(rightChannelAudioDataBatch[i], volumeFactor);
                        }
                    }
                }

                if (channel.Plugins != null)
                {
                    foreach (var plugin in channel.Plugins)
                    {
                        if (plugin.Name == "Анализ частот")
                        {
                            if (viewModel.frecPluginWindow.Channel == channel)
                            {
                                viewModel.frecPluginWindow.AddData(ConvertFloatToByte(leftChannelAudioDataBatch)); // WARR (only left)
                            }
                        }
                    }
                }

                channel.LeftAudioDataBatch = leftChannelAudioDataBatch;
                channel.RightAudioDataBatch = rightChannelAudioDataBatch;
                for (int i = 0; i < leftChannelAudioDataBatch.Length; i++)
                {
                    leftAudioDataBatch[i] += leftChannelAudioDataBatch[i];
                    rightAudioDataBatch[i] += rightChannelAudioDataBatch[i];
                }
            }
            
            if (masterChannel.Plugins != null)
            {
                foreach (var plugin in masterChannel.Plugins)
                {
                    if (plugin.Name == "Эквализатор")
                    {
                        leftAudioDataBatch = EqualizeAudio(leftAudioDataBatch, masterChannel.LowFreq,
                            masterChannel.MidFreq, masterChannel.HighFreq);
                        rightAudioDataBatch = EqualizeAudio(rightAudioDataBatch, masterChannel.LowFreq,
                            masterChannel.MidFreq, masterChannel.HighFreq);
                    }
                    if (plugin.Name == "Компрессор")
                    {
                        leftAudioDataBatch = CompressAudio(leftAudioDataBatch, masterChannel.Threshold,
                            masterChannel.Ratio);
                        rightAudioDataBatch = CompressAudio(rightAudioDataBatch, masterChannel.Threshold,
                            masterChannel.Ratio);
                    }
                }
            }
            
            float masterVolumeFactor = (float)Math.Pow(10, masterChannel.Volume / 20);
            for (int i = 0; i < leftAudioDataBatch.Length; i++)
            {
                leftAudioDataBatch[i] /= viewModel.Tracks.Count;
                leftAudioDataBatch[i] = AdjustVolume(leftAudioDataBatch[i], masterVolumeFactor);
                
                rightAudioDataBatch[i] /= viewModel.Tracks.Count;
                rightAudioDataBatch[i] = AdjustVolume(rightAudioDataBatch[i], masterVolumeFactor);
            }
            
            if (masterChannel.Plugins != null)
            {
                foreach (var plugin in masterChannel.Plugins)
                {
                    if (plugin.Name == "Анализ частот")
                    {
                        if (viewModel.frecPluginWindow.Channel == masterChannel)
                        {
                            viewModel.frecPluginWindow.AddData(ConvertFloatToByte(leftAudioDataBatch)); // WARR (only left)
                        }
                    }
                }
            }
            
            masterChannel.LeftAudioDataBatch = leftAudioDataBatch;
            masterChannel.RightAudioDataBatch = rightAudioDataBatch;
            return (leftAudioDataBatch, rightAudioDataBatch);
        }

        public float[] CompressAudio(float[] audioData, float threshold, float ratio)
        {
            float[] compressedAudioData = new float[audioData.Length];

            for (var i = 0; i < audioData.Length; i++)
            {
                var amplitude = Math.Abs(audioData[i]);

                if (amplitude > threshold)
                {
                    var gainReduction = (amplitude - threshold) / (ratio * (amplitude - threshold) + threshold);
                    compressedAudioData[i] = audioData[i] * gainReduction;
                    compressedAudioData[i] = Math.Max(-1, Math.Min(compressedAudioData[i], 1));
                }
            }
            return compressedAudioData;   
        }
        
        public float[] EqualizeAudio(float[] audioData, float lowGain, float midGain, float highGain)
        {
            int fftSize = 8192;
            int numChannels = 1;

            Complex[] fftSpectrum = new Complex[fftSize];
            for (int i = 0; i < fftSize; i++)
            {
                fftSpectrum[i].X = i < audioData.Length ? audioData[i] : 0;
                fftSpectrum[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftSize, 2), fftSpectrum);

            int numLowFreqBins = fftSize / 10;
            int numHighFreqBins = fftSize / 2;
            int numMidFreqBins = fftSize - numLowFreqBins - numHighFreqBins;
            int numTotalFreqBins = fftSize / 2;

            ApplyGainToFrequencyRange(fftSpectrum, 0, numLowFreqBins, lowGain);
            ApplyGainToFrequencyRange(fftSpectrum, numTotalFreqBins - numHighFreqBins, numTotalFreqBins, highGain);
            ApplyGainToFrequencyRange(fftSpectrum, numLowFreqBins, numLowFreqBins + numMidFreqBins, midGain);

            FastFourierTransform.FFT(false, (int)Math.Log(fftSize, 2), fftSpectrum);

            float[] equalizedAudioData = new float[audioData.Length];
            for (int i = 0; i < audioData.Length; i++)
            {
                equalizedAudioData[i] = Math.Max(-1, Math.Min(fftSpectrum[i].X, 1));
                ;
            }

            return equalizedAudioData;
        }

        private void ApplyGainToFrequencyRange(Complex[] spectrum, int startIndex, int endIndex, float gain)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                spectrum[i].X *= gain;
                spectrum[i].Y *= gain;
            }
        }
        
        public byte[] ConvertFloatToByte(float[] fullAudioData)
        {
            var audioBytes = new byte[fullAudioData.Length * 2];
            for (var i = 0; i < fullAudioData.Length; i++)
            {
                var sample = (short)(fullAudioData[i] * short.MaxValue);
                var bytes = BitConverter.GetBytes(sample);
                audioBytes[i * 2] = bytes[0];
                audioBytes[i * 2 + 1] = bytes[1];
            }

            return audioBytes;
        }
        
        public (float[], float[]) GetFullAudioData()
        {
            var lastEndPosition = 0;
            foreach (var soundblock in viewModel.SoundBlocks)
            {
                var endPosition = soundblock.Length + soundblock.Position;
                if (endPosition > lastEndPosition) lastEndPosition = endPosition;
            }

            var fullAudioData = GetAudioDataBatch(0, lastEndPosition);

            return fullAudioData;
        }

        public (float[], WaveFormat) LoadAudioData0(string filepath)
        {
            float[] audioData;
            WaveFormat audioFormat;

            using (var audioFile = new AudioFileReader(filepath))
            {
                audioFormat = new WaveFormat(audioFile.WaveFormat.SampleRate, 32, 1);

                var sampleCount = (int)audioFile.Length / (audioFile.WaveFormat.BitsPerSample / 8) /
                                  audioFile.WaveFormat.Channels;
                audioData = new float[sampleCount];

                int bytesRead;
                var sampleOffset = 0;
                var buffer = new byte[4096];

                while ((bytesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var sampleCountFloat = bytesRead / (audioFile.WaveFormat.BitsPerSample / 8) /
                                           audioFile.WaveFormat.Channels;

                    for (var i = 0; i < sampleCountFloat; i++)
                    {
                        // Извлечение левого канала
                        var sampleValue = BitConverter.ToSingle(buffer,
                            i * (audioFile.WaveFormat.BitsPerSample / 8) * audioFile.WaveFormat.Channels);
                        audioData[sampleOffset + i] = sampleValue;
                    }

                    sampleOffset += sampleCountFloat;
                }
            }

            return (audioData, audioFormat);
        }
        
        public AudioData LoadAudioData1(string filepath)
        {
            AudioData audioData = new AudioData();

            using (var audioFile = new AudioFileReader(filepath))
            {
                audioData.AudioFormat = new WaveFormat(audioFile.WaveFormat.SampleRate, 32, 2);

                var sampleCount = (int)audioFile.Length / (audioFile.WaveFormat.BitsPerSample / 8);
                var channelCount = audioFile.WaveFormat.Channels;
                var samplesPerChannel = sampleCount / channelCount;

                audioData.LeftChannel = new float[samplesPerChannel];
                audioData.RightChannel = new float[samplesPerChannel];

                var buffer = new byte[4096];
                var bytesRead = 0;
                var sampleOffset = 0;

                while ((bytesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var samplesRead = bytesRead / (audioFile.WaveFormat.BitsPerSample / 8);

                    for (var i = 0; i < samplesRead; i += channelCount)
                    {
                        var leftSample = BitConverter.ToSingle(buffer, i * (audioFile.WaveFormat.BitsPerSample / 8));
                        var rightSample = BitConverter.ToSingle(buffer, (i + 1) * (audioFile.WaveFormat.BitsPerSample / 8));

                        audioData.LeftChannel[sampleOffset + i / channelCount] = leftSample;
                        audioData.RightChannel[sampleOffset + i / channelCount] = rightSample;
                    }

                    sampleOffset += samplesRead / channelCount;
                }
            }

            return audioData;
        }
        public AudioData LoadAudioData(string filepath)
{
    AudioData audioData = new AudioData();

    using (var audioFile = new AudioFileReader(filepath))
    {
        // Check if the audio is stereo or mono
        if (audioFile.WaveFormat.Channels == 2)
        {
            audioData.AudioFormat = new WaveFormat(audioFile.WaveFormat.SampleRate, 32, 2);
        }
        else if (audioFile.WaveFormat.Channels == 1)
        {
            audioData.AudioFormat = new WaveFormat(audioFile.WaveFormat.SampleRate, 32, 1);
        }
        else
        {
            throw new NotSupportedException("Unsupported number of channels");
        }

        var sampleCount = (int)audioFile.Length / (audioFile.WaveFormat.BitsPerSample / 8);
        var channelCount = audioFile.WaveFormat.Channels;
        var samplesPerChannel = sampleCount / channelCount;

        audioData.LeftChannel = new float[samplesPerChannel];
        audioData.RightChannel = audioData.AudioFormat.Channels == 2 ? new float[samplesPerChannel] : null;

        var buffer = new byte[4096];
        var bytesRead = 0;
        var sampleOffset = 0;

        while ((bytesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
        {
            var samplesRead = bytesRead / (audioFile.WaveFormat.BitsPerSample / 8);

            for (var i = 0; i < samplesRead; i += channelCount)
            {
                var leftSample = BitConverter.ToSingle(buffer, i * (audioFile.WaveFormat.BitsPerSample / 8));
                var rightSample = audioData.AudioFormat.Channels == 2
                    ? BitConverter.ToSingle(buffer, (i + 1) * (audioFile.WaveFormat.BitsPerSample / 8))
                    : leftSample;

                audioData.LeftChannel[sampleOffset + i / channelCount] = leftSample;
                
                if (audioData.AudioFormat.Channels == 2)
                {
                    audioData.RightChannel[sampleOffset + i / channelCount] = rightSample;
                }
            }

            sampleOffset += samplesRead / channelCount;
        }
    }

    return audioData;
}

    }
}