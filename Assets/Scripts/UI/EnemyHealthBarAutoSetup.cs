using ActionRPG.Enemy;
using UnityEngine;

namespace ActionRPG.UI
{
    /// <summary>
    /// Automatically adds EnemyHealthBar to all supported enemy types when a scene starts.
    /// This keeps scene/prefab setup out of the manual workflow.
    /// </summary>
    public static class EnemyHealthBarAutoSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AddHealthBarsToEnemies()
        {
            AddHealthBars(FindObjectsByType<EnemyAI>(FindObjectsSortMode.None));
            AddHealthBars(FindObjectsByType<RangedEnemyAI>(FindObjectsSortMode.None));
        }

        private static void AddHealthBars<T>(T[] enemies) where T : Component
        {
            foreach (T enemy in enemies)
            {
                if (enemy == null) continue;

                if (!enemy.TryGetComponent<EnemyHealthBar>(out _))
                {
                    enemy.gameObject.AddComponent<EnemyHealthBar>();
                }
            }
        }
    }
}
