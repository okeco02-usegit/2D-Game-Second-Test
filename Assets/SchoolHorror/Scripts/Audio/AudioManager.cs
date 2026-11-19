using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Phát mọi âm thanh của game: nhạc nền, gió, bước chân theo mặt sàn, cửa kêu, tiếng quái vật và nhịp tim.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        AudioSource effects;
        AudioSource scrape;
        AudioSource ambient;
        AudioSource bgm;
        AudioSource footstepSource;
        AudioSource windowWindSource;

        AudioClip footstepFallback, bang, heartbeat, pickup, keyPickupClip, click, blink, rattle, creakFallback, jumpscare, winChime;
        AudioClip[] woodenSteps, stairsSteps, concreteSteps, creakingDoors, monsterGrowls, ambientWinds;
        AudioClip doorKnockingQuiet, doorSqueeky2, doorHandleJiggle, creepyWindHowl, woodenStep3;

        readonly System.Random rng = new System.Random(1234);
        static bool musicOn = true;
        static bool sfxOn = true;
        public bool MusicEnabled => musicOn;
        public bool SfxEnabled => sfxOn;

        float scrapeTarget;
        float danger;
        float heartTimer;
        int currentSurface = -1;
        float windowWindPlayTimer;
        float windowWindTargetVol;

        void Awake()
        {
            footstepFallback = SoundSynth.Footstep(rng);
            bang = SoundSynth.DoorBang(rng);
            heartbeat = SoundSynth.Heartbeat();
            pickup = SoundSynth.Pickup();
            click = SoundSynth.Click(rng);
            blink = SoundSynth.Blink(rng);
            rattle = SoundSynth.Rattle(rng);
            creakFallback = SoundSynth.Creak();
            jumpscare = SoundSynth.Jumpscare(rng);
            winChime = SoundSynth.WinChime();

            woodenSteps = Resources.LoadAll<AudioClip>("Audio/WoodenFootsteps");
            stairsSteps = Resources.LoadAll<AudioClip>("Audio/StairsFootsteps");
            concreteSteps = Resources.LoadAll<AudioClip>("Audio/ConcreteFootsteps");
            creakingDoors = TrimClips(Resources.LoadAll<AudioClip>("Audio/CreakingDoor"), 0.26f);
            monsterGrowls = Resources.LoadAll<AudioClip>("Audio/MonsterGrowl");
            keyPickupClip = Resources.Load<AudioClip>("Audio/MonsterGrowl/Monster Growl (6)") ?? pickup;
            ambientWinds = Resources.LoadAll<AudioClip>("Audio/AmbientWind");
            AudioClip bgmClip = Resources.Load<AudioClip>("Audio/BGM");

            doorKnockingQuiet = Resources.Load<AudioClip>("Audio/DoorKnockingQuiet");
            doorSqueeky2 = Resources.Load<AudioClip>("Audio/DoorSqueeky2");
            doorHandleJiggle = Resources.Load<AudioClip>("Audio/DoorHandleJiggle");
            creepyWindHowl = Resources.Load<AudioClip>("Audio/CreepyWindHowl");
            woodenStep3 = Resources.Load<AudioClip>("Audio/WoodenFootsteps/Wooden Foosteps (3)");

            effects = gameObject.AddComponent<AudioSource>();
            AudioClip angelStep = Resources.Load<AudioClip>("Audio/AngelFootstep") ?? SoundSynth.StoneScrape(rng);
            scrape = CreateLoop(angelStep, 0f);

            AudioClip windClip = Pick(ambientWinds) ?? SoundSynth.AmbientDrone();
            ambient = CreateLoop(windClip, 0.22f);

            if (bgmClip != null)
                bgm = CreateLoop(bgmClip, 0.45f);

            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;

            windowWindSource = gameObject.AddComponent<AudioSource>();
            windowWindSource.playOnAwake = false;

            ApplyMuteState();
        }

        public void ToggleMusic()
        {
            musicOn = !musicOn;
            ApplyMuteState();
        }

        public void ToggleSfx()
        {
            sfxOn = !sfxOn;
            ApplyMuteState();
        }

        void ApplyMuteState()
        {
            if (bgm != null) bgm.mute = !musicOn;
            if (ambient != null) ambient.mute = !musicOn;
            if (effects != null) effects.mute = !sfxOn;
            if (scrape != null) scrape.mute = !sfxOn;
            if (footstepSource != null) footstepSource.mute = !sfxOn;
            if (windowWindSource != null) windowWindSource.mute = !sfxOn;
        }

        AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[rng.Next(clips.Length)];
        }

        /// <summary>Cắt bỏ khoảng lặng đầu và rút gọn clip (kèm fade-out mượt) cho hợp thao tác tức thời.</summary>
        static AudioClip[] TrimClips(AudioClip[] clips, float maxSeconds)
        {
            if (clips == null) return null;
            var result = new AudioClip[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip src = clips[i];
                int channels = src.channels;
                int total = src.samples * channels;
                var data = new float[total];
                if (!src.GetData(data, 0)) { result[i] = src; continue; }

                int startSample = 0;
                for (int s = 0; s < src.samples; s++)
                {
                    if (Mathf.Abs(data[s * channels]) > 0.02f) { startSample = s; break; }
                }

                int keepSamples = Mathf.Min(src.samples - startSample, Mathf.Max(1, Mathf.RoundToInt(src.frequency * maxSeconds)));
                var trimmed = new float[keepSamples * channels];
                int fadeStart = (int)(keepSamples * 0.6f);
                for (int s = 0; s < keepSamples; s++)
                {
                    float fade = s >= fadeStart ? 1f - (float)(s - fadeStart) / (keepSamples - fadeStart) : 1f;
                    for (int c = 0; c < channels; c++)
                        trimmed[s * channels + c] = data[(startSample + s) * channels + c] * fade;
                }

                AudioClip dst = AudioClip.Create(src.name + "_short", keepSamples, channels, src.frequency, false);
                dst.SetData(trimmed, 0);
                result[i] = dst;
            }
            return result;
        }

        AudioSource CreateLoop(AudioClip clip, float volume)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
            return source;
        }

        void Update()
        {
            scrape.volume = Mathf.MoveTowards(scrape.volume, scrapeTarget, Time.deltaTime * 8f);

            if (windowWindPlayTimer > 0f)
            {
                windowWindPlayTimer -= Time.deltaTime;
                if (windowWindPlayTimer <= 0.35f && windowWindSource != null)
                    windowWindSource.volume = Mathf.Lerp(0f, windowWindTargetVol, windowWindPlayTimer / 0.35f);
                if (windowWindPlayTimer <= 0f && windowWindSource != null)
                    windowWindSource.Stop();
            }

            if (danger <= 0f) return;
            heartTimer -= Time.deltaTime;
            if (heartTimer <= 0f)
            {
                effects.PlayOneShot(heartbeat, 0.3f + 0.7f * danger);
                heartTimer = Mathf.Lerp(1.1f, 0.38f, danger); // càng gần tim đập càng dồn
            }
        }

        /// <summary>Cập nhật tiếng bước chân theo loại mặt sàn (0: gỗ phòng học, 1: bậc cửa/cầu thang, 2: bê tông hành lang).</summary>
        public void UpdateFootsteps(bool moving, bool running, TileType tile)
        {
            if (!moving)
            {
                if (footstepSource != null && footstepSource.isPlaying) footstepSource.Pause();
                return;
            }

            int surface = (tile == TileType.RoomFloor || tile == TileType.Podium) ? 0
                : (tile == TileType.Door) ? 1 : 2;

            footstepSource.pitch = running ? 1.32f : 1.0f;
            footstepSource.volume = running ? 0.55f : 0.38f;

            if (surface != currentSurface || footstepSource.clip == null)
            {
                currentSurface = surface;
                if (surface == 0)
                {
                    footstepSource.clip = woodenStep3 ?? Pick(woodenSteps);
                }
                else
                {
                    AudioClip[] pool = surface == 1 ? stairsSteps : concreteSteps;
                    footstepSource.clip = Pick(pool);
                }

                if (footstepSource.clip != null)
                {
                    footstepSource.time = (float)(rng.NextDouble() * Mathf.Max(0.1f, footstepSource.clip.length * 0.5f));
                    footstepSource.Play();
                }
            }
            else if (!footstepSource.isPlaying)
            {
                footstepSource.UnPause();
                if (!footstepSource.isPlaying) footstepSource.Play();
            }
        }

        /// <summary>Mức nguy hiểm 0..1 theo khoảng cách tới tượng; điều khiển nhịp tim và tiếng gầm.</summary>
        public void SetDanger(float value) => danger = value;
        public void SetScrapeVolume(float volume, float pitch = 1f)
        {
            scrapeTarget = volume;
            if (scrape != null) scrape.pitch = pitch;
        }

        public void PlayFootstep() => effects.PlayOneShot(footstepFallback, 0.3f);
        public void PlayBang(float volume) => effects.PlayOneShot(bang, volume);
        public void PlayDoorKnock(float volume) => effects.PlayOneShot(doorKnockingQuiet ?? bang, volume);
        public void PlayDoorHandleJiggle(float volume) => effects.PlayOneShot(doorHandleJiggle ?? rattle, volume);
        public void PlayDoorSqueak(float volume) => effects.PlayOneShot(doorSqueeky2 ?? creakFallback, volume);

        public void PlayWindowWind(float volume)
        {
            if (creepyWindHowl == null || windowWindSource == null) return;
            if (windowWindPlayTimer > 0f) return;
            windowWindSource.clip = creepyWindHowl;
            float maxStart = Mathf.Max(0f, creepyWindHowl.length - 2.5f);
            windowWindSource.time = (float)(rng.NextDouble() * maxStart);
            windowWindTargetVol = volume;
            windowWindSource.volume = volume;
            windowWindSource.Play();
            windowWindPlayTimer = 2.0f;
        }

        public void PlayPickup() => effects.PlayOneShot(pickup, 0.7f);
        public void PlayKeyPickup() => effects.PlayOneShot(keyPickupClip, 0.9f);
        public void PlayClick() => effects.PlayOneShot(click, 0.8f);
        public void PlayBlink() => effects.PlayOneShot(blink, 0.55f);
        public void PlayRattle() => effects.PlayOneShot(rattle, 0.6f);
        public void PlayCreak() => effects.PlayOneShot(Pick(creakingDoors) ?? creakFallback, 0.65f);
        public void PlayGrowl(float volume)
        {
            AudioClip clip = Pick(monsterGrowls);
            if (clip != null) effects.PlayOneShot(clip, volume);
        }
        public void PlayWin()
        {
            UpdateFootsteps(false, false, TileType.CorridorFloor);
            effects.PlayOneShot(winChime, 0.8f);
        }

        public void PlayJumpscare()
        {
            danger = 0f;
            scrapeTarget = 0f;
            UpdateFootsteps(false, false, TileType.CorridorFloor);
            ambient.Stop();
            if (bgm != null) bgm.Stop();
            PlayGrowl(1f);
            effects.PlayOneShot(jumpscare, 1f);
        }
    }
}
