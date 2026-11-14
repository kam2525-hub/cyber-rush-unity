using UnityEngine;

namespace CyberRush
{
    public class JumpPad : MonoBehaviour
    {
        public float launchSpeed = 19.5f;

        private void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<CyberParkourController>();
            if (pc != null)
            {
                pc.ApplyJumpPadBoost(launchSpeed);
                CyberAudio.Play("jumppad", 0.8f);
                CyberVFX.SpawnRingPulse(this, transform.position + Vector3.up * 0.35f, new Color(0.15f, 0.95f, 1f), 4.2f, 0.25f);
            }
        }
    }
}
