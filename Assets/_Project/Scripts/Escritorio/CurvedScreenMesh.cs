using UnityEngine;

/// <summary>
/// Builds a slightly bulged rectangular mesh (sagitta) for the CRT screen.
/// The center stays at z=0 (closest to the camera) and the edges sink toward +Z,
/// into the monitor, if the transform faces like the canvas (forward pointing inward).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CurvedScreenMesh : MonoBehaviour
{
    public const float DefaultWidth = 0.3605115f;
    public const float DefaultHeight = 0.3591041f;

    [SerializeField] float width = DefaultWidth;
    [SerializeField] float height = DefaultHeight;

    [Tooltip("Horizontal sagitta (left-right). On the CRT it is 0: the curve goes top to bottom.")]
    [SerializeField] float horizontalSagitta = 0f;

    [Tooltip("Vertical sagitta (top-bottom), in meters. The center bulges toward the camera.")]
    [SerializeField] float verticalSagitta = 0.01f;

    [SerializeField, Range(2, 64)] int segmentsX = 24;
    [SerializeField, Range(2, 64)] int segmentsY = 18;

    Mesh mesh;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    bool dirty = true;

    public float Width { get => width; set { width = value; Rebuild(); } }
    public float Height { get => height; set { height = value; Rebuild(); } }
    public float HorizontalSagitta { get => horizontalSagitta; set { horizontalSagitta = Mathf.Max(0f, value); Rebuild(); } }
    public float VerticalSagitta { get => verticalSagitta; set { verticalSagitta = Mathf.Max(0f, value); Rebuild(); } }

    void OnEnable() => Rebuild();

    void OnDisable()
    {
        if (mesh == null) return;
        if (meshFilter != null && meshFilter.sharedMesh == mesh) meshFilter.sharedMesh = null;
        if (meshCollider != null && meshCollider.sharedMesh == mesh) meshCollider.sharedMesh = null;
        DestroyMesh();
    }

    void OnValidate()
    {
        width = Mathf.Max(0.001f, width);
        height = Mathf.Max(0.001f, height);
        horizontalSagitta = Mathf.Max(0f, horizontalSagitta);
        verticalSagitta = Mathf.Max(0f, verticalSagitta);
        segmentsX = Mathf.Clamp(segmentsX, 2, 64);
        segmentsY = Mathf.Clamp(segmentsY, 2, 64);
        dirty = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= DelayedRebuild;
        UnityEditor.EditorApplication.delayCall += DelayedRebuild;
#endif
    }

#if UNITY_EDITOR
    void DelayedRebuild()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedRebuild;
        if (this != null && dirty) Rebuild();
    }
#endif

    public void Rebuild()
    {
        dirty = false;
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        if (meshFilter == null) return;

        if (mesh == null)
        {
            mesh = new Mesh { name = "CurvedScreen" };
            mesh.hideFlags = HideFlags.DontSave;
        }
        else mesh.Clear();

        int sx = Mathf.Clamp(segmentsX, 2, 64);
        int sy = Mathf.Clamp(segmentsY, 2, 64);
        int vx = sx + 1;
        int vy = sy + 1;
        var verts = new Vector3[vx * vy];
        var uvs = new Vector2[vx * vy];
        var tris = new int[sx * sy * 6];

        float w = Mathf.Max(0.001f, width);
        float h = Mathf.Max(0.001f, height);
        float hs = Mathf.Max(0f, horizontalSagitta);
        float vs = Mathf.Max(0f, verticalSagitta);

        for (int y = 0; y < vy; y++)
        {
            float v = (float)y / sy;
            float py = (v - 0.5f) * h;
            float nv = (v * 2f) - 1f;
            for (int x = 0; x < vx; x++)
            {
                float u = (float)x / sx;
                float px = (u - 0.5f) * w;
                float nu = (u * 2f) - 1f;
                // Edges toward +Z (inside the monitor). Center at z=0, closest to the camera.
                float z = hs * (nu * nu) + vs * (nv * nv);
                int i = y * vx + x;
                verts[i] = new Vector3(px, py, z);
                uvs[i] = new Vector2(u, v);
            }
        }

        int t = 0;
        for (int y = 0; y < sy; y++)
        {
            for (int x = 0; x < sx; x++)
            {
                int i0 = y * vx + x;
                int i1 = i0 + 1;
                int i2 = i0 + vx;
                int i3 = i2 + 1;
                // Clockwise seen from -Z (camera side). Unity culls back faces.
                tris[t++] = i0;
                tris[t++] = i2;
                tris[t++] = i1;
                tris[t++] = i1;
                tris[t++] = i2;
                tris[t++] = i3;
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.convex = false;
            meshCollider.sharedMesh = mesh;
        }
    }

    void DestroyMesh()
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh);
        else DestroyImmediate(mesh);
        mesh = null;
    }
}