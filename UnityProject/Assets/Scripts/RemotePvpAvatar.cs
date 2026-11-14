using UnityEngine;

namespace CyberRush
{
    public class RemotePvpAvatar : MonoBehaviour
    {
        public int remoteHp = 100;
        public int remoteKills = 0;

        private Vector3 _targetPos;
        private float _targetYaw;
        private bool _hasTarget;

        public void SetPose(Vector3 pos, float yaw, int hp, int kills)
        {
            if (!_hasTarget)
            {
                transform.position = pos;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                _hasTarget = true;
            }
            _targetPos = pos;
            _targetYaw = yaw;
            remoteHp = hp;
            remoteKills = kills;
        }

        public void PlayRemoteAction(int actionFlags, float pitch)
        {
            Vector3 chest = transform.position + Vector3.up * 1.3f;
            Quaternion aimRot = Quaternion.Euler(pitch, _targetYaw, 0f);

            if ((actionFlags & 1) != 0) // Katana Slash
            {
                CyberVFX.SpawnCrescentSlash(
                    this, chest + transform.forward * 1.1f, aimRot,
                    rollDeg: -20f, pitchDeg: 0f, sweepDeg: 135f,
                    outerRadius: 2.1f, innerRadius: 1.15f,
                    coreColor: Color.white, outerColor: new Color(1f, 0.22f, 0.72f), duration: 0.16f
                );
                CyberAudio.Play("slash", 0.55f);
            }
            if ((actionFlags & 2) != 0) // Sword Wave / Cyclone
            {
                CyberVFX.SpawnCrescentSlash(
                    this, chest, aimRot,
                    rollDeg: 0f, pitchDeg: 0f, sweepDeg: 340f,
                    outerRadius: 3.1f, innerRadius: 1.5f,
                    coreColor: Color.white, outerColor: new Color(1f, 0.2f, 0.72f), duration: 0.24f
                );
                CyberAudio.Play("wave", 0.65f);
            }
            if ((actionFlags & 4) != 0) // Blaster Shot visual
            {
                Vector3 fwd = aimRot * Vector3.forward;
                CyberVFX.SpawnDashStreaks(this, chest + fwd * 0.9f, aimRot, new Color(1f, 0.22f, 0.72f), 2);
                CyberAudio.Play("blaster", 0.5f);
            }
            if ((actionFlags & 8) != 0) // Dash
            {
                CyberVFX.SpawnDashStreaks(this, chest, transform.rotation, new Color(1f, 0.2f, 0.72f), 5);
            }
        }

        private void Update()
        {
            if (!_hasTarget) return;
            transform.position = Vector3.Lerp(transform.position, _targetPos, 18f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, _targetYaw, 0f), 18f * Time.deltaTime);
        }
    }
}
