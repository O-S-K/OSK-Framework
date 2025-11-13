#if UNITY_EDITOR
// File: Assets/OSK/Editor/Utility/WavWriter.cs
using UnityEngine;
using System.IO;

namespace OSK
{
    public static class WavWriter
    {
        private const int HEADER_SIZE = 44;

        /// <summary>
        /// Lưu một AudioClip đã chỉnh sửa thành file WAV mới.
        /// </summary>
        public static bool Save(string filePath, AudioClip clip)
        {
            if (string.IsNullOrEmpty(filePath) || clip == null)
            {
                Debug.LogError("FilePath hoặc AudioClip không hợp lệ.");
                return false;
            }

            // Đảm bảo đường dẫn là .wav
            if (!filePath.ToLower().EndsWith(".wav"))
                filePath += ".wav";

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                // 1. Ghi Header rỗng (để dành chỗ)
                for (int i = 0; i < HEADER_SIZE; i++)
                {
                    fileStream.WriteByte(0);
                }

                // 2. Lấy dữ liệu mẫu (Sample Data)
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                // 3. Chuyển đổi và Ghi dữ liệu mẫu (Float -> SINT16)
                ConvertAndWrite(fileStream, samples);

                // 4. Ghi lại Header (Cập nhật kích thước)
                fileStream.Seek(0, SeekOrigin.Begin);
                WriteHeader(fileStream, clip);

                return true;
            }
        }

        private static void ConvertAndWrite(FileStream fileStream, float[] samples)
        {
            // Chuyển đổi float samples (-1.0f đến 1.0f) sang định dạng SINT16 bytes (phổ biến cho WAV)
            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(samples[i] * short.MaxValue);
                byte[] bytes = System.BitConverter.GetBytes(value);
                fileStream.Write(bytes, 0, 2); // Ghi 2 bytes
            }
        }

        private static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;

            var totalDataSize = samples * channels * 2; // 2 bytes/sample (SINT16)
            var totalFileSize = totalDataSize + HEADER_SIZE - 8;

            // RIFF chunk
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4); 
            fileStream.Write(System.BitConverter.GetBytes(totalFileSize), 0, 4); 
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4); 

            // FMT chunk
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4); 
            fileStream.Write(System.BitConverter.GetBytes(16), 0, 4); // Subchunk1 Size (16 for PCM)
            fileStream.Write(System.BitConverter.GetBytes(1), 0, 2); // Audio Format (PCM)
            fileStream.Write(System.BitConverter.GetBytes(channels), 0, 2); // Number of Channels
            fileStream.Write(System.BitConverter.GetBytes(hz), 0, 4); // Sample Rate
            fileStream.Write(System.BitConverter.GetBytes(hz * channels * 2), 0, 4); // Byte Rate
            fileStream.Write(System.BitConverter.GetBytes(channels * 2), 0, 2); // Block Align
            fileStream.Write(System.BitConverter.GetBytes(16), 0, 2); // Bits per Sample

            // DATA chunk
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4); 
            fileStream.Write(System.BitConverter.GetBytes(totalDataSize), 0, 4); 
        }
    }
}
#endif