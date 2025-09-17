using System;
using UnityEngine;

namespace OSK
{
    [Serializable]
    public class SoundData
    {
        public string id = "";
        public AudioClip audioClip;
        public SoundType type = SoundType.SFX;
        
        [Range(0, 1)] public float volume = 1;
        public MinMaxFloat pitch =  new MinMaxFloat(1, 1);
        public string group = "Default";


#if UNITY_EDITOR
        public void Play(MinMaxFloat pitch)
        {
            if (audioClip == null)
            {
                OSKLogger.LogWarning("AudioClip is null.");
                return;
            }
            
            if (pitch != null)
            {
                SetPitch(pitch);
            }
            
            EditorAudioHelper.PlayClip(audioClip);
        } 

        public void Stop()
        {
            if (audioClip == null)
            {
                OSKLogger.LogWarning("AudioClip is null.");
                return;
            }
            EditorAudioHelper.StopClip(audioClip);
        }
        
        public void SetVolume(float volume)
        {
            this.volume = volume;
            EditorAudioHelper.SetVolume(audioClip, volume);
        }
        
        public void SetPitch(MinMaxFloat  pitch)
        {
            this.pitch = pitch;
            EditorAudioHelper.SetPitch(audioClip, pitch.RandomValue);
        } 

        public bool IsPlaying() => EditorAudioHelper.IsClipPlaying(audioClip);

        public void UpdateId()
        {
            id = audioClip != null ? audioClip.name : string.Empty;
        }
#endif
    }

    public enum SoundType
    {
        MUSIC = 0,    // Background music
        SFX = 1,      // Sound effects
        //AMBIENCE = 2, // Ambience sounds
        //VOICE = 3,   // Voice lines
    }
}