using System.Collections;
using UnityEngine;

namespace CyberRush
{
    public enum CyberWeaponMode
    {
        Katana = 0,
        Blaster = 1
    }

    [RequireComponent(typeof(CharacterController))]
    public class CyberParkourController : MonoBehaviour
    {
        [Header("Player Identity & Arena Mode")]
        public int playerId = 0; // 0 = P1 / Solo, 1 = P2 Split-Screen
        public bool isArenaFighter = false;
        public int arenaMaxHp = 100;

        [Header("Movement Speeds")]
        public float baseRunSpeed = 14.5f;
        public float wallRunSpeed = 17.5f;
        public float dashSpeed = 30.0f;
        public float dashDuration = 0.17f;
        public float slideSpeed = 20.5f;
        public float gravity = -26.0f;
        public float jumpForce = 10.6f;

        [Header("Mouse Look & References")]
        public float mouseSensitivity = 2.4f;
        public Transform cameraHolder;
        public Camera playerCamera;
        public Transform katanaPivot;
        public Transform blasterPivot;

        // Runtime State
        private CharacterController _cc;
        private Vector3 _velocity;
        private Vector3 _horizontalVel;
        private float _yaw;
        private float _pitch;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Wall-run
        private bool _isWallRunning;
        private int _wallSide; // -1 left, +1 right
        private Vector3 _wallNormal;
        private float _wallRunCooldown;
        private float _camTilt;

        // Dash & Slow-Mo
        private int _dashCharges = 2;
        private const int MaxDashCharges = 2;
        private float _dashRechargeTimer;
        private bool _isDashing;
        private float _slowMoEnergy = 1.0f;
        private bool _isSlowMo;
        private bool _rmbDownHeld;
        private float _rmbHoldTime;

        // Slide & Dive Slam
        private bool _isSliding;
        private float _slideTimer;
        private bool _isDivingSlam;
        private float _slamCooldown;
        private const float SlamMaxCooldown = 2.8f;

        // Grapple
        private bool _isGrappling;
        private GrappleAnchor _hoveredAnchor;
        private LineRenderer _grappleLine;

        // Weapons & Skills
        private CyberWeaponMode _weaponMode = CyberWeaponMode.Katana;
        private int _slashCombo;
        private float _slashCooldown;
        private float _slashAnimTimer;
        private float _blasterCooldown;
        private float _blasterRecoil;
        private float _skillQCooldown;
        private float _skillFCooldown;
        private const float SkillQMaxCooldown = 2.4f;
        private const float SkillFMaxCooldown = 3.5f;

        private readonly Vector3 _katanaRestLocalPos = new Vector3(0.42f, -0.34f, 0.56f);
        private readonly Quaternion _katanaRestLocalRot = Quaternion.Euler(-12f, -14f, 8f);
        private readonly Vector3 _blasterRestLocalPos = new Vector3(0.34f, -0.26f, 0.52f);
        private readonly Quaternion _blasterRestLocalRot = Quaternion.Euler(2f, -4f, 0f);
        private TrailRenderer _katanaTrail;

        // Arena / PvP Fighter Stats
        private Vector3 _arenaSpawnPos;
        private Quaternion _arenaSpawnRot = Quaternion.identity;
        public int ArenaHp { get; private set; } = 100;
        public int ArenaKills { get; private set; }
        public int ArenaDeaths { get; private set; }
        public bool IsArenaDead { get; private set; }
        private float _damageFlashTimer;

        // Public HUD / State Properties
        public float HorizontalSpeed => _cc != null ? new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude : 0f;
        public int DashCharges => _dashCharges;
        public int MaxDashes => MaxDashCharges;
        public float DashRechargeRatio => _dashCharges >= MaxDashCharges ? 1f : Mathf.Clamp01(1f - _dashRechargeTimer / 1.45f);
        public float SlowMoRatio => _slowMoEnergy;
        public bool IsSlowMo => _isSlowMo;
        public bool IsWallRunning => _isWallRunning;
        public GrappleAnchor HoveredGrapple => _hoveredAnchor;
        public bool HasTargetInSlashRange { get; private set; }
        public CyberWeaponMode ActiveWeapon => _weaponMode;
        public float SkillQReadyRatio => _skillQCooldown <= 0f ? 1f : Mathf.Clamp01(1f - _skillQCooldown / SkillQMaxCooldown);
        public float SkillFReadyRatio => _skillFCooldown <= 0f ? 1f : Mathf.Clamp01(1f - _skillFCooldown / SkillFMaxCooldown);
        public float SkillSlamReadyRatio => _slamCooldown <= 0f ? 1f : Mathf.Clamp01(1f - _slamCooldown / SlamMaxCooldown);
        public float SkillQRemaining => Mathf.Max(0f, _skillQCooldown);
        public float SkillFRemaining => Mathf.Max(0f, _skillFCooldown);
        public bool DamageFlashActive => _damageFlashTimer > 0f;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
            if (cameraHolder == null && playerCamera != null) cameraHolder = playerCamera.transform.parent;

            _yaw = transform.eulerAngles.y;
            ArenaHp = arenaMaxHp;

            var lrGo = new GameObject("GrappleTetherLine");
            lrGo.transform.SetParent(transform, false);
            _grappleLine = lrGo.AddComponent<LineRenderer>();
            _grappleLine.positionCount = 2;
            _grappleLine.startWidth = 0.06f;
            _grappleLine.endWidth = 0.04f;
            _grappleLine.enabled = false;
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material m = new Material(s);
            m.color = new Color(0.15f, 0.95f, 1.0f);
            _grappleLine.sharedMaterial = m;

            SetupKatanaTrail();
            ApplyWeaponVisibility();
        }

        private void SetupKatanaTrail()
        {
            if (katanaPivot == null) return;
            GameObject tip = new GameObject("KatanaBladeTip");
            tip.transform.SetParent(katanaPivot, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 1.05f);

            _katanaTrail = tip.AddComponent<TrailRenderer>();
            _katanaTrail.time = 0.14f;
            _katanaTrail.minVertexDistance = 0.03f;
            _katanaTrail.emitting = false;
            _katanaTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _katanaTrail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.24f),
                new Keyframe(0.6f, 0.12f),
                new Keyframe(1f, 0f)
            );
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.15f, 0.95f, 1f), 0.4f), new GradientColorKey(new Color(1f, 0.16f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            _katanaTrail.colorGradient = g;
            Material tm = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            tm.color = Color.white;
            _katanaTrail.sharedMaterial = tm;
        }

        public void SetCursorLocked(bool locked)
        {
            if (playerId != 0) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ConfigureArenaFighter(bool arenaMode, Vector3 spawnPos, Quaternion spawnRot)
        {
            isArenaFighter = arenaMode;
            _arenaSpawnPos = spawnPos;
            _arenaSpawnRot = spawnRot;
            ArenaHp = arenaMaxHp;
            ArenaKills = 0;
            ArenaDeaths = 0;
            IsArenaDead = false;
        }

        public void SetWeaponMode(CyberWeaponMode mode)
        {
            _weaponMode = mode;
            ApplyWeaponVisibility();
        }

        private void ApplyWeaponVisibility()
        {
            if (katanaPivot != null) katanaPivot.gameObject.SetActive(_weaponMode == CyberWeaponMode.Katana);
            if (blasterPivot != null) blasterPivot.gameObject.SetActive(_weaponMode == CyberWeaponMode.Blaster);
        }

        public void RespawnAt(Vector3 pos, Quaternion rot)
        {
            StopAllCoroutines();
            _isDashing = false;
            _isGrappling = false;
            _isWallRunning = false;
            _isSliding = false;
            _isDivingSlam = false;
            _isSlowMo = false;
            IsArenaDead = false;
            ArenaHp = arenaMaxHp;
            if (_grappleLine != null) _grappleLine.enabled = false;
            Time.timeScale = 1f;
            _dashCharges = MaxDashCharges;
            _slowMoEnergy = 1f;
            _skillQCooldown = 0f;
            _skillFCooldown = 0f;
            _slamCooldown = 0f;
            _velocity = Vector3.zero;
            _horizontalVel = Vector3.zero;
            _cc.height = 1.85f;
            _cc.center = new Vector3(0f, 0.92f, 0f);

            _cc.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            _yaw = rot.eulerAngles.y;
            _pitch = 0f;
            _cc.enabled = true;

            if (katanaPivot != null)
            {
                katanaPivot.localPosition = _katanaRestLocalPos;
                katanaPivot.localRotation = _katanaRestLocalRot;
            }
            if (blasterPivot != null)
            {
                blasterPivot.localPosition = _blasterRestLocalPos;
                blasterPivot.localRotation = _blasterRestLocalRot;
            }
            if (_katanaTrail != null) _katanaTrail.emitting = false;
            ApplyWeaponVisibility();

            if (playerId == 0)
            {
                var sm = StageManager.Instance;
                bool lockCursor = sm == null || (!sm.IsMenuOpen && !sm.IsOverlayOpen && sm.ActiveMode != GameMode.LocalPvp);
                SetCursorLocked(lockCursor);
            }
        }

        public void ApplyJumpPadBoost(float upSpeed)
        {
            _isWallRunning = false;
            _isSliding = false;
            _isDivingSlam = false;
            _velocity.y = Mathf.Max(_velocity.y, upSpeed);
            _horizontalVel += transform.forward * 4.0f;
            CyberHUD.ShowCallout("ブーストパッド！", new Color(0.15f, 0.95f, 1f));
        }

        public void OnKillDashReset()
        {
            _dashCharges = MaxDashCharges;
            _dashRechargeTimer = 0f;
            _slowMoEnergy = Mathf.Min(1f, _slowMoEnergy + 0.45f);
        }

        public void OnArenaKillScored()
        {
            ArenaKills++;
            StageManager.Instance?.CheckArenaVictory(this);
        }

        public void TakeHit()
        {
            TakeCombatDamage(isArenaFighter ? 20 : 100, null);
        }

        public bool TakeCombatDamage(int dmg, CyberParkourController attacker)
        {
            if (_isDashing || IsArenaDead) return false;

            if (!isArenaFighter)
            {
                CyberAudio.Play("damage", 0.85f);
                CyberHUD.ShowCallout("被弾！ リトライ", new Color(1f, 0.2f, 0.25f));
                if (StageManager.Instance != null)
                    StageManager.Instance.RestartCurrentStage();
                else
                    RespawnAt(new Vector3(0f, 1.5f, 0f), Quaternion.identity);
                return true;
            }

            ArenaHp = Mathf.Max(0, ArenaHp - dmg);
            _damageFlashTimer = 0.28f;
            CyberAudio.Play("damage", 0.7f);
            CyberVFX.SpawnShatterExplosion(this, transform.position + Vector3.up * 1.1f, new Color(1f, 0.2f, 0.3f), 6);

            if (ArenaHp <= 0)
            {
                IsArenaDead = true;
                ArenaDeaths++;
                if (attacker != null && attacker != this)
                {
                    attacker.OnArenaKillScored();
                    attacker.OnKillDashReset();
                }
                CyberAudio.Play("explosion", 0.9f);
                CyberHUD.ShowCallout($"PLAYER {playerId + 1} 撃破！", new Color(1f, 0.25f, 0.35f));
                StartCoroutine(ArenaRespawnRoutine());
                return true;
            }
            return false;
        }

        private IEnumerator ArenaRespawnRoutine()
        {
            yield return new WaitForSeconds(1.8f);
            if (isArenaFighter && (StageManager.Instance == null || (!StageManager.Instance.IsOverlayOpen && !StageManager.Instance.IsMenuOpen)))
            {
                RespawnAt(_arenaSpawnPos, _arenaSpawnRot);
            }
        }

        private void Update()
        {
            if (_damageFlashTimer > 0f) _damageFlashTimer -= Time.unscaledDeltaTime;

            var sm = StageManager.Instance;
            if (sm != null)
            {
                if (sm.IsMenuOpen) return;
                if (sm.IsOverlayOpen)
                {
                    if (playerId == 0 && Input.GetKeyDown(KeyCode.Space))
                        sm.AdvanceOrReplay();
                    else if (playerId == 0 && Input.GetKeyDown(KeyCode.R))
                        sm.RestartCurrentStage();
                    return;
                }
            }

            if (IsArenaDead) return;

            if (playerId == 0)
            {
                if (Input.GetKeyDown(KeyCode.R) && sm != null && sm.ActiveMode == GameMode.Parkour)
                {
                    sm.RestartCurrentStage();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    sm?.OpenMainMenu();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
                    SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
                }
                if (sm != null && sm.ActiveMode != GameMode.LocalPvp && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
                {
                    SetCursorLocked(true);
                }
            }

            HandleLook();
            UpdateTargets();
            HandleAbilities();
            HandleMovement();
            UpdateCameraVisuals();
            UpdateWeaponViewmodels();

            if (transform.position.y < -22f)
            {
                if (isArenaFighter)
                    RespawnAt(_arenaSpawnPos, _arenaSpawnRot);
                else
                    sm?.RestartCurrentStage();
            }
        }

        private void ReadMoveAxes(out float h, out float v)
        {
            if (playerId == 0)
            {
                h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
                v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            }
            else
            {
                h = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                v = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            }
        }

        private bool IsJumpPressed()
        {
            return playerId == 0
                ? Input.GetKeyDown(KeyCode.Space)
                : (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.RightShift));
        }

        private void HandleLook()
        {
            if (playerId == 0)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    _yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                    _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
                }
                else if (StageManager.Instance != null && StageManager.Instance.ActiveMode == GameMode.LocalPvp)
                {
                    // Allow Q/E fallback yaw? No, P1 can either lock cursor by clicking left half or press Z/X optionally; clicking locks if wanted, or keep C/Z subtle
                    float altYaw = (Input.GetKey(KeyCode.V) ? 1f : 0f) - (Input.GetKey(KeyCode.Z) ? 1f : 0f);
                    _yaw += altYaw * 125f * Time.unscaledDeltaTime;
                }
            }
            else
            {
                float turn = (Input.GetKey(KeyCode.L) ? 1f : 0f) - (Input.GetKey(KeyCode.J) ? 1f : 0f);
                float lookY = (Input.GetKey(KeyCode.I) ? 1f : 0f) - (Input.GetKey(KeyCode.K) ? 1f : 0f);
                _yaw += turn * 135f * Time.unscaledDeltaTime;
                _pitch -= lookY * 95f * Time.unscaledDeltaTime;
            }

            _pitch = Mathf.Clamp(_pitch, -82f, 84f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraHolder != null)
            {
                float targetTilt = _isWallRunning ? (-_wallSide * 14f) : (_isSliding ? -4f : 0f);
                _camTilt = Mathf.Lerp(_camTilt, targetTilt, 12f * Time.unscaledDeltaTime);
                cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, _camTilt);
            }
        }

        private void UpdateTargets()
        {
            _hoveredAnchor = null;
            if (playerCamera != null)
            {
                float bestScore = 0.80f;
                foreach (var anchor in FindObjectsByType<GrappleAnchor>(FindObjectsSortMode.None))
                {
                    Vector3 toA = anchor.transform.position - playerCamera.transform.position;
                    float dist = toA.magnitude;
                    if (dist > anchor.range || dist < 3.0f) continue;
                    float dot = Vector3.Dot(playerCamera.transform.forward, toA.normalized);
                    if (dot > bestScore)
                    {
                        bestScore = dot;
                        _hoveredAnchor = anchor;
                    }
                }
            }

            HasTargetInSlashRange = false;
            Vector3 origin = playerCamera != null ? playerCamera.transform.position + playerCamera.transform.forward * 1.8f : transform.position + transform.forward * 1.8f;
            foreach (var col in Physics.OverlapSphere(origin, 2.5f))
            {
                var enemy = col.GetComponentInParent<CyberEnemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    HasTargetInSlashRange = true;
                    break;
                }
                var bot = col.GetComponentInParent<CyberBotAI>();
                if (bot != null && !bot.IsDead)
                {
                    HasTargetInSlashRange = true;
                    break;
                }
                var opp = col.GetComponentInParent<CyberParkourController>();
                if (opp != null && opp != this && !opp.IsArenaDead)
                {
                    HasTargetInSlashRange = true;
                    break;
                }
            }
        }

        private void HandleAbilities()
        {
            float dtUnscaled = Time.unscaledDeltaTime;

            if (_dashCharges < MaxDashCharges)
            {
                _dashRechargeTimer -= dtUnscaled;
                if (_dashRechargeTimer <= 0f)
                {
                    _dashCharges++;
                    if (_dashCharges < MaxDashCharges) _dashRechargeTimer = 1.45f;
                }
            }

            if (_slashCooldown > 0f) _slashCooldown -= dtUnscaled;
            if (_blasterCooldown > 0f) _blasterCooldown -= dtUnscaled;
            if (_skillQCooldown > 0f) _skillQCooldown -= dtUnscaled;
            if (_skillFCooldown > 0f) _skillFCooldown -= dtUnscaled;
            if (_slamCooldown > 0f) _slamCooldown -= dtUnscaled;

            // Weapon Switch
            if (playerId == 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeaponMode(CyberWeaponMode.Katana);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeaponMode(CyberWeaponMode.Blaster);
                else if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.05f)
                    SetWeaponMode(_weaponMode == CyberWeaponMode.Katana ? CyberWeaponMode.Blaster : CyberWeaponMode.Katana);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.Backspace))
                    SetWeaponMode(_weaponMode == CyberWeaponMode.Katana ? CyberWeaponMode.Blaster : CyberWeaponMode.Katana);
            }

            // Dash
            bool dashPressed = playerId == 0
                ? Input.GetKeyDown(KeyCode.LeftShift)
                : (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.Keypad0));
            if (dashPressed && !_isDashing && _dashCharges > 0)
            {
                StartDash(GetMoveOrForwardDirection());
            }

            // Slow-Mo (Solo / Non-PvP P1 Right Click)
            bool allowSlowMo = playerId == 0 && (StageManager.Instance == null || (StageManager.Instance.ActiveMode != GameMode.LocalPvp && StageManager.Instance.ActiveMode != GameMode.LanPvp));
            if (allowSlowMo)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    _rmbDownHeld = true;
                    _rmbHoldTime = 0f;
                }
                if (_rmbDownHeld && Input.GetMouseButton(1))
                {
                    _rmbHoldTime += dtUnscaled;
                    if (_rmbHoldTime > 0.08f && _slowMoEnergy > 0.05f && !_isGrappling)
                    {
                        _isSlowMo = true;
                        Time.timeScale = 0.28f;
                        _slowMoEnergy = Mathf.Max(0f, _slowMoEnergy - 0.65f * dtUnscaled);
                        if (_slowMoEnergy <= 0.02f) ReleaseSlowMoDash();
                    }
                }
                if (_rmbDownHeld && Input.GetMouseButtonUp(1))
                {
                    ReleaseSlowMoDash();
                }
                if (!_isSlowMo)
                    _slowMoEnergy = Mathf.Min(1f, _slowMoEnergy + 0.35f * dtUnscaled);
            }

            // Grapple
            bool grapplePressed = playerId == 0 ? Input.GetKeyDown(KeyCode.E) : Input.GetKeyDown(KeyCode.RightBracket);
            if (grapplePressed && _hoveredAnchor != null && !_isGrappling)
            {
                StartCoroutine(GrappleRoutine(_hoveredAnchor));
            }

            // Quick Sidearm Shot ([G] for P1, [H] for P2) even while Katana equipped
            bool quickGunPressed = playerId == 0 ? Input.GetKeyDown(KeyCode.G) : Input.GetKeyDown(KeyCode.H);
            if (quickGunPressed && _blasterCooldown <= 0f)
            {
                FireBlasterBolt(0f, 18, 1);
            }

            // Primary Fire / Slash
            bool primaryPressed = playerId == 0
                ? (Input.GetMouseButtonDown(0) && (Cursor.lockState == CursorLockMode.Locked || (StageManager.Instance != null && StageManager.Instance.ActiveMode == GameMode.LocalPvp)))
                : Input.GetKeyDown(KeyCode.U);
            bool primaryHeld = playerId == 0
                ? (Input.GetMouseButton(0) && Cursor.lockState == CursorLockMode.Locked)
                : Input.GetKey(KeyCode.U);

            if (_weaponMode == CyberWeaponMode.Katana)
            {
                if (primaryPressed && _slashCooldown <= 0f)
                    PerformKatanaSlash();
            }
            else
            {
                if ((primaryPressed || primaryHeld) && _blasterCooldown <= 0f)
                    FireBlasterBolt(0f, 18, 1);
            }

            // Skill Q: Sword Wave (Katana) / Piercing Railgun (Blaster)
            bool skillQPressed = playerId == 0 ? Input.GetKeyDown(KeyCode.Q) : Input.GetKeyDown(KeyCode.O);
            if (skillQPressed && _skillQCooldown <= 0f)
            {
                _skillQCooldown = SkillQMaxCooldown;
                if (_weaponMode == CyberWeaponMode.Katana)
                    PerformSwordWave();
                else
                    FireRailgunBeam();
            }

            // Skill F: Cyclone Spin (Katana) / Scatter Plasma Burst (Blaster)
            bool skillFPressed = playerId == 0 ? Input.GetKeyDown(KeyCode.F) : Input.GetKeyDown(KeyCode.P);
            if (skillFPressed && _skillFCooldown <= 0f)
            {
                _skillFCooldown = SkillFMaxCooldown;
                if (_weaponMode == CyberWeaponMode.Katana)
                    PerformCycloneSlash();
                else
                    FireScatterBurst();
            }
        }

        private void ReleaseSlowMoDash()
        {
            _rmbDownHeld = false;
            bool wasSlow = _isSlowMo;
            _isSlowMo = false;
            if (Time.timeScale < 0.9f && Time.timeScale > 0.15f)
                Time.timeScale = 1f;

            if (!_isDashing && _dashCharges > 0 && (wasSlow || _rmbHoldTime <= 0.12f))
            {
                StartDash(GetMoveOrForwardDirection());
            }
        }

        private Vector3 GetMoveOrForwardDirection()
        {
            ReadMoveAxes(out float h, out float v);
            Vector3 input = (transform.forward * v + transform.right * h);
            if (input.sqrMagnitude > 0.05f) return input.normalized;
            return playerCamera != null ? new Vector3(playerCamera.transform.forward.x, playerCamera.transform.forward.y * 0.35f, playerCamera.transform.forward.z).normalized : transform.forward;
        }

        private void StartDash(Vector3 dir)
        {
            if (_dashCharges <= 0 || _isDashing) return;
            _dashCharges--;
            if (_dashCharges == MaxDashCharges - 1) _dashRechargeTimer = 1.45f;
            StartCoroutine(DashRoutine(dir.normalized));
        }

        private IEnumerator DashRoutine(Vector3 dir)
        {
            _isDashing = true;
            _isWallRunning = false;
            _isSliding = false;
            _isDivingSlam = false;
            if (Time.timeScale < 0.95f && playerId == 0) Time.timeScale = 1f;

            CyberAudio.Play("dash", 0.7f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(8);

            Quaternion lookRot = Quaternion.LookRotation(dir);
            Color dashCol = playerId == 0 ? new Color(0.15f, 0.95f, 1.0f) : new Color(1f, 0.22f, 0.72f);
            CyberVFX.SpawnDashStreaks(this, transform.position + Vector3.up * 1.1f, lookRot, dashCol);
            CyberVFX.SpawnRingPulse(this, transform.position + Vector3.up * 0.9f, dashCol, 2.4f, 0.18f);

            float t = 0f;
            while (t < dashDuration)
            {
                t += Time.unscaledDeltaTime;
                _cc.Move(dir * (dashSpeed * Time.unscaledDeltaTime));
                yield return null;
            }
            _horizontalVel = new Vector3(dir.x, 0f, dir.z) * (baseRunSpeed * 1.25f);
            _velocity.y = Mathf.Max(0f, dir.y * 8f);
            _isDashing = false;
        }

        private IEnumerator GrappleRoutine(GrappleAnchor anchor)
        {
            _isGrappling = true;
            _isWallRunning = false;
            _isSliding = false;
            _isDivingSlam = false;
            _isSlowMo = false;
            if (playerId == 0) Time.timeScale = 1f;
            _grappleLine.enabled = true;
            CyberAudio.Play("dash", 0.75f);
            CyberHUD.ShowCallout("GRAPPLE ZIP!", new Color(1.0f, 0.80f, 0.22f));

            Vector3 target = anchor.transform.position;
            float maxDur = 0.65f;
            float t = 0f;
            while (t < maxDur && Vector3.Distance(transform.position + Vector3.up * 1.0f, target) > 2.8f)
            {
                t += Time.deltaTime;
                Vector3 handPos = katanaPivot != null ? katanaPivot.position - transform.right * 0.45f : transform.position + Vector3.up * 1.2f;
                _grappleLine.SetPosition(0, handPos);
                _grappleLine.SetPosition(1, target);

                Vector3 pull = (target - (transform.position + Vector3.up * 0.9f)).normalized;
                _cc.Move(pull * (31f * Time.deltaTime));
                yield return null;
            }

            _grappleLine.enabled = false;
            _velocity.y = 9.5f;
            Vector3 exitFwd = (target - transform.position);
            exitFwd.y = 0f;
            if (exitFwd.sqrMagnitude > 0.01f)
                _horizontalVel = exitFwd.normalized * (baseRunSpeed * 1.25f);
            _isGrappling = false;
        }

        private void HandleMovement()
        {
            if (_isDashing || _isGrappling) return;

            float dt = Time.deltaTime;
            bool grounded = _cc.isGrounded;

            if (grounded)
            {
                _coyoteTimer = 0.14f;
                _isWallRunning = false;
                if (_isDivingSlam)
                {
                    _isDivingSlam = false;
                    TriggerDiveSlamImpact();
                }
                if (_velocity.y < 0f) _velocity.y = -3.0f;
            }
            else
            {
                _coyoteTimer -= dt;
            }

            if (_wallRunCooldown > 0f) _wallRunCooldown -= dt;
            if (IsJumpPressed()) _jumpBufferTimer = 0.13f;
            else if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;

            ReadMoveAxes(out float h, out float v);
            Vector3 wishDir = (transform.forward * v + transform.right * h);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            bool crouchOrSlamPressed = playerId == 0
                ? (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
                : Input.GetKeyDown(KeyCode.Semicolon);

            // Air + Crouch = Dive Plunge Slam!
            if (crouchOrSlamPressed && !grounded && !_isDivingSlam && !_isWallRunning && _slamCooldown <= 0f)
            {
                _isDivingSlam = true;
                _slamCooldown = SlamMaxCooldown;
                _velocity.y = -36f;
                _horizontalVel *= 0.35f;
                CyberAudio.Play("slash", 0.8f);
                CyberHUD.ShowCallout("DIVE SLAM!", new Color(1f, 0.35f, 0.85f));
            }
            // Ground + Crouch = Cyber Slide
            else if (crouchOrSlamPressed && grounded && HorizontalSpeed > 7f && !_isSliding)
            {
                _isSliding = true;
                _slideTimer = 0.65f;
                Vector3 slideDir = wishDir.sqrMagnitude > 0.1f ? wishDir : transform.forward;
                _horizontalVel = slideDir * slideSpeed;
            }

            if (_isSliding)
            {
                _slideTimer -= dt;
                _cc.height = 1.0f;
                _cc.center = new Vector3(0f, 0.50f, 0f);
                bool released = playerId == 0 ? Input.GetKeyUp(KeyCode.C) : Input.GetKeyUp(KeyCode.Semicolon);
                if (_slideTimer <= 0f || released)
                {
                    _isSliding = false;
                    _cc.height = 1.85f;
                    _cc.center = new Vector3(0f, 0.92f, 0f);
                }
            }

            if (!grounded && _wallRunCooldown <= 0f && v > 0.05f && !_isDivingSlam)
            {
                CheckWallRun();
            }
            else if (grounded)
            {
                _isWallRunning = false;
            }

            if (_isWallRunning)
            {
                Vector3 wallFwd = Vector3.Cross(_wallNormal, Vector3.up).normalized;
                if (Vector3.Dot(wallFwd, transform.forward) < 0f) wallFwd = -wallFwd;

                _horizontalVel = wallFwd * wallRunSpeed;
                _velocity.y = Mathf.Lerp(_velocity.y, -0.4f, 10f * dt);

                if (_jumpBufferTimer > 0f)
                {
                    _jumpBufferTimer = 0f;
                    _isWallRunning = false;
                    _wallRunCooldown = 0.24f;
                    _velocity.y = jumpForce * 1.05f;
                    _horizontalVel = (wallFwd * 13.5f) + (_wallNormal * 9.5f);
                    CyberAudio.Play("dash", 0.55f);
                    CyberVFX.SpawnDashStreaks(this, transform.position + Vector3.up * 1f, transform.rotation, new Color(1f, 0.16f, 0.72f), 5);
                }
            }
            else
            {
                if (!_isSliding && !_isDivingSlam)
                {
                    Vector3 targetVel = wishDir * baseRunSpeed;
                    float accel = grounded ? 16f : 7.5f;
                    _horizontalVel = Vector3.Lerp(_horizontalVel, targetVel, accel * dt);
                }

                if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
                {
                    _jumpBufferTimer = 0f;
                    _coyoteTimer = 0f;
                    _isSliding = false;
                    _cc.height = 1.85f;
                    _cc.center = new Vector3(0f, 0.92f, 0f);
                    _velocity.y = jumpForce;
                }

                _velocity.y += gravity * dt;
            }

            Vector3 finalMove = _horizontalVel + Vector3.up * _velocity.y;
            _cc.Move(finalMove * dt);
        }

        private void CheckWallRun()
        {
            Vector3 chest = transform.position + Vector3.up * 1.1f;
            float checkDist = 1.15f;

            bool rightHit = Physics.Raycast(chest, transform.right, out RaycastHit rh, checkDist) && IsWallRunnable(rh.collider);
            bool leftHit = Physics.Raycast(chest, -transform.right, out RaycastHit lh, checkDist) && IsWallRunnable(lh.collider);

            if (rightHit)
            {
                _isWallRunning = true;
                _wallSide = 1;
                _wallNormal = rh.normal;
            }
            else if (leftHit)
            {
                _isWallRunning = true;
                _wallSide = -1;
                _wallNormal = lh.normal;
            }
            else
            {
                _isWallRunning = false;
            }
        }

        private bool IsWallRunnable(Collider col)
        {
            if (col == null || col.isTrigger) return false;
            if (col.GetComponent<WallRunSurface>() != null) return true;
            return col.bounds.size.y > 2.4f;
        }

        private void PerformKatanaSlash()
        {
            _slashCooldown = 0.24f;
            _slashCombo = (_slashCombo % 3) + 1;
            _slashAnimTimer = 0.20f;
            CyberAudio.Play("slash", 0.85f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(1);

            if (_katanaTrail != null)
            {
                StopCoroutine(nameof(TrailBurstRoutine));
                StartCoroutine(TrailBurstRoutine());
            }

            if (playerCamera != null)
            {
                Vector3 arcPos = playerCamera.transform.position + playerCamera.transform.forward * 1.35f - playerCamera.transform.up * 0.12f;
                float roll = _slashCombo == 1 ? -18f : (_slashCombo == 2 ? 28f : 78f);
                Color outer = _slashCombo == 3 ? new Color(1.0f, 0.18f, 0.72f) : new Color(0.12f, 0.95f, 1.0f);
                CyberVFX.SpawnCrescentSlash(
                    this, arcPos, playerCamera.transform.rotation,
                    rollDeg: roll, pitchDeg: 0f, sweepDeg: 145f,
                    outerRadius: 2.15f, innerRadius: 1.15f,
                    coreColor: Color.white, outerColor: outer,
                    duration: 0.16f, reverseSweep: _slashCombo == 2
                );
            }

            Vector3 camOrigin = playerCamera != null ? playerCamera.transform.position : transform.position + Vector3.up * 1.4f;
            Vector3 camFwd = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Vector3 hitCenter = camOrigin + camFwd * 2.0f;

            bool anyHit = ApplyAreaAttack(hitCenter, 2.5f, camFwd, arenaDmg: _slashCombo == 3 ? 34 : 26, enemyUnitDmg: 2, allowDeflect: true, out bool deflected);
            if (deflected) CyberAudio.Play("deflect", 0.9f);
            if (anyHit && !_cc.isGrounded)
                _velocity.y = Mathf.Max(_velocity.y, 4.5f);
        }

        private void PerformSwordWave()
        {
            _slashAnimTimer = 0.22f;
            CyberAudio.Play("wave", 0.92f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(2);

            Transform aim = playerCamera != null ? playerCamera.transform : transform;
            Vector3 spawnPos = aim.position + aim.forward * 1.45f - aim.up * 0.1f;

            GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wave.name = "SwordWaveProjectile";
            wave.transform.position = spawnPos;
            wave.transform.rotation = aim.rotation;
            wave.transform.localScale = new Vector3(2.8f, 0.20f, 0.95f);

            var bc = wave.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(1.1f, 3.8f, 1.4f);

            var mr = wave.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Color waveCol = playerId == 0 ? new Color(0.14f, 0.95f, 1.0f) : new Color(1.0f, 0.22f, 0.74f);
            Material m = new Material(Shader.Find("Custom/CyberHardSurface") ?? Shader.Find("Sprites/Default"));
            m.SetColor("_Color", waveCol);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", waveCol * 5.0f);
            mr.sharedMaterial = m;

            CyberVFX.SpawnCrescentSlash(
                this, spawnPos, aim.rotation,
                rollDeg: 0f, pitchDeg: 0f, sweepDeg: 165f,
                outerRadius: 2.3f, innerRadius: 1.25f,
                coreColor: Color.white, outerColor: waveCol, duration: 0.20f
            );

            var proj = wave.AddComponent<CyberProjectile>();
            proj.InitCombat(aim.forward, 34f, playerId, this, arenaDamage: 36, enemyUnitDamage: 2, isSwordWave: true, life: 2.2f);
            CyberHUD.ShowCallout("斬撃波！", waveCol);
        }

        private void PerformCycloneSlash()
        {
            _slashAnimTimer = 0.28f;
            CyberAudio.Play("cyclone", 0.95f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(2);

            Vector3 center = transform.position + Vector3.up * 1.15f;
            Color cycCol = new Color(1.0f, 0.22f, 0.78f);

            CyberVFX.SpawnCrescentSlash(this, center, transform.rotation, 6f, 0f, 355f, 3.6f, 1.7f, Color.white, new Color(0.15f, 0.95f, 1f), 0.26f);
            CyberVFX.SpawnCrescentSlash(this, center + Vector3.up * 0.25f, transform.rotation, -12f, 0f, 355f, 3.8f, 1.9f, Color.white, cycCol, 0.28f, true);
            CyberVFX.SpawnRingPulse(this, center, cycCol, 4.2f, 0.25f);

            Vector3 fwd = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            ApplyAreaAttack(center, 4.1f, fwd, arenaDmg: 34, enemyUnitDmg: 2, allowDeflect: true, out bool deflected);
            if (deflected) CyberAudio.Play("deflect", 0.95f);

            if (!_cc.isGrounded)
                _velocity.y = Mathf.Max(_velocity.y, 7.2f);
            CyberHUD.ShowCallout("旋風斬！", cycCol);
        }

        private void TriggerDiveSlamImpact()
        {
            Vector3 center = transform.position + Vector3.up * 0.45f;
            Color slamCol = new Color(1f, 0.24f, 0.72f);
            CyberAudio.Play("slam", 0.95f);
            CyberVFX.SpawnRingPulse(this, center, slamCol, 6.2f, 0.34f);
            CyberVFX.SpawnShatterExplosion(this, center, slamCol, 14);
            ApplyAreaAttack(center, 5.4f, transform.forward, arenaDmg: 42, enemyUnitDmg: 2, allowDeflect: true, out _);
        }

        private void FireBlasterBolt(float spreadDeg, int arenaDmg, int enemyUnitDmg)
        {
            _blasterCooldown = 0.145f;
            _blasterRecoil = 1.0f;
            CyberAudio.Play("blaster", 0.72f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(4);

            Transform aim = playerCamera != null ? playerCamera.transform : transform;
            Vector3 targetPoint = aim.position + aim.forward * 80f;
            if (Physics.Raycast(aim.position + aim.forward * 0.6f, aim.forward, out RaycastHit aimHit, 100f))
            {
                if (aimHit.collider.GetComponentInParent<CyberParkourController>() != this)
                    targetPoint = aimHit.point;
            }

            Vector3 muzzle = aim.position + aim.forward * 0.95f + aim.right * 0.22f - aim.up * 0.13f;
            Vector3 baseDir = (targetPoint - muzzle).normalized;
            Quaternion jitter = spreadDeg > 0f
                ? Quaternion.Euler(Random.Range(-spreadDeg, spreadDeg), Random.Range(-spreadDeg, spreadDeg), 0f)
                : Quaternion.identity;
            Vector3 dir = (jitter * baseDir).normalized;

            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "PlayerPlasmaBolt";
            bolt.transform.position = muzzle;
            bolt.transform.localScale = new Vector3(0.26f, 0.26f, 0.95f);
            bolt.transform.rotation = Quaternion.LookRotation(dir);

            var sc = bolt.GetComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.85f;

            var mr = bolt.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Color c = playerId == 0 ? new Color(0.15f, 0.98f, 1f) : new Color(1f, 0.25f, 0.72f);
            Material m = new Material(Shader.Find("Custom/CyberHardSurface") ?? Shader.Find("Sprites/Default"));
            m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 5.2f);
            mr.sharedMaterial = m;

            var proj = bolt.AddComponent<CyberProjectile>();
            proj.InitCombat(dir, 62f, playerId, this, arenaDamage: arenaDmg, enemyUnitDamage: enemyUnitDmg, isSwordWave: false, life: 2.2f);
        }

        private void FireRailgunBeam()
        {
            _blasterRecoil = 1.8f;
            CyberAudio.Play("railgun", 0.95f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(4);

            Transform aim = playerCamera != null ? playerCamera.transform : transform;
            Vector3 start = aim.position + aim.forward * 1.1f + aim.right * 0.25f - aim.up * 0.14f;
            Vector3 end = aim.position + aim.forward * 85f;

            var hits = Physics.SphereCastAll(aim.position + aim.forward * 0.6f, 0.30f, aim.forward, 85f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var railHits = new System.Collections.Generic.HashSet<int>();
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                var self = h.collider.GetComponentInParent<CyberParkourController>();
                if (self == this) continue;

                if (h.collider.GetComponentInParent<CyberEnemy>() is CyberEnemy en && !en.IsDead && railHits.Add(en.gameObject.GetInstanceID()))
                {
                    en.TakeSliceDamage(this, 3);
                    CyberHUD.TriggerHitmarker("レールガン命中！", new Color(1f, 0.85f, 0.25f));
                }
                else if (h.collider.GetComponentInParent<CyberBotAI>() is CyberBotAI bot && !bot.IsDead && railHits.Add(bot.gameObject.GetInstanceID()))
                {
                    bot.TakeCombatDamage(48, this, playerId);
                    CyberHUD.TriggerHitmarker("レールガン -48!", new Color(1f, 0.85f, 0.25f));
                }
                else if (h.collider.GetComponentInParent<RemotePvpAvatar>() != null && railHits.Add(h.collider.transform.root.gameObject.GetInstanceID()))
                {
                    LanPvpManager.Instance?.QueueOutgoingDamage(48);
                    CyberHUD.TriggerHitmarker("レールガン -48!", new Color(1f, 0.85f, 0.25f));
                }
                else if (self != null && !self.IsArenaDead && railHits.Add(self.gameObject.GetInstanceID()))
                {
                    self.TakeCombatDamage(48, this);
                    CyberHUD.TriggerHitmarker("レールガン -48!", new Color(1f, 0.85f, 0.25f));
                }
                else if (!h.collider.isTrigger)
                {
                    end = h.point;
                    break;
                }
            }

            StartCoroutine(ShowRailgunBeamRoutine(start, end));
            CyberHUD.ShowCallout("レールガン発射！", new Color(1f, 0.85f, 0.25f));
        }

        private IEnumerator ShowRailgunBeamRoutine(Vector3 start, Vector3 end)
        {
            GameObject go = new GameObject("RailgunBeamVFX");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.22f;
            lr.endWidth = 0.08f;
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            mat.color = new Color(0.22f, 0.96f, 1.0f);
            lr.sharedMaterial = mat;

            float t = 0f;
            while (t < 0.20f)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - (t / 0.20f);
                lr.startWidth = 0.22f * k;
                lr.endWidth = 0.08f * k;
                yield return null;
            }
            Destroy(go);
        }

        private void FireScatterBurst()
        {
            CyberAudio.Play("scatter", 0.9f);
            if (playerId == 0) LanPvpManager.Instance?.NotifyLocalAction(4);
            for (int i = 0; i < 5; i++)
                FireBlasterBolt(4.2f, arenaDmg: 14, enemyUnitDmg: 1);
            _blasterCooldown = 0.35f;
            CyberHUD.ShowCallout("スキャッターショット！", new Color(0.18f, 0.95f, 1f));
        }

        private bool ApplyAreaAttack(Vector3 center, float radius, Vector3 aimFwd, int arenaDmg, int enemyUnitDmg, bool allowDeflect, out bool anyDeflect)
        {
            anyDeflect = false;
            bool anyHit = false;
            var hitObjects = new System.Collections.Generic.HashSet<int>();

            foreach (var col in Physics.OverlapSphere(center, radius))
            {
                if (col == null) continue;

                if (allowDeflect)
                {
                    var proj = col.GetComponent<CyberProjectile>();
                    if (proj != null && !proj.IsDeflected && proj.OwnerActorId != playerId)
                    {
                        proj.Deflect(aimFwd, playerId, this);
                        CyberVFX.TriggerHitStop(this, 0.035f, 0.1f);
                        CyberHUD.TriggerHitmarker("弾き返し！", new Color(0.2f, 0.98f, 1f));
                        anyDeflect = true;
                        anyHit = true;
                    }
                }

                var enemy = col.GetComponentInParent<CyberEnemy>();
                if (enemy != null && !enemy.IsDead && hitObjects.Add(enemy.gameObject.GetInstanceID()))
                {
                    if (enemy.TakeSliceDamage(this, enemyUnitDmg))
                    {
                        CyberHUD.TriggerHitmarker("斬撃命中！", new Color(1f, 0.25f, 0.72f));
                        anyHit = true;
                    }
                }

                var bot = col.GetComponentInParent<CyberBotAI>();
                if (bot != null && !bot.IsDead && hitObjects.Add(bot.gameObject.GetInstanceID()))
                {
                    bot.TakeCombatDamage(arenaDmg, this, playerId);
                    CyberHUD.TriggerHitmarker($"斬撃 -{arenaDmg}", new Color(1f, 0.25f, 0.72f));
                    anyHit = true;
                }

                var remote = col.GetComponentInParent<RemotePvpAvatar>();
                if (remote != null && hitObjects.Add(remote.gameObject.GetInstanceID()))
                {
                    LanPvpManager.Instance?.QueueOutgoingDamage(arenaDmg);
                    CyberHUD.TriggerHitmarker($"斬撃 -{arenaDmg}", new Color(1f, 0.25f, 0.72f));
                    anyHit = true;
                }

                var opp = col.GetComponentInParent<CyberParkourController>();
                if (opp != null && opp != this && !opp.IsArenaDead && hitObjects.Add(opp.gameObject.GetInstanceID()))
                {
                    opp.TakeCombatDamage(arenaDmg, this);
                    CyberHUD.TriggerHitmarker($"P{playerId + 1} 命中 -{arenaDmg}", new Color(1f, 0.25f, 0.72f));
                    anyHit = true;
                }
            }
            return anyHit;
        }

        private IEnumerator TrailBurstRoutine()
        {
            _katanaTrail.emitting = true;
            yield return new WaitForSecondsRealtime(0.22f);
            if (_katanaTrail != null) _katanaTrail.emitting = false;
        }

        private void UpdateCameraVisuals()
        {
            if (playerCamera == null) return;
            float speedFactor = Mathf.Clamp01((HorizontalSpeed - 12f) / 18f);
            float targetFov = _isSlowMo ? 62f : (_isDashing ? 86f : Mathf.Lerp(68f, 82f, speedFactor));
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 10f * Time.unscaledDeltaTime);

            if (cameraHolder != null)
            {
                float targetY = _isSliding ? 0.85f : 1.62f;
                Vector3 lp = cameraHolder.localPosition;
                lp.y = Mathf.Lerp(lp.y, targetY, 14f * Time.unscaledDeltaTime);
                cameraHolder.localPosition = lp;
            }
        }

        private void UpdateWeaponViewmodels()
        {
            float speed = HorizontalSpeed;
            float bobX = 0f, bobY = 0f;
            if (speed > 2f && (_cc.isGrounded || _isWallRunning))
            {
                float freq = _isWallRunning ? 12f : 9.5f;
                bobX = Mathf.Cos(Time.time * freq * 0.5f) * 0.025f;
                bobY = Mathf.Sin(Time.time * freq) * 0.022f;
            }

            if (katanaPivot != null && _weaponMode == CyberWeaponMode.Katana)
            {
                Vector3 targetPos = _katanaRestLocalPos + new Vector3(bobX, bobY, 0f);
                Quaternion targetRot = _katanaRestLocalRot;

                if (_slashAnimTimer > 0f)
                {
                    _slashAnimTimer -= Time.unscaledDeltaTime;
                    float p = 1f - Mathf.Clamp01(_slashAnimTimer / 0.20f);
                    float swing = Mathf.Sin(p * Mathf.PI);

                    if (_slashCombo == 1)
                    {
                        targetPos = Vector3.Lerp(new Vector3(0.45f, -0.20f, 0.62f), new Vector3(-0.48f, -0.35f, 0.74f), p);
                        targetRot = Quaternion.Euler(18f, -75f * p + 25f, 65f);
                    }
                    else if (_slashCombo == 2)
                    {
                        targetPos = Vector3.Lerp(new Vector3(-0.42f, -0.35f, 0.64f), new Vector3(0.45f, -0.15f, 0.72f), p);
                        targetRot = Quaternion.Euler(-15f, 70f * p - 25f, -68f);
                    }
                    else
                    {
                        targetPos = new Vector3(0.05f, Mathf.Lerp(0.18f, -0.45f, p), 0.76f);
                        targetRot = Quaternion.Euler(Mathf.Lerp(-68f, 45f, p), 0f, swing * 25f);
                    }

                    katanaPivot.localPosition = targetPos;
                    katanaPivot.localRotation = targetRot;
                }
                else
                {
                    if (_isWallRunning) targetRot *= Quaternion.Euler(0f, 0f, _wallSide * 18f);
                    katanaPivot.localPosition = Vector3.Lerp(katanaPivot.localPosition, targetPos, 14f * Time.unscaledDeltaTime);
                    katanaPivot.localRotation = Quaternion.Slerp(katanaPivot.localRotation, targetRot, 14f * Time.unscaledDeltaTime);
                }
            }

            if (blasterPivot != null && _weaponMode == CyberWeaponMode.Blaster)
            {
                _blasterRecoil = Mathf.Lerp(_blasterRecoil, 0f, 16f * Time.unscaledDeltaTime);
                Vector3 bPos = _blasterRestLocalPos + new Vector3(bobX * 0.7f, bobY * 0.7f + _blasterRecoil * 0.02f, -_blasterRecoil * 0.06f);
                Quaternion bRot = _blasterRestLocalRot * Quaternion.Euler(-_blasterRecoil * 12f, 0f, _isWallRunning ? (_wallSide * 14f) : 0f);
                blasterPivot.localPosition = Vector3.Lerp(blasterPivot.localPosition, bPos, 16f * Time.unscaledDeltaTime);
                blasterPivot.localRotation = Quaternion.Slerp(blasterPivot.localRotation, bRot, 16f * Time.unscaledDeltaTime);
            }
        }
    }
}
