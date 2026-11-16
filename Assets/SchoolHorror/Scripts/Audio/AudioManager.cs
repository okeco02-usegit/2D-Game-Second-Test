using UnityEngine;

namespace SchoolHorror
{
    /// <summary>Phát mọi âm thanh của game: hiệu ứng một lần, tiếng đá cọ xát, tiếng ù nền và nhịp tim theo mức nguy hiểm.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        AudioSource effects;
        AudioSource scrape;
        AudioSource ambient;

        AudioClip footstep, bang, heartbeat, pickup, click, rattle, creak, jumpscare, winChime;
        float scrapeTarget;
        float danger;
        float heartTimer;

        void Awake()
        {
            var rng = new System.Random(1234);
            footstep = SoundSynth.Footstep(rng);
            bang = SoundSynth.DoorBang(rng);
            heartbeat = SoundSynth.Heartbeat();
            pickup = SoundSynth.Pickup();
            click = SoundSynth.Click(rng);
            rattle = SoundSynth.Rattle(rng);
            creak = SoundSynth.Creak();
            jumpscare = SoundSynth.Jumpscare(rng);
            winChime = SoundSynth.WinChime();

            effects = gameObject.AddComponent<AudioSource>();
            scrape = CreateLoop(SoundSynth.StoneScrape(rng), 0f);
            ambient = CreateLoop(SoundSynth.AmbientDrone(), 0.35f);
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
            scrape.volume = Mathf.MoveTowards(scrape.volume, scrapeTarget, Time.deltaTime * 4f);

            if (danger <= 0f) return;
            heartTimer -= Time.deltaTime;
            if (heartTimer <= 0f)
            {
                effects.PlayOneShot(heartbeat, 0.3f + 0.7f * danger);
                heartTimer = Mathf.Lerp(1.1f, 0.38f, danger); // càng gần tim đập càng dồn
            }
        }

        /// <summary>Mức nguy hiểm 0..1 theo khoảng cách tới tượng; điều khiển nhịp tim.</summary>
        public void SetDanger(float value) => danger = value;
        public void SetScrapeVolume(float volume) => scrapeTarget = volume;

        public void PlayFootstep() => effects.PlayOneShot(footstep, 0.3f);
        public void PlayBang(float volume) => effects.PlayOneShot(bang, volume);
        public void PlayPickup() => effects.PlayOneShot(pickup, 0.7f);
        public void PlayClick() => effects.PlayOneShot(click, 0.8f);
        public void PlayRattle() => effects.PlayOneShot(rattle, 0.6f);
        public void PlayCreak() => effects.PlayOneShot(creak, 0.6f);
        public void PlayWin() => effects.PlayOneShot(winChime, 0.8f);

        public void PlayJumpscare()
        {
            danger = 0f;
            scrapeTarget = 0f;
            ambient.Stop();
            effects.PlayOneShot(jumpscare, 1f);
        }
    }
}
