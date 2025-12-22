using UnityEngine;

namespace OSK
{
    public class SoundBuilder
    {
        private string _id = "";
        private AudioClip _audioClip;
        private SoundType _audioType = SoundType.SFX;

        private float _startTime;
        private bool _loop;
        private float _playDelay;

        private Transform _transform;
        private VolumeFade _volume = new VolumeFade(1);

        private int _priority = 128;
        private MinMaxFloat _pitch = new MinMaxFloat(1, 1);
        private int _minDistance = 1;
        private int _maxDistance = 500;


        public SoundBuilder SetId(string id)
        {
            _id = id;
            return this;
        }

        public SoundBuilder SetClip(AudioClip audioClip)
        {
            _audioClip = audioClip;
            return this;
        }

        public SoundBuilder SetAudioType(SoundType audioType)
        {
            _audioType = audioType;
            return this;
        }

        public SoundBuilder SetStartTime(float startTime)
        {
            _startTime = startTime;
            return this;
        }

        public SoundBuilder SetLoop(bool loop)
        {
            _loop = loop;
            return this;
        }

        public SoundBuilder SetPlayDelay(float playDelay)
        {
            _playDelay = playDelay;
            return this;
        }

        public SoundBuilder SetPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        public SoundBuilder SetPitch(MinMaxFloat pitch)
        {
            _pitch = pitch;
            return this;
        }

        public SoundBuilder Set3D(Transform transform, int minDistance = 1, int maxDistance = 500)
        {
            _transform = transform;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            return this;
        }

        public SoundBuilder SetVolume(VolumeFade volume)
        {
            _volume = volume;
            return this;
        }

        public SoundBuilder PlayWithID()
        {
#if UNITY_EDITOR
            Validate();
#endif
            Main.Sound.Play(_id, _volume, _startTime, _loop, _playDelay, _priority, _pitch, _transform, _minDistance,
                _maxDistance);
            return this;
        }

        public SoundBuilder PlayWithClip()
        {
#if UNITY_EDITOR
            Validate();
#endif
            Main.Sound.PlayAudioClip(_audioClip, _audioType, _volume, _startTime, _loop, _playDelay, _priority, _pitch,
                _transform, _minDistance, _maxDistance);
            return this;
        }

#if UNITY_EDITOR
        private void Validate()
        {
            if (string.IsNullOrEmpty(_id) && _audioClip == null)
            {
                MyLogger.LogError("Either ID or AudioClip must be set.");
            }

            if (_transform == null && (_minDistance != 1 || _maxDistance != 500))
            {
                if (!string.IsNullOrEmpty(_id))
                {
                    MyLogger.LogError($"Audio {_id} Transform must be set for 3D sound.");
                }
                else if (_audioClip != null)
                {
                    MyLogger.LogError($"Audio {_audioClip.name} Transform must be set for 3D sound.");
                }
                else
                {
                    MyLogger.LogError("Transform must be set for 3D sound.");
                }
            }
        }
#endif
    }
}