using UnityEngine;

namespace ActionRPG.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public GameObject Attacker;
        public Vector3 KnockbackForce;
        public float HitStunDuration;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public bool IsBlockable;

        public DamageInfo(float amount, GameObject attacker, Vector3 knockbackForce, float hitStunDuration = 0.25f, Vector3 hitPoint = default, Vector3 hitNormal = default, bool isBlockable = true)
        {
            Amount = amount;
            Attacker = attacker;
            KnockbackForce = knockbackForce;
            HitStunDuration = hitStunDuration;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            IsBlockable = isBlockable;
        }
    }
}

