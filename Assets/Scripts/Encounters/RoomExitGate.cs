using System.Collections;
using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Reusable dungeon gate. Keeps a room physically sealed until its encounter is cleared,
    /// then raises both gate leaves to open the passage.
    /// </summary>
    public sealed class RoomExitGate : MonoBehaviour
    {
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;
        [SerializeField] private Collider[] blockingColliders;
        [SerializeField] private float openHeight = 4.5f;
        [SerializeField] private float openDuration = 0.55f;

        private Vector3 leftClosedPosition;
        private Vector3 rightClosedPosition;
        private bool isOpening;
        private bool isOpen;

        public void ConfigureRuntime(Transform left, Transform right, Collider[] colliders)
        {
            leftLeaf = left;
            rightLeaf = right;
            blockingColliders = colliders;
            CacheClosedPositions();
        }

        public void Bind(EncounterManager manager)
        {
            if (encounterManager == manager) return;
            if (encounterManager != null) encounterManager.OnEncounterCleared -= Open;
            encounterManager = manager;
            if (encounterManager == null) return;

            encounterManager.OnEncounterCleared += Open;
            if (encounterManager.CurrentState == EncounterState.Cleared) Open();
        }

        public void Open()
        {
            if (isOpen || isOpening) return;
            CacheClosedPositions();
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            isOpening = true;
            foreach (Collider blocker in blockingColliders)
            {
                if (blocker != null) blocker.enabled = false;
            }

            Vector3 leftTarget = leftClosedPosition + Vector3.up * openHeight;
            Vector3 rightTarget = rightClosedPosition + Vector3.up * openHeight;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                if (leftLeaf != null) leftLeaf.localPosition = Vector3.Lerp(leftClosedPosition, leftTarget, t);
                if (rightLeaf != null) rightLeaf.localPosition = Vector3.Lerp(rightClosedPosition, rightTarget, t);
                yield return null;
            }

            if (leftLeaf != null) leftLeaf.localPosition = leftTarget;
            if (rightLeaf != null) rightLeaf.localPosition = rightTarget;
            isOpen = true;
            isOpening = false;
            Debug.Log("[Encounter][Gate] Exit gate opened.");
        }

        private void CacheClosedPositions()
        {
            if (leftLeaf != null) leftClosedPosition = leftLeaf.localPosition;
            if (rightLeaf != null) rightClosedPosition = rightLeaf.localPosition;
        }

        private void OnDestroy()
        {
            if (encounterManager != null) encounterManager.OnEncounterCleared -= Open;
        }
    }
}
