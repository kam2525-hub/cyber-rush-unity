using UnityEngine;

namespace CyberRush
{
    public enum BeatGrade
    {
        OffBeat = 0,
        Good = 1,
        Perfect = 2
    }

    /// <summary>
    /// 128 BPM hardware-sample-locked rhythm engine driving Beat Timing, Combo Multiplier, Fever Overdrive & Visual Beat Pulse.
    /// </summary>
    public class CyberRhythm : MonoBehaviour
    {
        public static CyberRhythm Instance { get; private set; }

        public const float BPM = 128f;
        public const float BeatDuration = 60f / BPM; // 0.46875s exact

        public int BeatIndex { get; private set; }
        public float BeatPhase { get; private set; }     // 0.0 right ON beat -> 1.0 right before next beat
        public float BeatPulse { get; private set; }     // 1.0 spike on beat -> 0.0 decay
        public float TimeErrorSeconds { get; private set; }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public bool IsFever => Multiplier >= 8;
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int TotalActions { get; private set; }
        public int FeverKills { get; private set; }

        public float AccuracyPercent => TotalActions > 0
            ? Mathf.Clamp01((PerfectCount + GoodCount * 0.7f) / TotalActions) * 100f
            : 100f;

        public string LastJudgeLabel { get; private set; } = "";
        public Color LastJudgeColor { get; private set; } = Color.cyan;
        public float LastJudgeTimer { get; private set; }

        public static CyberRhythm EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<CyberRhythm>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }
            var go = new GameObject("CyberRhythm");
            Instance = go.AddComponent<CyberRhythm>();
            return Instance;
        }

        private void Awake()
        {
            Instance = this;
        }

        public void ResetSession()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            Multiplier = 1;
            PerfectCount = 0;
            GoodCount = 0;
            TotalActions = 0;
            FeverKills = 0;
            LastJudgeLabel = "";
            LastJudgeTimer = 0f;
        }

        private void Update()
        {
            float songSec = (CyberAudio.Instance != null && CyberAudio.Instance.BgmIsPlaying)
                ? CyberAudio.Instance.BgmSampleTimeSeconds
                : Time.unscaledTime;

            float totalBeats = songSec / BeatDuration;
            int currentIdx = Mathf.FloorToInt(totalBeats);
            BeatPhase = totalBeats - currentIdx;
            BeatIndex = currentIdx;

            float distPhase = Mathf.Min(BeatPhase, 1f - BeatPhase);
            TimeErrorSeconds = distPhase * BeatDuration;
            BeatPulse = Mathf.Pow(1f - BeatPhase, 2.8f);

            if (LastJudgeTimer > 0f)
                LastJudgeTimer -= Time.unscaledDeltaTime;
        }

        public BeatGrade JudgeBeatAction(bool allowComboPenalty = true)
        {
            TotalActions++;
            BeatGrade grade;
            if (TimeErrorSeconds <= 0.095f)
            {
                grade = BeatGrade.Perfect;
                PerfectCount++;
                IncrementCombo(120);
                LastJudgeLabel = "PERFECT BEAT!!";
                LastJudgeColor = new Color(0.22f, 0.98f, 1.0f);
                CyberAudio.Play("beat_perfect", 0.55f, 0.02f);
            }
            else if (TimeErrorSeconds <= 0.165f)
            {
                grade = BeatGrade.Good;
                GoodCount++;
                IncrementCombo(60);
                LastJudgeLabel = "JUST BEAT!";
                LastJudgeColor = new Color(0.35f, 1.0f, 0.65f);
                CyberAudio.Play("beat_good", 0.45f, 0.02f);
            }
            else
            {
                grade = BeatGrade.OffBeat;
                if (allowComboPenalty && Combo > 0)
                {
                    Combo = Mathf.Max(0, Combo / 2 - 1);
                    UpdateMultiplier(silent: true);
                }
                LastJudgeLabel = "OFF BEAT";
                LastJudgeColor = new Color(0.75f, 0.78f, 0.85f);
            }
            LastJudgeTimer = 0.52f;
            return grade;
        }

        public void RegisterKillScore(BeatGrade attackGrade, bool isBoss)
        {
            int basePts = isBoss ? 1500 : 300;
            bool onBeat = attackGrade != BeatGrade.OffBeat;
            if (onBeat)
            {
                FeverKills++;
                basePts += (attackGrade == BeatGrade.Perfect ? 250 : 120);
            }
            Score += basePts * Multiplier;
        }

        public string ComputeScoreRank(float elapsedSeconds, float sRankTarget)
        {
            float acc = AccuracyPercent;
            if (acc >= 82f && MaxCombo >= 12 && elapsedSeconds <= sRankTarget * 1.15f) return "SSS";
            if (acc >= 70f && MaxCombo >= 8) return "SS";
            if (elapsedSeconds <= sRankTarget || acc >= 60f) return "S";
            if (elapsedSeconds <= sRankTarget * 1.6f) return "A";
            return "B";
        }

        private void IncrementCombo(int basePoints)
        {
            Combo++;
            if (Combo > MaxCombo) MaxCombo = Combo;
            UpdateMultiplier(silent: false);
            Score += basePoints * Multiplier;
        }

        private void UpdateMultiplier(bool silent)
        {
            int prev = Multiplier;
            if (Combo >= 28) Multiplier = 16;
            else if (Combo >= 16) Multiplier = 8;
            else if (Combo >= 8) Multiplier = 4;
            else if (Combo >= 4) Multiplier = 2;
            else Multiplier = 1;

            if (!silent && Multiplier > prev && Multiplier >= 8)
            {
                CyberAudio.Play("fever_up", 0.90f);
                CyberHUD.ShowCallout(
                    Multiplier >= 16 ? "HYPER FEVER x16!!" : "FEVER OVERDRIVE x8!!",
                    Multiplier >= 16 ? new Color(1f, 0.85f, 0.22f) : new Color(1f, 0.22f, 0.78f));
            }
        }
    }
}
