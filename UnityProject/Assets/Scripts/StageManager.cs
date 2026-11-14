using System.Collections.Generic;
using UnityEngine;

namespace CyberRush
{
    public enum GameMode
    {
        MainMenu,
        Parkour,
        BossRush,
        RhythmSurvival,
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

        [Header("Survival Enemy Templates")]
        public GameObject survivalDroneTemplate;
        public GameObject survivalTrooperTemplate;
        public GameObject survivalBossTemplate;

        public GameMode ActiveMode { get; private set; } = GameMode.MainMenu;
        public bool IsMenuOpen => ActiveMode == GameMode.MainMenu;

        public int CurrentStage { get; private set; } = 1;
        public float StageElapsed { get; private set; }
        public int StageKills { get; private set; }
        public int TotalKills { get; private set; }
        public int SurvivalWave { get; private set; } = 1;
        public int BestSurvivalScore { get; private set; }
        public int BestSurvivalWave { get; private set; }
        public bool IsOverlayOpen { get; private set; }
        public bool IsAllStagesCleared { get; private set; }
        public string LastRank { get; private set; } = "S";
        public string ArenaWinnerText { get; private set; } = "";
        public int ArenaTargetKills { get; private set; } = 5;

        public CyberParkourController Player1 { get; private set; }
        public CyberParkourController Player2 { get; private set; }

        private readonly Dictionary<int, float> _bestTimes = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _bestScores = new Dictionary<int, int>();
        private CyberBotAI[] _bots;
        private int _arenaBotsKilledCount;
        private float _nextWaveTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CyberAudio.EnsureInstance();
            CyberRhythm.EnsureInstance();
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
                stages.Add(new StageConfig { stageName = "STAGE 1 // ネオンスカイライン", spawnPosition = new Vector3(0f, 2.0f, 0f), sRankTime = 18f, aRankTime = 28f });
                stages.Add(new StageConfig { stageName = "STAGE 2 // サイバーキャニオン", spawnPosition = new Vector3(120f, 2.0f, 0f), sRankTime = 22f, aRankTime = 34f });
                stages.Add(new StageConfig { stageName = "STAGE 3 // アルコン尖塔", spawnPosition = new Vector3(240f, 2.0f, 0f), sRankTime = 28f, aRankTime = 42f });
                stages.Add(new StageConfig { stageName = "STAGE 4 // キルチェイン回廊", spawnPosition = new Vector3(480f, 2.0f, 0f), sRankTime = 26f, aRankTime = 40f });
                stages.Add(new StageConfig { stageName = "STAGE 5 // スカイフォートレス", spawnPosition = new Vector3(600f, 2.0f, 0f), sRankTime = 36f, aRankTime = 55f });
            }

            OpenMainMenu();
        }

        private void Update()
        {
            if (!IsMenuOpen && !IsOverlayOpen)
            {
                StageElapsed += Time.unscaledDeltaTime;

                if (ActiveMode == GameMode.RhythmSurvival)
                {
                    if (_nextWaveTimer > 0f)
                    {
                        _nextWaveTimer -= Time.unscaledDeltaTime;
                        if (_nextWaveTimer <= 0f)
                        {
                            SurvivalWave++;
                            CyberAudio.Play("wave_next", 0.9f);
                            if (Player1 != null) Player1.HealArenaHp(20);
                            SpawnSurvivalWave(SurvivalWave);
                            CyberHUD.ShowCallout($"WAVE {SurvivalWave} 突入!! // 連撃を途切れさせるな！", new Color(0.22f, 0.98f, 1f));
                        }
                    }
                    else if (CountAliveSurvivalEnemies() == 0)
                    {
                        _nextWaveTimer = 1.0f;
                    }
                }
            }
        }

        public StageConfig GetCurrentConfig()
        {
            int idx = Mathf.Clamp(CurrentStage - 1, 0, stages.Count - 1);
            return stages[idx];
        }

        public float GetBestTime(int stage) => _bestTimes.TryGetValue(stage, out float t) ? t : 0f;
        public int GetBestScore(int stage) => _bestScores.TryGetValue(stage, out int s) ? s : 0;

        private void ClearDynamicSurvivalEnemies()
        {
            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
            {
                if (!e.isDynamicSurvivalEnemy) continue;
                GameObject go = e.gameObject;
                if (go == survivalDroneTemplate || go == survivalTrooperTemplate || go == survivalBossTemplate) continue;
                Destroy(go);
            }
        }

        private int CountAliveSurvivalEnemies()
        {
            int c = 0;
            foreach (var e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
            {
                if (e.isDynamicSurvivalEnemy && !e.IsDead) c++;
            }
            return c;
        }

        private void CleanupProjectilesAndToggleArenaActors(bool enableBots, bool enableP2, bool enableRemote)
        {
            ClearDynamicSurvivalEnemies();

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
            CyberVFX.ResetTimeScale();
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
            CyberVFX.ResetTimeScale();
            CyberRhythm.EnsureInstance()?.ResetSession();
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
            CyberHUD.ShowCallout("BOSS RUSH // 128 BPM アルコンコア戦！", new Color(1f, 0.22f, 0.72f));
        }

        public void StartRhythmSurvival()
        {
            ActiveMode = GameMode.RhythmSurvival;
            CurrentStage = 4;
            StageElapsed = 0f;
            StageKills = 0;
            SurvivalWave = 1;
            _nextWaveTimer = 0f;
            ArenaWinnerText = "";
            IsOverlayOpen = false;
            IsAllStagesCleared = false;
            CyberVFX.ResetTimeScale();
            CyberRhythm.EnsureInstance()?.ResetSession();
            CleanupProjectilesAndToggleArenaActors(enableBots: false, enableP2: false, enableRemote: false);

            if (Player1 != null)
            {
                Vector3 arenaP1 = new Vector3(360f, 2.2f, -18f);
                Player1.ConfigureArenaFighter(true, arenaP1, Quaternion.identity);
                Player1.RespawnAt(arenaP1, Quaternion.identity);
                Player1.SetCursorLocked(true);
            }

            SpawnSurvivalWave(1);
            CyberHUD.ShowCallout("BEAT SURVIVAL WAVE 1 // 128 BPM リズム連撃！", new Color(0.18f, 0.98f, 1f));
        }

        private void SpawnSurvivalWave(int wave)
        {
            int drones = Mathf.Min(4 + wave, 11);
            int troopers = Mathf.Min(1 + wave / 2, 5);
            bool spawnBoss = (wave % 3 == 0);

            Vector3 pPos = Player1 != null ? Player1.transform.position : new Vector3(360f, 2.2f, -18f);
            float scaledFire = Mathf.Max(1.05f, 2.5f - wave * 0.1f);

            for (int i = 0; i < drones; i++)
                SpawnSurvivalUnit(survivalDroneTemplate, true, pPos, scaledFire, wave);

            for (int i = 0; i < troopers; i++)
                SpawnSurvivalUnit(survivalTrooperTemplate, false, pPos, scaledFire * 0.92f, wave);

            if (spawnBoss && survivalBossTemplate != null)
            {
                Vector3 bossPos = new Vector3(360f, 3.0f, 0f);
                if (Vector3.Distance(bossPos, pPos) < 10f) bossPos = new Vector3(360f, 3.0f, 16f);
                var bossGo = Instantiate(survivalBossTemplate, bossPos, Quaternion.LookRotation((pPos - bossPos).normalized));
                var bossAi = bossGo.GetComponent<CyberEnemy>();
                if (bossAi != null)
                {
                    bossAi.isDynamicSurvivalEnemy = true;
                    bossAi.detectRange = 65f;
                    bossAi.fireInterval = Mathf.Max(1.1f, scaledFire);
                    bossAi.maxHp = 2 + wave / 3;
                }
                bossGo.SetActive(true);
            }
        }

        private void SpawnSurvivalUnit(GameObject template, bool aerial, Vector3 playerPos, float fireInterval, int wave)
        {
            if (template == null) return;
            Vector3 spawnPos = new Vector3(360f, aerial ? 4.2f : 1.1f, 8f);
            for (int tries = 0; tries < 8; tries++)
            {
                float x = 360f + Random.Range(-21f, 21f);
                float z = Random.Range(-21f, 21f);
                float y = aerial ? Random.Range(3.2f, 6.2f) : (Mathf.Abs(x - 360f) < 6f && Mathf.Abs(z) < 6f ? 2.8f : 1.05f);
                spawnPos = new Vector3(x, y, z);
                if (Vector3.Distance(spawnPos, playerPos) > 9f) break;
            }

            Vector3 look = (playerPos - spawnPos);
            look.y = 0f;
            Quaternion rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look.normalized) : Quaternion.identity;

            var go = Instantiate(template, spawnPos, rot);
            var ai = go.GetComponent<CyberEnemy>();
            if (ai != null)
            {
                ai.isDynamicSurvivalEnemy = true;
                ai.detectRange = 65f;
                ai.fireInterval = fireInterval;
                ai.maxHp = (!aerial && wave >= 5) ? 2 : 1;
            }
            go.SetActive(true);
        }

        public void OnSurvivalGameOver()
        {
            if (IsOverlayOpen || ActiveMode != GameMode.RhythmSurvival) return;
            int sc = CyberRhythm.Instance != null ? CyberRhythm.Instance.Score : 0;
            if (sc > BestSurvivalScore) BestSurvivalScore = sc;
            if (SurvivalWave > BestSurvivalWave) BestSurvivalWave = SurvivalWave;

            LastRank = sc >= 30000 ? "SSS" : (sc >= 16000 ? "SS" : (sc >= 8000 ? "S" : (sc >= 3500 ? "A" : "B")));
            ArenaWinnerText = $"RHYTHM SURVIVAL // WAVE {SurvivalWave} 記録！";
            IsOverlayOpen = true;
            CyberVFX.ResetTimeScale();
            CyberAudio.Play("clear", 0.9f);
            if (Player1 != null) Player1.SetCursorLocked(false);
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
            CyberVFX.ResetTimeScale();
            CyberRhythm.EnsureInstance()?.ResetSession();

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
            else if (ActiveMode == GameMode.RhythmSurvival) StartRhythmSurvival();
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
                if (ActiveMode == GameMode.RhythmSurvival)
                    CyberHUD.ShowCallout("WAVE BOSS 撃破!! // ボーナス +HP", new Color(1f, 0.85f, 0.22f));
                else
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

            int sc = CyberRhythm.Instance != null ? CyberRhythm.Instance.Score : 0;
            if (ActiveMode == GameMode.Parkour && (!_bestScores.TryGetValue(CurrentStage, out int bestSc) || sc > bestSc))
                _bestScores[CurrentStage] = sc;

            LastRank = CyberRhythm.Instance != null
                ? CyberRhythm.Instance.ComputeScoreRank(StageElapsed, cfg.sRankTime)
                : (StageElapsed <= cfg.sRankTime ? "S" : (StageElapsed <= cfg.aRankTime ? "A" : "B"));
            ArenaWinnerText = ActiveMode == GameMode.BossRush ? "アルコンコア撃破！" : "";
            IsOverlayOpen = true;
            IsAllStagesCleared = (ActiveMode == GameMode.BossRush) || (CurrentStage >= stages.Count);
            CyberVFX.ResetTimeScale();
            CyberAudio.Play("clear", 0.95f);
            if (Player1 != null) Player1.SetCursorLocked(false);
        }

        public void AdvanceOrReplay()
        {
            if (ActiveMode == GameMode.BossRush || ActiveMode == GameMode.RhythmSurvival || ActiveMode == GameMode.VsCpuArena || ActiveMode == GameMode.LocalPvp)
                RestartCurrentStage();
            else if (IsAllStagesCleared)
                StartStage(1);
            else
                StartStage(CurrentStage + 1);
        }
    }
}
