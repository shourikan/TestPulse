using UnityEngine;

namespace Pulse
{
    public sealed class PulseAudio : MonoBehaviour
    {
        AudioSource music;
        readonly AudioClip[] themes = new AudioClip[4];
        int nextTheme = 1;
        bool themeStarted;
        readonly AudioSource[] voices = new AudioSource[6];
        AudioClip gold, shield, impact, shieldBreak, death, pulse, record, slow, click;
        int voice;
        public float EffectiveMusicVolume => music != null ? music.volume : 0;
        public bool MusicPlaying => music != null && music.isPlaying;

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false; music.loop = true; music.spatialBlend = 0;
            for (int i = 0; i < themes.Length; i++)
                themes[i] = Resources.Load<AudioClip>("Audio/GameBg-" + (i + 1));
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false; voices[i].spatialBlend = 0;
            }
            gold = MakeChime("Gold chime", 659.25f, 987.77f);
            shield = MakeChime("Shield chime", 440, 659.25f);
            impact = MakeImpact("Obstacle break", .33f, 190);
            shieldBreak = MakeImpact("Shield break", .44f, 158);
            death = MakeImpact("Signal dissolve", .8f, 123);
            pulse = Resources.Load<AudioClip>("Audio/Shield");
            record = Resources.Load<AudioClip>("Audio/Record");
            slow = Resources.Load<AudioClip>("Audio/Slow"); click = MakeClick();
        }
        public void Refresh(RunState state, bool newRun = false)
        {
            bool playing = state == RunState.Playing;
            music.volume = PulseSettings.Music * (playing ? .45f : .22f);
            music.loop = !playing;
            if (newRun) PlayNextTheme();
            else if (state == RunState.Title && music.clip != themes[0]) PlayTheme(themes[0]);
            else if (!music.isPlaying && music.clip != null) music.Play();
        }

        void Update()
        {
            if (music.isPlaying) themeStarted = true;
            else if (themeStarted && !music.loop && !AudioListener.pause) PlayNextTheme();
        }

        void PlayNextTheme()
        {
            for (int i = 0; i < themes.Length; i++)
            {
                var theme = themes[nextTheme];
                nextTheme = (nextTheme + 1) % themes.Length;
                if (theme == null) continue;
                PlayTheme(theme);
                return;
            }
            PlayTheme(null);
        }

        void PlayTheme(AudioClip theme)
        {
            themeStarted = false;
            music.Stop();
            music.clip = theme;
            if (theme != null) music.Play();
        }

        public void Click() => Play(click, .5f);
        public void Feedback(FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.Gold: Play(gold, .48f, Random.Range(.985f, 1.015f)); break;
                case FeedbackKind.Shield: Play(shield, .45f); break;
                case FeedbackKind.Pulse: Play(pulse, .45f); break;
                case FeedbackKind.Death: Play(death, .73f); break;
                case FeedbackKind.ShieldBreak: Play(shieldBreak, .65f); break;
                case FeedbackKind.Impact: Play(impact, .51f, Random.Range(.96f, 1.04f)); break;
                case FeedbackKind.Record: Play(record, .45f); break;
                case FeedbackKind.Slow: Play(slow, .3f); break;
            }
        }
        void Play(AudioClip clip, float gain, float pitch = 1)
        {
            if (clip == null || PulseSettings.Sfx <= 0) return;
            var source = voices[voice++ % voices.Length];
            source.Stop(); source.pitch = pitch; source.volume = PulseSettings.Sfx * gain;
            source.clip = clip; source.Play();
        }
        static AudioClip MakeClick()
        {
            const int count = 1920;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / 48000f;
                samples[i] = Mathf.Sin(t * 880 * Mathf.PI * 2) * Mathf.Exp(-t * 110) * Mathf.Min(1, i / 240f) * Mathf.Min(1, (count - i) / 240f) * .25f;
            }
            var clip = AudioClip.Create("Pulse UI", count, 1, 48000, false);
            clip.SetData(samples, 0); return clip;
        }

        static AudioClip MakeChime(string name, float firstNote, float secondNote)
        {
            return MakeEffect(name, .35f, t =>
                Mallet(t, firstNote, .075f) * .34f + Mallet(t - .058f, secondNote, .085f) * .23f);
        }

        static float Mallet(float t, float frequency, float decay)
        {
            if (t < 0) return 0;
            float phase = 2 * Mathf.PI * frequency * t;
            float tone = Mathf.Sin(phase) + .15f * Mathf.Sin(phase * 2) * Mathf.Exp(-t / .036f);
            return tone * Mathf.Min(1, t / .009f) * Mathf.Exp(-t / decay);
        }

        static AudioClip MakeImpact(string name, float duration, float basePitch)
        {
            var random = new System.Random(23);
            float filtered = 0, phase = 0;
            return MakeEffect(name, duration, t =>
            {
                filtered += ((float)random.NextDouble() * 2 - 1 - filtered) * .17f;
                phase += 2 * Mathf.PI * basePitch * (1 + .55f * Mathf.Exp(-t / .05f)) / EffectSampleRate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-5 * t / duration) * .49f;
                float scatter = filtered * Mathf.Exp(-6 * t / duration) * .44f;
                float shimmer = Mallet(t - .035f, basePitch * 3, duration * .23f) * .14f;
                return thump + scatter + shimmer;
            });
        }

        const int EffectSampleRate = 24000;
        static AudioClip MakeEffect(string name, float duration, System.Func<float, float> sample)
        {
            var samples = new float[Mathf.RoundToInt(duration * EffectSampleRate)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)EffectSampleRate;
                float fade = Mathf.Min(1, Mathf.Min(t / .009f, (duration - t) / .045f));
                samples[i] = sample(t) * fade;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, EffectSampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            Destroy(click);
            Destroy(gold);
            Destroy(shield);
            Destroy(impact);
            Destroy(shieldBreak);
            Destroy(death);
        }
    }
}
