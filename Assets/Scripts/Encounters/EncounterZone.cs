using UnityEngine;

namespace ActionRPG.Encounters
{
    [RequireComponent(typeof(Collider))]
    public class EncounterZone : MonoBehaviour
    {
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private bool triggerOnlyOnce = true;

        private bool hasTriggered;

        public void SetManager(EncounterManager manager)
        {
            encounterManager = manager;
        }

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && triggerOnlyOnce) return;
            if (!other.CompareTag("Player")) return;

            if (encounterManager == null)
            {
                encounterManager = GetComponentInParent<EncounterManager>();
            }

            if (encounterManager == null)
            {
                Debug.LogWarning("[Encounter] EncounterZone has no EncounterManager assigned.", this);
                return;
            }

            if (encounterManager.TryStartEncounter()) hasTriggered = true;
        }
    }
}