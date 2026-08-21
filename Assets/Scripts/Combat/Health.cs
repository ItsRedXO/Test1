using System;
using UnityEngine;

namespace ActionRPG.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private Faction faction = Faction.Player;
        [SerializeField] private float maxHealth = 100f;

        public Faction TargetFaction => faction;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDeath;

        /// <summary>
        /// Optional delegate to modify damage before it is applied (e.g., block mitigation).
        /// </summary>
        public System.Func<DamageInfo, DamageInfo> DamageFilter { get; set; }

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            if (DamageFilter != null)
            {
                damageInfo = DamageFilter(damageInfo);
            }

            if (damageInfo.Amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDamaged?.Invoke(damageInfo);

            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void SetMaxHealth(float newMax, bool resetCurrent = true)
        {
            maxHealth = newMax;
            if (resetCurrent) CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
