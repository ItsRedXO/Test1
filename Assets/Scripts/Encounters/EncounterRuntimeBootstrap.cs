using System.Collections.Generic;
using System.Linq;
using ActionRPG.Enemy;
using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Temporary scene bridge for the current binary-serialized SampleScene.
    /// Builds Encounter_01 at runtime from the enemy templates already placed in the scene,
    /// so no manual scene editing is required.
    /// </summary>
    public sealed class EncounterRuntimeBootstrap : MonoBehaviour
    {
        private readonly List<GameObject> meleeTemplates = new();
        private readonly List<GameObject> rangedTemplates = new();
        private bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindAnyObjectByType<EncounterRuntimeBootstrap>() != null) return;
            new GameObject("EncounterRuntimeBootstrap").AddComponent<EncounterRuntimeBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var melee = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var ranged = FindObjectsByType<RangedEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var enemy in melee) meleeTemplates.Add(enemy.gameObject);
            foreach (var enemy in ranged) rangedTemplates.Add(enemy.gameObject);

            if (meleeTemplates.Count == 0 && rangedTemplates.Count == 0)
            {
                Debug.LogWarning("[Encounter] No scene enemies were found for Encounter_01.");
                return;
            }

            BuildEncounter();
        }

        private void BuildEncounter()
        {
            var allTemplates = meleeTemplates.Concat(rangedTemplates).Distinct().ToList();
            Vector3 center = Vector3.zero;
            foreach (var enemy in allTemplates) center += enemy.transform.position;
            center /= allTemplates.Count;

            var root = new GameObject("Encounter_01");
            var manager = root.AddComponent<EncounterManager>();

            var zone = new GameObject("EncounterZone");
            zone.transform.SetParent(root.transform);
            zone.transform.position = center;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(24f, 6f, 24f);
            var zoneComponent = zone.AddComponent<EncounterZone>();
            zoneComponent.SetManager(manager);

            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(root.transform);
            var spawnPoints = new List<EnemySpawnPoint>();
            for (int i = 0; i < allTemplates.Count; i++)
            {
                var pointObject = new GameObject($"SpawnPoint_{i + 1:00}");
                pointObject.transform.SetParent(spawnRoot.transform);
                pointObject.transform.SetPositionAndRotation(allTemplates[i].transform.position, allTemplates[i].transform.rotation);
                spawnPoints.Add(pointObject.AddComponent<EnemySpawnPoint>());
                allTemplates[i].SetActive(false);
            }

            var wave1 = new List<GameObject>();
            var wave2 = new List<GameObject>();

            for (int i = 0; i < 3 && i < meleeTemplates.Count; i++) wave1.Add(meleeTemplates[i]);
            if (wave1.Count == 0 && rangedTemplates.Count > 0) wave1.Add(rangedTemplates[0]);

            for (int i = 0; i < 2 && i < meleeTemplates.Count; i++) wave2.Add(meleeTemplates[i]);
            if (rangedTemplates.Count > 0) wave2.Add(rangedTemplates[0]);

            manager.ConfigureRuntime(wave1, wave2, spawnPoints.ToArray());

            Debug.Log($"[Encounter] Encounter_01 ready: Wave 1 = {wave1.Count}, Wave 2 = {wave2.Count}. Enter the arena to begin.");
        }
    }
}
