#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in geometry creation; never edits the existing platform or saves a scene.</summary>
public static class ValidationSquareBuilder
{
    [MenuItem("Tools/VR Telemetry/Create Exact 2m Validation Platform")]
    public static void Create()
    {
        var root = new GameObject("ValidationSquare_2m");
        Undo.RegisterCreatedObjectUndo(root, "Create validation square");
        // Root deliberately has no parent: ancestor scale must not change dimensions.
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Platform_2m_X_2m_Z_SurfaceY0";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0, -0.025f, 0);
        floor.transform.localScale = new Vector3(2, 0.05f, 2);
        // Corner centers, not marker outside edges, define the route.
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"CornerCenter_X{x}_Z{z}";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(x, 0.005f, z);
            marker.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }
        Selection.activeGameObject = root;
        Debug.Log("Created 2 x 2 m platform: surface Y=0, corner centers X/Z = +/-1. " +
                  "Keep root scale (1,1,1). Place in a validation scene without overlapping floors.");
    }
}
#endif
