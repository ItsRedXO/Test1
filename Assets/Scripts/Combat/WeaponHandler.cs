using System;
using System.Collections;
using UnityEngine;

namespace ActionRPG.Combat
{
    public class WeaponHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeleeHitbox meleeHitbox;
        [SerializeField] private GameObject swordVisual;
        [SerializeField] private GameObject shieldVisual;
        [SerializeField] private Transform attackVisualContainer; // Rotates/animates placeholder swing arc

        [Header("Active Weapon")]
        [SerializeField] private WeaponData currentWeaponData;

        public WeaponData CurrentWeaponData => currentWeaponData;
        public event Action<WeaponData> OnWeaponChanged;

        private void Start()
        {
            if (currentWeaponData != null)
            {
                SetWeapon(currentWeaponData);
            }
        }

        public void SetWeapon(WeaponData newWeapon)
        {
            currentWeaponData = newWeapon;

            if (swordVisual != null) swordVisual.SetActive(currentWeaponData != null);
            if (shieldVisual != null) shieldVisual.SetActive(currentWeaponData != null && currentWeaponData.CanBlock);

            OnWeaponChanged?.Invoke(currentWeaponData);
        }

        public void TriggerHitbox(GameObject attacker, Faction faction, ComboStep comboStep)
        {
            if (meleeHitbox == null) return;

            float hitStun = comboStep.HitStunDuration > 0f ? comboStep.HitStunDuration : 0.25f;

            meleeHitbox.ActivateHitbox(
                attacker,
                faction,
                comboStep.BaseDamage,
                comboStep.KnockbackForce,
                hitStun,
                comboStep.HitboxRadius,
                comboStep.HitboxOffset
            );
        }

        public void StopHitbox()
        {
            if (meleeHitbox != null)
            {
                meleeHitbox.DeactivateHitbox();
            }
        }

        public void ShowSwingVisual(int comboIndex, Vector3 facingDir, ComboStep step)
        {
            if (attackVisualContainer == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateSwingVisual(comboIndex, facingDir, step));
        }

        private IEnumerator AnimateSwingVisual(int comboIndex, Vector3 facingDir, ComboStep step)
        {
            attackVisualContainer.gameObject.SetActive(true);
            Vector3 originalScale = attackVisualContainer.localScale;

            bool isFinisher = (currentWeaponData != null && comboIndex == currentWeaponData.ComboChain.Length - 1);
            if (isFinisher)
            {
                attackVisualContainer.localScale = originalScale * 1.4f;
            }

            float windup = step.GetWindup();
            float active = step.GetActive();
            float recovery = step.GetRecovery();

            float arcAngle = step.SwingArcAngle > 0f ? step.SwingArcAngle : 120f;

            float startAngle, endAngle;
            if (arcAngle >= 300f)
            {
                startAngle = -180f;
                endAngle = 180f;
            }
            else if (comboIndex % 2 == 0)
            {
                startAngle = -arcAngle / 2f;
                endAngle = arcAngle / 2f;
            }
            else
            {
                startAngle = arcAngle / 2f;
                endAngle = -arcAngle / 2f;
            }

            Quaternion baseRot = Quaternion.LookRotation(facingDir);

            // Windup phase
            float elapsed = 0f;
            while (elapsed < windup)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windup;
                float angle = Mathf.Lerp(0f, -startAngle * 0.3f, t);
                attackVisualContainer.rotation = baseRot * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            // Active swing phase
            elapsed = 0f;
            while (elapsed < active)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / active;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                attackVisualContainer.rotation = baseRot * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            // Recovery phase
            elapsed = 0f;
            while (elapsed < recovery)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recovery;
                float angle = Mathf.Lerp(endAngle, 0f, t);
                attackVisualContainer.rotation = baseRot * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            attackVisualContainer.localScale = originalScale;
            attackVisualContainer.gameObject.SetActive(false);
        }
    }
}

