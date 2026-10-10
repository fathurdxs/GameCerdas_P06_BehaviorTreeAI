using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyBTController : MonoBehaviour
{
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
    [SerializeField] private int lowHealthThreshold = 30;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private string currentAction = "None";

    // ------------------------------------------------------------
    // Toggle eksperimen modul bagian 57-59.
    //
    // Ketiganya OFF secara default, sehingga struktur Behavior Tree
    // yang terbentuk persis sama dengan modul bagian 31.
    //
    // Setelah mengubah toggle saat Play, jalankan context menu
    // "Rebuild Behavior Tree" supaya pohonnya disusun ulang.
    // ------------------------------------------------------------

    [Header("Eksperimen (modul bagian 57-59)")]

    // Bagian 57: Attack diletakkan sebelum Flee di Selector.
    [SerializeField] private bool eksperimen1_AttackSebelumFlee = false;

    // Bagian 58: Condition IsPlayerInAttackRange dihapus.
    [SerializeField] private bool eksperimen2_TanpaAttackRange = false;

    // Bagian 59: CooldownDecorator dilepas, Attack langsung.
    [SerializeField] private bool eksperimen3_TanpaCooldown = false;

    [Header("Gizmo")]

    [SerializeField] private bool showGizmos = true;

    // Isi kerucut FOV digambar semi-transparan. Matikan kalau
    // mengganggu saat mengambil screenshot.
    [SerializeField] private bool showFovCone = true;

    [SerializeField]
    private Color visionColor = new Color(1f, 0.92f, 0.2f);      // kuning

    [SerializeField]
    private Color attackColor = new Color(1f, 0.25f, 0.2f);      // merah

    [SerializeField]
    private Color chaseColor = new Color(1f, 0.6f, 0.1f);        // oranye

    [SerializeField]
    private Color patrolColor = new Color(0.3f, 0.7f, 1f);       // biru

    [SerializeField]
    private Color fleeColor = new Color(1f, 0.3f, 0.9f);         // magenta

    private NavMeshAgent agent;
    private BTNode rootNode;

    private int currentHealth;
    private int currentPatrolIndex = 0;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        BuildBehaviorTree();
    }

    private void Update()
    {
        if (rootNode != null)
        {
            rootNode.Tick();
        }
    }

    private void BuildBehaviorTree()
    {
        // ------------------------------
        // FLEE
        // ------------------------------

        BTNode fleeSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(IsHealthLow),
                    new ActionNode(Flee)
                }
            );

        // ------------------------------
        // ATTACK
        // ------------------------------

        BTNode attackAction =
            new ActionNode(AttackPlayer);

        // Eksperimen 3 (bagian 59): tanpa Decorator, Attack
        // dipanggil setiap frame.
        BTNode attackLeaf =
            eksperimen3_TanpaCooldown
                ? attackAction
                : new CooldownDecorator(
                    attackAction,
                    attackCooldown
                  );

        List<BTNode> attackChildren = new List<BTNode>();

        attackChildren.Add(new ConditionNode(CanSeePlayer));

        // Eksperimen 2 (bagian 58): Condition jarak serang dilepas,
        // sehingga Attack bisa terjadi dari jarak sangat jauh.
        if (!eksperimen2_TanpaAttackRange)
        {
            attackChildren.Add(
                new ConditionNode(IsPlayerInAttackRange));
        }

        attackChildren.Add(attackLeaf);

        BTNode attackSequence =
            new SequenceNode(attackChildren);

        // ------------------------------
        // CHASE
        // ------------------------------

        BTNode chaseSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(CanSeePlayer),
                    new ActionNode(ChasePlayer)
                }
            );

        // ------------------------------
        // PATROL
        // ------------------------------

        BTNode patrolAction =
            new ActionNode(Patrol);

        // ------------------------------
        // ROOT SELECTOR
        // ------------------------------

        List<BTNode> rootChildren = new List<BTNode>();

        // Eksperimen 1 (bagian 57): urutan Flee dan Attack ditukar,
        // sehingga Attack punya prioritas lebih tinggi.
        if (eksperimen1_AttackSebelumFlee)
        {
            rootChildren.Add(attackSequence);
            rootChildren.Add(fleeSequence);
        }
        else
        {
            rootChildren.Add(fleeSequence);
            rootChildren.Add(attackSequence);
        }

        rootChildren.Add(chaseSequence);
        rootChildren.Add(patrolAction);

        rootNode = new SelectorNode(rootChildren);

        LogTreeConfiguration();
    }

    // Cetak konfigurasi pohon supaya terlihat eksperimen mana
    // yang sedang aktif (berguna untuk lampiran laporan).
    private void LogTreeConfiguration()
    {
        string urutan =
            eksperimen1_AttackSebelumFlee
                ? "Attack > Flee > Chase > Patrol  (Eksperimen 1)"
                : "Flee > Attack > Chase > Patrol  (modul)";

        string kondisiAttack =
            eksperimen2_TanpaAttackRange
                ? "CanSeePlayer saja  (Eksperimen 2)"
                : "CanSeePlayer + IsPlayerInAttackRange  (modul)";

        string cooldown =
            eksperimen3_TanpaCooldown
                ? "tanpa Decorator  (Eksperimen 3)"
                : "CooldownDecorator " + attackCooldown + "s  (modul)";

        Debug.Log(
            name + " Behavior Tree dibangun:"
            + "\n  Urutan Selector : " + urutan
            + "\n  Condition Attack: " + kondisiAttack
            + "\n  Attack          : " + cooldown);
    }

    // ==================================================
    // CONDITIONS
    // ==================================================

    private bool IsHealthLow()
    {
        return currentHealth <= lowHealthThreshold;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        return distance <= attackRange;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 eyePosition =
            transform.position
            + Vector3.up * 1.5f;

        Vector3 targetPosition =
            player.position
            + Vector3.up * 1f;

        Vector3 directionToPlayer =
            targetPosition - eyePosition;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        // Player terlalu jauh.
        if (distanceToPlayer > visionRange)
            return false;

        // Player berada di luar Field of View.
        float angle =
            Vector3.Angle(
                transform.forward,
                directionToPlayer
            );

        if (angle > visionAngle * 0.5f)
            return false;

        // Periksa apakah ada obstacle.
        bool blocked =
            Physics.Raycast(
                eyePosition,
                directionToPlayer.normalized,
                distanceToPlayer,
                obstacleMask
            );

        return !blocked;
    }

    // ==================================================
    // ACTIONS
    // ==================================================

    private NodeState Patrol()
    {
        currentAction = "PATROL";

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return NodeState.Failure;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        Transform target =
            patrolPoints[currentPatrolIndex];

        agent.SetDestination(target.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPatrolIndex++;

            if (currentPatrolIndex
                >= patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }
        }

        return NodeState.Running;
    }

    private NodeState ChasePlayer()
    {
        if (player == null)
            return NodeState.Failure;

        currentAction = "CHASE";

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance =
            attackRange * 0.8f;

        agent.SetDestination(player.position);

        return NodeState.Running;
    }

    private NodeState AttackPlayer()
    {
        if (player == null)
            return NodeState.Failure;

        currentAction = "ATTACK";

        agent.isStopped = true;

        FacePlayer();

        Debug.Log(
            name
            + " attacks Player! Damage = "
            + attackDamage
        );

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }

        return NodeState.Success;
    }

    private NodeState Flee()
    {
        if (safePoint == null)
            return NodeState.Failure;

        currentAction = "FLEE";

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;

        agent.SetDestination(
            safePoint.position
        );

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.7f)
        {
            agent.isStopped = true;

            return NodeState.Success;
        }

        return NodeState.Running;
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position
            - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude
            < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }

    // ==================================================
    // HEALTH
    // ==================================================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0,
                maxHealth
            );

        Debug.Log(
            name
            + " Health = "
            + currentHealth
        );
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

    // ==================================================
    // EKSPERIMEN (modul bagian 57-61)
    // ==================================================

    // Panggil ini setelah mengubah toggle eksperimen atau
    // Attack Cooldown saat Play. CooldownDecorator menyimpan
    // nilai cooldown saat dibuat, jadi pohonnya harus
    // disusun ulang supaya perubahan ikut terpakai.
    [ContextMenu("Rebuild Behavior Tree")]
    private void RebuildBehaviorTree()
    {
        BuildBehaviorTree();
    }

    [ContextMenu("Preset: Enemy Normal (modul)")]
    private void PresetNormal()
    {
        visionRange = 10f;
        visionAngle = 90f;
        attackRange = 2f;
        attackCooldown = 1.5f;
        lowHealthThreshold = 30;
        patrolSpeed = 2f;
        chaseSpeed = 4f;
        fleeSpeed = 5f;

        eksperimen1_AttackSebelumFlee = false;
        eksperimen2_TanpaAttackRange = false;
        eksperimen3_TanpaCooldown = false;

        BuildBehaviorTree();
    }

    // Modul bagian 60.
    [ContextMenu("Preset: Enemy Aggressive (bagian 60)")]
    private void PresetAggressive()
    {
        visionRange = 15f;
        visionAngle = 150f;
        attackRange = 2.5f;
        attackCooldown = 0.8f;
        lowHealthThreshold = 15;
        chaseSpeed = 5f;

        BuildBehaviorTree();
    }

    // Modul bagian 61.
    [ContextMenu("Preset: Enemy Coward (bagian 61)")]
    private void PresetCoward()
    {
        visionRange = 10f;
        visionAngle = 90f;
        attackCooldown = 2f;
        lowHealthThreshold = 70;
        chaseSpeed = 3f;
        fleeSpeed = 6f;

        BuildBehaviorTree();
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        Vector3 position = transform.position;

        // ----------------------------------------------------
        // Perception - kuning
        // ----------------------------------------------------

        Vector3 leftDirection =
            Quaternion.Euler(0, -visionAngle * 0.5f, 0)
            * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0, visionAngle * 0.5f, 0)
            * transform.forward;

#if UNITY_EDITOR
        // Isi kerucut FOV, supaya batas pandangnya terbaca jelas
        // (Pengujian 6 pada modul bagian 47).
        if (showFovCone)
        {
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
        }
#endif

        Gizmos.color = visionColor;
        Gizmos.DrawWireSphere(position, visionRange);
        Gizmos.DrawRay(position, leftDirection * visionRange);
        Gizmos.DrawRay(position, rightDirection * visionRange);

        // ----------------------------------------------------
        // Attack Range - merah
        // ----------------------------------------------------

        Gizmos.color = attackColor;
        Gizmos.DrawWireSphere(position, attackRange);

        // ----------------------------------------------------
        // Rute Patrol - biru
        // ----------------------------------------------------

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = patrolColor;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Transform a = patrolPoints[i];

                Transform b =
                    patrolPoints[(i + 1) % patrolPoints.Length];

                if (a == null || b == null)
                {
                    continue;
                }

                Gizmos.DrawLine(a.position, b.position);
                Gizmos.DrawWireCube(a.position, Vector3.one * 0.5f);
            }

            // Waypoint yang sedang dituju - kubus terisi.
            Transform target =
                patrolPoints[
                    Mathf.Clamp(
                        currentPatrolIndex,
                        0,
                        patrolPoints.Length - 1)];

            if (target != null)
            {
                Gizmos.DrawCube(
                    target.position,
                    Vector3.one * 0.5f);
            }
        }

        // ----------------------------------------------------
        // SafePoint - magenta
        // ----------------------------------------------------

        if (safePoint != null)
        {
            Gizmos.color = fleeColor;
            Gizmos.DrawLine(position, safePoint.position);

            Gizmos.DrawWireCube(
                safePoint.position,
                Vector3.one * 0.7f);
        }

        // ----------------------------------------------------
        // Garis ke Player - hijau terlihat, merah terhalang
        // ----------------------------------------------------

        if (player != null)
        {
            Vector3 eyePosition = position + Vector3.up * 1.5f;
            Vector3 targetPosition = player.position + Vector3.up * 1f;

            Gizmos.color =
                CanSeePlayer()
                    ? new Color(0.2f, 1f, 0.3f)
                    : new Color(0.8f, 0.2f, 0.2f);

            Gizmos.DrawLine(eyePosition, targetPosition);
        }

        // ----------------------------------------------------
        // Indikator state - bola di atas kepala
        // ----------------------------------------------------

        Gizmos.color = GetStateColor();

        Gizmos.DrawSphere(
            position + Vector3.up * 2.4f,
            0.25f);

#if UNITY_EDITOR
        Handles.color = Color.white;

        Handles.Label(
            position + Vector3.up * 2.9f,
            currentAction);
#endif
    }

    // Warna indikator mengikuti action yang sedang aktif.
    private Color GetStateColor()
    {
        switch (currentAction)
        {
            case "PATROL": return patrolColor;
            case "CHASE": return chaseColor;
            case "ATTACK": return attackColor;
            case "FLEE": return fleeColor;
            default: return Color.gray;
        }
    }
}
