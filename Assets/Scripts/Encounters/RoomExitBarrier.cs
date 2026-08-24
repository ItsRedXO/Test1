using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Simple physical progression barrier. It blocks the room exit while the encounter is active
    /// and becomes passable immediately when the encounter is cleared.
    /// </summary>
    public sealed class RoomExitBarrier : MonoBehaviour
    {
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private Renderer barrierRenderer;
        [SerializeField] private EncounterManager encounterManager;

        public void ConfigureRuntime(Collider blocker, Renderer visual)
        {
            blockingCollider = blocker;
            barrierRenderer = visual;
        }

        public void Bind(EncounterManager manager)
        {
            if (encounterManager == manager) return;
            if (encounterManager != null) encounterManager.OnEncounterCleared -= OpenBarrier;

            encounterManager = manager;
            if (encounterManager != null)
            {
                encounterManager.OnEncounterCleared += OpenBarrier;
                if (encounterManager.CurrentState == EncounterState.Cleared) OpenBarrier();
            }
        }

        public void OpenBarrier()
        {
            if (blockingCollider != null) blockingCollider.enabled = false;
            if (barrierRenderer != null) barrierRenderer.enabled = false;
            Debug.Log("[Encounter][Barrier] Exit opened.");
        }

        private void OnDestroy()
        {
            if (encounterManager != null) encounterManager.OnEncounterCleared -= OpenBarrier;
        }
    }
}
