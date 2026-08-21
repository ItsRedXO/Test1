using ActionRPG.Combat;
using UnityEngine;

namespace ActionRPG.Enemy
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class EnemyProjectile : MonoBehaviour
    {
        private const float LaunchHeight = 0.75f;
        private const float AimHeight = 1f;

        private Rigidbody body;
        private Collider projectileCollider;
        private GameObject attacker;
        private Faction ownerFaction;
        private Vector3 travelDirection;
        private float travelSpeed;
        private float damage;
        private float hitStunDuration;
        private float knockback;
        private bool initialized;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            projectileCollider = GetComponent<Collider>();
            body.useGravity = false;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileCollider.isTrigger = true;
        }

        public void Initialize(GameObject source, Faction faction, Vector3 direction, float speed, float amount, float lifetime, float hitStun, float knockbackForce = 0f)
        {
            attacker = source;
            ownerFaction = faction;
            travelDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            travelSpeed = speed;
            damage = amount;
            hitStunDuration = hitStun;
            knockback = knockbackForce;
            initialized = true;

            transform.forward = travelDirection;
            body.linearVelocity = travelDirection * travelSpeed;

            CancelInvoke(nameof(Expire));
            Invoke(nameof(Expire), Mathf.Max(0.01f, lifetime));
        }

        public static Vector3 GetLaunchPosition(Vector3 originPosition)
        {
            return originPosition + Vector3.up * LaunchHeight;
        }

        public static Vector3 GetAimPoint(Vector3 targetPosition)
        {
            return targetPosition + Vector3.up * AimHeight;
        }

        public static DamageInfo BuildDamageInfo(GameObject source, float amount, Vector3 knockbackDirection, float hitStun, Vector3 hitPoint)
        {
            return new DamageInfo(amount, source, knockbackDirection, hitStun, hitPoint, default, true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || IsPartOfAttacker(other)) return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (damageable.TargetFaction == ownerFaction)
                {
                    Expire();
                    return;
                }

                Vector3 knockbackDirection = travelDirection * knockback;
                damageable.TakeDamage(BuildDamageInfo(attacker, damage, knockbackDirection, hitStunDuration, other.ClosestPoint(transform.position)));
            }

            Expire();
        }

        private bool IsPartOfAttacker(Collider other)
        {
            return attacker != null && (other.gameObject == attacker || other.transform.IsChildOf(attacker.transform));
        }

        private void Expire()
        {
            Destroy(gameObject);
        }
    }
}
