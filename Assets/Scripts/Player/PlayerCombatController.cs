using System;
using System.Collections;
using ActionRPG.Combat;
using ActionRPG.Input;
using UnityEngine;

namespace ActionRPG.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponHandler weaponHandler;

        public bool IsAttacking { get; private set; }
        public int CurrentComboIndex { get; private set; }

        public event Action<int, ComboStep> OnAttackStarted;
        public event Action OnComboReset;

        private PlayerController playerController;
        private Coroutine attackCoroutine;
        private bool nextComboQueued;
        private float comboResetTimer;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            if (weaponHandler == null) weaponHandler = GetComponentInChildren<WeaponHandler>();
        }

        private void Update()
        {
            if (InputHandler.Instance == null || weaponHandler == null || weaponHandler.CurrentWeaponData == null) return;

            // Attack input check
            if (InputHandler.Instance.AttackPressed)
            {
                var blockSys = GetComponent<PlayerBlockSystem>();
                if (blockSys != null && blockSys.IsStaggered) return;

                if (!IsAttacking)
                {
                    ExecuteAttack();
                }
                else
                {
                    nextComboQueued = true;
                }
            }

            // Combo window reset timer check
            if (!IsAttacking && CurrentComboIndex > 0)
            {
                comboResetTimer -= Time.deltaTime;
                if (comboResetTimer <= 0f)
                {
                    ResetCombo();
                }
            }
        }

        private void ExecuteAttack()
        {
            var comboChain = weaponHandler.CurrentWeaponData.ComboChain;
            if (comboChain == null || comboChain.Length == 0) return;

            if (CurrentComboIndex >= comboChain.Length)
            {
                CurrentComboIndex = 0;
            }

            ComboStep currentStep = comboChain[CurrentComboIndex];

            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(PerformAttackRoutine(currentStep));
        }

        private IEnumerator PerformAttackRoutine(ComboStep step)
        {
            IsAttacking = true;
            nextComboQueued = false;

            float windup = step.GetWindup();
            float active = step.GetActive();
            float recovery = step.GetRecovery();

            // Apply movement speed modifier during attack
            playerController.MoveSpeedMultiplier = step.MoveSpeedMultiplier;

            OnAttackStarted?.Invoke(CurrentComboIndex, step);

            // Trigger visual swing
            Vector3 facingDir = playerController.CurrentFacingDirection;
            weaponHandler.ShowSwingVisual(CurrentComboIndex, facingDir, step);

            // 1. Windup phase (hitbox inactive)
            yield return new WaitForSeconds(windup);

            // 2. Active hit phase (hitbox active)
            weaponHandler.TriggerHitbox(gameObject, Faction.Player, step);
            yield return new WaitForSeconds(active);
            weaponHandler.StopHitbox();

            // 3. Recovery phase
            yield return new WaitForSeconds(recovery);

            playerController.MoveSpeedMultiplier = 1f;
            IsAttacking = false;

            int comboLength = weaponHandler.CurrentWeaponData.ComboChain.Length;
            bool reachedEnd = (CurrentComboIndex == comboLength - 1);

            if (reachedEnd)
            {
                // Completed full combo chain
                CurrentComboIndex = 0;
                comboResetTimer = 0f;
                nextComboQueued = false;
            }
            else
            {
                // Advance combo index
                CurrentComboIndex++;
                comboResetTimer = step.ComboWindowDuration;

                // If player clicked during swing, trigger next combo step
                if (nextComboQueued)
                {
                    nextComboQueued = false;
                    ExecuteAttack();
                }
            }
        }

        public void ResetCombo()
        {
            CurrentComboIndex = 0;
            comboResetTimer = 0f;
            IsAttacking = false;
            if (playerController != null) playerController.MoveSpeedMultiplier = 1f;
            if (weaponHandler != null) weaponHandler.StopHitbox();
            OnComboReset?.Invoke();
        }
    }
}

