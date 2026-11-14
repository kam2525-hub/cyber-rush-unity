using System.Collections.Generic;
using UnityEngine;

namespace CyberRush
{
    public enum GameMode
    {
        MainMenu,
        Parkour,
        BossRush,
        VsCpuArena,
        LocalPvp,
        LanPvp
    }

    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [System.Serializable]
        public class StageConfig
        {
            public string stageName;
            public Vector3 spawnPosition;
            public float sRankTime;
            public float aRankTime;
        }

        public List<StageConfig> stages = new List<StageConfig>();

        public GameMode ActiveMode { get; private set; } = GameMode.MainMenu;
        public bool IsMenuOpen => ActiveMode == GameMode.MainMenu;

        public int CurrentStage { get; private set; } = 1;
        public float StageElapsed { get; private set; }
        public int StageKills { get; private set; }
        public int TotalKills { get; private set; }
        public bool IsOverlayOpen { get; private set; }
        public bool IsAllStagesCleared { get; private set; }
        public string LastRank { get; private set; } = "S";
        public string ArenaWinnerText { get; private set; } = "";
        public int ArenaTargetKills { get; private set; } = 5;

        public CyberParkourController Player1 { get; private set; }
        public CyberParkourController Player2 { get; private set; }

        private readonly Dictionary<int, float> _bestTimes = new Dictionary<int, float>();
        private CyberBotAI[] _bots;
        private int _arenaBotsKilledCount;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CyberAudio.EnsureInstance();
            LanPvpManager.EnsureInstance();

            foreach (var p in FindObjectsByType<CyberParkourController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.playerId == 0) Player1 = p;
                else if (p.playerId == 1) Player2 = p;
            }
            _bots = FindObjectsByType<CyberBotAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var remote = FindFirstObjectByType<RemotePvpAvatar>(FindObjectsInactive.Include);
            if (remote != null && LanPvpManager.Instance != null)
                LanPvpManager.Instance.SetRemoteAvatar(remote);

            if (stages.Count == 0)
            {
                stages.Add(new StageConfig { stageName = "STAGE 1 // NEON SKYLINE", spawnPosition = new Vector3(0f, 2.0f, 0f), sRankTime = 18f, aRankTime = 28f });
                stages.Add(new StageConfig { stageName = "STAGE 2 // CYBER CANYON", spawnPosition = new Vector3(120f, 2.0f, 0f), sRankTime = 22f, aRankTime = 34f });
                stages.Add(new StageConfig { stageName = "STAGE 3 // ARCHON SPIRE", spawnPosition = new Vector3(240f, 2.0f, 0f), sRankTime = 28f, aRankTime = 42f });
            }

            OpenMainMenu();
        }

        private void Update()
        {
            if (!IsMenuOpen && !IsOverlayOpen)
            {
                StageElapsed += Time.unscaledDeltaTime;
            }
        }

        public StageConfig GetCurrentConfig()
        {
            int idx = Mathf.Clamp(CurrentStage - 1, 0, stages.Count - 1);
            return stages[idx];
        }

        public float GetBestTime(int stage) => _bestTimes.TryGetValue(stage, out float t) ? t : 0f;

        private void CleanupProjectilesAndToggleArenaActors(bool enableBots, bool enableP2, bool enableRemote)
        {
            foreach (var p in FindObjectsByType<CyberProjectile>(FindObjectsSortMode.None))
                Destroy(p.gameObject);

            if (_bots != null)
            {
                foreach (var b in _bots)
                    b.SetArenaActive(enableBots);
            }

            if (Player1 != null && Player1.playerCamera != null)
                Player1.playerCamera.rect = enableP2 ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 1f, 1f);

            if (Player2 != null)
            {
                Player2.gameObject.SetActive(enableP2);
                if (Player2.playerCamera != null)
                    Player2.playerCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            }

            if (!enableRemote && LanPvpManager.Instance != null)
                LanPvpManager.Instance.StopNetwork();
        }

        public void OpenMainMenu()
        {
            ActiveMode = GameMode.MainMenu;
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            Time.timeScale = 1f;
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: false, enableRemote: false);

            if (Player1 != null)
            {
                Player1.ConfigureArenaFighter(false, stages[0].spawnPosition, Quaternion.identity);
                Player1.RespawnAt(stages[0].spawnPosition, Quaternion.identity);
                Player1.SetCursorLocked(false);
            }
        }

        public void StartParkour(int stage)
        {
            ActiveMode = GameMode.Parkour;
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: false, enableRemote: false);
            StartStage(stage);
        }

        public void StartBossRush()
        {
            ActiveMode = GameMode.BossRush;
            CurrentStage = 3;
            StageElapsed = 0f;
            StageKills = 0;
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: false, enableRemote: false);

            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                e.ResetEnemy();

            if (Player1 != null)
            {
                // S3_BossArena は center.y=7.0, height=2.6 のため上面は y=8.3。余裕をもって y=9.5 に配置！
                Vector3 bossApproach = new Vector3(240f, 9.5f, 43.5f);
                Player1.ConfigureArenaFighter(true, bossApproach, Quaternion.identity);
                Player1.RespawnAt(bossApproach, Quaternion.identity);
                Player1.SetCursorLocked(true);
            }
            CyberHUD.ShowCallout("BOSS RUSH // アルコンコア戦！", new Color(1f, 0.22f, 0.72f));
        }

        public void StartVsCpuArena()
        {
            ActiveMode = GameMode.VsCpuArena;
            CurrentStage = 4;
            StageElapsed = 0f;
            StageKills = 0;
            _arenaBotsKilledCount = 0;
            ArenaTargetKills = 8;
            ArenaWinnerText = "";
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            CleanupProjectilesAndToggleArenaActors(enableBots: true, enableP2: false, enableRemote: false);

            if (Player1 != null)
            {
                Vector3 arenaP1 = new Vector3(360f, 2.2f, -20f);
                Player1.ConfigureArenaFighter(true, arenaP1, Quaternion.identity);
                Player1.RespawnAt(arenaP1, Quaternion.identity);
                Player1.SetCursorLocked(true);
            }
            CyberHUD.ShowCallout("CPUアリーナ // 8キル先取バトル！", new Color(0.18f, 0.95f, 1f));
        }

        public void StartLocalPvp()
        {
            ActiveMode = GameMode.LocalPvp;
            CurrentStage = 4;
            StageElapsed = 0f;
            StageKills = 0;
            ArenaTargetKills = 5;
            ArenaWinnerText = "";
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: true, enableRemote: false);

            Vector3 p1Spawn = new Vector3(360f, 2.2f, -20f);
            Vector3 p2Spawn = new Vector3(360f, 2.2f, 20f);

            if (Player1 != null)
            {
                Player1.ConfigureArenaFighter(true, p1Spawn, Quaternion.identity);
                Player1.RespawnAt(p1Spawn, Quaternion.identity);
                Player1.SetCursorLocked(false);
            }
            if (Player2 != null)
            {
                Player2.ConfigureArenaFighter(true, p2Spawn, Quaternion.Euler(0f, 180f, 0f));
                Player2.RespawnAt(p2Spawn, Quaternion.Euler(0f, 180f, 0f));
            }
            CyberHUD.ShowCallout("ローカル2P対人戦 // 5キル先取！", new Color(1f, 0.25f, 0.72f));
        }

        public void StartLanPvp(bool host, string ip, int port)
        {
            ActiveMode = GameMode.LanPvp;
            CurrentStage = 4;
            StageElapsed = 0f;
            StageKills = 0;
            ArenaTargetKills = 5;
            ArenaWinnerText = "";
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: false, enableRemote: true);

            Vector3 spawn = host ? new Vector3(360f, 2.2f, -20f) : new Vector3(360f, 2.2f, 20f);
            Quaternion rot = host ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            if (Player1 != null)
            {
                Player1.ConfigureArenaFighter(true, spawn, rot);
                Player1.RespawnAt(spawn, rot);
                Player1.SetCursorLocked(true);
            }

            if (host) LanPvpManager.Instance?.StartHost(port);
            else LanPvpManager.Instance?.StartJoin(ip, port);

            CyberHUD.ShowCallout(host ? $"LAN PVP HOST (:{port})" : $"LAN PVP JOIN ({ip}:{port})", new Color(0.2f, 0.95f, 1f));
        }

        public void StartStage(int stage)
        {
            CurrentStage = Mathf.Clamp(stage, 1, stages.Count);
            StageElapsed = 0f;
            StageKills = 0;
            IsOverlayOpen = false;
            IsAllStagesCleared = false;

            foreach (var p in FindObjectsByType<CyberProjectile>(FindObjectsSortMode.None))
                Destroy(p.gameObject);

            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                e.ResetEnemy();

            if (Player1 != null)
            {
                Player1.ConfigureArenaFighter(false, GetCurrentConfig().spawnPosition, Quaternion.identity);
                Player1.RespawnAt(GetCurrentConfig().spawnPosition, Quaternion.identity);
            }
        }

        public void RestartCurrentStage()
        {
            if (ActiveMode == GameMode.BossRush) StartBossRush();
            else if (ActiveMode == GameMode.VsCpuArena) StartVsCpuArena();
            else if (ActiveMode == GameMode.LocalPvp) StartLocalPvp();
            else StartParkour(CurrentStage);
        }

        public bool AreBossPylonsCleared()
        {
            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
            {
                if (e.stageIndex == CurrentStage && e.kind == EnemyKind.ShieldPylon && !e.IsDead)
                    return false;
            }
            return true;
        }

        public int RemainingBossPylons()
        {
            int c = 0;
            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
            {
                if (e.stageIndex == CurrentStage && e.kind == EnemyKind.ShieldPylon && !e.IsDead)
                    c++;
            }
            return c;
        }

        public void RegisterKill(CyberEnemy enemy)
        {
            StageKills++;
            TotalKills++;
            if (enemy.kind == EnemyKind.ShieldPylon)
            {
                int rem = RemainingBossPylons();
                if (rem > 0) CyberHUD.ShowCallout($"パイロン破壊！ 残り {rem} 基", new Color(0.2f, 0.95f, 1f));
                else CyberHUD.ShowCallout("シールド解除！ ボス本体を攻撃しろ！", new Color(1f, 0.25f, 0.72f));
            }
            else if (enemy.kind == EnemyKind.CoreBoss)
            {
                OnStageReachedGoal(CurrentStage);
            }
        }

        public void OnArenaBotKilled(CyberBotAI bot)
        {
            _arenaBotsKilledCount++;
            StageKills++;
            TotalKills++;
            CyberHUD.ShowCallout($"{bot.botName} 撃破！ ({_arenaBotsKilledCount}/{ArenaTargetKills})", new Color(0.15f, 0.96f, 1f));
            if (_arenaBotsKilledCount >= ArenaTargetKills && !IsOverlayOpen)
            {
                ArenaWinnerText = "プレイヤーの勝利！（CPU撃破達成）";
                LastRank = StageElapsed <= 45f ? "S" : "A";
                IsOverlayOpen = true;
                CyberAudio.Play("clear", 0.95f);
                if (Player1 != null) Player1.SetCursorLocked(false);
            }
        }

        public void CheckArenaVictory(CyberParkourController scorer)
        {
            if (ActiveMode != GameMode.LocalPvp && ActiveMode != GameMode.LanPvp) return;
            if (scorer.ArenaKills >= ArenaTargetKills && !IsOverlayOpen)
            {
                ArenaWinnerText = $"PLAYER {scorer.playerId + 1} の勝利！";
                LastRank = "WIN";
                IsOverlayOpen = true;
                CyberAudio.Play("clear", 0.95f);
                if (Player1 != null) Player1.SetCursorLocked(false);
            }
        }

        public void OnStageReachedGoal(int portalStage)
        {
            if (IsOverlayOpen || IsMenuOpen) return;
            if (ActiveMode != GameMode.Parkour && ActiveMode != GameMode.BossRush) return;
            if (portalStage != CurrentStage) return;

            var cfg = GetCurrentConfig();
            if (ActiveMode == GameMode.Parkour && (!_bestTimes.TryGetValue(CurrentStage, out float best) || StageElapsed < best))
                _bestTimes[CurrentStage] = StageElapsed;

            LastRank = StageElapsed <= cfg.sRankTime ? "S" : (StageElapsed <= cfg.aRankTime ? "A" : "B");
            ArenaWinnerText = ActiveMode == GameMode.BossRush ? "アルコンコア撃破！" : "";
            IsOverlayOpen = true;
            IsAllStagesCleared = (ActiveMode == GameMode.BossRush) || (CurrentStage >= stages.Count);
            Time.timeScale = 1f;
            CyberAudio.Play("clear", 0.95f);
            if (Player1 != null) Player1.SetCursorLocked(false);
        }

        public void AdvanceOrReplay()
        {
            if (ActiveMode == GameMode.BossRush || ActiveMode == GameMode.VsCpuArena || ActiveMode == GameMode.LocalPvp)
                RestartCurrentStage();
            else if (IsAllStagesCleared)
                StartStage(1);
            else
                StartStage(CurrentStage + 1);
        }
    }
}
