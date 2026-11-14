using UnityEngine;

namespace CyberRush
{
    public class CyberHUD : MonoBehaviour
    {
        private Font _jpFont;
        private Texture2D _panelBgTex;
        private Texture2D _cardBgTex;
        private Texture2D _btnNormalTex;
        private Texture2D _btnHoverTex;

        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bigTimerStyle;
        private GUIStyle _calloutStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _smallLabelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _cardStyle;

        private bool _showControlsHelp = false;

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
            _hitmarkerTimer = 0.65f;
        }

        private void Update()
        {
            if (_calloutTimer > 0f) _calloutTimer -= Time.unscaledDeltaTime;
            if (_hitmarkerTimer > 0f) _hitmarkerTimer -= Time.unscaledDeltaTime;
        }

        private static Texture2D MakeSolidTex(Color col)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _jpFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic", "Arial" }, 16);

            _panelBgTex = MakeSolidTex(new Color(0.03f, 0.05f, 0.11f, 0.94f));
            _cardBgTex = MakeSolidTex(new Color(0.07f, 0.10f, 0.19f, 0.96f));
            _btnNormalTex = MakeSolidTex(new Color(0.10f, 0.16f, 0.30f, 0.98f));
            _btnHoverTex = MakeSolidTex(new Color(0.16f, 0.28f, 0.48f, 1.0f));

            _panelStyle = new GUIStyle();
            _panelStyle.normal.background = _panelBgTex;

            _cardStyle = new GUIStyle();
            _cardStyle.normal.background = _cardBgTex;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _bigTimerStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            _calloutStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _centerStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            _labelStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);

            _smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font = _jpFont,
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            _smallLabelStyle.normal.textColor = new Color(0.78f, 0.88f, 0.98f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = _jpFont,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle.normal.background = _btnNormalTex;
            _buttonStyle.hover.background = _btnHoverTex;
            _buttonStyle.active.background = _btnHoverTex;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = new Color(0.25f, 0.98f, 1f);
            _buttonStyle.active.textColor = Color.white;
        }

        private static void DrawOutlineBox(Rect r, Color borderCol, float thickness = 2f)
        {
            Color prev = GUI.color;
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture);
            GUI.color = prev;
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

                GUI.color = new Color(0.15f, 0.95f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(halfW - 2f, 0, 4f, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else if (sm.Player1 != null)
            {
                DrawWorldTargetMarkers(sm, sm.Player1.playerCamera);
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
            float panelW = 640f;
            float panelH = _showControlsHelp ? 640f : 552f;
            Rect panelRect = new Rect(cx - panelW * 0.5f, cy - panelH * 0.5f, panelW, panelH);

            GUI.Box(panelRect, GUIContent.none, _panelStyle);
            DrawOutlineBox(panelRect, new Color(0.16f, 0.92f, 1f, 0.85f), 2f);

            float y = panelRect.y + 18f;
            _headerStyle.normal.textColor = new Color(0.22f, 0.96f, 1f);
            GUI.Label(new Rect(panelRect.x + 20, y, panelW - 40, 44), "CYBER BEAT RUSH // リズム剣戟×銃撃", _headerStyle);
            y += 40f;

            _subHeaderStyle.normal.textColor = new Color(0.84f, 0.92f, 1f);
            GUI.Label(new Rect(panelRect.x + 20, y, panelW - 40, 22), "128 BPM のビートに合わせて斬り・撃ち・スローモ連撃を繋げ！ (最大 x16 FEVER)", _subHeaderStyle);
            y += 32f;

            float bw = 520f;
            float bx = cx - bw * 0.5f;

            string survivalLabel = sm.BestSurvivalWave > 0
                ? $"RHYTHM SURVIVAL // 無限ビート連撃サバイバル  (最高 WAVE {sm.BestSurvivalWave} / {sm.BestSurvivalScore:N0}pts)"
                : "RHYTHM SURVIVAL // 無限ビート連撃サバイバル [新モード・コンボ爽快]";
            if (DrawAccentButton(new Rect(bx, y, bw, 46), survivalLabel, new Color(1f, 0.82f, 0.22f)))
                sm.StartRhythmSurvival();
            y += 54f;

            if (DrawAccentButton(new Rect(bx, y, bw, 42), "ステージ攻略スタート (STAGE 1 から通しプレイ)", new Color(0.16f, 0.95f, 1f)))
                sm.StartParkour(1);
            y += 50f;

            // Direct Stage Select Card (Stages 1 - 5)
            Rect listBox = new Rect(bx, y, bw, 198f);
            GUI.Box(listBox, GUIContent.none, _cardStyle);
            DrawOutlineBox(listBox, new Color(0.32f, 0.55f, 0.88f, 0.65f), 1f);
            GUI.Label(new Rect(bx + 14, y + 6, 360, 22), "ステージ選択 (クリックで直接プレイ・ビートスコア対応)", _labelStyle);

            float halfW = (bw - 36f) * 0.5f;
            if (DrawAccentButton(new Rect(bx + 12, y + 32, halfW, 38), "STAGE 1: スカイライン", new Color(0.18f, 0.92f, 1f)))
                sm.StartParkour(1);
            if (DrawAccentButton(new Rect(bx + 24 + halfW, y + 32, halfW, 38), "STAGE 2: キャニオン", new Color(0.18f, 0.92f, 1f)))
                sm.StartParkour(2);

            if (DrawAccentButton(new Rect(bx + 12, y + 78, halfW, 38), "STAGE 3: アルコン尖塔 (BOSS)", new Color(1f, 0.35f, 0.78f)))
                sm.StartParkour(3);
            if (DrawAccentButton(new Rect(bx + 24 + halfW, y + 78, halfW, 38), "STAGE 4: キルチェイン回廊 [新]", new Color(0.25f, 0.98f, 0.65f)))
                sm.StartParkour(4);

            if (DrawAccentButton(new Rect(bx + 12, y + 124, bw - 24f, 44), "STAGE 5: スカイフォートレス決戦 [新・大量撃破＆ボス]", new Color(1f, 0.82f, 0.22f)))
                sm.StartParkour(5);
            y += 206f;

            if (DrawAccentButton(new Rect(bx, y, bw, 40), "BOSS RUSH // アルコンコア直接対決", new Color(1f, 0.24f, 0.68f)))
                sm.StartBossRush();
            y += 48f;

            // Bottom Footer Buttons
            bool muted = CyberAudio.Instance != null && CyberAudio.Instance.IsMuted;
            string muteLabel = muted ? "音声: ミュート中 [M]" : "音声: ON [M]";
            if (DrawAccentButton(new Rect(bx, y, halfW, 36), muteLabel, muted ? new Color(1f, 0.3f, 0.35f) : new Color(0.2f, 0.95f, 1f)))
                CyberAudio.Instance?.ToggleMute();

            string helpLabel = _showControlsHelp ? "操作ガイドを閉じる ▲" : "操作ガイドを見る ▼";
            if (DrawAccentButton(new Rect(bx + 12 + halfW, y, halfW, 36), helpLabel, new Color(0.75f, 0.85f, 1f)))
                _showControlsHelp = !_showControlsHelp;
            y += 44f;

            if (_showControlsHelp)
            {
                Rect helpRect = new Rect(bx, y, bw, 80);
                GUI.Box(helpRect, GUIContent.none, _cardStyle);
                GUI.Label(new Rect(bx + 12, y + 6, bw - 24, 70),
                    "【リズム】照準左右のビートバーが中央に重なる瞬間に攻撃/ダッシュで「PERFECT BEAT!!」\n" +
                    "　　　　  コンボで倍率 x1→x2→x4→x8→x16 HYPER FEVER！ 範囲拡大＆威力2倍＆HP回復！\n" +
                    "【操作】WASD:移動 | Space:ジャンプ | Shift/右クリ:ダッシュ/スローモ | E:ワイヤー | C:スライディング\n" +
                    "【武器】[1]刀 [2]銃 (ホイール切替) | 左クリ:通常 | [Q]斬撃波/レールガン | [F]旋風斬/拡散銃 | [G]クイック撃",
                    _smallLabelStyle);
            }
        }

        private bool DrawAccentButton(Rect r, string text, Color accent)
        {
            bool clicked = GUI.Button(r, text, _buttonStyle);
            DrawOutlineBox(r, accent, 1.5f);
            return clicked;
        }

        private void DrawWorldTargetMarkers(StageManager sm, Camera cam)
        {
            if (cam == null) return;

            if (sm.ActiveMode == GameMode.VsCpuArena)
            {
                foreach (var bot in FindObjectsByType<CyberBotAI>(FindObjectsSortMode.None))
                {
                    if (bot.IsDead) continue;
                    Vector3 worldPos = bot.transform.position + Vector3.up * 2.25f;
                    Vector3 sp = cam.WorldToScreenPoint(worldPos);
                    if (sp.z <= 0.2f) continue;

                    float x = sp.x - 48f;
                    float y = Screen.height - sp.y - 24f;
                    GUI.Box(new Rect(x, y, 96f, 24f), GUIContent.none, _panelStyle);
                    _smallLabelStyle.alignment = TextAnchor.MiddleCenter;
                    _smallLabelStyle.normal.textColor = bot.accentColor;
                    GUI.Label(new Rect(x, y - 1f, 96f, 16f), $"{bot.botName.Replace("CYBER NINJA ", "AI ")}", _smallLabelStyle);
                    _smallLabelStyle.alignment = TextAnchor.UpperLeft;

                    GUI.color = new Color(0.2f, 0.22f, 0.28f);
                    GUI.DrawTexture(new Rect(x + 6, y + 16, 84f, 5f), Texture2D.whiteTexture);
                    GUI.color = bot.accentColor;
                    GUI.DrawTexture(new Rect(x + 6, y + 16, 84f * Mathf.Clamp01(bot.CurrentHp / (float)bot.maxHp), 5f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            else if (sm.ActiveMode == GameMode.BossRush || (sm.ActiveMode == GameMode.Parkour && (sm.CurrentStage == 3 || sm.CurrentStage == 5)))
            {
                foreach (var en in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                {
                    if (en.IsDead || en.kind != EnemyKind.CoreBoss) continue;
                    Vector3 sp = cam.WorldToScreenPoint(en.transform.position + Vector3.up * 4.5f);
                    if (sp.z <= 0.2f) continue;

                    float x = sp.x - 65f;
                    float y = Screen.height - sp.y - 26f;
                    GUI.Box(new Rect(x, y, 130f, 26f), GUIContent.none, _panelStyle);
                    _smallLabelStyle.alignment = TextAnchor.MiddleCenter;
                    _smallLabelStyle.normal.textColor = new Color(1f, 0.35f, 0.78f);
                    GUI.Label(new Rect(x, y, 130f, 16f), $"アルコンコア HP:{en.CurrentHp}/{en.maxHp}", _smallLabelStyle);
                    _smallLabelStyle.alignment = TextAnchor.UpperLeft;

                    GUI.color = new Color(0.25f, 0.2f, 0.28f);
                    GUI.DrawTexture(new Rect(x + 8, y + 17, 114f, 6f), Texture2D.whiteTexture);
                    GUI.color = new Color(1f, 0.22f, 0.72f);
                    GUI.DrawTexture(new Rect(x + 8, y + 17, 114f * Mathf.Clamp01(en.CurrentHp / (float)en.maxHp), 6f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawGlobalHeader(StageManager sm)
        {
            float cx = Screen.width * 0.5f;

            Rect tlRect = new Rect(14, 14, 290, 74);
            GUI.Box(tlRect, GUIContent.none, _panelStyle);
            Color tlBorder = sm.ActiveMode == GameMode.RhythmSurvival ? new Color(1f, 0.82f, 0.22f, 0.85f) : new Color(0.18f, 0.92f, 1f, 0.7f);
            DrawOutlineBox(tlRect, tlBorder, 1.5f);

            _titleStyle.normal.textColor = sm.ActiveMode == GameMode.RhythmSurvival ? new Color(1f, 0.85f, 0.25f) : new Color(0.22f, 0.95f, 1f);
            string titleText = sm.ActiveMode == GameMode.Parkour
                ? $"ステージ {sm.CurrentStage} / {sm.stages.Count}"
                : (sm.ActiveMode == GameMode.RhythmSurvival ? $"ビートサバイバル // WAVE {sm.SurvivalWave}" : "ボスラッシュ // アルコンコア戦");
            GUI.Label(new Rect(24, 18, 272, 24), titleText, _titleStyle);

            _bigTimerStyle.normal.textColor = new Color(1f, 0.86f, 0.25f);
            GUI.Label(new Rect(24, 42, 130, 36), $"{sm.StageElapsed:00.00}秒", _bigTimerStyle);
            GUI.Label(new Rect(150, 50, 145, 24), $"撃破:{sm.StageKills}  [ESC]メニュー", _smallLabelStyle);

            // Top-Right: Rhythm Score, Combo & Fever Multiplier Panel
            var rhythm = CyberRhythm.Instance;
            if (rhythm != null)
            {
                Rect trRect = new Rect(Screen.width - 278f, 14f, 264f, 84f);
                GUI.Box(trRect, GUIContent.none, _panelStyle);
                Color beatBorder = rhythm.IsFever
                    ? Color.Lerp(new Color(1f, 0.22f, 0.75f), new Color(1f, 0.88f, 0.22f), rhythm.BeatPulse)
                    : Color.Lerp(new Color(0.16f, 0.65f, 0.85f), new Color(0.22f, 0.98f, 1f), rhythm.BeatPulse);
                DrawOutlineBox(trRect, beatBorder, rhythm.BeatPulse > 0.45f ? 2.5f : 1.5f);

                _titleStyle.normal.textColor = rhythm.IsFever ? new Color(1f, 0.85f, 0.22f) : Color.white;
                GUI.Label(new Rect(trRect.x + 12, trRect.y + 6, 175, 24), $"SCORE: {rhythm.Score:N0}", _titleStyle);

                _titleStyle.normal.textColor = rhythm.Multiplier >= 8 ? new Color(1f, 0.28f, 0.75f) : new Color(0.22f, 0.98f, 1f);
                _titleStyle.alignment = TextAnchor.UpperRight;
                GUI.Label(new Rect(trRect.x + 180, trRect.y + 6, 72, 24), $"x{rhythm.Multiplier}", _titleStyle);
                _titleStyle.alignment = TextAnchor.UpperLeft;

                _labelStyle.normal.textColor = rhythm.Combo > 0 ? new Color(1f, 0.86f, 0.25f) : new Color(0.70f, 0.78f, 0.90f);
                string comboStr = rhythm.Multiplier >= 16
                    ? $"COMBO {rhythm.Combo} // HYPER FEVER!!"
                    : (rhythm.IsFever ? $"COMBO {rhythm.Combo} // FEVER!!" : $"COMBO: {rhythm.Combo}");
                GUI.Label(new Rect(trRect.x + 12, trRect.y + 33, 240, 22), comboStr, _labelStyle);
                _labelStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);

                GUI.Label(new Rect(trRect.x + 12, trRect.y + 57, 240, 20),
                    $"最大:{rhythm.MaxCombo}コンボ  |  128 BPM 精度:{rhythm.AccuracyPercent:0}%",
                    _smallLabelStyle);
            }

            // Top-Center Objectives
            if ((sm.ActiveMode == GameMode.Parkour && (sm.CurrentStage == 3 || sm.CurrentStage == 5)) || sm.ActiveMode == GameMode.BossRush)
            {
                int rem = sm.RemainingBossPylons();
                Rect objRect = new Rect(cx - 210f, 14, 420, 40);
                GUI.Box(objRect, GUIContent.none, _panelStyle);
                Color border = rem > 0 ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.25f, 0.72f);
                DrawOutlineBox(objRect, border, 1.5f);
                _centerStyle.normal.textColor = border;
                string bossHint = rem > 0
                    ? $"ボスのシールド装置（パイロン）を破壊せよ！ (残り {rem} 基)"
                    : "シールド解除！ ボス本体（アルコンコア）を撃破せよ！";
                GUI.Label(new Rect(cx - 205f, 20, 410, 28), bossHint, _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.RhythmSurvival)
            {
                Rect objRect = new Rect(cx - 200f, 14, 400, 40);
                GUI.Box(objRect, GUIContent.none, _panelStyle);
                DrawOutlineBox(objRect, new Color(1f, 0.82f, 0.22f), 1.5f);
                _centerStyle.normal.textColor = new Color(1f, 0.88f, 0.30f);
                GUI.Label(new Rect(cx - 195f, 20, 390, 28), $"128 BPM ビートに合わせて敵を殲滅せよ！ (WAVE {sm.SurvivalWave})", _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.VsCpuArena && sm.Player1 != null)
            {
                Rect objRect = new Rect(cx - 190f, 14, 380, 40);
                GUI.Box(objRect, GUIContent.none, _panelStyle);
                DrawOutlineBox(objRect, new Color(0.25f, 0.98f, 0.65f), 1.5f);
                _centerStyle.normal.textColor = new Color(0.25f, 0.98f, 0.65f);
                GUI.Label(new Rect(cx - 185f, 20, 370, 28), $"目標：AI忍者を倒せ！ (撃破 {sm.Player1.ArenaKills} / {sm.ArenaTargetKills})", _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.LocalPvp && sm.Player1 != null && sm.Player2 != null)
            {
                Rect objRect = new Rect(cx - 195f, 14, 390, 40);
                GUI.Box(objRect, GUIContent.none, _panelStyle);
                DrawOutlineBox(objRect, Color.white, 1.5f);
                _centerStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(cx - 190f, 20, 380, 28),
                    $"P1: {sm.Player1.ArenaKills}勝   vs   P2: {sm.Player2.ArenaKills}勝   ({sm.ArenaTargetKills}勝先取)",
                    _centerStyle);
            }
            else if (sm.ActiveMode == GameMode.LanPvp && LanPvpManager.Instance != null)
            {
                Rect objRect = new Rect(cx - 230f, 14, 460, 40);
                GUI.Box(objRect, GUIContent.none, _panelStyle);
                Color c = LanPvpManager.Instance.IsConnected ? new Color(0.25f, 0.98f, 0.65f) : new Color(1f, 0.8f, 0.25f);
                DrawOutlineBox(objRect, c, 1.5f);
                _centerStyle.normal.textColor = c;
                int rHp = LanPvpManager.Instance.RemoteAvatar != null ? LanPvpManager.Instance.RemoteAvatar.remoteHp : 0;
                GUI.Label(new Rect(cx - 225f, 20, 450, 28), $"{LanPvpManager.Instance.StatusText}  |  相手HP: {rHp}", _centerStyle);
            }
        }

        private void DrawPlayerHud(StageManager sm, CyberParkourController p, Rect vr)
        {
            float cx = vr.x + vr.width * 0.5f;
            float cy = vr.y + vr.height * 0.5f;

            // Speedlines / Damage / Kill Slow-Mo Edge Flash
            if (p.DamageFlashActive || CyberVFX.IsKillSlowMoActive || p.HorizontalSpeed > 16.5f || p.IsSlowMo)
            {
                Color edgeCol = p.DamageFlashActive
                    ? new Color(1f, 0.12f, 0.18f, 0.42f)
                    : (CyberVFX.IsKillSlowMoActive
                        ? new Color(1f, 0.85f, 0.22f, 0.36f)
                        : (p.IsSlowMo ? new Color(1f, 0.16f, 0.72f, 0.25f) : new Color(0.12f, 0.95f, 1.0f, 0.14f)));
                int borderW = CyberVFX.IsKillSlowMoActive ? 8 : 5;
                GUI.color = edgeCol;
                GUI.DrawTexture(new Rect(vr.x, vr.y, vr.width, borderW), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.x, vr.yMax - borderW, vr.width, borderW), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.x, vr.y, borderW, vr.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(vr.xMax - borderW, vr.y, borderW, vr.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Crosshair + 128 BPM Converging Beat Sync Bars
            Color retCol = p.HasTargetInSlashRange
                ? new Color(1f, 0.2f, 0.68f)
                : (p.HoveredGrapple != null ? new Color(1f, 0.82f, 0.22f) : new Color(0.2f, 0.96f, 1f));
            GUI.color = retCol;
            GUI.DrawTexture(new Rect(cx - 9, cy - 1, 6, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 3, cy - 1, 6, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - 9, 2, 6), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy + 3, 2, 6), Texture2D.whiteTexture);

            if (p.playerId == 0 && CyberRhythm.Instance != null)
            {
                var r = CyberRhythm.Instance;
                float pulse = r.BeatPulse;
                // Center Beat Target Brackets (±14px around crosshair)
                Color zoneCol = r.IsFever
                    ? Color.Lerp(new Color(1f, 0.28f, 0.78f, 0.45f), new Color(1f, 0.90f, 0.25f, 1f), pulse)
                    : Color.Lerp(new Color(0.20f, 0.92f, 1f, 0.35f), Color.white, pulse);
                GUI.color = zoneCol;
                float bh = 16f + pulse * 8f;
                GUI.DrawTexture(new Rect(cx - 15f, cy - bh * 0.5f, 3f, bh), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + 12f, cy - bh * 0.5f, 3f, bh), Texture2D.whiteTexture);

                // Sliding Beat Note Bars converging smoothly inward to hit zone at BeatPhase == 0/1
                float offset = 14f + (1f - r.BeatPhase) * 76f;
                Color noteCol = r.IsFever ? new Color(1f, 0.85f, 0.22f, 0.92f) : new Color(0.18f, 0.98f, 1f, 0.92f);
                GUI.color = noteCol;
                GUI.DrawTexture(new Rect(cx - offset - 3f, cy - 9f, 4f, 18f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx + offset - 1f, cy - 9f, 4f, 18f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            if (p.playerId == 0 && CyberRhythm.Instance != null && CyberRhythm.Instance.LastJudgeTimer > 0f)
            {
                _centerStyle.normal.textColor = CyberRhythm.Instance.LastJudgeColor;
                GUI.Label(new Rect(cx - 120, cy + 24, 240, 24), CyberRhythm.Instance.LastJudgeLabel, _centerStyle);
            }

            if (p.HoveredGrapple != null)
            {
                _centerStyle.normal.textColor = new Color(1f, 0.85f, 0.22f);
                GUI.Label(new Rect(cx - 90, cy + 16, 180, 24), p.playerId == 0 ? "[E] ワイヤー移動" : "[]] ワイヤー移動", _centerStyle);
            }
            if (p.playerId == 0 && _hitmarkerTimer > 0f)
            {
                _centerStyle.normal.textColor = _hitmarkerColor;
                GUI.Label(new Rect(cx - 120, cy - 42, 240, 24), _hitmarkerText, _centerStyle);
            }
            if (p.playerId == 0 && _calloutTimer > 0f)
            {
                _calloutStyle.normal.textColor = _calloutColor;
                GUI.Label(new Rect(cx - 240, cy - 110, 480, 34), _calloutMsg, _calloutStyle);
            }
            if (p.IsArenaDead)
            {
                _calloutStyle.normal.textColor = new Color(1f, 0.25f, 0.3f);
                GUI.Label(new Rect(cx - 140, cy - 20, 280, 40), "リスポーン中...", _calloutStyle);
            }

            // Bottom-Left: HP, Speed, Dash Charges
            float blW = Mathf.Min(275f, vr.width * 0.46f);
            float blH = p.isArenaFighter ? 92f : 68f;
            Rect blRect = new Rect(vr.x + 14f, vr.yMax - blH - 14f, blW, blH);
            GUI.Box(blRect, GUIContent.none, _panelStyle);
            DrawOutlineBox(blRect, new Color(0.18f, 0.92f, 1f, 0.7f), 1.5f);

            float rowY = blRect.y + 8f;
            if (p.isArenaFighter)
            {
                GUI.Label(new Rect(blRect.x + 10, rowY - 1, 76, 22), $"HP {p.ArenaHp}", _labelStyle);
                GUI.color = new Color(0.18f, 0.20f, 0.28f);
                GUI.DrawTexture(new Rect(blRect.x + 86, rowY + 4, blW - 98, 12), Texture2D.whiteTexture);
                GUI.color = p.ArenaHp > 35 ? new Color(0.2f, 0.98f, 0.65f) : new Color(1f, 0.2f, 0.3f);
                GUI.DrawTexture(new Rect(blRect.x + 86, rowY + 4, (blW - 98) * Mathf.Clamp01(p.ArenaHp / (float)p.arenaMaxHp), 12), Texture2D.whiteTexture);
                GUI.color = Color.white;
                rowY += 24f;
            }

            int kmh = Mathf.RoundToInt(p.HorizontalSpeed * 3.6f);
            GUI.Label(new Rect(blRect.x + 10, rowY, 95, 22), $"速度 {kmh}km/h", _labelStyle);
            float pipW = (blW - 122f) * 0.48f;
            for (int d = 0; d < p.MaxDashes; d++)
            {
                float px = blRect.x + 108f + d * (pipW + 6f);
                GUI.color = new Color(0.16f, 0.20f, 0.28f);
                GUI.DrawTexture(new Rect(px, rowY + 5, pipW, 12), Texture2D.whiteTexture);
                float fill = d < p.DashCharges ? 1f : (d == p.DashCharges ? p.DashRechargeRatio : 0f);
                GUI.color = new Color(0.15f, 0.95f, 1f);
                GUI.DrawTexture(new Rect(px, rowY + 5, pipW * fill, 12), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            rowY += 25f;
            GUI.Label(new Rect(blRect.x + 10, rowY, blW - 16, 20),
                p.playerId == 0 ? "[Shift]ダッシュ  [C]スライディング/急降下" : "[/]ダッシュ  [;]急降下  [RCtrl]武器切替",
                _smallLabelStyle);

            // Bottom-Right: Weapon & Skill Bar
            float brW = Mathf.Min(310f, vr.width * 0.48f);
            float brH = 92f;
            Rect brRect = new Rect(vr.xMax - brW - 14f, vr.yMax - brH - 14f, brW, brH);
            GUI.Box(brRect, GUIContent.none, _panelStyle);
            Color wepCol = p.ActiveWeapon == CyberWeaponMode.Katana ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.78f, 0.22f);
            DrawOutlineBox(brRect, wepCol, 1.5f);

            string wepName = p.ActiveWeapon == CyberWeaponMode.Katana ? "装備中: カタナ  ([1]/[2]で銃へ切替)" : "装備中: サイバー銃  ([1]/[2]で刀へ切替)";
            if (p.playerId == 1)
                wepName = p.ActiveWeapon == CyberWeaponMode.Katana ? "P2装備: カタナ" : "P2装備: サイバー銃";
            _labelStyle.normal.textColor = wepCol;
            GUI.Label(new Rect(brRect.x + 10, brRect.y + 7, brW - 18, 22), wepName, _labelStyle);
            _labelStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);

            // Skill Q & Skill F Bars
            string qLabel = p.ActiveWeapon == CyberWeaponMode.Katana ? (p.playerId == 0 ? "[Q] 斬撃波" : "[O] 斬撃波") : (p.playerId == 0 ? "[Q] レールガン" : "[O] レールガン");
            string fLabel = p.ActiveWeapon == CyberWeaponMode.Katana ? (p.playerId == 0 ? "[F] 旋風斬" : "[P] 旋風斬") : (p.playerId == 0 ? "[F] 拡散ショット" : "[P] 拡散弾");
            float halfSkillW = (brW - 28f) * 0.5f;

            DrawSkillSlot(new Rect(brRect.x + 10, brRect.y + 32, halfSkillW, 30), qLabel, p.SkillQReadyRatio, new Color(0.18f, 0.95f, 1f));
            DrawSkillSlot(new Rect(brRect.x + 18 + halfSkillW, brRect.y + 32, halfSkillW, 30), fLabel, p.SkillFReadyRatio, new Color(1f, 0.28f, 0.75f));

            GUI.Label(new Rect(brRect.x + 10, brRect.y + 66, brW - 18, 20),
                p.playerId == 0 ? "[左クリ]攻撃  [G]クイック銃撃  [M]消音" : "[U]攻撃  [H]クイック銃撃",
                _smallLabelStyle);
        }

        private void DrawSkillSlot(Rect r, string label, float readyRatio, Color accent)
        {
            GUI.Box(r, GUIContent.none, _cardStyle);
            GUI.color = new Color(accent.r, accent.g, accent.b, readyRatio >= 0.99f ? 0.30f : 0.14f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 5f, r.width * readyRatio, 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            _smallLabelStyle.normal.textColor = readyRatio >= 0.99f ? Color.white : new Color(0.55f, 0.62f, 0.72f);
            _smallLabelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(r.x, r.y + 1, r.width, 22), label, _smallLabelStyle);
            _smallLabelStyle.alignment = TextAnchor.UpperLeft;
            _smallLabelStyle.normal.textColor = new Color(0.78f, 0.88f, 0.98f);
        }

        private void DrawResultOverlay(StageManager sm)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            Rect ovRect = new Rect(cx - 250, cy - 135, 500, 270);
            GUI.Box(ovRect, GUIContent.none, _panelStyle);
            DrawOutlineBox(ovRect, new Color(0.16f, 0.95f, 1f), 2f);

            _calloutStyle.normal.textColor = new Color(0.2f, 0.96f, 1f);
            string header = !string.IsNullOrEmpty(sm.ArenaWinnerText)
                ? sm.ArenaWinnerText
                : (sm.IsAllStagesCleared ? "全ステージ完全クリア！！" : "ステージクリア！");
            GUI.Label(new Rect(cx - 230, cy - 110, 460, 40), header, _calloutStyle);

            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.22f);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx - 190, cy - 58, 380, 32), $"タイム: {sm.StageElapsed:00.00}秒   |   ランク: {sm.LastRank}", _titleStyle);
            _titleStyle.alignment = TextAnchor.UpperLeft;

            _centerStyle.normal.textColor = Color.white;
            var r = CyberRhythm.Instance;
            string statsLine = r != null
                ? $"スコア: {r.Score:N0}   |   最大コンボ: {r.MaxCombo}   |   撃破: {sm.StageKills} (精度 {r.AccuracyPercent:0}%)"
                : $"撃破数: {sm.StageKills} (合計 {sm.TotalKills})";
            GUI.Label(new Rect(cx - 235, cy - 16, 470, 26), statsLine, _centerStyle);

            string nextLabel = sm.ActiveMode == GameMode.Parkour && !sm.IsAllStagesCleared ? "次のステージへ [Space]" : "もう一度遊ぶ [Space]";
            if (DrawAccentButton(new Rect(cx - 220, cy + 44, 165, 44), nextLabel, new Color(0.16f, 0.95f, 1f)))
                sm.AdvanceOrReplay();

            if (DrawAccentButton(new Rect(cx - 45, cy + 44, 115, 44), "リトライ [R]", new Color(1f, 0.28f, 0.72f)))
                sm.RestartCurrentStage();

            if (DrawAccentButton(new Rect(cx + 80, cy + 44, 140, 44), "メインメニュー", Color.white))
                sm.OpenMainMenu();
        }
    }
}
