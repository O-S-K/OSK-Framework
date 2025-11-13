#if UNITY_EDITOR
using System;
using UnityEngine;

namespace OSK
{
    public sealed class AudioProcessor : IDisposable
    {
        private float[] _samples;
        private int _channels;
        private int _frequency;
        private AudioClip _originalClip;
        private bool _disposed;

        public AudioProcessor(AudioClip sourceClip)
        {
            if (sourceClip == null) throw new ArgumentNullException(nameof(sourceClip));
            _originalClip = sourceClip;
            _channels = sourceClip.channels;
            _frequency = sourceClip.frequency;

            // allocate once
            int total = sourceClip.samples * _channels;
            _samples = new float[total];
            sourceClip.GetData(_samples, 0);
        }

        // Trim keeps only samples in [startTime, endTime)
        public void Trim(float startTime, float endTime)
        {
            int startSample = Mathf.Clamp((int)(startTime * _frequency), 0, _originalClip.samples - 1);
            int endSample = Mathf.Clamp((int)(endTime * _frequency), 0, _originalClip.samples);
            if (endSample <= startSample) return;

            int startIndex = startSample * _channels;
            int endIndex = Mathf.Min(endSample * _channels, _samples.Length);
            int samplesToKeep = endIndex - startIndex;
            if (samplesToKeep <= 0) return;

            var newArr = new float[samplesToKeep];
            Array.Copy(_samples, startIndex, newArr, 0, samplesToKeep);
            _samples = newArr;
        }

        public void AdjustVolume(float volume)
        {
            // clamp volume early and use local for perf
            volume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(volume, 1f)) return;
            float[] s = _samples;
            int len = s.Length;
            for (int i = 0; i < len; i++)
            {
                s[i] *= volume;
            }
        }

        public void Reverse()
        {
            if (_channels <= 0) return;
            int frameSize = _channels;
            int totalFrames = _samples.Length / frameSize;
            int lastFrameIndex = totalFrames - 1;
            float[] s = _samples;

            // swap frame by frame using local temp array for frame
            var tmp = new float[frameSize];
            for (int i = 0; i < totalFrames / 2; i++)
            {
                int a = i * frameSize;
                int b = (lastFrameIndex - i) * frameSize;

                // copy a -> tmp
                for (int c = 0; c < frameSize; c++) tmp[c] = s[a + c];
                // copy b -> a
                for (int c = 0; c < frameSize; c++) s[a + c] = s[b + c];
                // copy tmp -> b
                for (int c = 0; c < frameSize; c++) s[b + c] = tmp[c];
            }
        }

        public void ConvertToMono(MonoChannelMode mode)
        {
            if (_channels <= 1) return;
            int frames = _samples.Length / _channels;
            var mono = new float[frames];

            if (mode == MonoChannelMode.Downmixing)
            {
                for (int i = 0; i < frames; i++)
                {
                    int baseIdx = i * _channels;
                    float sum = 0f;
                    for (int c = 0; c < _channels; c++)
                        sum += _samples[baseIdx + c];
                    mono[i] = sum / _channels;
                }
            }
            else if (mode == MonoChannelMode.Left)
            {
                for (int i = 0; i < frames; i++)
                    mono[i] = _samples[i * _channels];
            }
            else // Right
            {
                int rightIndex = Math.Min(1, _channels - 1);
                for (int i = 0; i < frames; i++)
                    mono[i] = _samples[i * _channels + rightIndex];
            }

            _samples = mono;
            _channels = 1;
        }

        public void ApplyFading(float fadeInDuration, float fadeOutDuration)
        {
            int totalFrames = _samples.Length / _channels;
            if (totalFrames <= 0) return;

            int fadeInFrames = Mathf.Clamp((int)(fadeInDuration * _frequency), 0, totalFrames);
            int fadeOutFrames = Mathf.Clamp((int)(fadeOutDuration * _frequency), 0, totalFrames);

            // Fade-in
            if (fadeInFrames > 0)
            {
                float[] s = _samples;
                int ch = _channels;
                // precompute reciprocal
                float invIn = 1f / fadeInFrames;
                for (int frame = 0; frame < fadeInFrames; frame++)
                {
                    float factor = frame * invIn;
                    int idx = frame * ch;
                    for (int c = 0; c < ch; c++)
                        s[idx + c] *= factor;
                }
            }

            // Fade-out
            if (fadeOutFrames > 0)
            {
                float[] s = _samples;
                int ch = _channels;
                float invOut = 1f / fadeOutFrames;
                for (int i = 0; i < fadeOutFrames; i++)
                {
                    int frameIndex = totalFrames - fadeOutFrames + i;
                    float factor = 1f - (i * invOut);
                    int idx = frameIndex * ch;
                    for (int c = 0; c < ch; c++)
                        s[idx + c] *= factor;
                }
            }
        }

        public AudioClip GetResultClip()
        {
            if (_samples == null) return null;
            int samplesCount = _samples.Length / _channels;
            string newName = _originalClip != null ? _originalClip.name + "_OSK_Edited" : "OSK_Edited";
            var newClip = AudioClip.Create(newName, samplesCount, _channels, _frequency, false);
            newClip.SetData(_samples, 0);
            return newClip;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _samples = null;
            _originalClip = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

#endif