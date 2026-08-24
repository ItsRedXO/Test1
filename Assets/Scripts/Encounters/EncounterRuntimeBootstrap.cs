using System.Collections.Generic;
using System.Linq;
using ActionRPG.Combat;
using ActionRPG.Enemy;
using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Temporary bridge for the current scene format. Builds a room encounter at runtime
    /// without modifying the Unity scene file. The existing enemies remain where the level
    /// designer placed them; they are tracked as one room instead of being respawned in waves.
    /// </summary>
    public sealed class EncounterRuntimeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindAnyObjectByType<EncounterRuntimeBootstrap>() != null) return;
            new GameObject("EncounterRuntimeBootstrap").AddComponent<EncounterRuntimeBootstrap>();
        }

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Start()
        {
            var melee = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var ranged = FindObjectsByType<RangedEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var enemies = melee.Select(enemy => enemy.GetComponent<Health>())
                .Concat(ranged.Select(enemy => enemy.GetComponent<Health>()))
                .Where(health => health != null)
                .Distinct()
                .ToArray();

            Debug.Log($"[Encounter][Bootstrap] Building room encounter from {enemies.Length} scene enemies.");
            if (enemies.Length == 0)
            {
                Debug.LogWarning("[Encounter][Bootstrap] No trackable scene enemies found.");
                return;
            }

            BuildRoomEncounter(enemies);
        }

        private static void BuildRoomEncounter(Health[] enemies)
        {
            Vector3 center = Vector3.zero;
            foreach (Health enemy in enemies) center += enemy.transform.position;
            center /= enemies.Length;

            var root = new GameObject("Encounter_01");
            var manager = root.AddComponent<EncounterManager>();
            manager.ConfigureRuntime(enemies);

            var zone = new GameObject("EncounterZone");
            zone.transform.SetParent(root.transform);
            zone.transform.position = center;

            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(24f, 6f, 24f);

            var zoneComponent = zone.AddComponent<EncounterZone>();
            zoneComponent.SetManager(manager);

            Debug.Log($"[Encounter][Bootstrap] Room ready. Center={center}, Zone={box.size}, Enemies={enemies.Length}.");
        }
    }
}
