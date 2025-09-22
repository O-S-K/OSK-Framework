using UnityEngine;

namespace OSK
{
    [System.Serializable]
    public class PlayingSound
    {
        public SoundData SoundData = null;
        public AudioSource AudioSource = null;
        public bool IsPaused = false;
        public bool IsPlaying => AudioSource.isPlaying;
        public float RawVolume = 1f;
    }
}