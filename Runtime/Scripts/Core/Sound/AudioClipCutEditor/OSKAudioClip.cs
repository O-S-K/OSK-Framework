#if UNITY_EDITOR
using UnityEngine;
using System;

namespace OSK
{
    [Serializable]
    public class OSKAudioClip
    {
        // Tham chiếu đến AudioClip gốc
        [SerializeField] private AudioClip _sourceClip; 

        // Các thông số chỉnh sửa cơ bản
        [Range(0f, 1f)]
        public float Volume = 1f;

        // Điểm bắt đầu và kết thúc của đoạn cắt (tính bằng giây)
        [Range(0f, 1000f)] // Giả định max length là 1000s
        public float StartTime = 0f;
        [Range(0f, 1000f)]
        public float EndTime = 0f;

        // Các hiệu ứng (mô phỏng)
        public float FadeInDuration = 0f;
        public float FadeOutDuration = 0f;
        public bool IsReversed = false;
        public bool ConvertToMono = false;
        public MonoChannelMode MonoMode = MonoChannelMode.Downmixing;

        public AudioClip SourceClip => _sourceClip;

        // Constructor
        public OSKAudioClip(AudioClip clip)
        {
            _sourceClip = clip;
            if (clip != null)
            {
                EndTime = clip.length;
            }
        }
    }

    public enum MonoChannelMode 
    { 
        Downmixing, // Trộn tất cả các kênh lại với nhau
        Left,       // Chỉ giữ kênh Left
        Right       // Chỉ giữ kênh Right
    }
}
#endif