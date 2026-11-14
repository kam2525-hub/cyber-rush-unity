using UnityEngine;

namespace CyberRush
{
    public class GrappleAnchor : MonoBehaviour
    {
        public float range = 30f;

        private void Update()
        {
            transform.Rotate(Vector3.up, 55f * Time.deltaTime);
        }
    }
}
