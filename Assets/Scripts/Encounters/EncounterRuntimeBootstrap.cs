using System.Linq;
using ActionRPG.Combat;
using ActionRPG.Enemy;
using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Temporary bridge for the current scene format. Builds a room encounter and its exit barrier
    /// at runtime without modifying the binary Unity scene file. Existing enemies stay where the
    /// level designer placed them; the barrier opens when the room is cleared.
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
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;

            foreach (Health enemy in enemies)
            {
                Vector3 position = enemy.transform.position;
                center += position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                maxZ = Mathf.Max(maxZ, position.z);
            }
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

            CreateExitBarrier(root.transform, manager, center, minX, maxX, maxZ);

            Debug.Log($"[Encounter][Bootstrap] Room ready. Center={center}, Zone={box.size}, Enemies={enemies.Length}.");
        }

        private static void CreateExitBarrier(Transform parent, EncounterManager manager, Vector3 center, float minX, float maxX, float maxZ)
        {
            float width = Mathf.Max(10f, (maxX - minX) + 6f);
            var barrierRoot = new GameObject("ExitBarrier");
            barrierRoot.transform.SetParent(parent);
            barrierRoot.transform.position = new Vector3(center.x, 1.75f, maxZ + 2.5f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "BarrierVisual";
            visual.transform.SetParent(barrierRoot.transform, false);
            visual.transform.localScale = new Vector3(width, 3.5f, 0.75f);

            var barrier = barrierRoot.AddComponent<RoomExitBarrier>();
            barrier.ConfigureRuntime(visual.GetComponent<Collider>(), visual.GetComponent<Renderer>());
            barrier.Bind(manager);

            Debug.Log($"[Encounter][Bootstrap] Exit barrier ready at {barrierRoot.transform.position}, width={width}.");
        }
    }
}
