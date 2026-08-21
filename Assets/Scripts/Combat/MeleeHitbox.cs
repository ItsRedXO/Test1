using System.Collections.Generic;
using UnityEngine;

namespace ActionRPG.Combat
{
    public class MeleeHitbox : MonoBehaviour
    {
        [SerializeField] private Faction ownerFaction = Faction.Player;

        private readonly List<IDamageable> hitTargets = new List<IDamageable>();
        private bool isActive;
        private float currentDamage;
        private float currentKnockback;
        private float currentHitStun;
        private float hitboxRadius;
        private Vector3 hitboxOffset;
        private GameObject attackerObject;

        public void ActivateHitbox(GameObject attacker, Faction faction, float damage, float knockback, float hitStun, float radius, Vector3 offset)
        {
            attackerObject = attacker;
            ownerFaction = faction;
            currentDamage = damage;
            currentKnockback = knockback;
            currentHitStun = hitStun;
            hitboxRadius = radius;
            hitboxOffset = offset;

            hitTargets.Clear();
            isActive = true;
        }

        public void DeactivateHitbox()
        {
            isActive = false;
            hitTargets.Clear();
        }

        private void Update()
        {
            if (!isActive) return;

            Vector3 center = transform.TransformPoint(hitboxOffset);
            Collider[] colliders = Physics.OverlapSphere(center, hitboxRadius);

            foreach (var col in colliders)
            {
                if (col.gameObject == attackerObject) continue;

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive && damageable.TargetFaction != ownerFaction)
                {
                    if (!hitTargets.Contains(damageable))
                    {
                        hitTargets.Add(damageable);

                        Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                        knockbackDir.y = 0f;
                        Vector3 knockback = knockbackDir * currentKnockback;

                        DamageInfo info = new DamageInfo(currentDamage, attackerObject, knockback, currentHitStun, col.transform.position);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (isActive)
            {
                Gizmos.color = Color.red;
                Vector3 center = transform.TransformPoint(hitboxOffset);
                Gizmos.DrawWireSphere(center, hitboxRadius);
            }
        }
    }
}

