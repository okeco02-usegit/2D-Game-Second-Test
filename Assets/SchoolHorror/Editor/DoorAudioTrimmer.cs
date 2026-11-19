#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SchoolHorror.Editor
{
    [InitializeOnLoad]
    public static class DoorAudioTrimmer
    {
        static DoorAudioTrimmer()
        {
            EditorApplication.delayCall += Process;
        }

        [MenuItem("Tools/Trim Door Audio Files")]
        public static void Process()
        {
            string dir = "Assets/SchoolHorror/Resources/Audio/CreakingDoor";
            if (!Directory.Exists(dir)) return;

            string[] mp3Files = Directory.GetFiles(dir, "*.mp3");
            if (mp3Files.Length == 0) return;

            Debug.Log($"[DoorAudioTrimmer] Found {mp3Files.Length} MP3 door sounds. Converting to trimmed WAV files...");
            int converted = 0;

            for (int i = 1; i <= 12; i++)
            {
                string relPath = $"Audio/CreakingDoor/Creaking Door ({i})";
                AudioClip src = Resources.Load<AudioClip>(relPath);
                if (src == null)
                {
                    Debug.LogWarning($"[DoorAudioTrimmer] Could not load {relPath}");
                    continue;
                }

                int channels = src.channels;
                int total = src.samples * channels;
                var data = new float[total];
                if (!src.GetData(data, 0))
                {
                    Debug.LogWarning($"[DoorAudioTrimmer] Could not GetData for {src.name}");
                    continue;
                }

                int startSample = 0;
                for (int s = 0; s < src.samples; s++)
                {
                    if (Mathf.Abs(data[s * channels]) > 0.02f) { startSample = s; break; }
                }

                int keepSamples = Mathf.Min(src.samples - startSample, Mathf.Max(1, Mathf.RoundToInt(src.frequency * 0.28f)));
                var trimmed = new float[keepSamples * channels];
                int fadeStart = (int)(keepSamples * 0.6f);
                for (int s = 0; s < keepSamples; s++)
                {
                    float fade = s >= fadeStart ? 1f - (float)(s - fadeStart) / (keepSamples - fadeStart) : 1f;
                    for (int c = 0; c < channels; c++)
                        trimmed[s * channels + c] = data[(startSample + s) * channels + c] * fade;
                }

                string wavPath = Path.Combine(dir, $"Creaking Door ({i}).wav");
                SaveWav(wavPath, trimmed, channels, src.frequency);
                converted++;
            }

            Debug.Log($"[DoorAudioTrimmer] Converted {converted} files to WAV. Cleaning up MP3s...");
            foreach (var mp3 in mp3Files)
            {
                File.Delete(mp3);
                string meta = mp3 + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }

            AssetDatabase.Refresh();
            Debug.Log("[DoorAudioTrimmer] Successfully replaced all 12 door sounds with pre-trimmed 0.28s WAV files!");
        }

        static void SaveWav(string path, float[] samples, int channels, int sampleRate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int sampleCount = samples.Length;
                int byteCount = sampleCount * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + byteCount);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM format
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(sampleRate * channels * 2);
                bw.Write((short)(channels * 2));
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(byteCount);
                for (int i = 0; i < sampleCount; i++)
                {
                    short pcm = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(pcm);
                }
            }
        }
    }
}
#endif

