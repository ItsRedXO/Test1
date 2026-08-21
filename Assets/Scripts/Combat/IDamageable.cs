using UnityEngine;

namespace ActionRPG.Combat
{
    public interface IDamageable
    {
        Faction TargetFaction { get; }
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damageInfo);
    }
}
