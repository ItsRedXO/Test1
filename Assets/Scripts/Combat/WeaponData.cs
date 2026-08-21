using UnityEngine;

namespace ActionRPG.Combat
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "ActionRPG/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Identity")]
        public string WeaponName = "Sword";
        public WeaponType WeaponType = WeaponType.Sword;

        [Header("Attack Combo Configuration")]
        public ComboStep[] ComboChain;

        [Header("Shield / Blocking Configuration")]
        public bool CanBlock = false;
        public float MaxBlockDurability = 100f;
        [Range(0f, 1f)]
        public float BlockDamageReduction = 1.0f; // 1.0 = 100% absorbed by shield durability
        public float BlockRegenDelay = 1.5f;        // Delay before durability starts recovering
        public float BlockRegenRate = 25f;          // Durability points recovered per second
        public float GuardBreakThreshold = 0f;      // Reaches 0 -> Guard break
        public float GuardRecoveryThreshold = 30f;  // Durability needed to exit guard broken state
        public float BlockMoveSpeedMultiplier = 0.6f; // Speed multiplier while blocking (e.g. 0.6 = 60% speed)
        public float GuardBreakStaggerDuration = 0.8f; // Stagger duration on guard break
        public float GuardBreakMoveSpeedMultiplier = 0.2f; // Movement speed multiplier during guard break stagger
        public float ExcessDamageMultiplier = 1.0f; // Multiplier applied to excess damage past shield durability
    }
}

