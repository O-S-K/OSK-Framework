using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    [CreateAssetMenu(fileName = "ListSoundSO", menuName = "OSK/Sound/ListSoundSO")]
    public class ListSoundSO : ScriptableObject
    {
        public bool showSoundSettings = true;
        
        [FilePath]
        [HideInInspector]
        [Title("Sound Settings")]
        public string filePathSoundID = "Assets";

        [Title("Sound Settings")]
        [ShowIf("showSoundSettings")]
        [Space]
        [Title("Max Capacity")]
        [Tooltip("Max capacity music when play, If you want to add more sound, please increase this value.")]
        public int maxCapacityMusic = 5;

        [ShowIf("showSoundSettings")]
        [Tooltip("Max capacity SFX when play, If you want to add more sound, please increase this value.")]
        public int maxCapacitySFX = 25;

        [ShowIf("showSoundSettings")]
        [Space]
        [Title("List Sound Infos")]
        [TableList, SerializeField] 
        private List<SoundData> _listSoundInfos = new List<SoundData>();
        public List<SoundData> ListSoundInfos => _listSoundInfos;


#if UNITY_EDITOR 
        private void OnValidate()
        {
            // check unique audio clip name in the list
            var audioClipNames = new List<string>();
            foreach (var soundInfo in _listSoundInfos)
            {
                if(soundInfo.audioClip == null || string.IsNullOrEmpty(soundInfo.audioClip.name))
                    continue;
                if (audioClipNames.Contains(soundInfo.audioClip.name))
                {
                    MyLogger.LogError(
                        $"Audio Clip Name {soundInfo.audioClip.name} exists in the list. Please remove it or rename it.");
                }
                else
                { 
                    audioClipNames.Add(soundInfo.audioClip.name);
                }
            }
        }
#endif
    }
}