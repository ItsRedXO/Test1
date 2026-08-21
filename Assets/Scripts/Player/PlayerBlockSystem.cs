using System;
using System.Collections;
using ActionRPG.Combat;
using ActionRPG.Input;
using UnityEngine;

namespace ActionRPG.Player
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBlockSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponHandler weaponHandler;

        [Header("Block Arc & Settings")]
        [SerializeField] private float blockAngle = 140f; // Cone angle in front of player

        public float CurrentBlockDurability { get; private set; }
        public float MaxBlockDurability { get; private set; } = 100f;
        public bool IsBlocking { get; private set; }
        public bool IsGuardBroken { get; private set; }
        public bool IsStaggered { get; private set; }

        public event Action<float, float, bool, bool> OnBlockDurabilityChanged; // (current, max, isGuardBroken, isBlocking)
        public event Action OnGuardBreak;
        public event Action OnGuardRecovered;

        private Health health;
        private PlayerController playerController;
        private PlayerCombatController combatController;
        private float regenTimer;
        private Coroutine staggerCoroutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            playerController = GetComponent<PlayerController>();
            combatController = GetComponent<PlayerCombatController>();
            if (weaponHandler == null) weaponHandler = GetComponentInChildren<WeaponHandler>();
        }

        private void OnEnable()
        {
            if (health != null) health.DamageFilter = ProcessIncomingDamage;
            if (weaponHandler != null) weaponHandler.OnWeaponChanged += HandleWeaponChanged;
        }

        private void OnDisable()
        {
            if (health != null && health.DamageFilter == ProcessIncomingDamage) health.DamageFilter = null;
            if (weaponHandler != null) weaponHandler.OnWeaponChanged -= HandleWeaponChanged;
        }

        private void Start()
        {
            if (weaponHandler != null && weaponHandler.CurrentWeaponData != null)
            {
                HandleWeaponChanged(weaponHandler.CurrentWeaponData);
            }
        }

        private void HandleWeaponChanged(WeaponData weaponData)
        {
            if (weaponData != null && weaponData.CanBlock)
            {
                MaxBlockDurability = weaponData.MaxBlockDurability;
                if (CurrentBlockDurability <= 0f && !IsGuardBroken)
                {
                    CurrentBlockDurability = MaxBlockDurability;
                }
                else
                {
                    CurrentBlockDurability = Mathf.Min(CurrentBlockDurability > 0f ? CurrentBlockDurability : MaxBlockDurability, MaxBlockDurability);
                }
            }
            else
            {
                if (IsBlocking)
                {
                    IsBlocking = false;
                    if (playerController != null && !IsStaggered && (combatController == null || !combatController.IsAttacking))
                    {
                        playerController.MoveSpeedMultiplier = 1f;
                    }
                }
            }
            NotifyStateChanged();
        }

        private void Update()
        {
            if (weaponHandler == null || weaponHandler.CurrentWeaponData == null) return;
            WeaponData weaponData = weaponHandler.CurrentWeaponData;

            if (!weaponData.CanBlock)
            {
                if (IsBlocking)
                {
                    IsBlocking = false;
                    NotifyStateChanged();
                }
                return;
            }

            // Update Block Holding State
            bool wantsBlock = InputHandler.Instance != null && InputHandler.Instance.BlockHeld;
            bool canBlockNow = wantsBlock && !IsGuardBroken && !IsStaggered && (combatController == null || !combatController.IsAttacking);

            if (IsBlocking != canBlockNow)
            {
                IsBlocking = canBlockNow;

                // Adjust movement speed multiplier for blocking vs normal
                if (!IsStaggered && (combatController == null || !combatController.IsAttacking))
                {
                    if (IsBlocking)
                    {
                        float blockSpeedMult = weaponData.BlockMoveSpeedMultiplier > 0f ? weaponData.BlockMoveSpeedMultiplier : 0.6f;
                        playerController.MoveSpeedMultiplier = blockSpeedMult;
                    }
                    else
                    {
                        playerController.MoveSpeedMultiplier = 1f;
                    }
                }

                NotifyStateChanged();
            }

            // Regeneration Logic
            if (CurrentBlockDurability < MaxBlockDurability)
            {
                if (regenTimer > 0f)
                {
                    regenTimer -= Time.deltaTime;
                }
                else if (!IsBlocking) // Regeneration occurs only when not actively holding block
                {
                    float regenRate = weaponData.BlockRegenRate > 0f ? weaponData.BlockRegenRate : 25f;
                    CurrentBlockDurability = Mathf.Min(MaxBlockDurability, CurrentBlockDurability + regenRate * Time.deltaTime);

                    // Check Guard Recovery
                    float recoveryThreshold = weaponData.GuardRecoveryThreshold > 0f ? weaponData.GuardRecoveryThreshold : 30f;
                    if (IsGuardBroken && CurrentBlockDurability >= recoveryThreshold)
                    {
                        IsGuardBroken = false;
                        OnGuardRecovered?.Invoke();
                    }

                    NotifyStateChanged();
                }
            }
        }

        private DamageInfo ProcessIncomingDamage(DamageInfo rawDamage)
        {
            if (weaponHandler == null || weaponHandler.CurrentWeaponData == null) return rawDamage;
            WeaponData weaponData = weaponHandler.CurrentWeaponData;

            if (!weaponData.CanBlock || !IsBlocking || IsGuardBroken || !rawDamage.IsBlockable)
            {
                return rawDamage;
            }

            // Angle check: Attacker position vs player facing direction
            if (rawDamage.Attacker != null)
            {
                Vector3 directionToAttacker = (rawDamage.Attacker.transform.position - transform.position).normalized;
                directionToAttacker.y = 0f;

                float angle = Vector3.Angle(playerController.CurrentFacingDirection, directionToAttacker);
                if (angle > blockAngle * 0.5f)
                {
                    // Hit from side/behind -> Not blocked
                    return rawDamage;
                }
            }

            // Block Successful! Reset recovery delay timer whenever taking a blocked hit
            float regenDelay = weaponData.BlockRegenDelay > 0f ? weaponData.BlockRegenDelay : 1.5f;
            regenTimer = regenDelay;

            float incomingDamage = rawDamage.Amount;

            if (CurrentBlockDurability >= incomingDamage)
            {
                // Full block by durability
                CurrentBlockDurability -= incomingDamage;

                if (CurrentBlockDurability <= weaponData.GuardBreakThreshold)
                {
                    CurrentBlockDurability = 0f;
                    TriggerGuardBreak(weaponData);
                }

                NotifyStateChanged();

                // Return 0 damage -> player health remains unchanged
                DamageInfo blockedInfo = rawDamage;
                blockedInfo.Amount = 0f;
                return blockedInfo;
            }
            else
            {
                // Attack exceeds remaining durability -> consume remaining durability, trigger Guard Break, pass excess damage
                float excessDamage = incomingDamage - CurrentBlockDurability;
                CurrentBlockDurability = 0f;

                TriggerGuardBreak(weaponData);

                NotifyStateChanged();

                float excessMult = weaponData.ExcessDamageMultiplier > 0f ? weaponData.ExcessDamageMultiplier : 1.0f;
                DamageInfo excessInfo = rawDamage;
                excessInfo.Amount = excessDamage * excessMult;
                return excessInfo;
            }
        }

        private void TriggerGuardBreak(WeaponData weaponData)
        {
            IsGuardBroken = true;
            IsBlocking = false;

            float regenDelay = weaponData.BlockRegenDelay > 0f ? weaponData.BlockRegenDelay : 1.5f;
            regenTimer = regenDelay;

            OnGuardBreak?.Invoke();
            Debug.Log("[Combat] GUARD BREAK! Block durability reached zero.");

            if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
            staggerCoroutine = StartCoroutine(PerformGuardBreakStagger(weaponData));
        }

        private IEnumerator PerformGuardBreakStagger(WeaponData weaponData)
        {
            IsStaggered = true;

            float staggerDuration = weaponData.GuardBreakStaggerDuration > 0f ? weaponData.GuardBreakStaggerDuration : 0.8f;
            float staggerSpeedMult = weaponData.GuardBreakMoveSpeedMultiplier >= 0f ? weaponData.GuardBreakMoveSpeedMultiplier : 0.2f;

            if (playerController != null)
            {
                playerController.MoveSpeedMultiplier = staggerSpeedMult;
            }

            yield return new WaitForSeconds(staggerDuration);

            IsStaggered = false;

            if (playerController != null && !IsBlocking && (combatController == null || !combatController.IsAttacking))
            {
                playerController.MoveSpeedMultiplier = 1f;
            }
        }

        private void NotifyStateChanged()
        {
            OnBlockDurabilityChanged?.Invoke(CurrentBlockDurability, MaxBlockDurability, IsGuardBroken, IsBlocking);
        }
    }
}

