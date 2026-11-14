using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyberRush
{
    /// <summary>
    /// Procedural DSP Synthwave BGM + Cyber Combat SFX Engine (zero external audio files required).
    /// </summary>
    public class CyberAudio : MonoBehaviour
{
    public static CyberAudio Instance { get; private set; }

    public bool IsMuted { get; private set; }
    [Range(0f, 1f)] public float masterVolume = 0.72f;
    [Range(0f, 1f)] public float bgmVolume = 0.28f;
    [Range(0f, 1f)] public float sfxVolume = 0.65f;

    private AudioSource bgmSource;
    private AudioSource[] sfxPool;
    private int poolIdx;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private const int SampleRate = 44100;

    public static CyberAudio EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindFirstObjectByType<CyberAudio>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        var go = new GameObject("CyberAudio");
        Instance = go.AddComponent<CyberAudio>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (FindFirstObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;

        sfxPool = new AudioSource[14];
        for (int i = 0; i < sfxPool.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            sfxPool[i] = src;
        }

        BuildAllClips();
        StartBgm();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMute();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        float m = IsMuted ? 0f : masterVolume;
        if (bgmSource != null)
            bgmSource.volume = m * bgmVolume;
    }

    public void StartBgm()
    {
        if (bgmSource != null && clips.TryGetValue("bgm", out var clip))
        {
            bgmSource.clip = clip;
            ApplyVolumes();
            if (!bgmSource.isPlaying)
                bgmSource.Play();
        }
    }

    public static void Play(string key, float volumeScale = 1f, float pitchJitter = 0.05f)
    {
        var inst = Instance != null ? Instance : EnsureInstance();
        inst.PlayInternal(key, volumeScale, pitchJitter);
    }

    private void PlayInternal(string key, float volumeScale, float pitchJitter)
    {
        if (IsMuted) return;
        if (!clips.TryGetValue(key, out var clip) || clip == null) return;
        if (sfxPool == null || sfxPool.Length == 0) return;

        var src = sfxPool[poolIdx];
        poolIdx = (poolIdx + 1) % sfxPool.Length;
        src.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
        src.volume = masterVolume * sfxVolume * Mathf.Clamp(volumeScale, 0.05f, 1.5f);
        src.PlayOneShot(clip);
    }

    private void BuildAllClips()
    {
        clips["slash"] = MakeSweepNoiseClip("SFX_Slash", 0.19f, 1500f, 380f, 0.55f, 0.55f);
        clips["wave"] = MakeWaveClip("SFX_SwordWave", 0.34f);
        clips["cyclone"] = MakeCycloneClip("SFX_Cyclone", 0.42f);
        clips["slam"] = MakeExplosionClip("SFX_DiveSlam", 0.38f, 165f, 42f, 0.72f);
        clips["blaster"] = MakeHeavyGunshotClip("SFX_Blaster", 0.17f, punchFreq: 195f, crackGain: 0.92f);
        clips["railgun"] = MakeRailgunClip("SFX_Railgun", 0.50f);
        clips["scatter"] = MakeHeavyGunshotClip("SFX_Scatter", 0.28f, punchFreq: 145f, crackGain: 1.05f);
        clips["hit"] = MakeHitClangClip("SFX_HitClang", 0.16f);
        clips["deflect"] = MakeDeflectClip("SFX_Deflect", 0.24f);
        clips["explosion"] = MakeExplosionClip("SFX_Explosion", 0.48f, 140f, 32f, 0.85f);
        clips["dash"] = MakeSweepNoiseClip("SFX_Dash", 0.18f, 2100f, 550f, 0.78f, 0.32f);
        clips["jumppad"] = MakeJumpPadClip("SFX_JumpPad", 0.32f);
        clips["damage"] = MakeSweepNoiseClip("SFX_Damage", 0.24f, 180f, 65f, 0.65f, 0.72f);
        clips["clear"] = MakeClearFanfareClip("SFX_Clear", 0.95f);
        clips["bgm"] = MakeSynthwaveBgmClip("BGM_Synthwave");
    }

    private static AudioClip MakeHeavyGunshotClip(string name, float dur, float punchFreq, float crackGain)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float pSub = 0f;
        float lp = 0f;
        float prevNoise = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            // Fast 3ms mechanical snap + sub-bass chest thump + gritty bandpass propellant tail
            float punchEnv = Mathf.Exp(-t * 18.0f);
            float crackEnv = Mathf.Min(1f, t * 140f) * Mathf.Exp(-t * 11.5f);
            float snapEnv = Mathf.Exp(-t * 48.0f);

            pSub += Mathf.Lerp(punchFreq, 38f, Mathf.Pow(t, 0.35f)) / SampleRate;
            float subBody = Mathf.Sin(pSub * 6.28318f) * punchEnv * 0.75f;

            float raw = HashNoise(i * 5 + 17);
            float hp = raw - prevNoise * 0.65f; // Crisp mid/high crack without harsh low mud
            prevNoise = raw;
            lp = Mathf.Lerp(lp, hp, 0.42f);

            // Metallic bolt clack ~14ms
            float mechClack = Mathf.Sin((i / (float)SampleRate) * 6.28318f * 1180f) * snapEnv * 0.22f;

            float mixed = (subBody + lp * crackEnv * crackGain + mechClack);
            // Soft-clip saturation for punchy firearm bite
            d[i] = Mathf.Clamp((float)Math.Tanh(mixed * 1.9f) * 0.88f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip CreateClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float HashNoise(int i)
    {
        uint x = (uint)i * 747796405u + 2891336453u;
        x = ((x >> ((int)((x >> 28) & 15) + 4)) ^ x) * 277803737u;
        x = (x >> 22) ^ x;
        return (x / (float)uint.MaxValue) * 2f - 1f;
    }

    private static AudioClip MakeSweepNoiseClip(string name, float dur, float fStart, float fEnd, float noiseMix, float gain)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float phase = 0f;
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.Min(1f, t * 14f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - t, 1.8f);
            float f = Mathf.Lerp(fStart, fEnd, Mathf.Pow(t, 0.7f));
            phase += f / SampleRate;
            float saw = (phase - Mathf.Floor(phase)) * 2f - 1f;
            float sin = Mathf.Sin(phase * Mathf.PI * 2f);
            lp = Mathf.Lerp(lp, HashNoise(i), 0.25f);
            d[i] = Mathf.Clamp((saw * 0.45f + sin * 0.55f) * (1f - noiseMix) + lp * noiseMix, -1f, 1f) * env * gain;
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeWaveClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Min(1f, t * 20f) * Mathf.Pow(1f - t, 1.5f);
            float f = Mathf.Lerp(980f, 340f, t);
            p1 += f / SampleRate;
            p2 += (f * 1.5f) / SampleRate;
            float s = Mathf.Sin(p1 * 6.28318f) * 0.55f + Mathf.Sin(p2 * 6.28318f) * 0.30f + HashNoise(i) * 0.18f * (1f - t);
            d[i] = Mathf.Clamp(s * env * 0.72f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeCycloneClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float p = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(t * Mathf.PI);
            float lfo = 0.65f + 0.35f * Mathf.Sin(t * 6.28318f * 5f);
            float f = 360f + 540f * Mathf.Sin(t * Mathf.PI * 0.7f);
            p += f / SampleRate;
            float saw = (p - Mathf.Floor(p)) * 2f - 1f;
            d[i] = Mathf.Clamp((saw * 0.55f + HashNoise(i) * 0.45f) * env * lfo * 0.68f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeRailgunClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float pSub = 0f, pZap = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Pow(1f - t, 1.35f);
            pSub += Mathf.Lerp(190f, 45f, Mathf.Pow(t, 0.5f)) / SampleRate;
            pZap += Mathf.Lerp(2600f, 380f, t) / SampleRate;
            float s = Mathf.Sin(pSub * 6.28318f) * 0.75f + ((pZap - Mathf.Floor(pZap)) * 2f - 1f) * 0.45f + HashNoise(i) * 0.40f * Mathf.Pow(1f - t, 2.4f);
            d[i] = Mathf.Clamp(s * env * 0.78f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeHitClangClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Pow(1f - t, 2.5f);
            float sec = i / (float)SampleRate;
            float s = Mathf.Sin(sec * 6.28318f * 1320f) * 0.45f
                    + Mathf.Sin(sec * 6.28318f * 2040f) * 0.35f
                    + HashNoise(i) * 0.35f * Mathf.Pow(1f - t, 5f);
            d[i] = Mathf.Clamp(s * env * 0.75f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeDeflectClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float sec = i / (float)SampleRate;
            float env = Mathf.Pow(1f - t, 2.2f);
            float f = Mathf.Lerp(1760f, 2640f, Mathf.Min(1f, t * 4f));
            float s = Mathf.Sin(sec * 6.28318f * f) * 0.6f + Mathf.Sin(sec * 6.28318f * (f * 1.5f)) * 0.4f;
            d[i] = Mathf.Clamp(s * env * 0.78f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeExplosionClip(string name, float dur, float fStart, float fEnd, float gain)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float p = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Pow(1f - t, 1.7f);
            p += Mathf.Lerp(fStart, fEnd, t) / SampleRate;
            lp = Mathf.Lerp(lp, HashNoise(i), 0.14f);
            float s = Mathf.Sin(p * 6.28318f) * 0.65f + lp * 0.75f;
            d[i] = Mathf.Clamp(s * env * gain, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeJumpPadClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float p = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Min(1f, t * 18f) * Mathf.Pow(1f - t, 1.3f);
            p += Mathf.Lerp(260f, 1180f, Mathf.Sqrt(t)) / SampleRate;
            d[i] = Mathf.Clamp(Mathf.Sin(p * 6.28318f) * env * 0.65f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeClearFanfareClip(string name, float dur)
    {
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] d = new float[n];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float sec = i / (float)SampleRate;
            int idx = Mathf.Clamp((int)(t * notes.Length), 0, notes.Length - 1);
            float localT = (t * notes.Length) - idx;
            float env = Mathf.Min(1f, localT * 12f) * Mathf.Pow(1f - t, 0.9f);
            float f = notes[idx];
            float s = Mathf.Sin(sec * 6.28318f * f) * 0.55f + Mathf.Sin(sec * 6.28318f * f * 2f) * 0.25f;
            d[i] = Mathf.Clamp(s * env * 0.72f, -1f, 1f);
        }
        return CreateClip(name, d);
    }

    private static AudioClip MakeSynthwaveBgmClip(string name)
    {
        // 4 bars @ 128 BPM (4 beats/bar = 16 beats = 7.50s exact seamless loop)
        const float bpm = 128f;
        float beatDur = 60f / bpm;
        float loopDur = beatDur * 16f;
        int n = Mathf.RoundToInt(loopDur * SampleRate);
        float[] d = new float[n];

        // Minor cyber chord progression: C#m (C#2) - A (A1) - E (E2) - B (B1)
        float[] bassFreqs = { 69.30f, 55.00f, 82.41f, 61.74f };
        float[] arpNotes = { 277.18f, 329.63f, 415.30f, 554.37f, 220.00f, 277.18f, 329.63f, 440.00f,
                             329.63f, 415.30f, 493.88f, 659.25f, 246.94f, 311.13f, 369.99f, 493.88f };

        float pBass = 0f, pArp = 0f, lpBass = 0f;
        for (int i = 0; i < n; i++)
        {
            float sec = i / (float)SampleRate;
            float beatPos = sec / beatDur;
            int bar = Mathf.Clamp((int)(beatPos / 4f), 0, 3);
            float beatFrac = beatPos - Mathf.Floor(beatPos);
            int sixteenth = (int)(beatPos * 4f) % 4;
            float subFrac = (beatPos * 4f) - Mathf.Floor(beatPos * 4f);

            // Driving 16th-note sawtooth cyber bass with sidechain duck on beat
            float sidechain = Mathf.Lerp(0.32f, 1f, Mathf.Min(1f, beatFrac * 2.8f));
            float bf = bassFreqs[bar] * (sixteenth == 3 ? 2f : 1f);
            pBass = (pBass + bf / SampleRate) % 1f;
            float sawBass = (pBass * 2f - 1f) * 0.65f + Mathf.Sin(pBass * 6.28318f) * 0.45f;
            lpBass = Mathf.Lerp(lpBass, sawBass, 0.16f);
            float bassEnv = Mathf.Pow(1f - subFrac * 0.55f, 1.3f) * sidechain;

            // Shimmering 16th arpeggio
            int arpIdx = bar * 4 + sixteenth;
            pArp = (pArp + arpNotes[arpIdx] / SampleRate) % 1f;
            float arpTri = 1f - 4f * Mathf.Abs(pArp - 0.5f);
            float arpEnv = Mathf.Pow(1f - subFrac, 2.2f);

            // 4-on-the-floor Kick
            float kick = 0f;
            if (beatFrac < 0.28f)
            {
                float kt = beatFrac / 0.28f;
                float kf = Mathf.Lerp(135f, 44f, Mathf.Pow(kt, 0.35f));
                kick = Mathf.Sin(kt * 0.28f * 6.28318f * kf) * Mathf.Pow(1f - kt, 2.4f);
            }

            // Crisp Cyber Snare on beats 2 & 4
            float snare = 0f;
            int beatInBar = ((int)beatPos) % 4;
            if ((beatInBar == 1 || beatInBar == 3) && beatFrac < 0.22f)
            {
                float st = beatFrac / 0.22f;
                snare = HashNoise(i * 3) * Mathf.Pow(1f - st, 3.0f) * 0.42f;
            }

            float sample = lpBass * bassEnv * 0.42f + arpTri * arpEnv * 0.18f + kick * 0.52f + snare;
            d[i] = Mathf.Clamp((float)Math.Tanh(sample * 1.15f) * 0.85f, -1f, 1f);
        }
        return CreateClip(name, d);
    }
}
}
