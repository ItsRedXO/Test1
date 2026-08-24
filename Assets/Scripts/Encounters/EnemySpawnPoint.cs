using UnityEngine;

namespace ActionRPG.Encounters
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform spawnTransform;

        public Vector3 Position => (spawnTransform != null ? spawnTransform : transform).position;
        public Quaternion Rotation => (spawnTransform != null ? spawnTransform : transform).rotation;

        public void Spawn(GameObject enemyPrefab, Transform parent = null)
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("[Encounter] Tried to spawn a missing enemy prefab.", this);
                return;
            }

            Instantiate(enemyPrefab, Position, Rotation, parent);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Position, 0.35f);
            Gizmos.DrawLine(Position, Position + Rotation * Vector3.forward * 0.75f);
        }
    }
}