using System.Collections;
using UnityEngine;

namespace CyberRush
{
    public enum EnemyKind
    {
        Drone,
        Trooper,
        ShieldPylon,
        CoreBoss
    }

    public class CyberEnemy : MonoBehaviour
    {
        public EnemyKind kind = EnemyKind.Drone;
        public int stageIndex = 1;
        public float fireInterval = 2.1f;
        public float detectRange = 36f;
        public int maxHp = 1;
        public bool isDynamicSurvivalEnemy = false;

        public bool IsDead { get; private set; }
        public int CurrentHp { get; private set; }

        private Vector3 _spawnPos;
        private Quaternion _spawnRot;
        private Vector3 _baseScale = Vector3.one;
        private float _fireTimer;
        private Transform _player;

        private void Start()
        {
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
            _baseScale = transform.localScale;
            CurrentHp = maxHp;
            _fireTimer = Random.Range(0.5f, fireInterval);
            var sm = StageManager.Instance;
            if (sm != null && sm.Player1 != null)
                _player = sm.Player1.transform;
            else
            {
                var pc = FindFirstObjectByType<CyberParkourController>();
                if (pc != null) _player = pc.transform;
            }
        }

        public void ResetEnemy()
        {
            IsDead = false;
            CurrentHp = maxHp;
            transform.position = _spawnPos;
            transform.rotation = _spawnRot;
            if (_player == null && StageManager.Instance != null && StageManager.Instance.Player1 != null)
                _player = StageManager.Instance.Player1.transform;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
            _fireTimer = Random.Range(0.4f, fireInterval);
        }

        private void Update()
        {
            if (IsDead) return;
            if (_player == null && StageManager.Instance != null && StageManager.Instance.Player1 != null)
                _player = StageManager.Instance.Player1.transform;
            if (_player == null) return;

            var sm = StageManager.Instance;
            if (sm != null)
            {
                if (sm.ActiveMode == GameMode.MainMenu || sm.IsOverlayOpen) return;
                if (sm.ActiveMode == GameMode.RhythmSurvival)
                {
                    if (!isDynamicSurvivalEnemy) return;
                }
                else if (sm.ActiveMode == GameMode.BossRush)
                {
                    if (stageIndex != 3) return;
                }
                else if (sm.CurrentStage != stageIndex)
                {
                    return;
                }
            }

            // Visual Beat Sync Pulse!
            float beatPulse = CyberRhythm.Instance != null ? CyberRhythm.Instance.BeatPulse : 0f;
            transform.localScale = _baseScale * (1f + beatPulse * 0.14f);

            float dt = Time.deltaTime;
            Vector3 toPlayer = (_player.position + Vector3.up * 0.9f) - transform.position;
            float dist = toPlayer.magnitude;

            if (kind == EnemyKind.Drone)
            {
                if (isDynamicSurvivalEnemy && dist > 6.5f)
                {
                    Vector3 approach = toPlayer.normalized;
                    approach.y *= 0.35f;
                    transform.position += approach * (7.2f * dt);
                }
                else if (!isDynamicSurvivalEnemy)
                {
                    float hover = Mathf.Sin(Time.time * 3.0f + _spawnPos.x) * 0.28f;
                    Vector3 pos = _spawnPos;
                    pos.y += hover;
                    transform.position = pos;
                }

                if (dist < detectRange)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * dt);
                    TryShoot(toPlayer.normalized, dt);
                }
            }
            else if (kind == EnemyKind.Trooper)
            {
                if (isDynamicSurvivalEnemy && dist > 4.5f)
                {
                    Vector3 step = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
                    transform.position += step * (5.2f * dt);
                }
                if (dist < detectRange)
                {
                    Vector3 flatDir = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
                    if (flatDir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDir), 7f * dt);
                    TryShoot(toPlayer.normalized, dt);
                }
            }
            else if (kind == EnemyKind.ShieldPylon)
            {
                transform.Rotate(Vector3.up, 75f * dt);
            }
            else if (kind == EnemyKind.CoreBoss)
            {
                transform.Rotate(Vector3.up, 42f * dt);
                if (dist < 42f)
                {
                    TryShoot(toPlayer.normalized, dt);
                }
            }
        }

        private void TryShoot(Vector3 dir, float dt)
        {
            _fireTimer -= dt;
            if (_fireTimer > 0f) return;
            _fireTimer = kind == EnemyKind.CoreBoss ? 1.1f : fireInterval;

            Vector3 muzzle = transform.position + Vector3.up * (kind == EnemyKind.Trooper ? 1.45f : (kind == EnemyKind.CoreBoss ? 2.4f : 1.15f)) + dir * 0.9f;
            SpawnBolt(muzzle, dir);
        }

        private void SpawnBolt(Vector3 pos, Vector3 dir)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "EnemyPlasmaBolt";
            bolt.transform.position = pos;
            bolt.transform.localScale = Vector3.one * 0.42f;
            var sc = bolt.GetComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.85f;

            var mr = bolt.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material m = new Material(Shader.Find("Custom/CyberHardSurface") ?? Shader.Find("Sprites/Default"));
            Color crimson = new Color(1.0f, 0.14f, 0.22f);
            m.SetColor("_Color", crimson);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", crimson * 4.5f);
            mr.sharedMaterial = m;

            var proj = bolt.AddComponent<CyberProjectile>();
            proj.Init(dir, kind == EnemyKind.CoreBoss ? 22f : 16f, this);
        }

        public bool CanBeSliced()
        {
            if (IsDead) return false;
            if (kind == EnemyKind.CoreBoss)
            {
                return StageManager.Instance == null || StageManager.Instance.AreBossPylonsCleared();
            }
            return true;
        }

        public bool TakeSliceDamage(CyberParkourController attacker, int dmg = 1, BeatGrade beatGrade = BeatGrade.Good)
        {
            if (IsDead) return false;

            if (!CanBeSliced())
            {
                CyberVFX.SpawnRingPulse(this, transform.position + Vector3.up * 2.2f, new Color(1f, 0.2f, 0.65f), 5.5f, 0.25f);
                CyberHUD.ShowCallout("シールド展開中！ パイロンを破壊せよ", new Color(1f, 0.35f, 0.6f));
                return false;
            }

            CurrentHp -= dmg;
            if (CurrentHp > 0)
            {
                CyberAudio.Play("hit", 0.9f);
                CyberVFX.SpawnShatterExplosion(this, transform.position + Vector3.up * 2.2f, new Color(1f, 0.2f, 0.7f), 8);
                CyberVFX.TriggerHitStop(this, 0.035f, 0.1f);
                return true;
            }

            IsDead = true;
            bool isBoss = kind == EnemyKind.CoreBoss;
            bool onBeat = beatGrade != BeatGrade.OffBeat;
            CyberAudio.Play(isBoss ? "boss_kill" : "kill_deluxe", isBoss ? 1.15f : (onBeat ? 1.08f : 0.90f), 0.04f);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Color fxCol = onBeat
                ? new Color(0.18f, 0.98f, 1.0f)
                : (kind == EnemyKind.Drone ? new Color(1.0f, 0.22f, 0.45f) : new Color(1.0f, 0.18f, 0.72f));

            CyberVFX.SpawnShatterExplosion(this, transform.position + Vector3.up * 1.2f, fxCol, isBoss ? 38 : (onBeat ? 24 : 16));
            CyberVFX.SpawnRingPulse(this, transform.position + Vector3.up * 1.0f, fxCol, isBoss ? 10.5f : (onBeat ? 6.2f : 4.6f), isBoss ? 0.55f : 0.34f);
            CyberVFX.TriggerStylishKillSlowMo(isBoss);

            CyberRhythm.Instance?.RegisterKillScore(beatGrade, isBoss);

            if (attacker != null)
            {
                attacker.OnKillDashReset();
                if (onBeat && attacker.isArenaFighter)
                    attacker.HealArenaHp(isBoss ? 35 : 12);
            }

            if (StageManager.Instance != null)
            {
                StageManager.Instance.RegisterKill(this);
            }
            if (isDynamicSurvivalEnemy)
            {
                Destroy(gameObject, 0.1f);
            }
            return true;
        }
    }
}
