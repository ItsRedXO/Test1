using System;
using UnityEngine;

namespace ActionRPG.Combat
{
    public enum WeaponType
    {
        Sword,
        SwordAndShield
    }

    [Serializable]
    public struct ComboStep
    {
        public string StepName;
        public float BaseDamage;
        public float WindupDuration;        // Time before hitbox activates
        public float ActiveDuration;        // Active hitbox duration
        public float RecoveryDuration;      // Recovery time after swing before attack completes
        public float SwingDuration;         // Legacy / fallback total duration
        public float ComboWindowDuration;   // Extra window after attack finishes to press next combo hit
        public float MoveSpeedMultiplier;   // Movement speed scaling during swing (e.g. 0.7 = 70% speed)
        public float KnockbackForce;
        public float HitStunDuration;       // Hit stun duration applied to targets
        public float HitboxRadius;
        public Vector3 HitboxOffset;
        public Color DebugColor;            // Visual indicator swing color
        public float SwingArcAngle;         // Arc angle for visual swing (e.g. 110 or 360)

        public float GetWindup() => WindupDuration > 0f ? WindupDuration : 0.05f;
        public float GetActive() => ActiveDuration > 0f ? ActiveDuration : (SwingDuration > 0f ? SwingDuration : 0.2f);
        public float GetRecovery() => RecoveryDuration >= 0f ? RecoveryDuration : 0.1f;
        public float GetTotalDuration() => GetWindup() + GetActive() + GetRecovery();
    }
}

