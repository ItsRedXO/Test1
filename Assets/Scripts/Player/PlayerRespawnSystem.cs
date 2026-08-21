using System.Collections;
using ActionRPG.Combat;
using ActionRPG.Input;
using UnityEngine;

namespace ActionRPG.Player
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerCombatController))]
    public class PlayerRespawnSystem : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField] private PlayerSpawnPoint spawnPoint;
        [SerializeField] private float respawnDelay = 2f;

        [Header("Death Feedback")]
        [SerializeField] private Renderer playerRenderer;

        public bool IsRespawning { get; private set; }

        private Health health;
        private PlayerController playerController;
        private PlayerCombatController combatController;
        private PlayerBlockSystem blockSystem;
        private Color originalColor;
        private bool hasOriginalColor;
        private Coroutine respawnCoroutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            playerController = GetComponent<PlayerController>();
            combatController = GetComponent<PlayerCombatController>();
            blockSystem = GetComponent<PlayerBlockSystem>();

            if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null && playerRenderer.material != null)
            {
                originalColor = playerRenderer.material.color;
                hasOriginalColor = true;
            }
        }

        private void OnEnable()
        {
            if (health != null) health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDeath -= HandleDeath;
            if (respawnCoroutine != null) StopCoroutine(respawnCoroutine);
        }

        private void HandleDeath()
        {
            if (IsRespawning) return;

            IsRespawning = true;
            SetPlayerActive(false);
            SetDeathFeedback();

            if (!TryResolveSpawnPoint())
            {
                Debug.LogWarning("[Player] Cannot respawn because no PlayerSpawnPoint is assigned or active.", this);
                return;
            }

            respawnCoroutine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            playerController.Teleport(spawnPoint.transform.position, spawnPoint.transform.rotation);
            health.RestoreFullHealth();
            RestorePlayerFeedback();
            SetPlayerActive(true);

            IsRespawning = false;
            respawnCoroutine = null;
            Debug.Log("[Player] Respawned.");
        }

        private bool TryResolveSpawnPoint()
        {
            if (spawnPoint != null) return true;

            spawnPoint = PlayerSpawnPoint.Active;
            if (spawnPoint == null) spawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();

            return spawnPoint != null;
        }

        private void SetPlayerActive(bool active)
        {
            if (InputHandler.Instance != null) InputHandler.Instance.SetGameplayInputEnabled(active);

            playerController.CanMove = active;
            playerController.CanRotate = active;
            playerController.MoveSpeedMultiplier = 1f;
            combatController.SetCombatEnabled(active);
            if (blockSystem != null) blockSystem.SetBlockingEnabled(active);
        }

        private void SetDeathFeedback()
        {
            if (playerRenderer != null && playerRenderer.material != null)
            {
                playerRenderer.material.color = Color.gray;
            }

            Debug.Log("[Player] Died. Respawning soon.");
        }

        private void RestorePlayerFeedback()
        {
            if (hasOriginalColor && playerRenderer != null && playerRenderer.material != null)
            {
                playerRenderer.material.color = originalColor;
            }
        }
    }
}
