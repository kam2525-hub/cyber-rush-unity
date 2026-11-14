using System.Collections.Generic;
using UnityEngine;

namespace CyberRush
{
    public class CyberProjectile : MonoBehaviour
    {
        private Vector3 _dir;
        private float _speed;
        private bool _deflected;
        private CyberEnemy _ownerEnemy;
        private CyberParkourController _ownerPlayer;
        private int _ownerActorId = -1;
        private int _arenaDamage = 18;
        private int _enemyUnitDamage = 1;
        private bool _isSwordWave;
        private float _life = 4.5f;
        private readonly HashSet<int> _hitIds = new HashSet<int>();

        public bool IsDeflected => _deflected;
        public int OwnerActorId => _ownerActorId;

        public void Init(Vector3 dir, float speed, CyberEnemy owner)
        {
            _dir = dir.normalized;
            _speed = speed;
            _ownerEnemy = owner;
            _ownerActorId = -1;
            _arenaDamage = 20;
            _enemyUnitDamage = 1;
        }

        public void InitCombat(
            Vector3 dir,
            float speed,
            int ownerActorId,
            CyberParkourController ownerPlayer,
            int arenaDamage,
            int enemyUnitDamage,
            bool isSwordWave = false,
            float life = 3.2f)
        {
            _dir = dir.normalized;
            _speed = speed;
            _ownerActorId = ownerActorId;
            _ownerPlayer = ownerPlayer;
            _arenaDamage = arenaDamage;
            _enemyUnitDamage = enemyUnitDamage;
            _isSwordWave = isSwordWave;
            _life = life;
        }

        public void Deflect(Vector3 aimDir, int deflectorActorId = 0, CyberParkourController deflector = null)
        {
            if (_deflected || _isSwordWave) return;
            _deflected = true;
            _speed = 40f;
            _life = 3.0f;
            _ownerActorId = deflectorActorId;
            if (deflector != null) _ownerPlayer = deflector;
            _arenaDamage = Mathf.Max(_arenaDamage, 32);
            _enemyUnitDamage = 2;

            if (_ownerEnemy != null && !_ownerEnemy.IsDead)
            {
                _dir = (_ownerEnemy.transform.position + Vector3.up * 1.1f - transform.position).normalized;
            }
            else
            {
                _dir = aimDir.normalized;
            }

            var mr = GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                Color cyan = new Color(0.25f, 0.96f, 1.0f);
                mr.material.SetColor("_Color", cyan);
                if (mr.material.HasProperty("_EmissionColor")) mr.material.SetColor("_EmissionColor", cyan * 5f);
            }
            transform.localScale = Vector3.one * 0.52f;
        }

        private void Update()
        {
            transform.position += _dir * (_speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            int iid = other.gameObject.GetInstanceID();
            if (_isSwordWave && _hitIds.Contains(iid)) return;

            bool fromPlayerOrDeflected = _deflected || _ownerActorId >= 0;

            // 1. Hit Stage Enemies (Drones, Troopers, Pylons, CoreBoss)
            var enemy = other.GetComponentInParent<CyberEnemy>();
            if (enemy != null)
            {
                if (_ownerActorId == -1 && !_deflected) return;
                if (!enemy.IsDead && fromPlayerOrDeflected)
                {
                    _hitIds.Add(enemy.gameObject.GetInstanceID());
                    CyberParkourController credit = _ownerPlayer != null ? _ownerPlayer : FindFirstObjectByType<CyberParkourController>();
                    enemy.TakeSliceDamage(credit, _enemyUnitDamage);
                    CyberAudio.Play("hit", 0.7f);
                    if (!_isSwordWave) Destroy(gameObject);
                }
                return;
            }

            // 2. Hit AI Cyber Ninja Bots
            var bot = other.GetComponentInParent<CyberBotAI>();
            if (bot != null)
            {
                if (bot.BotActorId == _ownerActorId) return;
                if (!bot.IsDead)
                {
                    _hitIds.Add(bot.gameObject.GetInstanceID());
                    bot.TakeCombatDamage(_arenaDamage, _ownerPlayer, _ownerActorId);
                    CyberAudio.Play("hit", 0.75f);
                    if (!_isSwordWave) Destroy(gameObject);
                }
                return;
            }

            // 3. Hit Remote LAN PvP Avatar (send damage packet via LanPvpManager)
            var remote = other.GetComponentInParent<RemotePvpAvatar>();
            if (remote != null)
            {
                if (_ownerActorId == 99) return;
                _hitIds.Add(remote.gameObject.GetInstanceID());
                if (LanPvpManager.Instance != null)
                {
                    LanPvpManager.Instance.QueueOutgoingDamage(_arenaDamage);
                    CyberAudio.Play("hit", 0.8f);
                    CyberHUD.TriggerHitmarker($"HIT -{_arenaDamage}", new Color(0.18f, 0.95f, 1f));
                }
                if (!_isSwordWave) Destroy(gameObject);
                return;
            }

            // 4. Hit Players (including Opposing Split-Screen Player!)
            var pc = other.GetComponentInParent<CyberParkourController>();
            if (pc != null)
            {
                if (pc.playerId == _ownerActorId) return; // Don't hit self!
                _hitIds.Add(pc.gameObject.GetInstanceID());
                pc.TakeCombatDamage(_arenaDamage, _ownerPlayer);
                if (!_isSwordWave) Destroy(gameObject);
                return;
            }

            // 5. Hit Arena/Stage World Geometry
            if (!other.isTrigger)
            {
                if (!_isSwordWave)
                    Destroy(gameObject);
            }
        }
    }
}
