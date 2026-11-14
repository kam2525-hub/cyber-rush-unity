# CYBER RUSH // KATANA x RAILGUN

ハイスピード一人称サイバーパンク・カタナ＆ガンアクションゲーム（Unity 6.2 Built-in RP + Blender 4.5 ハードサーフェス3Dアセット）。

---

## 主な特徴 (Features)

1. **パルクールアクション (`PARKOUR STAGES 1-3`)**
   - ウォールラン、ウォールジャンプ、高速ダッシュ（Iフレーム付き）、バレットタイム・スローモーション、グラップルフック、スライディング。
   - **Stage 1: NEON SKYLINE** / **Stage 2: CYBER CANYON** / **Stage 3: ARCHON SPIRE**（ボス戦＆シールドパイロン破壊ギミック）。
2. **多彩な剣技＆サイバー銃アクション (`Katana & Cyber Blaster`)**
   - **武器切り替え (`[1]` Katana / `[2]` Blaster / マウスホイール)** + カタナ装備中でも **`[G]`** でクイックショット可能。
   - **Katana 技**:
     - `[LMB]`: 3段コンボ斬撃 & 敵プラズマ弾のジャスト反射 (Deflect)
     - `[Q]`: **Sword Wave（斬撃波）** — 敵を貫通するネオン三日月波を発射
     - `[F]`: **Cyclone Spin（旋風斬）** — 周囲360°を斬り払い、弾丸を一斉反射＋上昇リフト
     - `空中 + [C]`: **Dive Plunge Slam（急降下斬撃）** — 着地時に衝撃波リングと範囲ダメージ
   - **Blaster 技**:
     - `[LMB]`: 高速パルスレーザー連射
     - `[Q]`: **Piercing Railgun** — 即着貫通レールガンビーム
     - `[F]`: **Scatter Burst** — 5発拡散プラズマショット
3. **プロシージャル・オーディオ (`CyberAudio.cs`)**
   - 外部音声ファイル不要のDSPリアルタイム合成により、シンセウェーブBGM（ループ）と各種剣戟・銃声・反射・爆発・ダッシュ・ステージクリアSEを搭載（`[M]` でミュート切替）。
4. **豊富な対戦・バトルモード**
   - **Boss Rush (`BOSS RUSH`)**: Archon Core ボスとの直接対決モード（HP制）
   - **CPU戦 (`VS CPU ARENA`)**: Blender製サイバー忍者AI (`CyberBotAI`) 2体と戦うアリーナ戦
   - **オフライン2P対人戦 (`LOCAL 2P PVP`)**: 1台のPCで画面左右分割（P1 vs P2）で対戦可能
   - **LAN / IP 対人戦 (`LAN / IP PVP`)**: UDP同期 (`LanPvpManager`) によるHost/Join型リアルタイム通信対戦

---

## 操作方法 (Controls)

### P1 / ソロプレイ
| 入力 | アクション |
| --- | --- |
| `W / A / S / D` | 移動 |
| `Mouse` | 視点操作（`Alt` でカーソル切替、`ESC` でメインメニュー） |
| `Space` | ジャンプ / ウォールジャンプ |
| `Left Shift` / `RMB` | ダッシュ / 長押しでスローモーション |
| `C` (地上 / 空中) | スライディング / Dive Plunge Slam |
| `E` | グラップルアンカーへジップ |
| `1` / `2` / `Wheel` | Katana ⇔ Cyber Blaster 切替 |
| `LMB` | 通常攻撃（斬撃＋弾反射 / パルスレーザー） |
| `Q` | スキル1（Sword Wave / Railgun） |
| `F` | スキル2（Cyclone Spin / Scatter Burst） |
| `G` | クイックショット（いつでも銃撃） |
| `R` / `M` | リトライ / サウンドミュート |

### P2 (Local 2P Split-Screen PvP)
| 入力 | アクション |
| --- | --- |
| `方向キー (Arrows)` | 移動 |
| `I / J / K / L` | 視点操作 |
| `Enter` / `Right Shift` | ジャンプ / ウォールジャンプ |
| `/` または `Numpad 0` | ダッシュ |
| `U` / `H` | 攻撃 / クイックショット |
| `O` / `P` | スキル1 / スキル2 |
| `Right Ctrl` / `Backspace` | 武器切替 |
| `;` / `]` | スライディング＆急降下斬撃 / グラップル |

---

## プロジェクト構成 & ビルド方法

- **Unity Version**: `6000.2.10f1` (`UnityProject/`)
- **Blender Scripts**: `BlenderScripts/build_cyber_assets.py`（Blender 4.5 にて全9種のFBXモデルを生成・出力）
- **Editor Build Menu**: Unity エディタ上部メニュー `CyberRush > Build CyberRush & Executable` を実行すると、`Assets/Scenes/CyberRush.unity` と Windows 64bit 実行ファイル (`Builds/CyberRush.exe`) が自動生成されます。
