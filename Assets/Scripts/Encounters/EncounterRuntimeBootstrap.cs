using System.Linq;
using ActionRPG.Combat;
using ActionRPG.Enemy;
using UnityEngine;

namespace ActionRPG.Encounters
{
    /// <summary>
    /// Temporary bridge for the current scene format. Builds a room encounter and a reusable
    /// dungeon-style exit gate at runtime without modifying the Unity scene file.
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
            zone.AddComponent<EncounterZone>().SetManager(manager);

            CreateExitGate(root.transform, manager, center, minX, maxX, maxZ);
            Debug.Log($"[Encounter][Bootstrap] Room ready with {enemies.Length} enemies and an animated exit gate.");
        }

        private static void CreateExitGate(Transform parent, EncounterManager manager, Vector3 center, float minX, float maxX, float maxZ)
        {
            float width = Mathf.Clamp(Mathf.Max(5f, (maxX - minX) * 0.5f), 5f, 7f);
            float height = 4.5f;
            float thickness = 0.65f;

            var gateRoot = new GameObject("ExitGate");
            gateRoot.transform.SetParent(parent);
            gateRoot.transform.position = new Vector3(center.x, 0f, maxZ + 2.5f);

            Transform leftPost = CreateCube("LeftPost", gateRoot.transform, new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.55f, height, thickness));
            Transform rightPost = CreateCube("RightPost", gateRoot.transform, new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.55f, height, thickness));
            Transform topBeam = CreateCube("TopBeam", gateRoot.transform, new Vector3(0f, height, 0f), new Vector3(width + 0.55f, 0.55f, thickness));

            float leafWidth = (width - 0.55f) * 0.5f;
            Transform leftLeaf = CreateCube("LeftGateLeaf", gateRoot.transform, new Vector3(-leafWidth * 0.5f, height * 0.5f, 0f), new Vector3(leafWidth, height, thickness));
            Transform rightLeaf = CreateCube("RightGateLeaf", gateRoot.transform, new Vector3(leafWidth * 0.5f, height * 0.5f, 0f), new Vector3(leafWidth, height, thickness));

            var colliders = new[]
            {
                leftLeaf.GetComponent<Collider>(),
                rightLeaf.GetComponent<Collider>()
            };

            var gate = gateRoot.AddComponent<RoomExitGate>();
            gate.ConfigureRuntime(leftLeaf, rightLeaf, colliders);
            gate.Bind(manager);
        }

        private static Transform CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            return cube.transform;
        }
    }
}
