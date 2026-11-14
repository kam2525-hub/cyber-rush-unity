using System.Collections;
using UnityEngine;

namespace CyberRush
{
    [RequireComponent(typeof(CharacterController))]
    public class CyberBotAI : MonoBehaviour
    {
        public string botName = "CYBER NINJA";
        public int botIndex = 0;
        public int maxHp = 100;
        public float moveSpeed = 12.2f;
        public float slashRange = 3.1f;
        public float shootCooldown = 1.15f;
        public float slashCooldown = 0.85f;
        public Color accentColor = new Color(1.0f, 0.22f, 0.68f);

        public int BotActorId => -2 - botIndex;
        public int CurrentHp { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public bool IsDead { get; private set; }

        private CharacterController _cc;
        private Vector3 _spawnPos;
        private Quaternion _spawnRot;
        private Vector3 _velocity;
        private float _shootTimer;
        private float _slashTimer;
        private float _dashTimer;
        private float _strafeSign = 1f;
        private float _strafeFlipTimer;
        private bool _isActive;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
            CurrentHp = maxHp;
        }

        public void SetArenaActive(bool active)
        {
            _isActive = active;
            gameObject.SetActive(active);
            if (active) ResetBot(fullReset: true);
        }

        public void ResetBot(bool fullReset)
        {
            StopAllCoroutines();
            IsDead = false;
            CurrentHp = maxHp;
            if (fullReset)
            {
                Kills = 0;
                Deaths = 0;
            }
            _shootTimer = Random.Range(0.4f, shootCooldown);
            _slashTimer = 0.4f;
            _dashTimer = Random.Range(1.5f, 3.0f);
            _velocity = Vector3.zero;

            if (_cc != null) _cc.enabled = false;
            transform.position = _spawnPos;
            transform.rotation = _spawnRot;
            if (_cc != null) _cc.enabled = true;

            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
        }

        private void Update()
        {
            if (!_isActive || IsDead) return;
            var sm = StageManager.Instance;
            if (sm == null || sm.ActiveMode != GameMode.VsCpuArena || sm.IsMenuOpen || sm.IsOverlayOpen) return;

            var target = FindTargetPlayer();
            if (target == null) return;

            float dt = Time.deltaTime;
            Vector3 toTarget = target.transform.position - transform.position;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = flatToTarget.magnitude;
            Vector3 fwd = dist > 0.05f ? flatToTarget / dist : transform.forward;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd), 10f * dt);

            _strafeFlipTimer -= dt;
            if (_strafeFlipTimer <= 0f)
            {
                _strafeFlipTimer = Random.Range(1.2f, 2.4f);
                _strafeSign = -_strafeSign;
            }

            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 wish = Vector3.zero;
            if (dist > 7.5f)
                wish = fwd + right * (_strafeSign * 0.45f);
            else if (dist < 2.2f)
                wish = right * _strafeSign - fwd * 0.35f;
            else
                wish = fwd * 0.55f + right * (_strafeSign * 0.85f);

            if (wish.sqrMagnitude > 0.01f) wish.Normalize();

            // Periodic evasion dash + parkour hop
            _dashTimer -= dt;
            if (_dashTimer <= 0f && dist < 24f)
            {
                _dashTimer = Random.Range(2.4f, 4.0f);
                Vector3 dashDir = (right * _strafeSign + fwd * 0.35f).normalized;
                _velocity.x = dashDir.x * 22f;
                _velocity.z = dashDir.z * 22f;
                if (_cc.isGrounded && Random.value > 0.35f)
                    _velocity.y = 9.5f;
                CyberVFX.SpawnDashStreaks(this, transform.position + Vector3.up * 1.0f, Quaternion.LookRotation(dashDir), accentColor, 5);
            }
            else
            {
                _velocity.x = Mathf.Lerp(_velocity.x, wish.x * moveSpeed, 7.5f * dt);
                _velocity.z = Mathf.Lerp(_velocity.z, wish.z * moveSpeed, 7.5f * dt);
            }

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2.5f;
                if (toTarget.y > 2.2f && Random.value < 0.04f)
                    _velocity.y = 11.0f;
            }
            else
            {
                _velocity.y -= 24f * dt;
            }

            _cc.Move(_velocity * dt);

            // Arena safety fall recovery
            if (transform.position.y < -15f)
            {
                if (_cc != null) _cc.enabled = false;
                transform.position = _spawnPos;
                if (_cc != null) _cc.enabled = true;
                _velocity = Vector3.zero;
            }

            // Close-range Katana Crescent Slash vs Mid-range Blaster Shot
            _slashTimer -= dt;
            _shootTimer -= dt;

            if (dist <= slashRange && Mathf.Abs(toTarget.y) < 2.4f && _slashTimer <= 0f)
            {
                _slashTimer = slashCooldown;
                PerformBotSlash(target);
            }
            else if (dist > slashRange * 0.9f && dist < 38f && _shootTimer <= 0f)
            {
                _shootTimer = shootCooldown + Random.Range(-0.15f, 0.25f);
                FireBotBlaster(target);
            }
        }

        private CyberParkourController FindTargetPlayer()
        {
            foreach (var p in FindObjectsByType<CyberParkourController>(FindObjectsSortMode.None))
            {
                if (p.playerId == 0 && p.gameObject.activeInHierarchy && !p.IsArenaDead)
                    return p;
            }
            return null;
        }

        private void PerformBotSlash(CyberParkourController target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.25f + transform.forward * 0.9f;
            CyberVFX.SpawnCrescentSlash(
                this, origin, transform.rotation,
                rollDeg: Random.Range(-30f, 30f), pitchDeg: 0f, sweepDeg: 130f,
                outerRadius: 2.1f, innerRadius: 1.15f,
                coreColor: Color.white, outerColor: accentColor, duration: 0.16f
            );
            CyberAudio.Play("slash", 0.55f);

            if (Vector3.Distance(transform.position, target.transform.position) <= slashRange + 0.4f)
            {
                bool died = target.TakeCombatDamage(22, null);
                if (died) Kills++;
            }
        }

        private void FireBotBlaster(CyberParkourController target)
        {
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.75f;
            Vector3 aimPoint = target.transform.position + Vector3.up * 1.15f + Random.insideUnitSphere * 0.45f;
            Vector3 dir = (aimPoint - muzzle).normalized;

            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "BotPlasmaBolt";
            bolt.transform.position = muzzle;
            bolt.transform.localScale = Vector3.one * 0.34f;
            var sc = bolt.GetComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.85f;

            var mr = bolt.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material m = new Material(Shader.Find("Custom/CyberHardSurface") ?? Shader.Find("Sprites/Default"));
            m.SetColor("_Color", accentColor);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", accentColor * 4.5f);
            mr.sharedMaterial = m;

            var proj = bolt.AddComponent<CyberProjectile>();
            proj.InitCombat(dir, 26f, BotActorId, null, arenaDamage: 14, enemyUnitDamage: 1, isSwordWave: false, life: 3.2f);
            CyberAudio.Play("blaster", 0.45f);
        }

        public void TakeCombatDamage(int dmg, CyberParkourController attacker, int attackerActorId)
        {
            if (IsDead || !_isActive) return;

            CurrentHp = Mathf.Max(0, CurrentHp - dmg);
            CyberVFX.SpawnShatterExplosion(this, transform.position + Vector3.up * 1.1f, accentColor, 5);
            CyberAudio.Play("hit", 0.85f);

            if (CurrentHp <= 0)
            {
                IsDead = true;
                Deaths++;
                if (attacker != null)
                {
                    attacker.OnArenaKillScored();
                    attacker.OnKillDashReset();
                }
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.OnArenaBotKilled(this);
                }

                CyberAudio.Play("explosion", 0.85f);
                CyberVFX.SpawnShatterExplosion(this, transform.position + Vector3.up * 1.1f, accentColor, 16);
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
                foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
                StartCoroutine(RespawnCoroutine());
            }
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(2.2f);
            if (_isActive)
                ResetBot(fullReset: false);
        }
    }
}
