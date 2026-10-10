using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#endif

// ============================================================
// Visualisasi Gizmo berwarna untuk Enemy (alat bantu debugging).
//
// Script ini SENGAJA dipisahkan dari EnemyBTController agar
// EnemyBTController tetap sama dengan kode modul bagian 31.
//
// Fungsinya memperjelas apa yang sudah digambar modul pada
// bagian 50 (Vision Range, Attack Range, FOV Boundary), dengan
// menambahkan warna, isi kerucut FOV, rute patrol, dan indikator
// state. Tidak ada logika AI di sini.
//
// Seluruh isinya hanya aktif di Editor; tidak ikut ter-compile
// ke dalam build.
// ============================================================

[RequireComponent(typeof(EnemyBTController))]
public class EnemyGizmoVisualizer : MonoBehaviour
{
    [Header("Tampilan")]

    [SerializeField] private bool showGizmos = true;

    // Isi kerucut FOV digambar semi-transparan. Matikan kalau
    // mengganggu saat mengambil screenshot.
    [SerializeField] private bool showFovCone = true;

    [SerializeField] private bool showPatrolRoute = true;

    [SerializeField] private bool showStateLabel = true;

    [Header("Warna")]

    [SerializeField]
    private Color visionColor = new Color(1f, 0.92f, 0.2f);    // kuning

    [SerializeField]
    private Color attackColor = new Color(1f, 0.25f, 0.2f);    // merah

    [SerializeField]
    private Color chaseColor = new Color(1f, 0.6f, 0.1f);      // oranye

    [SerializeField]
    private Color patrolColor = new Color(0.3f, 0.7f, 1f);     // biru

    [SerializeField]
    private Color fleeColor = new Color(1f, 0.3f, 0.9f);       // magenta

    [SerializeField]
    private Color visibleColor = new Color(0.2f, 1f, 0.3f);    // hijau

    [SerializeField]
    private Color blockedColor = new Color(0.8f, 0.2f, 0.2f);  // merah gelap

#if UNITY_EDITOR

    // Field pada EnemyBTController bersifat private (sesuai modul),
    // jadi nilainya dibaca lewat reflection. Ini hanya untuk
    // keperluan menggambar gizmo, tidak mengubah apa pun.
    private static readonly Dictionary<string, FieldInfo> fieldCache =
        new Dictionary<string, FieldInfo>();

    private EnemyBTController controller;

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        if (controller == null)
        {
            controller = GetComponent<EnemyBTController>();
        }

        if (controller == null)
        {
            return;
        }

        Vector3 position = transform.position;

        float visionRange = Get("visionRange", 10f);
        float visionAngle = Get("visionAngle", 90f);
        float attackRange = Get("attackRange", 2f);

        Transform player = Get<Transform>("player", null);
        Transform safePoint = Get<Transform>("safePoint", null);
        Transform[] patrolPoints = Get<Transform[]>("patrolPoints", null);

        string currentAction = Get("currentAction", "None");
        int patrolIndex = Get("currentPatrolIndex", 0);
        LayerMask obstacleMask = Get("obstacleMask", default(LayerMask));

        // ----------------------------------------------------
        // Perception - kuning
        // ----------------------------------------------------

        Vector3 leftDirection =
            Quaternion.Euler(0, -visionAngle * 0.5f, 0)
            * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0, visionAngle * 0.5f, 0)
            * transform.forward;

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

        if (showPatrolRoute &&
            patrolPoints != null &&
            patrolPoints.Length > 0)
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
            int safeIndex =
                Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);

            if (patrolPoints[safeIndex] != null)
            {
                Gizmos.DrawCube(
                    patrolPoints[safeIndex].position,
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

            bool visible =
                EvaluateVisibility(
                    eyePosition,
                    targetPosition,
                    visionRange,
                    visionAngle,
                    obstacleMask);

            Gizmos.color = visible ? visibleColor : blockedColor;
            Gizmos.DrawLine(eyePosition, targetPosition);
        }

        // ----------------------------------------------------
        // Indikator state - bola di atas kepala
        // ----------------------------------------------------

        Gizmos.color = GetStateColor(currentAction);

        Gizmos.DrawSphere(
            position + Vector3.up * 2.4f,
            0.25f);

        if (showStateLabel)
        {
            Handles.color = Color.white;

            Handles.Label(
                position + Vector3.up * 2.9f,
                currentAction);
        }
    }

    // Mengulang pemeriksaan yang sama dengan CanSeePlayer() pada
    // EnemyBTController, murni untuk menentukan warna garis.
    private bool EvaluateVisibility(
        Vector3 eyePosition,
        Vector3 targetPosition,
        float visionRange,
        float visionAngle,
        LayerMask obstacleMask)
    {
        Vector3 directionToPlayer = targetPosition - eyePosition;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionRange)
            return false;

        float angle =
            Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > visionAngle * 0.5f)
            return false;

        bool blocked =
            Physics.Raycast(
                eyePosition,
                directionToPlayer.normalized,
                distanceToPlayer,
                obstacleMask);

        return !blocked;
    }

    private Color GetStateColor(string currentAction)
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

    private T Get<T>(string fieldName, T fallback)
    {
        FieldInfo info = GetField(fieldName);

        if (info == null)
        {
            return fallback;
        }

        object value = info.GetValue(controller);

        if (value is T typed)
        {
            return typed;
        }

        return fallback;
    }

    private static FieldInfo GetField(string fieldName)
    {
        if (fieldCache.TryGetValue(fieldName, out FieldInfo cached))
        {
            return cached;
        }

        FieldInfo info =
            typeof(EnemyBTController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

        fieldCache[fieldName] = info;
        return info;
    }

#endif
}
