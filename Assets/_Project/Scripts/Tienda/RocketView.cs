using System.Collections.Generic;
using UnityEngine;

// Low poly rocket of the rocket model screen (task 9b, P2). RocketModel puts it in a hidden studio and films it. Low poly, built from Unity primitives at runtime.
// It draws gm.rocketParts and redraws by itself when they change:
//   stand (always) -> SLS (4 RS-25 engines, orange core stage with intertank band, 2 boosters, stage adapter + ICPS;
//   Block 1B: Exploration Upper Stage) -> Orion European service module with 4 solar wings in an X
//   -> crew module (cut cone, heat shield) -> launch abort system (fairing, tower, nose).
// Proportions from the real vehicle at 1 unit = 10 m (SLS Block 1 crew ~98 m tall).
//   Optional parts (modules, science, antenna, battery) go as small blocks around the service module.
// Parts that are not mounted are not drawn, so the player sees the rocket grow.
// Local units: about 10.5 tall with everything mounted. If the object has a BoxCollider it is resized to the model.
public class RocketView : MonoBehaviour
{
    public float spinSpeed = 12f; // degrees per second, 0 = still

    static readonly Color StandColor = new Color(0.25f, 0.25f, 0.28f);
    static readonly Color CoreColor = new Color(0.9f, 0.5f, 0.15f);   // SLS orange foam
    static readonly Color WhiteColor = new Color(0.92f, 0.92f, 0.9f);
    static readonly Color GreyColor = new Color(0.6f, 0.62f, 0.66f);
    static readonly Color PanelColor = new Color(0.15f, 0.25f, 0.6f);
    static readonly Color IntertankColor = new Color(0.72f, 0.36f, 0.1f);
    static readonly Color SilverColor = new Color(0.78f, 0.8f, 0.84f);
    static readonly Color DarkColor = new Color(0.16f, 0.16f, 0.18f);
    static readonly Color ModuleColor = new Color(0.8f, 0.7f, 0.45f);
    static readonly Color ScienceColor = new Color(0.95f, 0.8f, 0.2f);

    Transform model;
    string shownParts = null;
    float height = 1f;
    readonly List<Material> materials = new List<Material>();
    static Material baseMaterial;

    void Update()
    {
        string parts = PartsKey(GameManager.Instance.rocketParts);
        if (parts != shownParts) Rebuild();
        if (model != null && spinSpeed != 0f) model.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    void OnDestroy()
    {
        foreach (Material m in materials) Destroy(m);
    }

    // ---------- Building ----------

    public void Rebuild()
    {
        List<PartData> parts = GameManager.Instance.rocketParts;
        shownParts = PartsKey(parts);

        float angle = model != null ? model.localEulerAngles.y : 0f;
        if (model != null) Destroy(model.gameObject);
        foreach (Material m in materials) Destroy(m);
        materials.Clear();

        model = new GameObject("Model").transform;
        model.SetParent(transform, false);
        model.localEulerAngles = new Vector3(0f, angle, 0f);

        // Scale: 1 unit = 10 m, proportions from the real SLS Block 1 (crew) and Orion
        float y = 0f;
        Cylinder("Stand", 0f, y, 0f, 1.2f, 0.15f, StandColor);
        y += 0.15f;

        PartData sls = Find(parts, PartType.LaunchService);
        if (sls != null)
        {
            bool block1B = sls.payloadCapacity > 30000f || (sls.catalogId != null && sls.catalogId.Contains("1B"));

            // 4 RS-25 engines under the core stage
            for (int i = 0; i < 4; i++)
            {
                float ex = (i % 2 == 0 ? -1f : 1f) * 0.15f;
                float ez = (i < 2 ? -1f : 1f) * 0.15f;
                Cone("RS-25 engine " + (i + 1), ex, y, ez, 0.1f, 0.35f, DarkColor, 0.045f);
            }
            y += 0.35f;

            // Core stage: 8.4 m wide, ~65 m tall, orange foam, darker intertank band
            Cylinder("Core stage", 0f, y, 0f, 0.42f, 6.46f, CoreColor);
            Cylinder("Intertank", 0f, y + 4.6f, 0f, 0.425f, 0.35f, IntertankColor);

            // 2 solid rocket boosters: 3.7 m wide, ~54 m tall, nozzle below and nose cone on top
            for (int side = -1; side <= 1; side += 2)
            {
                float bx = side * 0.61f;
                string label = side < 0 ? "L" : "R";
                Cone("Booster nozzle " + label, bx, y - 0.3f, 0f, 0.13f, 0.3f, DarkColor, 0.09f);
                Cylinder("Booster " + label, bx, y, 0f, 0.185f, 4.9f, WhiteColor);
                Cylinder("Booster band " + label, bx, y + 0.5f, 0f, 0.19f, 0.08f, DarkColor);
                Cone("Booster nose " + label, bx, y + 4.9f, 0f, 0.185f, 0.5f, WhiteColor, 0.02f);
            }
            y += 6.46f;

            if (block1B)
            {
                // Exploration Upper Stage (as wide as the core) + universal stage adapter
                Cylinder("Exploration upper stage", 0f, y, 0f, 0.42f, 1.3f, WhiteColor);
                y += 1.3f;
                Cone("Universal stage adapter", 0f, y, 0f, 0.42f, 0.6f, GreyColor, 0.27f);
                y += 0.6f;
            }
            else
            {
                // Launch vehicle stage adapter (8.4 m -> 5 m) + ICPS + Orion stage adapter
                Cone("Stage adapter", 0f, y, 0f, 0.42f, 0.6f, WhiteColor, 0.25f);
                y += 0.6f;
                Cylinder("Interim upper stage (ICPS)", 0f, y, 0f, 0.25f, 0.9f, WhiteColor);
                y += 0.9f;
                Cylinder("Orion stage adapter", 0f, y, 0f, 0.27f, 0.15f, GreyColor);
                y += 0.15f;
            }
        }

        float serviceY = y;
        if (Find(parts, PartType.ServiceModule) != null)
        {
            // European service module: 5.2 m wide, ~4 m tall, 4 solar wings in an X (~19 m span)
            Cylinder("Service module", 0f, y, 0f, 0.26f, 0.4f, SilverColor);
            Cylinder("Crew module adapter", 0f, y + 0.4f, 0f, 0.255f, 0.08f, GreyColor);
            for (int i = 0; i < 4; i++)
            {
                Transform wing = Box("Solar wing " + (i + 1), 0f, y + 0.2f, 0f, new Vector3(0.7f, 0.02f, 0.2f), PanelColor);
                wing.localRotation = Quaternion.Euler(0f, 45f + 90f * i, 0f);
                wing.localPosition += wing.localRotation * new Vector3(0.26f + 0.36f, 0f, 0f);
            }
            y += 0.48f;
        }

        float capsuleY = y;
        if (Find(parts, PartType.Capsule) != null)
        {
            // Crew module: 5 m wide, 3.3 m tall cut cone, dark heat shield underneath
            Cylinder("Heat shield", 0f, y, 0f, 0.25f, 0.03f, DarkColor);
            Cone("Crew module", 0f, y + 0.03f, 0f, 0.25f, 0.3f, SilverColor, 0.08f);
            y += 0.33f;
        }

        if (Find(parts, PartType.AbortSystem) != null)
        {
            // Launch abort system: fairing over the capsule, tower, attitude control motor and nose
            Cone("Abort fairing", 0f, capsuleY, 0f, 0.27f, 0.6f, WhiteColor, 0.06f);
            y = Mathf.Max(y, capsuleY + 0.6f);
            Cylinder("Abort motor", 0f, y, 0f, 0.06f, 0.45f, GreyColor);
            Cylinder("Attitude control motor", 0f, y + 0.45f, 0f, 0.075f, 0.12f, DarkColor);
            Cone("Abort nose", 0f, y + 0.57f, 0f, 0.075f, 0.2f, WhiteColor);
            y += 0.77f;
        }

        // Optional game parts in a ring around the service module (or on the stand if there is none)
        List<PartData> extras = new List<PartData>();
        foreach (PartData p in parts)
            if (p != null && !IsCore(p.type)) extras.Add(p);
        for (int i = 0; i < extras.Count; i++)
        {
            float a = (360f / extras.Count) * i + 20f;
            Vector3 pos = Quaternion.Euler(0f, a, 0f) * new Vector3(0.36f, 0f, 0f);
            Extra(extras[i], pos.x, serviceY + 0.1f, pos.z);
        }

        height = Mathf.Max(y, 0.5f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = new Vector3(0f, height / 2f, 0f);
            box.size = new Vector3(3.2f, height, 3.2f);
        }
    }

    void Extra(PartData p, float x, float y, float z)
    {
        if (p.type == PartType.Antenna)
        {
            Cylinder(p.partName + " mast", x, y, z, 0.015f, 0.2f, GreyColor);
            Transform dish = Sphere(p.partName, x, y + 0.24f, z, new Vector3(0.2f, 0.05f, 0.2f), WhiteColor);
            dish.localRotation = Quaternion.Euler(30f, 0f, 0f);
        }
        else if (p.type == PartType.Instrument)
            Box(p.partName, x, y, z, new Vector3(0.12f, 0.12f, 0.12f), ScienceColor);
        else if (p.type == PartType.Battery || p.type == PartType.SolarPanel)
            Box(p.partName, x, y, z, new Vector3(0.1f, 0.18f, 0.1f), DarkColor);
        else
            Cylinder(p.partName, x, y, z, 0.07f, 0.2f, ModuleColor);
    }

    static bool IsCore(PartType type)
    {
        return type == PartType.LaunchService || type == PartType.ServiceModule
            || type == PartType.Capsule || type == PartType.AbortSystem;
    }

    static PartData Find(List<PartData> parts, PartType type)
    {
        foreach (PartData p in parts) if (p != null && p.type == type) return p;
        return null;
    }

    static string PartsKey(List<PartData> parts)
    {
        string key = "";
        foreach (PartData p in parts) key += (p != null ? p.name : "-") + ",";
        return key;
    }

    // ---------- Shapes (y = bottom of the shape, in local units) ----------

    Transform Cylinder(string name, float x, float y, float z, float radius, float h, Color color)
    {
        Transform t = Primitive(PrimitiveType.Cylinder, name, color);
        t.localScale = new Vector3(radius * 2f, h / 2f, radius * 2f); // Unity's cylinder is 2 tall
        t.localPosition = new Vector3(x, y + h / 2f, z);
        return t;
    }

    Transform Box(string name, float x, float y, float z, Vector3 size, Color color)
    {
        Transform t = Primitive(PrimitiveType.Cube, name, color);
        t.localScale = size;
        t.localPosition = new Vector3(x, y + size.y / 2f, z);
        return t;
    }

    Transform Sphere(string name, float x, float y, float z, Vector3 size, Color color)
    {
        Transform t = Primitive(PrimitiveType.Sphere, name, color);
        t.localScale = size;
        t.localPosition = new Vector3(x, y, z);
        return t;
    }

    Transform Primitive(PrimitiveType type, string name, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Destroy(go.GetComponent<Collider>()); // the station uses the BoxCollider of the root
        go.transform.SetParent(model, false);
        go.GetComponent<Renderer>().sharedMaterial = NewMaterial(color);
        return go.transform;
    }

    // Cone (or cut cone when topRadius > 0), 12 sides: low poly
    Transform Cone(string name, float x, float y, float z, float radius, float h, Color color, float topRadius = 0f)
    {
        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(model, false);
        go.transform.localPosition = new Vector3(x, y, z);
        go.GetComponent<MeshFilter>().mesh = ConeMesh(radius, topRadius, h, 12);
        go.GetComponent<MeshRenderer>().sharedMaterial = NewMaterial(color);
        return go.transform;
    }

    static Mesh ConeMesh(float bottom, float top, float h, int sides)
    {
        List<Vector3> v = new List<Vector3>();
        List<int> tris = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            float a0 = Mathf.PI * 2f * i / sides;
            float a1 = Mathf.PI * 2f * (i + 1) / sides;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * bottom, 0f, Mathf.Sin(a0) * bottom);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * bottom, 0f, Mathf.Sin(a1) * bottom);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * top, h, Mathf.Sin(a0) * top);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * top, h, Mathf.Sin(a1) * top);
            // side (two triangles, separate vertices = flat low poly shading)
            int s = v.Count;
            v.Add(b0); v.Add(t0); v.Add(t1); v.Add(b1);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
            // bottom cap
            s = v.Count;
            v.Add(Vector3.zero); v.Add(b0); v.Add(b1);
            tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            // top cap (only for a cut cone)
            if (top > 0f)
            {
                s = v.Count;
                v.Add(new Vector3(0f, h, 0f)); v.Add(t1); v.Add(t0);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = "LowPolyCone";
        mesh.SetVertices(v);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Copy of the default material of the render pipeline (URP Lit), with our color
    Material NewMaterial(Color color)
    {
        if (baseMaterial == null)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = tmp.GetComponent<Renderer>().sharedMaterial;
            Destroy(tmp);
        }
        Material m = new Material(baseMaterial);
        m.color = color;
        materials.Add(m);
        return m;
    }

    // In the editor the model only exists in Play mode: show its size so it can be placed on the desk
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.9f, 0.5f, 0.15f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, 5.5f, 0f), new Vector3(3.2f, 11f, 3.2f));
    }
}
