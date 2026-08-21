using System.Collections;
using ActionRPG.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace ActionRPG.Enemy
{
    public enum RangedEnemyState
    {
        Idle,
        Chase,
        Retreat,
        AttackWindup,
        Attacking,
        Cooldown,
        HitStun,
        Dead
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class RangedEnemyAI : MonoBehaviour
    {
        [Header("Distance and Movement")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float preferredAttackRange = 8f;
        [SerializeField] private float minimumSafeDistance = 4f;
        [SerializeField] private float chaseSpeed = 3.5f;
        [SerializeField] private float retreatSpeed = 4f;
        [SerializeField] private float distanceBuffer = 0.5f;

        [Header("Attack")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private float projectileDamage = 15f;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileLifetime = 5f;
        [SerializeField] private float attackWindupTime = 0.6f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float projectileHitStun = 0.25f;
        [SerializeField] private float projectileKnockback = 3f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer meshRenderer;

        public RangedEnemyState CurrentState { get; private set; } = RangedEnemyState.Idle;

        private NavMeshAgent agent;
        private Health health;
        private Transform playerTarget;
        private Health playerHealth;
        private Color originalColor;
        private float attackCooldownTimer;
        private float targetSearchTimer;
        private bool warnedMissingProjectile;
        private Coroutine attackCoroutine;
        private Coroutine hitStunCoroutine;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalColor = meshRenderer.material.color;
            }

            agent.speed = chaseSpeed;
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

            StopAttackRoutine();
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
        }

        private void Start()
        {
            FindPlayerTarget();
        }

        private void Update()
        {
            if (CurrentState == RangedEnemyState.Dead || CurrentState == RangedEnemyState.HitStun) return;

            if (playerTarget == null)
            {
                targetSearchTimer -= Time.deltaTime;
                if (targetSearchTimer <= 0f)
                {
                    FindPlayerTarget();
                    targetSearchTimer = 1f;
                }

                return;
            }

            if (playerHealth == null || !playerHealth.IsAlive)
            {
                StopAttackingUnavailablePlayer();
                return;
            }

            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

            if (CurrentState == RangedEnemyState.AttackWindup || CurrentState == RangedEnemyState.Attacking)
            {
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer > detectionRange)
            {
                StopMovement();
                CurrentState = RangedEnemyState.Idle;
                return;
            }

            if (ShouldRetreat(distanceToPlayer))
            {
                RetreatFromPlayer();
                return;
            }

            if (ShouldChase(distanceToPlayer))
            {
                ChasePlayer();
                return;
            }

            StopMovement();
            if (attackCooldownTimer <= 0f)
            {
                StartAttack();
            }
            else
            {
                CurrentState = RangedEnemyState.Cooldown;
            }
        }

        private void FindPlayerTarget()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;

            playerTarget = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
        }

        private bool ShouldRetreat(float distanceToPlayer)
        {
            float retreatThreshold = CurrentState == RangedEnemyState.Retreat
                ? minimumSafeDistance + distanceBuffer
                : minimumSafeDistance;
            return distanceToPlayer < retreatThreshold;
        }

        private bool ShouldChase(float distanceToPlayer)
        {
            float chaseThreshold = CurrentState == RangedEnemyState.Chase
                ? preferredAttackRange - distanceBuffer
                : preferredAttackRange;
            return distanceToPlayer > chaseThreshold;
        }

        private void ChasePlayer()
        {
            CurrentState = RangedEnemyState.Chase;
            if (!CanNavigate()) return;

            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }

        private void RetreatFromPlayer()
        {
            CurrentState = RangedEnemyState.Retreat;
            if (!CanNavigate()) return;

            Vector3 awayFromPlayer = transform.position - playerTarget.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude < 0.001f) awayFromPlayer = -transform.forward;
            awayFromPlayer.Normalize();

            agent.speed = retreatSpeed;
            agent.isStopped = false;
            agent.SetDestination(transform.position + awayFromPlayer * (minimumSafeDistance + 1f));
        }

        private void StartAttack()
        {
            if (projectilePrefab == null)
            {
                if (!warnedMissingProjectile)
                {
                    Debug.LogWarning("[RangedEnemy] No projectile prefab is assigned.", this);
                    warnedMissingProjectile = true;
                }

                attackCooldownTimer = attackCooldown;
                CurrentState = RangedEnemyState.Cooldown;
                return;
            }

            if (attackCoroutine == null) attackCoroutine = StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            CurrentState = RangedEnemyState.AttackWindup;
            StopMovement();
            SetColor(Color.yellow);

            yield return new WaitForSeconds(attackWindupTime);

            if (!IsTargetAlive() || Vector3.Distance(transform.position, playerTarget.position) < minimumSafeDistance)
            {
                SetColor(originalColor);
                CurrentState = RangedEnemyState.Idle;
                attackCoroutine = null;
                yield break;
            }

            CurrentState = RangedEnemyState.Attacking;
            SetColor(Color.red);
            FireProjectile();

            SetColor(originalColor);
            attackCooldownTimer = attackCooldown;
            CurrentState = RangedEnemyState.Cooldown;
            attackCoroutine = null;
        }

        private void FireProjectile()
        {
            Transform origin = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
            Vector3 direction = playerTarget.position - origin.position;
            if (direction.sqrMagnitude < 0.001f) return;

            EnemyProjectile projectile = Instantiate(projectilePrefab, origin.position, Quaternion.LookRotation(direction.normalized));
            projectile.Initialize(
                gameObject,
                Faction.Enemy,
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifetime,
                projectileHitStun,
                projectileKnockback
            );
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (CurrentState == RangedEnemyState.Dead) return;

            StopAttackRoutine();
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);
            hitStunCoroutine = StartCoroutine(HitStunRoutine(info.KnockbackForce, info.HitStunDuration));
        }

        private IEnumerator HitStunRoutine(Vector3 knockbackForce, float stunDuration)
        {
            CurrentState = RangedEnemyState.HitStun;
            StopMovement();
            SetColor(Color.white);

            float duration = Mathf.Max(0.05f, stunDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (CanNavigate() && knockbackForce.sqrMagnitude > 0.01f)
                {
                    agent.Move(Vector3.Lerp(knockbackForce, Vector3.zero, t) * Time.deltaTime);
                }

                yield return null;
            }

            SetColor(originalColor);
            if (CurrentState != RangedEnemyState.Dead) CurrentState = RangedEnemyState.Idle;
            hitStunCoroutine = null;
        }

        private void StopAttackingUnavailablePlayer()
        {
            bool changed = CurrentState != RangedEnemyState.Idle || attackCoroutine != null || (CanNavigate() && !agent.isStopped);
            StopAttackRoutine();
            StopMovement();
            CurrentState = RangedEnemyState.Idle;
            if (changed) SetColor(originalColor);
        }

        private void StopAttackRoutine()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            if (CurrentState == RangedEnemyState.AttackWindup || CurrentState == RangedEnemyState.Attacking)
            {
                SetColor(originalColor);
            }
        }

        private bool IsTargetAlive()
        {
            return playerTarget != null && playerHealth != null && playerHealth.IsAlive;
        }

        private void StopMovement()
        {
            if (CanNavigate()) agent.isStopped = true;
        }

        private bool CanNavigate()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private void HandleDeath()
        {
            StopAttackRoutine();
            if (hitStunCoroutine != null) StopCoroutine(hitStunCoroutine);

            CurrentState = RangedEnemyState.Dead;
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            SetColor(Color.gray);
            Destroy(gameObject, 2f);
        }

        private void SetColor(Color color)
        {
            if (meshRenderer != null && meshRenderer.material != null) meshRenderer.material.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, preferredAttackRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, minimumSafeDistance);
        }
    }
}
