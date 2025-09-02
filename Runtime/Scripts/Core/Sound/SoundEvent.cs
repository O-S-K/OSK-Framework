using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace OSK
{
    public class SoundEvent : MonoBehaviour
    {
        [Tooltip("ID or Clip")]
        [LabelWidth(100)]
        [EnumToggleButtons]
        public enum ETypePlay
        {
            ID,
            Clip
        }

        public ETypePlay typePlay;

        [LabelText("Setting")] 
        public SoundSetup[] soundSetupInfo = new SoundSetup[1];

        private void Awake()
        {
            if (Main.Observer) Main.Observer.Add(KEY_OBSERVER.KEY_SOUND_EVENT, Play);
        }

        private void OnDestroy()
        {
            if (Main.Observer) Main.Observer.Remove(KEY_OBSERVER.KEY_SOUND_EVENT, Play);
        }

        public void Play(object data)
        {
            Play();
        }
        
        [Button]
        public void Play()
        {
            foreach (var soundSetup in soundSetupInfo)
            {
                if (typePlay == ETypePlay.ID) Main.Sound.PlayID(soundSetup);
                else Main.Sound.PlayClip(soundSetup);
            }
        }
       

        [Button]
        public void Stop()
        {
            foreach (var soundSetup in soundSetupInfo)
            {
                if (typePlay == ETypePlay.ID) Main.Sound.Stop(soundSetup.id);
                else Main.Sound.Stop(soundSetup.audioClip);
            }
        }
    }
}