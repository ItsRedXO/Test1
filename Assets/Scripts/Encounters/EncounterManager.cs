using System;
using System.Collections;
using System.Collections.Generic;
using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.Encounters
{
    public enum EncounterState { Waiting, Starting, InProgress, BetweenWaves, Completed }

    [Serializable]
    public class EnemySpawnGroup
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int count = 1;
        [SerializeField] private EnemySpawnPoint[] spawnPoints;
        public GameObject EnemyPrefab => enemyPrefab;
        public int Count => Mathf.Max(0, count);
        public EnemySpawnPoint[] SpawnPoints => spawnPoints;
        public EnemySpawnGroup(GameObject prefab, int amount, EnemySpawnPoint[] points) { enemyPrefab = prefab; count = amount; spawnPoints = points; }
    }

    [Serializable]
    public class EncounterWave
    {
        [SerializeField] private string waveName = "Wave";
        [SerializeField] private float delayBeforeWave;
        [SerializeField] private EnemySpawnGroup[] spawnGroups;
        public string WaveName => string.IsNullOrWhiteSpace(waveName) ? "Wave" : waveName;
        public float DelayBeforeWave => Mathf.Max(0f, delayBeforeWave);
        public EnemySpawnGroup[] SpawnGroups => spawnGroups;
        public EncounterWave(string name, float delay, EnemySpawnGroup[] groups) { waveName = name; delayBeforeWave = delay; spawnGroups = groups; }
    }

    public class EncounterManager : MonoBehaviour
    {
        [SerializeField] private EncounterWave[] waves;
        [SerializeField] private bool startAutomatically;
        [SerializeField] private bool destroySpawnedEnemiesOnDisable = true;
        [SerializeField] private Transform spawnedEnemyParent;
        [SerializeField] private float delayBetweenWaves = 1f;

        public EncounterState CurrentState { get; private set; } = EncounterState.Waiting;
        public int CurrentWaveIndex { get; private set; } = -1;
        public int AliveEnemyCount => aliveEnemies.Count;
        public bool IsEncounterActive => CurrentState == EncounterState.Starting || CurrentState == EncounterState.InProgress || CurrentState == EncounterState.BetweenWaves;
        public event Action OnEncounterStarted;
        public event Action<int, EncounterWave> OnWaveStarted;
        public event Action<int, EncounterWave> OnWaveCompleted;
        public event Action OnEncounterCompleted;
        public event Action<int> OnAliveEnemyCountChanged;

        private readonly List<Health> aliveEnemies = new();
        private Coroutine encounterCoroutine;

        public void ConfigureRuntime(IReadOnlyList<GameObject> waveOne, IReadOnlyList<GameObject> waveTwo, EnemySpawnPoint[] points)
        {
            spawnedEnemyParent = transform;
            delayBetweenWaves = 1f;
            waves = new[]
            {
                BuildRuntimeWave("Wave 1", 0f, waveOne, points),
                BuildRuntimeWave("Wave 2", 1f, waveTwo, points)
            };
        }

        private static EncounterWave BuildRuntimeWave(string name, float delay, IReadOnlyList<GameObject> enemies, EnemySpawnPoint[] points)
        {
            var groups = new List<EnemySpawnGroup>();
            if (enemies != null)
            {
                foreach (GameObject enemy in enemies)
                {
                    if (enemy != null) groups.Add(new EnemySpawnGroup(enemy, 1, points));
                }
            }
            return new EncounterWave(name, delay, groups.ToArray());
        }

        private void Start() { if (startAutomatically) TryStartEncounter(); }

        private void OnDisable()
        {
            if (encounterCoroutine != null) StopCoroutine(encounterCoroutine);
            encounterCoroutine = null;
            foreach (Health enemy in aliveEnemies)
            {
                if (enemy != null) enemy.OnDeath -= HandleTrackedEnemyDeath;
                if (destroySpawnedEnemiesOnDisable && enemy != null) Destroy(enemy.gameObject);
            }
            aliveEnemies.Clear();
        }

        public bool TryStartEncounter()
        {
            if (IsEncounterActive || CurrentState == EncounterState.Completed) return false;
            if (waves == null || waves.Length == 0) return false;
            encounterCoroutine = StartCoroutine(RunEncounter());
            return true;
        }

        private IEnumerator RunEncounter()
        {
            CurrentState = EncounterState.Starting;
            CurrentWaveIndex = -1;
            OnEncounterStarted?.Invoke();
            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                EncounterWave wave = waves[waveIndex];
                CurrentWaveIndex = waveIndex;
                float delay = wave.DelayBeforeWave > 0f ? wave.DelayBeforeWave : (waveIndex > 0 ? delayBetweenWaves : 0f);
                if (delay > 0f) { CurrentState = EncounterState.BetweenWaves; yield return new WaitForSeconds(delay); }
                CurrentState = EncounterState.InProgress;
                SpawnWave(wave);
                OnWaveStarted?.Invoke(waveIndex, wave);
                while (aliveEnemies.Count > 0) yield return null;
                OnWaveCompleted?.Invoke(waveIndex, wave);
            }
            CurrentState = EncounterState.Completed;
            CurrentWaveIndex = waves.Length;
            encounterCoroutine = null;
            OnEncounterCompleted?.Invoke();
        }

        private void SpawnWave(EncounterWave wave)
        {
            if (wave.SpawnGroups == null) return;
            foreach (EnemySpawnGroup group in wave.SpawnGroups)
            {
                if (group == null || group.EnemyPrefab == null || group.Count <= 0 || group.SpawnPoints == null || group.SpawnPoints.Length == 0) continue;
                for (int i = 0; i < group.Count; i++)
                {
                    EnemySpawnPoint point = GetValidSpawnPoint(group.SpawnPoints, i);
                    if (point == null) continue;
                    GameObject enemyObject = Instantiate(group.EnemyPrefab, point.Position, point.Rotation, spawnedEnemyParent);
                    Health enemyHealth = enemyObject.GetComponent<Health>();
                    if (enemyHealth != null) TrackEnemy(enemyHealth);
                }
            }
        }

        private static EnemySpawnPoint GetValidSpawnPoint(EnemySpawnPoint[] points, int spawnIndex)
        {
            var valid = new List<EnemySpawnPoint>();
            foreach (EnemySpawnPoint point in points) if (point != null) valid.Add(point);
            return valid.Count == 0 ? null : valid[spawnIndex % valid.Count];
        }

        private void TrackEnemy(Health enemyHealth)
        {
            if (enemyHealth == null || aliveEnemies.Contains(enemyHealth)) return;
            aliveEnemies.Add(enemyHealth);
            enemyHealth.OnDeath += HandleTrackedEnemyDeath;
            OnAliveEnemyCountChanged?.Invoke(aliveEnemies.Count);
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
        }
    }
}