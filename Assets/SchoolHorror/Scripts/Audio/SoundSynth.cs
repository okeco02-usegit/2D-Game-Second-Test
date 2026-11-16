using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Tổng hợp mọi hiệu ứng âm thanh bằng mã (sóng sin, răng cưa, nhiễu) - không cần file âm thanh.</summary>
    public static class SoundSynth
    {
        const int Rate = 44100;
        const float Tau = Mathf.PI * 2f;

        static AudioClip Create(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float[] Buffer(float seconds) => new float[(int)(seconds * Rate)];
        static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

        public static AudioClip Footstep(System.Random rng)
        {
            float[] d = Buffer(0.12f);
            float low = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                low += (Noise(rng) - low) * 0.18f;
                d[i] = (low * 0.9f + Mathf.Sin(Tau * 85f * t) * 0.6f) * Mathf.Exp(-t * 38f);
            }
            return Create("Footstep", d);
        }

        /// <summary>Tiếng đá nặng cọ xát dưới sàn (lặp vòng).</summary>
        public static AudioClip StoneScrape(System.Random rng)
        {
            float[] d = Buffer(2f);
            float low = 0f;
            int fade = Rate / 40;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                low += (Noise(rng) - low) * 0.06f;
                float grind = 0.55f + 0.45f * Mathf.Sin(Tau * 27f * t) * (0.6f + 0.4f * Mathf.Sin(Tau * 3f * t));
                float edge = Mathf.Min(1f, Mathf.Min(i, d.Length - 1 - i) / (float)fade);
                d[i] = Mathf.Clamp(low * 5f * grind, -1f, 1f) * 0.7f * edge;
            }
            return Create("StoneScrape", d);
        }

        public static AudioClip DoorBang(System.Random rng)
        {
            float[] d = Buffer(0.4f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = Mathf.Sin(Tau * (62f - 40f * t) * t) * Mathf.Exp(-t * 14f) + Noise(rng) * Mathf.Exp(-t * 50f) * 0.5f;
            }
            return Create("DoorBang", d);
        }

        public static AudioClip Heartbeat()
        {
            float[] d = Buffer(0.45f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = Thump(t) + 0.8f * Thump(t - 0.17f);
            }
            return Create("Heartbeat", d);
        }

        static float Thump(float t) => t < 0f ? 0f : Mathf.Sin(Tau * 52f * t) * Mathf.Exp(-t * 26f);

        public static AudioClip Pickup()
        {
            float[] d = Buffer(0.22f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float freq = t < 0.09f ? 880f : 1320f;
                d[i] = Mathf.Sin(Tau * freq * t) * Mathf.Exp(-t * 10f) * 0.4f;
            }
            return Create("Pickup", d);
        }

        public static AudioClip Click(System.Random rng)
        {
            float[] d = Buffer(0.07f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                d[i] = Noise(rng) * Mathf.Exp(-t * 90f) * 0.7f + Mathf.Sin(Tau * 420f * t) * Mathf.Exp(-t * 60f) * 0.5f;
            }
            return Create("Click", d);
        }

        /// <summary>Lay tay nắm cửa/cổng đang khóa.</summary>
        public static AudioClip Rattle(System.Random rng)
        {
            float[] d = Buffer(0.3f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate % 0.1f;
                d[i] = Noise(rng) * Mathf.Exp(-t * 70f) * 0.6f;
            }
            return Create("Rattle", d);
        }

        public static AudioClip Creak()
        {
            const float duration = 0.45f;
            float[] d = Buffer(duration);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float freq = 160f + 120f * t / duration + 15f * Mathf.Sin(Tau * 9f * t);
                phase += freq / Rate;
                d[i] = (2f * (phase % 1f) - 1f) * Mathf.Sin(Mathf.PI * t / duration) * 0.25f;
            }
            return Create("Creak", d);
        }

        public static AudioClip Jumpscare(System.Random rng)
        {
            const float duration = 1.6f;
            float[] d = Buffer(duration);
            float[] phases = new float[5];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float sum = 0f;
                for (int k = 0; k < phases.Length; k++)
                {
                    float freq = (310f + k * 97f) * (1f + 0.9f * t / duration) + 20f * Mathf.Sin(Tau * (11f + k) * t);
                    phases[k] += freq / Rate;
                    sum += 2f * (phases[k] % 1f) - 1f;
                }
                float envelope = Mathf.Min(1f, t * 60f) * Mathf.Exp(-Mathf.Max(0f, t - 1.1f) * 6f);
                d[i] = Mathf.Clamp((sum / phases.Length + Noise(rng) * 0.4f) * 1.8f, -1f, 1f) * envelope;
            }
            return Create("Jumpscare", d);
        }

        /// <summary>Tiếng ù trầm nền (lặp vòng liền mạch vì mọi tần số đều tròn chu kỳ trong 6 giây).</summary>
        public static AudioClip AmbientDrone()
        {
            const float duration = 6f;
            float[] d = Buffer(duration);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float swell = 0.5f + 0.5f * Mathf.Sin(Tau * t / duration);
                d[i] = Mathf.Sin(Tau * 55f * t) * 0.35f + Mathf.Sin(Tau * 58.5f * t) * 0.3f + Mathf.Sin(Tau * 82.5f * t) * 0.14f * swell;
            }
            return Create("AmbientDrone", d);
        }

        public static AudioClip WinChime()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f };
            float[] d = Buffer(0.9f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                int note = Mathf.Min(notes.Length - 1, (int)(t / 0.2f));
                float local = t - note * 0.2f;
                d[i] = Mathf.Sin(Tau * notes[note] * t) * Mathf.Exp(-local * 5f) * 0.4f;
            }
            return Create("WinChime", d);
        }
    }
}
