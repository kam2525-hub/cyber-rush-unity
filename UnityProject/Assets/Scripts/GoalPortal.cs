using UnityEngine;

namespace CyberRush
{
    public class GoalPortal : MonoBehaviour
    {
        public int stageIndex = 1;

        private void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponentInParent<CyberParkourController>();
            if (pc != null && StageManager.Instance != null)
            {
                StageManager.Instance.OnStageReachedGoal(stageIndex);
            }
        }
    }
}
