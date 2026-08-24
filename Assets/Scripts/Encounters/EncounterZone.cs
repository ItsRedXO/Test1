using UnityEngine;

namespace ActionRPG.Encounters
{
    [RequireComponent(typeof(Collider))]
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private bool triggerOnlyOnce = true;
        private bool hasTriggered;

        public void SetManager(EncounterManager manager) => encounterManager = manager;

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Encounter][Zone] Trigger entered by {other.name} (tag={other.tag}).");
            if (hasTriggered && triggerOnlyOnce) return;
            if (!other.CompareTag("Player")) return;

            if (encounterManager == null) encounterManager = GetComponentInParent<EncounterManager>();
            if (encounterManager == null)
            {
                Debug.LogWarning("[Encounter][Zone] No EncounterManager assigned.", this);
                return;
            }

            bool started = encounterManager.TryStartEncounter();
            Debug.Log($"[Encounter][Zone] Player detected. Start result={started}.");
            if (started) hasTriggered = true;
        }
    }
}