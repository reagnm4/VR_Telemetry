#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

/// <summary>Creates a separate, editable white-box room using the installed rig.</summary>
public static class ObjectApproachSceneBuilder
{
    private const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    [MenuItem("Tools/VR Telemetry/Create Object Approach Room")]
    public static void Create()
    {
        if (Application.isPlaying) { Debug.LogWarning("Exit Play Mode before creating a scene."); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (prefab == null) { Debug.LogError("Installed Starter Assets XR rig not found: " + RigPath); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.transform.localScale = Vector3.one;
        var origin = rig.GetComponent<XROrigin>();
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        // Keep continuous locomotion; exclude teleport/grab/climb/jump for this instrument.
        foreach (string name in new[] { "Teleportation", "Climb", "Grab Move", "Jump" })
        {
            var locomotion = rig.transform.Find("Locomotion/" + name);
            if (locomotion != null) locomotion.gameObject.SetActive(false);
        }
        new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
        var input = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
        input.GetComponent<XRUIInputModule>().enableXRInput = true;
        input.GetComponent<XRUIInputModule>().enableMouseInput = true;

        Cube("Floor", new Vector3(0, -0.05f, 1), new Vector3(6, 0.1f, 8));
        Cube("Back wall", new Vector3(0, 1.5f, 5), new Vector3(6, 3, 0.1f));
        Cube("Left wall", new Vector3(-3, 1.5f, 1), new Vector3(0.1f, 3, 8));
        Cube("Right wall", new Vector3(3, 1.5f, 1), new Vector3(0.1f, 3, 8));
        var start = Cube("Start marker center (0,0,0)", new Vector3(0, 0.003f, 0), new Vector3(0.4f, 0.006f, 0.4f));
        Object.DestroyImmediate(start.GetComponent<Collider>());
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        var markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMaterial.color = new Color(0.12f, 0.35f, 0.55f);
        AssetDatabase.CreateAsset(markerMaterial, AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/ApproachStartMarker.mat"));
        start.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = "Neutral approach target";
        target.transform.position = new Vector3(0, 1.1f, 1.5f);
        target.transform.localScale = Vector3.one * 0.3f;
        var targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        targetMaterial.color = new Color(0.5f, 0.5f, 0.5f);
        AssetDatabase.CreateAsset(targetMaterial, AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/ApproachNeutralTarget.mat"));
        target.GetComponent<Renderer>().sharedMaterial = targetMaterial;
        target.SetActive(false);
        var light = new GameObject("Room light", typeof(Light));
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.GetComponent<Light>().type = LightType.Directional;
        light.GetComponent<Light>().intensity = 1.2f;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

        var system = new GameObject("TelemetrySystem");
        var logger = system.AddComponent<TelemetryLogger>();
        logger.hmd = origin.Camera.transform;
        logger.xrOrigin = rig.transform;
        logger.leftController = rig.transform.Find("Camera Offset/Left Controller");
        logger.rightController = rig.transform.Find("Camera Offset/Right Controller");
        var session = system.AddComponent<SessionManager>();
        session.telemetry = logger; session.autoStartOnPlay = false; session.autoStopAfterSeconds = 0;
        session.participantId = "P000"; session.environmentId = "object_approach_room_v1";
        session.condition = "neutral_prototype";
        var task = system.AddComponent<ObjectApproachController>(); task.session = session; task.target = target;
        var panel = system.AddComponent<ObjectApproachPanel>(); panel.task = task; panel.hmd = logger.hmd;
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/ObjectApproachRoom.unity");
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = system;
        Debug.Log("Object-Approach room saved to " + path + ". Enter Play, settle tracking, then use Begin session. Headset validation pending.");
    }
    // Batch-mode build verification; does not enter Play or claim headset validation.
    public static void BuildAndCheck()
    {
        Create();
        var task = Object.FindAnyObjectByType<ObjectApproachController>();
        if (task == null || task.session == null || task.target == null || task.target.activeSelf)
            throw new System.Exception("Task wiring or target initial state invalid");
        var telemetry = task.session.telemetry;
        if (telemetry.hmd == null || telemetry.xrOrigin == null || telemetry.leftController == null || telemetry.rightController == null)
            throw new System.Exception("Missing tracked reference");
        if (task.session.autoStartOnPlay || task.session.autoStopAfterSeconds != 0)
            throw new System.Exception("Task must start manually and own session termination");
        if (Object.FindObjectsByType<EventSystem>().Length != 1)
            throw new System.Exception("Expected exactly one EventSystem");
        if (task.target.transform.position != new Vector3(0, 1.1f, 1.5f))
            throw new System.Exception("Unexpected target position");
        Debug.Log("OBJECT_APPROACH_BUILD_CHECK_PASSED");
    }
    private static GameObject Cube(string name, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.position = position; go.transform.localScale = scale;
        return go;
    }
}
#endif
