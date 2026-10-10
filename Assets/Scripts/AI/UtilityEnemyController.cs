using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ============================================================
// Praktikum 06 - Bagian II: Utility-Based Decision AI
// (modul bagian 64-74, dan Tugas Pengembangan 80D)
//
// Perilakunya sama dengan EnemyBTController: Patrol, Chase,
// Attack, Flee. Yang berbeda adalah CARA MEMILIH-nya.
//
//   Behavior Tree -> prioritas tetap (urutan child Selector)
//   Utility AI    -> skor tertinggi (dihitung ulang tiap frame)
//
// Dua pengaman dari modul bagian 73-74 ikut diimplementasikan:
//   - Action Commitment  (minimumActionDuration)
//   - Hysteresis         (switchMargin)
// ============================================================

public class UtilityEnemyController : MonoBehaviour
{
    public enum UtilityAction
    {
        Patrol,
        Chase,
        Attack,
        Flee
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform safePoint;

    [Header("Perception")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 10;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Utility - Bobot")]

    // Skor Patrol dibuat konstan kecil supaya selalu ada
    // minimal satu aksi yang terpilih (modul bagian 70).
    [SerializeField] private float patrolScoreConstant = 0.1f;

    // Rasa terancam ketika Player tidak terlihat. Tanpa ini,
    // Flee tidak akan pernah menang saat Player tidak terlihat.
    [Range(0f, 1f)]
    [SerializeField] private float threatWhenUnseen = 0.5f;

    [Header("Utility - Stabilitas")]

    // Action Commitment (modul bagian 73).
    [SerializeField] private float minimumActionDuration = 1f;

    // Hysteresis (modul bagian 74).
    [SerializeField] private float switchMargin = 0.15f;

    [Header("Debug - Skor (read only saat Play)")]
    [SerializeField] private float attackScore;
    [SerializeField] private float chaseScore;
    [SerializeField] private float fleeScore;
    [SerializeField] private float patrolScore;
    [SerializeField] private string currentAction = "None";

    private NavMeshAgent agent;

    private int currentHealth;
    private int currentPatrolIndex = 0;

    private UtilityAction selectedAction = UtilityAction.Patrol;
    private float actionStartTime;
    private float nextAttackTime;

    private bool canSeePlayerCached;
    private float distanceToPlayerCached;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        actionStartTime = Time.time;
    }

    private void Update()
    {
        UpdatePerception();
        EvaluateScores();
        ChooseAction();
        ExecuteAction();
    }

    // ==================================================
    // PERCEPTION
    // ==================================================

    private void UpdatePerception()
    {
        canSeePlayerCached = false;
        distanceToPlayerCached = Mathf.Infinity;

        if (player == null)
            return;

        Vector3 eyePosition =
            transform.position + Vector3.up * 1.5f;

        Vector3 targetPosition =
            player.position + Vector3.up * 1f;

        Vector3 directionToPlayer =
            targetPosition - eyePosition;

        float distanceToPlayer = directionToPlayer.magnitude;

        distanceToPlayerCached =
            Vector3.Distance(
                transform.position,
                player.position);

        if (distanceToPlayer > visionRange)
            return;

        float angle =
            Vector3.Angle(
                transform.forward,
                directionToPlayer);

        if (angle > visionAngle * 0.5f)
            return;

        bool blocked =
            Physics.Raycast(
                eyePosition,
                directionToPlayer.normalized,
                distanceToPlayer,
                obstacleMask);

        canSeePlayerCached = !blocked;
    }

    // ==================================================
    // SCORING (modul bagian 66-70)
    // ==================================================

    private void EvaluateScores()
    {
        float visibilityScore = canSeePlayerCached ? 1f : 0f;

        float healthPercent =
            maxHealth <= 0
                ? 1f
                : (float)currentHealth / maxHealth;

        float lowHealthScore = 1f - healthPercent;

        // --- Attack: makin dekat makin tinggi, wajib terlihat.
        float attackDistanceScore =
            Mathf.Clamp01(
                1f - (distanceToPlayerCached / attackRange));

        attackScore = attackDistanceScore * visibilityScore;

        // --- Chase: wajib terlihat, makin jauh makin perlu mengejar.
        float distanceNeedScore =
            Mathf.Clamp01(
                distanceToPlayerCached / visionRange);

        chaseScore = visibilityScore * distanceNeedScore;

        // --- Flee: health rendah dikali rasa terancam.
        float threatScore =
            canSeePlayerCached ? 1f : threatWhenUnseen;

        fleeScore = lowHealthScore * threatScore;

        // --- Patrol: default action.
        patrolScore = patrolScoreConstant;
    }

    // ==================================================
    // DECISION (modul bagian 71-74)
    // ==================================================

    private void ChooseAction()
    {
        // Action Commitment: tahan aksi sekarang minimal
        // beberapa saat sebelum boleh berganti.
        if (Time.time - actionStartTime < minimumActionDuration)
        {
            return;
        }

        UtilityAction bestAction = UtilityAction.Patrol;
        float bestScore = -1f;

        if (patrolScore > bestScore)
        {
            bestScore = patrolScore;
            bestAction = UtilityAction.Patrol;
        }

        if (chaseScore > bestScore)
        {
            bestScore = chaseScore;
            bestAction = UtilityAction.Chase;
        }

        if (attackScore > bestScore)
        {
            bestScore = attackScore;
            bestAction = UtilityAction.Attack;
        }

        if (fleeScore > bestScore)
        {
            bestScore = fleeScore;
            bestAction = UtilityAction.Flee;
        }

        if (bestAction == selectedAction)
        {
            return;
        }

        // Hysteresis: kandidat baru harus menang cukup jauh,
        // bukan hanya 0.01 lebih tinggi.
        float currentScore = GetScore(selectedAction);

        if (bestScore < currentScore + switchMargin)
        {
            return;
        }

        selectedAction = bestAction;
        actionStartTime = Time.time;
    }

    private float GetScore(UtilityAction action)
    {
        switch (action)
        {
            case UtilityAction.Attack: return attackScore;
            case UtilityAction.Chase: return chaseScore;
            case UtilityAction.Flee: return fleeScore;
            default: return patrolScore;
        }
    }

    // ==================================================
    // EXECUTION
    // ==================================================

    private void ExecuteAction()
    {
        switch (selectedAction)
        {
            case UtilityAction.Flee:
                Flee();
                break;

            case UtilityAction.Attack:
                AttackPlayer();
                break;

            case UtilityAction.Chase:
                ChasePlayer();
                break;

            default:
                Patrol();
                break;
        }
    }

    private void Patrol()
    {
        currentAction = "PATROL";

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        agent.SetDestination(
            patrolPoints[currentPatrolIndex].position);

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPatrolIndex++;

            if (currentPatrolIndex >= patrolPoints.Length)
                currentPatrolIndex = 0;
        }
    }

    private void ChasePlayer()
    {
        if (player == null)
            return;

        currentAction = "CHASE";

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;

        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        if (player == null)
            return;

        currentAction = "ATTACK";

        agent.isStopped = true;

        FacePlayer();

        // Cooldown di sini ditangani langsung oleh timer,
        // bukan oleh Decorator seperti pada Behavior Tree.
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        Debug.Log(
            name
            + " (Utility) attacks Player! Damage = "
            + attackDamage);

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    private void Flee()
    {
        if (safePoint == null)
            return;

        currentAction = "FLEE";

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;

        agent.SetDestination(safePoint.position);
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime);
    }

    // ==================================================
    // HEALTH
    // ==================================================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(
            name
            + " (Utility) Health = "
            + currentHealth);
    }

    [ContextMenu("Test Damage 25")]
    private void TestDamage25()
    {
        TakeDamage(25);
    }

    [ContextMenu("Reset Health")]
    private void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    [ContextMenu("Log Semua Skor")]
    private void LogScores()
    {
        Debug.Log(
            name + " (Utility) -> " +
            "Attack = " + attackScore.ToString("F2") + ", " +
            "Chase = " + chaseScore.ToString("F2") + ", " +
            "Flee = " + fleeScore.ToString("F2") + ", " +
            "Patrol = " + patrolScore.ToString("F2") + " | " +
            "Terpilih: " + selectedAction);
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        Color visionColor = new Color(1f, 0.92f, 0.2f);
        Color attackColor = new Color(1f, 0.25f, 0.2f);

        Vector3 leftDirection =
            Quaternion.Euler(0, -visionAngle * 0.5f, 0)
            * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0, visionAngle * 0.5f, 0)
            * transform.forward;

#if UNITY_EDITOR
        Handles.color = new Color(
            visionColor.r,
            visionColor.g,
            visionColor.b,
            0.12f);

        Handles.DrawSolidArc(
            position,
            Vector3.up,
            leftDirection,
            visionAngle,
            visionRange);
#endif

        Gizmos.color = visionColor;
        Gizmos.DrawWireSphere(position, visionRange);
        Gizmos.DrawRay(position, leftDirection * visionRange);
        Gizmos.DrawRay(position, rightDirection * visionRange);

        Gizmos.color = attackColor;
        Gizmos.DrawWireSphere(position, attackRange);

        if (safePoint != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.9f);
            Gizmos.DrawLine(position, safePoint.position);
        }

        if (player != null)
        {
            Gizmos.color =
                canSeePlayerCached
                    ? new Color(0.2f, 1f, 0.3f)
                    : new Color(0.8f, 0.2f, 0.2f);

            Gizmos.DrawLine(
                position + Vector3.up * 1.5f,
                player.position + Vector3.up * 1f);
        }

        // Indikator aksi terpilih - ungu, menandakan Utility AI.
        Gizmos.color = new Color(0.65f, 0.3f, 0.95f);

        Gizmos.DrawSphere(
            position + Vector3.up * 2.4f,
            0.25f);

#if UNITY_EDITOR
        // Keempat skor ditampilkan langsung di Scene View, supaya
        // dasar keputusan Utility AI terlihat tanpa membuka Inspector.
        Handles.color = Color.white;

        Handles.Label(
            position + Vector3.up * 3.3f,
            "UTILITY - " + selectedAction
            + "\nAttack  " + attackScore.ToString("F2")
            + "\nChase   " + chaseScore.ToString("F2")
            + "\nFlee    " + fleeScore.ToString("F2")
            + "\nPatrol  " + patrolScore.ToString("F2"));
#endif
    }
}
