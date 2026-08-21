using System.Collections;
using ActionRPG.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace ActionRPG.Enemy
{
    public enum EnemyState
    {
        Idle,
        Chase,
        AttackWindup,
        Attacking,
        HitStun,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("AI Parameters")]
        [SerializeField] private float detectionRadius = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackWindupTime = 0.4f;
        [SerializeField] private float attackActiveTime = 0.2f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float hitStunDuration = 0.25f;

        [Header("Hitbox / Attack Settings")]
        [SerializeField] private float attackRadius = 1.2f;
        [SerializeField] private Vector3 attackOffset = new Vector3(0f, 0f, 1f);

        [Header("Visual Feedback")]
        [SerializeField] private Renderer meshRenderer;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

        private NavMeshAgent agent;
        private Health health;
        private Transform playerTarget;
        private Color originalColor;
        private float attackCooldownTimer;
        private Coroutine hitStunCoroutine;
        private Coroutine attackCoroutine;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
                health.OnDeath -= HandleDeath;
            }
        }

        private void Start()
        {
            FindPlayerTarget();
        }

        private void Update()
        {
            if (CurrentState == EnemyState.Dead || CurrentState == EnemyState.HitStun) return;

            if (playerTarget == null)
            {
                FindPlayerTarget();
                if (playerTarget == null) return;
            }

            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            switch (CurrentState)
            {
                case EnemyState.Idle:
                    if (distanceToPlayer <= detectionRadius)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    break;

                case EnemyState.Chase:
                    if (distanceToPlayer <= attackRange)
                    {
                        if (attackCooldownTimer <= 0f)
                        {
                            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                            attackCoroutine = StartCoroutine(PerformAttackRoutine());
                        }
                    }
                    else if (distanceToPlayer <= detectionRadius)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(playerTarget.position);
                    }
                    else
                    {
                        agent.isStopped = true;
                        CurrentState = EnemyState.Idle;
                    }
                    break;
            }
        }

        private void FindPlayerTarget()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        private IEnumerator PerformAttackRoutine()
        {
            CurrentState = EnemyState.AttackWindup;
            agent.isStopped = true;

            // Rotate toward player
            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

            // Telegraph visual (flash yellow)
            SetColor(Color.yellow);
            yield return new WaitForSeconds(attackWindupTime);

            if (CurrentState == EnemyState.HitStun || CurrentState == EnemyState.Dead) yield break;

            // Execute Attack
            CurrentState = EnemyState.Attacking;
            SetColor(Color.red);

            Vector3 attackCenter = transform.TransformPoint(attackOffset);
            Collider[] hitCols = Physics.OverlapSphere(attackCenter, attackRadius);
            foreach (var col in hitCols)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive && damageable.TargetFaction == Faction.Player)
                {
                    Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                    DamageInfo damageInfo = new DamageInfo(attackDamage, gameObject, knockbackDir * 5f, hitStunDuration, col.transform.position);
                    damageable.TakeDamage(damageInfo);
                }
            }

            yield return new WaitForSeconds(attackActiveTime);

            SetColor(originalColor);
            attackCooldownTimer = attackCooldown;
            if (CurrentState != EnemyState.HitStun && CurrentState != EnemyState.Dead)
            {
                CurrentState = EnemyState.Chase;
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (CurrentState == EnemyState.Dead) return;

            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            if (hitStunCoroutine != null)
            {
                StopCoroutine(hitStunCoroutine);
            }

            float stun = info.HitStunDuration > 0f ? info.HitStunDuration : hitStunDuration;
            hitStunCoroutine = StartCoroutine(HitStunRoutine(info.KnockbackForce, stun));
        }

        private IEnumerator HitStunRoutine(Vector3 knockbackForce, float stunDuration)
        {
            if (CurrentState == EnemyState.Dead) yield break;

            CurrentState = EnemyState.HitStun;
            if (agent.enabled) agent.isStopped = true;

            SetColor(Color.white); // Hit flash

            float duration = Mathf.Max(0.05f, stunDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Vector3 currentKnockbackVelocity = Vector3.Lerp(knockbackForce, Vector3.zero, t);
                if (agent.enabled && currentKnockbackVelocity.sqrMagnitude > 0.01f)
                {
                    agent.Move(currentKnockbackVelocity * Time.deltaTime);
                }

                yield return null;
            }

            SetColor(originalColor);
            if (CurrentState != EnemyState.Dead)
            {
                CurrentState = EnemyState.Chase;
            }
        }

        private void HandleDeath()
        {
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);

            CurrentState = EnemyState.Dead;
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Disable colliders
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            SetColor(Color.gray);
            Destroy(gameObject, 2f);
        }

        private void SetColor(Color c)
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = c;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Vector3 center = transform.TransformPoint(attackOffset);
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}

