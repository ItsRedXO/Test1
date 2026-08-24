using System;
using System.Collections.Generic;
using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.Encounters
{
    public enum EncounterState { Waiting, Active, Cleared }

    /// <summary>
    /// Tracks one room encounter. Enemies are placed in the scene as part of the level;
    /// this manager simply activates the encounter and knows when the room is cleared.
    /// </summary>
    public class EncounterManager : MonoBehaviour
    {
        [SerializeField] private Health[] roomEnemies;
        [SerializeField] private bool startAutomatically;

        public EncounterState CurrentState { get; private set; } = EncounterState.Waiting;
        public int AliveEnemyCount => aliveEnemies.Count;
        public bool IsEncounterActive => CurrentState == EncounterState.Active;

        public event Action OnEncounterStarted;
        public event Action OnEncounterCleared;
        public event Action<int> OnAliveEnemyCountChanged;

        private readonly List<Health> aliveEnemies = new();
        private bool configured;

        public void ConfigureRuntime(IEnumerable<Health> enemies)
        {
            roomEnemies = enemies == null ? Array.Empty<Health>() : new List<Health>(enemies).ToArray();
            configured = true;
        }

        private void Start()
        {
            if (!configured) ConfigureRuntime(roomEnemies);
            if (startAutomatically) TryStartEncounter();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
            aliveEnemies.Clear();
        }

        public bool TryStartEncounter()
        {
            if (CurrentState != EncounterState.Waiting) return false;
            if (!configured) ConfigureRuntime(roomEnemies);

            aliveEnemies.Clear();
            foreach (Health enemy in roomEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                aliveEnemies.Add(enemy);
                enemy.OnDeath += HandleTrackedEnemyDeath;
            }

            CurrentState = EncounterState.Active;
            Debug.Log($"[Encounter][Room] Started. Alive enemies={aliveEnemies.Count}.");
            OnEncounterStarted?.Invoke();
            OnAliveEnemyCountChanged?.Invoke(aliveEnemies.Count);

            if (aliveEnemies.Count == 0) ClearEncounter();
            return true;
        }

        private void HandleTrackedEnemyDeath()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                Health enemy = aliveEnemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    if (enemy != null) enemy.OnDeath -= HandleTrackedEnemyDeath;
                    aliveEnemies.RemoveAt(i);
                }
            }

            OnAliveEnemyCountChanged?.Invoke(aliveEnemies.Count);
            if (aliveEnemies.Count == 0) ClearEncounter();
        }

        private void ClearEncounter()
        {
            if (CurrentState != EncounterState.Active) return;
            CurrentState = EncounterState.Cleared;
            Debug.Log("[Encounter][Room] Cleared.");
            OnEncounterCleared?.Invoke();
        }

        private void UnsubscribeAll()
        {
            foreach (Health enemy in aliveEnemies)
            {
                if (enemy != null) enemy.OnDeath -= HandleTrackedEnemyDeath;
            }
        }
    }
}
