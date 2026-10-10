using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// ============================================================
// Praktikum 06 - Behavior Tree & Utility-Based Decision AI
//
// Menu:
//   Praktikum 06/Build Scene P06_BehaviorTree
//   Praktikum 06/Bake Ulang NavMesh (scene aktif)
//
// Builder ini membuat scene sesuai struktur pada modul bagian 4:
//   Ground, Wall01-03 (layer Obstacle), Player, Enemy (NavMeshAgent),
//   PatrolPoints (Waypoint01-04), SafePoint, Navigation (NavMeshSurface).
// ============================================================

public static class Praktikum06SceneBuilder
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MaterialsFolder = "Assets/Materials";

    private const string ScenePath =
        ScenesFolder + "/P06_BehaviorTree.unity";

    private const string NavMeshDataPath =
        ScenesFolder + "/P06_BehaviorTree_NavMeshData.asset";

    private static int obstacleLayer;
    private static int playerLayer;

    [MenuItem("Praktikum 06/Build Scene P06_BehaviorTree")]
    public static void BuildScene()
    {
        if (File.Exists(ScenePath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Praktikum 06",
                "Scene P06_BehaviorTree.unity sudah ada.\n\n" +
                "Build ulang akan menimpa scene tersebut. Lanjutkan?",
                "Timpa",
                "Batal");

            if (!overwrite)
            {
                return;
            }
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolder(ScenesFolder);
        EnsureFolder(MaterialsFolder);

        obstacleLayer = EnsureLayer("Obstacle");
        playerLayer = EnsureLayer("Player");

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        // ----------------------------------------------------
        // Environment
        // ----------------------------------------------------

        GameObject environment = new GameObject("Environment");

        GameObject ground = CreatePlane(
            "Ground",
            Vector3.zero,
            new Vector3(3f, 1f, 3f));

        ground.transform.SetParent(environment.transform);

        CreateWall(
            "Wall01",
            new Vector3(0f, 1f, 3f),
            new Vector3(6f, 2f, 1f))
            .transform.SetParent(environment.transform);

        CreateWall(
            "Wall02",
            new Vector3(-4f, 1f, -2f),
            new Vector3(1f, 2f, 5f))
            .transform.SetParent(environment.transform);

        CreateWall(
            "Wall03",
            new Vector3(4f, 1f, -4f),
            new Vector3(1f, 2f, 5f))
            .transform.SetParent(environment.transform);

        // ----------------------------------------------------
        // Player
        // ----------------------------------------------------

        GameObject player = GameObject.CreatePrimitive(
            PrimitiveType.Capsule);

        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.layer = playerLayer;
        SetMat(player, "P06_Player", new Color(0.2f, 0.5f, 1f));

        player.AddComponent<SimplePlayerController>();
        player.AddComponent<PlayerHealth>();

        // Player bergerak, jadi jangan ikut di-bake ke NavMesh.
        IgnoreFromBake(player);

        // ----------------------------------------------------
        // Patrol points & safe point
        // ----------------------------------------------------

        GameObject patrolRoot = new GameObject("PatrolPoints");

        Transform[] waypoints = new Transform[4];

        waypoints[0] = CreateMarker(
            "Waypoint01",
            new Vector3(-7f, 0f, -7f),
            "P06_Waypoint",
            new Color(0.2f, 0.9f, 0.4f)).transform;

        waypoints[1] = CreateMarker(
            "Waypoint02",
            new Vector3(-7f, 0f, 7f),
            "P06_Waypoint",
            new Color(0.2f, 0.9f, 0.4f)).transform;

        waypoints[2] = CreateMarker(
            "Waypoint03",
            new Vector3(7f, 0f, 7f),
            "P06_Waypoint",
            new Color(0.2f, 0.9f, 0.4f)).transform;

        waypoints[3] = CreateMarker(
            "Waypoint04",
            new Vector3(7f, 0f, -7f),
            "P06_Waypoint",
            new Color(0.2f, 0.9f, 0.4f)).transform;

        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i].SetParent(patrolRoot.transform);
        }

        GameObject safePoint = CreateMarker(
            "SafePoint",
            new Vector3(-10f, 0f, -10f),
            "P06_SafePoint",
            new Color(1f, 0.85f, 0.1f));

        // ----------------------------------------------------
        // Enemy
        // ----------------------------------------------------

        GameObject enemy = GameObject.CreatePrimitive(
            PrimitiveType.Capsule);

        enemy.name = "Enemy";
        enemy.transform.position = new Vector3(6f, 1f, 5f);
        SetMat(enemy, "P06_Enemy", new Color(0.9f, 0.2f, 0.2f));

        IgnoreFromBake(enemy);

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.agentTypeID = 0;
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.speed = 3f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
        agent.autoBraking = true;

        // Pivot capsule berada di tengah, sedangkan NavMesh berada di y = 0.
        // baseOffset = 1 membuat kaki agent menempel pada NavMesh.
        agent.baseOffset = 1f;

        EnemyBTController controller =
            enemy.AddComponent<EnemyBTController>();

        // Visualisasi gizmo berwarna, dipisahkan dari controller
        // supaya EnemyBTController tetap sama dengan kode modul.
        enemy.AddComponent<EnemyGizmoVisualizer>();

        SetRef(controller, "player", player.transform);
        SetRef(controller, "safePoint", safePoint.transform);
        SetTransformArray(controller, "patrolPoints", waypoints);

        SetMask(controller, "obstacleMask", 1 << obstacleLayer);

        SetFloat(controller, "visionRange", 10f);
        SetFloat(controller, "visionAngle", 90f);
        SetFloat(controller, "attackRange", 2f);
        SetFloat(controller, "attackCooldown", 1.5f);
        SetInt(controller, "attackDamage", 10);
        SetInt(controller, "maxHealth", 100);
        SetInt(controller, "lowHealthThreshold", 30);
        SetFloat(controller, "patrolSpeed", 2f);
        SetFloat(controller, "chaseSpeed", 4f);
        SetFloat(controller, "fleeSpeed", 5f);

        // ----------------------------------------------------
        // Camera & light
        // ----------------------------------------------------

        CreateCamera(
            new Vector3(0f, 20f, -19f),
            new Vector3(48f, 0f, 0f));

        CreateLight();

        // ----------------------------------------------------
        // Navigation + bake
        // ----------------------------------------------------

        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = CreateSurface(navigation);

        EditorSceneManager.SaveScene(scene, ScenePath);
        BakeSurface(surface, NavMeshDataPath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[Praktikum06] Scene selesai dibuat: " + ScenePath +
            "\nLayer Obstacle = " + obstacleLayer +
            ", Layer Player = " + playerLayer);
    }

    [MenuItem("Praktikum 06/Bake Ulang NavMesh (scene aktif)")]
    public static void RebakeActiveScene()
    {
        NavMeshSurface surface =
            Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogWarning(
                "[Praktikum06] Tidak ada NavMeshSurface di scene aktif.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        BakeSurface(surface, NavMeshDataPath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ========================================================
    // Menambahkan visualizer gizmo ke scene yang sudah jadi,
    // tanpa harus membangun ulang seluruh scene.
    // ========================================================

    [MenuItem("Praktikum 06/Tambah Gizmo Visualizer ke Enemy")]
    public static void AddGizmoVisualizer()
    {
        Scene scene = SceneManager.GetActiveScene();
        int added = 0;

        foreach (EnemyBTController controller in
                 Object.FindObjectsByType<EnemyBTController>(
                     FindObjectsSortMode.None))
        {
            if (controller.GetComponent<EnemyGizmoVisualizer>() != null)
            {
                continue;
            }

            controller.gameObject
                .AddComponent<EnemyGizmoVisualizer>();

            added++;
        }

        if (added == 0)
        {
            Debug.Log(
                "[Praktikum06] Tidak ada Enemy yang perlu " +
                "ditambahkan visualizer.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "[Praktikum06] EnemyGizmoVisualizer ditambahkan ke " +
            added + " Enemy.");
    }

    // ========================================================
    // Bonus D - Enemy versi Utility AI (modul bagian 64-74, 80D)
    // ========================================================

    [MenuItem("Praktikum 06/Tambah Enemy Utility (Bonus D)")]
    public static void AddUtilityEnemy()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            Debug.LogWarning(
                "[Praktikum06] Buka dulu " + ScenePath +
                " sebelum menambahkan Enemy Utility.");
            return;
        }

        if (GameObject.Find("EnemyUtility") != null)
        {
            Debug.LogWarning(
                "[Praktikum06] EnemyUtility sudah ada di scene.");
            return;
        }

        GameObject source = GameObject.Find("Enemy");

        if (source == null)
        {
            Debug.LogWarning(
                "[Praktikum06] GameObject 'Enemy' tidak ditemukan.");
            return;
        }

        GameObject clone = Object.Instantiate(source);
        clone.name = "EnemyUtility";
        clone.transform.position = new Vector3(-6f, 1f, 5f);

        SetMat(
            clone,
            "P06_EnemyUtility",
            new Color(0.65f, 0.3f, 0.95f));

        // Buang controller Behavior Tree, ganti versi Utility.
        // EnemyGizmoVisualizer harus dibuang lebih dulu karena
        // [RequireComponent] membuatnya bergantung pada controller.
        EnemyGizmoVisualizer visualizer =
            clone.GetComponent<EnemyGizmoVisualizer>();

        if (visualizer != null)
        {
            Object.DestroyImmediate(visualizer);
        }

        EnemyBTController bt = clone.GetComponent<EnemyBTController>();

        if (bt != null)
        {
            Object.DestroyImmediate(bt);
        }

        UtilityEnemyController utility =
            clone.AddComponent<UtilityEnemyController>();

        GameObject player = GameObject.Find("Player");
        GameObject safePoint = GameObject.Find("SafePoint");
        GameObject patrolRoot = GameObject.Find("PatrolPoints");

        SetRef(
            utility,
            "player",
            player != null ? player.transform : null);

        SetRef(
            utility,
            "safePoint",
            safePoint != null ? safePoint.transform : null);

        if (patrolRoot != null)
        {
            int count = patrolRoot.transform.childCount;
            Transform[] waypoints = new Transform[count];

            for (int i = 0; i < count; i++)
            {
                waypoints[i] = patrolRoot.transform.GetChild(i);
            }

            SetTransformArray(utility, "patrolPoints", waypoints);
        }

        int obstacle = LayerMask.NameToLayer("Obstacle");

        if (obstacle != -1)
        {
            SetMask(utility, "obstacleMask", 1 << obstacle);
        }

        SetFloat(utility, "visionRange", 10f);
        SetFloat(utility, "visionAngle", 90f);
        SetFloat(utility, "attackRange", 2f);
        SetFloat(utility, "attackCooldown", 1.5f);
        SetInt(utility, "attackDamage", 10);
        SetInt(utility, "maxHealth", 100);
        SetFloat(utility, "patrolSpeed", 2f);
        SetFloat(utility, "chaseSpeed", 4f);
        SetFloat(utility, "fleeSpeed", 5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = clone;

        Debug.Log(
            "[Praktikum06] EnemyUtility ditambahkan di (-6, 1, 5). " +
            "Enemy ungu = Utility AI, Enemy merah = Behavior Tree.");
    }

    // ========================================================
    // Helpers - Navigation
    // ========================================================

    private static NavMeshSurface CreateSurface(GameObject host)
    {
        NavMeshSurface surface = host.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 0; // Humanoid
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        return surface;
    }

    private static void BakeSurface(
        NavMeshSurface surface,
        string dataPath)
    {
        surface.RemoveData();
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError(
                "[Praktikum06] Bake NavMesh gagal untuk " + dataPath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath) != null)
        {
            AssetDatabase.DeleteAsset(dataPath);
        }

        AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();

        Debug.Log("[Praktikum06] NavMesh di-Bake: " + dataPath);
    }

    private static void IgnoreFromBake(GameObject go)
    {
        if (go.GetComponent<NavMeshModifier>() != null)
        {
            return;
        }

        NavMeshModifier modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
    }

    // ========================================================
    // Helpers - Scene objects
    // ========================================================

    private static GameObject CreatePlane(
        string name,
        Vector3 position,
        Vector3 scale)
    {
        GameObject plane = GameObject.CreatePrimitive(
            PrimitiveType.Plane);

        plane.name = name;
        plane.transform.position = position;
        plane.transform.localScale = scale;
        SetMat(plane, "P06_Ground", new Color(0.26f, 0.3f, 0.27f));
        return plane;
    }

    private static GameObject CreateWall(
        string name,
        Vector3 position,
        Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(
            PrimitiveType.Cube);

        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = obstacleLayer;
        SetMat(wall, "P06_Wall", new Color(0.55f, 0.55f, 0.6f));
        return wall;
    }

    private static GameObject CreateMarker(
        string name,
        Vector3 position,
        string matName,
        Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(
            PrimitiveType.Sphere);

        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.4f;

        Object.DestroyImmediate(marker.GetComponent<Collider>());

        SetMat(marker, matName, color);
        return marker;
    }

    private static Camera CreateCamera(
        Vector3 position,
        Vector3 euler)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);

        Camera cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();

        return cam;
    }

    private static void CreateLight()
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
    }

    // ========================================================
    // Helpers - Material
    // ========================================================

    private static void SetMat(
        GameObject go,
        string matName,
        Color color)
    {
        Renderer r = go.GetComponent<Renderer>();

        if (r != null)
        {
            r.sharedMaterial = GetOrCreateMaterial(matName, color);
        }
    }

    private static Material GetOrCreateMaterial(
        string matName,
        Color color)
    {
        string path = MaterialsFolder + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        Shader shader = null;

        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            shader = GraphicsSettings.defaultRenderPipeline.defaultShader;
        }

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ========================================================
    // Helpers - Mengisi [SerializeField] private
    // ========================================================

    private static void SetRef(
        Object component,
        string field,
        Object value)
    {
        SerializedProperty property =
            FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetTransformArray(
        Object component,
        string field,
        Transform[] values)
    {
        SerializedProperty property =
            FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property
                .GetArrayElementAtIndex(i)
                .objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMask(
        Object component,
        string field,
        int mask)
    {
        SetInt(component, field, mask);
    }

    private static void SetInt(
        Object component,
        string field,
        int value)
    {
        SerializedProperty property =
            FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(
        Object component,
        string field,
        float value)
    {
        SerializedProperty property =
            FindProperty(component, field, out SerializedObject so);

        if (property == null)
        {
            return;
        }

        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SerializedProperty FindProperty(
        Object component,
        string field,
        out SerializedObject so)
    {
        so = null;

        if (component == null)
        {
            return null;
        }

        so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(field);

        if (property == null)
        {
            Debug.LogWarning(
                "[Praktikum06] Field '" + field +
                "' tidak ditemukan pada " +
                component.GetType().Name + ".");
        }

        return property;
    }

    // ========================================================
    // Helpers - Folder & Layer
    // ========================================================

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path
            .GetDirectoryName(path)
            .Replace('\\', '/');

        string leaf = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);

        if (existing != -1)
        {
            return existing;
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning(
            "[Praktikum06] Tidak ada slot layer kosong untuk '" +
            layerName + "'.");

        return 0;
    }
}
