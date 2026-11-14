using UnityEngine;

namespace CyberRush
{
    public class CyberHUD : MonoBehaviour
    {
        private GUIStyle _titleStyle;
        private GUIStyle _bigTimerStyle;
        private GUIStyle _calloutStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _menuHeaderStyle;

        private string _lanIp = "127.0.0.1";
        private string _lanPortStr = "7777";

        private static string _calloutMsg = "";
        private static Color _calloutColor = Color.cyan;
        private static float _calloutTimer;
        private static string _hitmarkerText = "";
        private static Color _hitmarkerColor = Color.white;
        private static float _hitmarkerTimer;

        public static void ShowCallout(string msg, Color col)
        {
            _calloutMsg = msg;
            _calloutColor = col;
            _calloutTimer = 2.0f;
        }

        public static void TriggerHitmarker(string label, Color col)
        {
            _hitmarkerText = label;
            _hitmarkerColor = col;
            _hitmarkerTimer = 0.55f;
        }

        private void Update()
        {
            if (_calloutTimer > 0f) _calloutTimer -= Time.unscaledDeltaTime;
            if (_hitmarkerTimer > 0f) _hitmarkerTimer -= Time.unscaledDeltaTime;
        }

        private void InitStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _bigTimerStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            _calloutStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _menuHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        private void OnGUI()
        {
            InitStyles();
            var sm = StageManager.Instance;
            if (sm == null) return;

            if (sm.IsMenuOpen)
            {
                DrawMainMenu(sm);
                return;
            }

            if (sm.ActiveMode == GameMode.LocalPvp && sm.Player1 != null && sm.Player2 != null)
            {
                float halfW = Screen.width * 0.5f;
                DrawPlayerHud(sm, sm.Player1, new Rect(0, 0, halfW, Screen.height));
                DrawPlayerHud(sm, sm.Player2, new Rect(halfW, 0, halfW, Screen.height));

                // Split-Screen Center Neon Divider
                GUI.color = new Color(0.15f, 0.95f, 1f, 0.85f);
                GUI.DrawTexture(new Rect(halfW - 2f, 0, 4f, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else if (sm.Player1 != null)
            {
                DrawPlayerHud(sm, sm.Player1, new Rect(0, 0, Screen.width, Screen.height));
            }

            DrawGlobalHeader(sm);

            if (sm.IsOverlayOpen)
                DrawResultOverlay(sm);
        }

        private void DrawMainMenu(StageManager sm)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.84f);
            GUI.Box(new Rect(cx - 330, cy - 265, 660, 530), GUIContent.none);
            GUI.color = new Color(0.15f, 0.95f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(cx - 330, cy - 265, 660, 4), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.2f, 0.72f, 0.8f);
            GUI.DrawTexture(new Rect(cx - 330, cy + 261, 660, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            _menuHeaderStyle.normal.textColor = new Color(0.18f, 0.95f, 1f);
            GUI.Label(new Rect(cx - 310, cy - 252, 620, 48), "CYBER RUSH // KATANA x RAILGUN", _menuHeaderStyle);

            _centerStyle.normal.textColor = new Color(0.85f, 0.9f, 1f);
            GUI.Label(new Rect(cx - 300, cy - 208, 600, 24), "FPS PARKOUR  |  SWORD SKILLS  |  BLASTER & RAILGUN  |  CPU & PVP DUEL", _centerStyle);

            float bw = 380f;
            float bx = cx - bw * 0.5f;
            float y = cy - 172f;

            GUI.color = new Color(0.16f, 0.95f, 1f);
            if (GUI.Button(new Rect(bx, y, bw, 38), "1. PARKOUR STAGES (STAGE 1 - 3)"))
                sm.StartParkour(1);
            y += 45f;

            GUI.color = new Color(1f, 0.78f, 0.22f);
            if (GUI.Button(new Rect(bx, y, bw, 38), "2. BOSS RUSH (ARCHON CORE DUEL)"))
                sm.StartBossRush();
            y += 45f;

            GUI.color = new Color(0.25f, 0.98f, 0.65f);
            if (GUI.Button(new Rect(bx, y, bw, 38), "3. VS CPU ARENA (1v2 CYBER NINJAS)"))
                sm.StartVsCpuArena();
            y += 45f;

            GUI.color = new Color(1f, 0.26f, 0.72f);
            if (GUI.Button(new Rect(bx, y, bw, 38), "4. LOCAL 2P PVP (SPLIT-SCREEN ONE PC)"))
                sm.StartLocalPvp();
            y += 48f;

            // LAN / Direct-IP Row
            GUI.color = new Color(0.05f, 0.08f, 0.16f, 0.9f);
            GUI.Box(new Rect(cx - 260, y, 520, 56), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 248, y + 6, 120, 20), "5. LAN / IP PVP:");
            _lanIp = GUI.TextField(new Rect(cx - 248, y + 26, 165, 22), _lanIp);
            _lanPortStr = GUI.TextField(new Rect(cx - 75, y + 26, 65, 22), _lanPortStr);

            int port = int.TryParse(_lanPortStr, out int p) ? p : 7777;
            GUI.color = new Color(0.18f, 0.95f, 1f);
            if (GUI.Button(new Rect(cx + 0, y + 14, 115, 32), "HOST GAME"))
                sm.StartLanPvp(true, _lanIp, port);
            GUI.color = new Color(1f, 0.55f, 0.2f);
            if (GUI.Button(new Rect(cx + 125, y + 14, 120, 32), "JOIN IP"))
                sm.StartLanPvp(false, _lanIp, port);

            y += 66f;
            GUI.color = new Color(0.75f, 0.88f, 0.98f);
            GUI.Label(new Rect(cx - 300, y, 600, 118),
                "P1 CONTROLS: [WASD] Move | [Space] Jump/WallJump | [Shift/RMB] Dash/SlowMo | [E] Grapple\n" +
                "WEAPONS & SKILLS: [1] Katana  [2] Blaster (or Wheel) | [G] Quick Shot | [M] Sound\n" +
                "   Katana: [LMB] 3-Hit & Deflect | [Q] Sword Wave | [F] Cyclone Spin | Air+[C] Dive Slam\n" +
                "   Blaster: [LMB] Pulse Laser  | [Q] Railgun Beam | [F] Scatter Shot\n" +
                "P2 (SPLIT-SCREEN): [Arrows] Move | [IJKL] Look | [Enter] Jump | [/] Dash | [U] Attack | [O] Skill1 | [P] Skill2 | [RCtrl] Weapon");

            bool muted = CyberAudio.Instance != null && CyberAudio.Instance.IsMuted;
            GUI.color = muted ? new Color(1f, 0.3f, 0.35f) : new Color(0.2f, 0.95f, 1f);
            if (GUI.Button(new Rect(cx - 85, cy + 220, 170, 30), muted ? "AUDIO: MUTED [M]" : "AUDIO: ON [M]"))
            {
                CyberAudio.Instance?.ToggleMute();
            }
            GUI.color = Color.white;
        }

        private void DrawGlobalHeader(StageManager sm)
        {
            float cx = Screen.width * 0.5f;

            // Top-Left Card (Stage or Match Info)
            GUI.color = new Color(0.03f, 0.04f, 0.10f, 0.78f);
            GUI.Box(new Rect(14, 14, 310, 74), GUIContent.none);
            GUI.color = Color.white;

            _titleStyle.normal.textColor = new Color(0.18f, 0.95f, 1f);
            string titleText = sm.ActiveMode == GameMode.Parkour
                ? sm.GetCurrentConfig().stageName
                : (sm.ActiveMode == GameMode.BossRush
                    ? "BOSS RUSH // ARCHON SPIRE"
                    : (sm.ActiveMode == GameMode.VsCpuArena
                        ? "VS CPU ARENA // 1v2 NINJAS"
                        : (sm.ActiveMode == GameMode.LocalPvp ? "LOCAL 2P PVP DUEL" : "LAN PVP DUEL")));
            GUI.Label(new Rect(24, 18, 295, 26), titleText, _titleStyle);

            _bigTimerStyle.normal.textColor = new Color(1f, 0.85f, 0.25f);
            GUI.Label(new Rect(24, 44, 160, 38), $"{sm.StageElapsed:00.00}s", _bigTimerStyle);
            GUI.color = new Color(0.75f, 0.88f, 1f);
            GUI.Label(new Rect(168, 52, 145, 24), $"[ESC] MENU  |  KILLS: {sm.StageKills}");
            GUI.color = Color.white;

            // Top-Center Match Objectives
            if ((sm.ActiveMode == GameMode.Parkour && sm.CurrentStage == 3) || sm.ActiveMode == GameMode.BossRush)
            {
                int rem = sm.RemainingBossPylons();
                GUI.color = new Color(0.04f, 0.04f, 0.12f, 0.82f);
                GUI.Box(new Rect(cx - 210f, 14, 420, 38), GUIContent.none);
                GUI.color = rem > 0 ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.25f, 0.72f);
                string bossHint = rem > 0 ? $"SHATTER BOSS SHIELD PYLONS ({3 - rem}/3)" : "BOSS SHIELD DOWN! DESTROY THE ARCHON CORE!";
                GUI.Label(new Rect(cx - 210f, 20, 420, 28), bossHint, _centerStyle);
                GUI.color = Color.white;
            }
            else if (sm.ActiveMode == GameMode.VsCpuArena && sm.Player1 != null)
            {
                GUI.color = new Color(0.04f, 0.04f, 0.12f, 0.82f);
                GUI.Box(new Rect(cx - 190f, 14, 380, 38), GUIContent.none);
                _centerStyle.normal.textColor = new Color(0.25f, 0.98f, 0.65f);
                GUI.Label(new Rect(cx - 190f, 20, 380, 28), $"SCORE: PLAYER {sm.Player1.ArenaKills} / {sm.ArenaTargetKills}", _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.LocalPvp && sm.Player1 != null && sm.Player2 != null)
            {
                GUI.color = new Color(0.04f, 0.04f, 0.12f, 0.86f);
                GUI.Box(new Rect(cx - 195f, 14, 390, 38), GUIContent.none);
                _centerStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(cx - 195f, 20, 390, 28),
                    $"P1: {sm.Player1.ArenaKills} KILLS   vs   P2: {sm.Player2.ArenaKills} KILLS   (FIRST TO {sm.ArenaTargetKills})",
                    _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.LanPvp && LanPvpManager.Instance != null)
            {
                GUI.color = new Color(0.04f, 0.04f, 0.12f, 0.86f);
                GUI.Box(new Rect(cx - 230f, 14, 460, 38), GUIContent.none);
                _centerStyle.normal.textColor = LanPvpManager.Instance.IsConnected ? new Color(0.25f, 0.98f, 0.65f) : new Color(1f, 0.8f, 0.25f);
                int rHp = LanPvpManager.Instance.RemoteAvatar != null ? LanPvpManager.Instance.RemoteAvatar.remoteHp : 0;
                GUI.Label(new Rect(cx - 230f, 20, 460, 28), $"{LanPvpManager.Instance.StatusText}  |  OPPONENT HP: {rHp}", _centerStyle);
            }
            GUI.color = Color.white;
        }

        private void DrawPlayerHud(StageManager sm, CyberParkourController p, Rect vr)
        {
            float cx = vr.x + vr.width * 0.5f;
            float cy = vr.y + vr.height * 0.5f;

            // Speedlines / Damage / Slow-Mo Edge Flash
            if (p.DamageFlashActive || p.HorizontalSpeed > 16.5f || p.IsSlowMo)
            {
                Color edgeCol = p.DamageFlashActive
                    ? new Color(1f, 0.12f, 0.18f, 0.35f)
                    : (p.IsSlowMo ? new Color(1f, 0.16f, 0.72f, 0.22f) : new Color(0.12f, 0.95f, 1.0f, 0.14f));
                GUI.color = edgeCol;
                GUI.DrawTexture(new Rect(vr.x, vr.y, vr.width, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.x, vr.yMax - 5, vr.width, 5), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.x, vr.y, 5, vr.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.xMax - 5, vr.y, 5, vr.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Crosshair
            Color retCol = p.HasTargetInSlashRange
                ? new Color(1f, 0.2f, 0.68f)
                : (p.HoveredGrapple != null ? new Color(1f, 0.82f, 0.22f) : new Color(0.2f, 0.96f, 1f));
            GUI.color = retCol;
            GUI.DrawTexture(new Rect(cx - 8, cy - 1, 5, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 3, cy - 1, 5, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - 8, 2, 5), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy + 3, 2, 5), Texture2D.whiteTexture);

            if (p.HoveredGrapple != null)
            {
                _centerStyle.normal.textColor = new Color(1f, 0.85f, 0.22f);
                GUI.Label(new Rect(cx - 90, cy + 16, 180, 24), p.playerId == 0 ? "[E] GRAPPLE" : "[]] GRAPPLE", _centerStyle);
            }
            if (p.playerId == 0 && _hitmarkerTimer > 0f)
            {
                _centerStyle.normal.textColor = _hitmarkerColor;
                GUI.Label(new Rect(cx - 120, cy - 40, 240, 24), _hitmarkerText, _centerStyle);
            }
            if (p.playerId == 0 && _calloutTimer > 0f)
            {
                _calloutStyle.normal.textColor = _calloutColor;
                GUI.Label(new Rect(cx - 220, cy - 102, 440, 32), _calloutMsg, _calloutStyle);
            }
            if (p.IsArenaDead)
            {
                _calloutStyle.normal.textColor = new Color(1f, 0.25f, 0.3f);
                GUI.Label(new Rect(cx - 140, cy - 20, 280, 40), "RESPAWNING...", _calloutStyle);
            }
            GUI.color = Color.white;

            // Bottom-Left Stats Card inside Viewport
            float boxW = Mathf.Min(315f, vr.width - 24f);
            float boxH = p.isArenaFighter ? 104f : 86f;
            float bx = vr.x + 12f;
            float by = vr.yMax - boxH - 12f;
            GUI.color = new Color(0.03f, 0.04f, 0.10f, 0.78f);
            GUI.Box(new Rect(bx, by, boxW, boxH), GUIContent.none);

            float rowY = by + 8f;
            if (p.isArenaFighter)
            {
                GUI.color = new Color(0.95f, 0.98f, 1f);
                GUI.Label(new Rect(bx + 10, rowY - 2, 80, 22), $"HP {p.ArenaHp}");
                GUI.color = new Color(0.18f, 0.18f, 0.24f);
                GUI.DrawTexture(new Rect(bx + 90, rowY + 3, boxW - 104, 12), Texture2D.whiteTexture);
                GUI.color = p.ArenaHp > 35 ? new Color(0.2f, 0.98f, 0.65f) : new Color(1f, 0.2f, 0.3f);
                GUI.DrawTexture(new Rect(bx + 90, rowY + 3, (boxW - 104) * Mathf.Clamp01(p.ArenaHp / (float)p.arenaMaxHp), 12), Texture2D.whiteTexture);
                rowY += 22f;
            }

            GUI.color = Color.white;
            int kmh = Mathf.RoundToInt(p.HorizontalSpeed * 3.6f);
            GUI.Label(new Rect(bx + 10, rowY, 105, 22), $"SPD {kmh}");
            float pipW = (boxW - 128f) * 0.48f;
            for (int d = 0; d < p.MaxDashes; d++)
            {
                float px = bx + 114f + d * (pipW + 6f);
                GUI.color = new Color(0.15f, 0.2f, 0.28f);
                GUI.DrawTexture(new Rect(px, rowY + 4, pipW, 12), Texture2D.whiteTexture);
                float fill = d < p.DashCharges ? 1f : (d == p.DashCharges ? p.DashRechargeRatio : 0f);
                GUI.color = new Color(0.15f, 0.95f, 1f);
                GUI.DrawTexture(new Rect(px, rowY + 4, pipW * fill, 12), Texture2D.whiteTexture);
            }

            rowY += 24f;
            string wepName = p.ActiveWeapon == CyberWeaponMode.Katana ? "[1] KATANA" : "[2] BLASTER";
            GUI.color = p.ActiveWeapon == CyberWeaponMode.Katana ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.78f, 0.22f);
            GUI.Label(new Rect(bx + 10, rowY, 130, 22), wepName);

            string qName = p.ActiveWeapon == CyberWeaponMode.Katana ? "WAVE" : "RAIL";
            string fName = p.ActiveWeapon == CyberWeaponMode.Katana ? "SPIN" : "SCAT";
            GUI.color = p.SkillQReadyRatio >= 0.99f ? new Color(0.2f, 0.98f, 1f) : new Color(0.5f, 0.55f, 0.65f);
            GUI.Label(new Rect(bx + 138, rowY, 82, 22), $"{(p.playerId == 0 ? "[Q]" : "[O]")}{qName}");
            GUI.color = p.SkillFReadyRatio >= 0.99f ? new Color(1f, 0.28f, 0.75f) : new Color(0.5f, 0.55f, 0.65f);
            GUI.Label(new Rect(bx + 220, rowY, 82, 22), $"{(p.playerId == 0 ? "[F]" : "[P]")}{fName}");

            rowY += 22f;
            GUI.color = new Color(0.75f, 0.86f, 0.95f);
            GUI.Label(new Rect(bx + 10, rowY, boxW - 18, 22),
                p.playerId == 0 ? "[G] QuickShot  Air+[C] DiveSlam  [M] Mute" : "[Arrow/IJKL] [U]Atk [;/Ctrl]Slam/Wep");
            GUI.color = Color.white;
        }

        private void DrawResultOverlay(StageManager sm)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
            GUI.Box(new Rect(cx - 245, cy - 135, 490, 270), GUIContent.none);

            _bigTimerStyle.normal.textColor = new Color(0.15f, 0.95f, 1f);
            string header = !string.IsNullOrEmpty(sm.ArenaWinnerText)
                ? sm.ArenaWinnerText
                : (sm.IsAllStagesCleared ? "CYBER RUSH // ALL STAGES CLEAR!" : "STAGE CLEAR!");
            GUI.Label(new Rect(cx - 225, cy - 112, 450, 44), header, _calloutStyle);

            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.22f);
            GUI.Label(new Rect(cx - 190, cy - 58, 380, 34), $"TIME: {sm.StageElapsed:00.00}s   |   RANK: {sm.LastRank}", _calloutStyle);

            _centerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(cx - 190, cy - 10, 380, 26), $"SLICES / KILLS: {sm.StageKills} (SESSION TOTAL {sm.TotalKills})", _centerStyle);

            GUI.color = new Color(0.15f, 0.95f, 1f);
            string nextLabel = sm.ActiveMode == GameMode.Parkour && !sm.IsAllStagesCleared ? "NEXT STAGE [SPACE]" : "PLAY AGAIN [SPACE]";
            if (GUI.Button(new Rect(cx - 210, cy + 44, 155, 44), nextLabel))
                sm.AdvanceOrReplay();

            GUI.color = new Color(1f, 0.28f, 0.72f);
            if (GUI.Button(new Rect(cx - 45, cy + 44, 115, 44), "RETRY [R]"))
                sm.RestartCurrentStage();

            GUI.color = new Color(0.85f, 0.92f, 1f);
            if (GUI.Button(new Rect(cx + 80, cy + 44, 130, 44), "MAIN MENU"))
                sm.OpenMainMenu();

            GUI.color = Color.white;
        }
    }
}
