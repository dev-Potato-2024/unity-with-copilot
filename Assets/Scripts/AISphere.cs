using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class AISphere : MonoBehaviour
{
    [Range(2, 128)]
    public int chunkResolution = 8;

    [Range(0.1f, 100f)]
    public float radius = 1f;

    [Range(1, 36)]
    public int chunkColumns = 36;

    [Range(1, 20)]
    public int chunkRows = 20;

    public bool enableRotation = false;

    [Range(-360f, 360f)]
    public float spinRateDegreesPerSecond = 15f;

    private int lastChunkColumns;
    private int lastChunkRows;
    private const string ChunkPrefix = "AISphereChunk_";
#if UNITY_EDITOR
    private double lastEditorUpdateTime;
#endif

    void OnEnable()
    {
        UpdateMesh();
#if UNITY_EDITOR
        lastEditorUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
#endif
    }

    void OnValidate()
    { 
        chunkResolution = Mathf.Max(2, chunkResolution);
        radius = Mathf.Max(0.001f, radius);
        chunkColumns = Mathf.Max(1, chunkColumns);
        chunkRows = Mathf.Max(1, chunkRows);
        UpdateMesh();
    }

    void Update()
    {
        if (chunkColumns != lastChunkColumns || chunkRows != lastChunkRows)
            UpdateMesh();

        if (enableRotation && Application.isPlaying)
            transform.Rotate(Vector3.up, spinRateDegreesPerSecond * Time.deltaTime, Space.Self);
    }

#if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        if (Application.isPlaying || !enableRotation)
        {
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - lastEditorUpdateTime);
        lastEditorUpdateTime = now;

        if (deltaTime <= 0f)
            return;

        transform.Rotate(Vector3.up, spinRateDegreesPerSecond * deltaTime, Space.Self);
        EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    void UpdateMesh()
    {
        chunkResolution = Mathf.Max(2, chunkResolution);
        radius = Mathf.Max(0.001f, radius);
        chunkColumns = Mathf.Max(1, chunkColumns);
        chunkRows = Mathf.Max(1, chunkRows);

        ClearChunks();

        int longitudeSegments = chunkColumns * chunkResolution;
        int latitudeSegments = chunkRows * chunkResolution;

        for (int cy = 0; cy < chunkRows; cy++)
        {
            int iyStart = cy * chunkResolution;
            int iyEnd = iyStart + chunkResolution;
            for (int cx = 0; cx < chunkColumns; cx++)
            {
                int ixStart = cx * chunkResolution;
                int ixEnd = ixStart + chunkResolution;
                CreateChunk(cx, cy, ixStart, ixEnd, iyStart, iyEnd, longitudeSegments, latitudeSegments);
            }
        }

        var rootFilter = GetComponent<MeshFilter>();
        if (rootFilter != null)
            rootFilter.sharedMesh = null;

        lastChunkColumns = chunkColumns;
        lastChunkRows = chunkRows;
    }

    public void RedrawChunks()
    {
        UpdateMesh();
    }

    private void ClearChunks()
    {
        var childrenToRemove = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith(ChunkPrefix))
                childrenToRemove.Add(child);
        }

        foreach (var child in childrenToRemove)
        {
            if (Application.isEditor)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }
    }

    private void CreateChunk(int chunkX, int chunkY, int ixStart, int ixEnd, int iyStart, int iyEnd, int longitudeSegments, int latitudeSegments)
    {
        string chunkName = ChunkPrefix + chunkY + "_" + chunkX;
        GameObject chunkObject = new GameObject(chunkName);
        chunkObject.transform.SetParent(transform, false);

        var filter = chunkObject.AddComponent<MeshFilter>();
        var renderer = chunkObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetChunkMaterial();

        Mesh chunkMesh = new Mesh();
        chunkMesh.name = chunkName + "Mesh";

        int width = ixEnd - ixStart + 1;
        int height = iyEnd - iyStart + 1;
        int vertexCount = width * height;

        var vertices = new List<Vector3>(vertexCount);
        var normals = new List<Vector3>(vertexCount);
        var uvs = new List<Vector2>(vertexCount);
        var triangles = new List<int>((width - 1) * (height - 1) * 6);

        for (int iy = iyStart; iy <= iyEnd; iy++)
        {
            float v = (float)iy / latitudeSegments;
            float phi = Mathf.PI * v;
            float cosPhi = Mathf.Cos(phi);
            float sinPhi = Mathf.Sin(phi);

            for (int ix = ixStart; ix <= ixEnd; ix++)
            {
                float u = (float)ix / longitudeSegments;
                float theta = 2f * Mathf.PI * u;
                float cosTheta = Mathf.Cos(theta);
                float sinTheta = Mathf.Sin(theta);

                Vector3 normal = new Vector3(sinPhi * cosTheta, cosPhi, sinPhi * sinTheta);
                vertices.Add(normal * radius);
                normals.Add(normal);
                uvs.Add(new Vector2(u, 1f - v));
            }
        }

        for (int iy = 0; iy < height - 1; iy++)
        {
            for (int ix = 0; ix < width - 1; ix++)
            {
                int i0 = iy * width + ix;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;

                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);

                triangles.Add(i1);
                triangles.Add(i3);
                triangles.Add(i2);
            }
        }

        chunkMesh.SetVertices(vertices);
        chunkMesh.SetNormals(normals);
        chunkMesh.SetUVs(0, uvs);
        chunkMesh.SetTriangles(triangles, 0);
        chunkMesh.RecalculateBounds();

        filter.sharedMesh = chunkMesh;
    }

    private Material GetChunkMaterial()
    {
        var parentRenderer = GetComponent<MeshRenderer>();
        if (parentRenderer != null && parentRenderer.sharedMaterial != null)
        {
            return parentRenderer.sharedMaterial;
        }

        return GetDefaultMaterial();
    }

    private Material GetDefaultMaterial()
    {
        Material material = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default"));
        material.hideFlags = HideFlags.DontSave;
        return material;
    }
}
