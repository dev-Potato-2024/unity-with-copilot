using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AISphere : MonoBehaviour
{
    [Range(2, 128)]
    public int resolution = 24;

    [Range(0.1f, 100f)]
    public float radius = 1f;

    [Tooltip("Stitch together eight image tiles into a 2x4 atlas for the sphere material.")]
    public Texture2D A1;
    [Tooltip("Texture tile at column B, row 1.")]
    public Texture2D B1;
    [Tooltip("Texture tile at column C, row 1.")]
    public Texture2D C1;
    [Tooltip("Texture tile at column D, row 1.")]
    public Texture2D D1;
    [Tooltip("Texture tile at column A, row 2.")]
    public Texture2D A2;
    [Tooltip("Texture tile at column B, row 2.")]
    public Texture2D B2;
    [Tooltip("Texture tile at column C, row 2.")]
    public Texture2D C2;
    [Tooltip("Texture tile at column D, row 2.")]
    public Texture2D D2;

    private Texture2D generatedAtlas;
    private Mesh generatedMesh;

    void OnEnable()
    {
        UpdateMesh();
        ApplyTexture();
    }

    void OnValidate()
    {
        resolution = Mathf.Max(2, resolution);
        radius = Mathf.Max(0.001f, radius);
        UpdateMesh();
        ApplyTexture();
    }

    void UpdateMesh()
    {
        resolution = Mathf.Max(2, resolution);
        radius = Mathf.Max(0.001f, radius);

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = "LatLongSphere";
        }
        else
        {
            generatedMesh.Clear();
        }

        int longitudeSegments = resolution;
        int latitudeSegments = resolution;
        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);

        var vertices = new List<Vector3>(vertexCount);
        var normals = new List<Vector3>(vertexCount);
        var uvs = new List<Vector2>(vertexCount);
        var triangles = new List<int>(longitudeSegments * latitudeSegments * 6);

        for (int iy = 0; iy <= latitudeSegments; iy++)
        {
            float v = (float)iy / latitudeSegments;
            float phi = Mathf.PI * v;
            float cosPhi = Mathf.Cos(phi);
            float sinPhi = Mathf.Sin(phi);

            for (int ix = 0; ix <= longitudeSegments; ix++)
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

        for (int iy = 0; iy < latitudeSegments; iy++)
        {
            for (int ix = 0; ix < longitudeSegments; ix++)
            {
                int i0 = iy * (longitudeSegments + 1) + ix;
                int i1 = i0 + 1;
                int i2 = i0 + longitudeSegments + 1;
                int i3 = i2 + 1;

                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    private static Vector3Int GetGridCoords(int face, int ix, int iy, int resolution)
    {
        int max = resolution - 1;
        int x = 0, y = 0, z = 0;
        int u = ix * 2 - max;
        int v = iy * 2 - max;

        switch (face)
        {
            case 0: x = max; y = v; z = u; break;
            case 1: x = -max; y = v; z = -u; break;
            case 2: x = u; y = max; z = v; break;
            case 3: x = u; y = -max; z = -v; break;
            case 4: x = u; y = v; z = max; break;
            case 5: x = -u; y = v; z = -max; break;
        }

        return new Vector3Int(x, y, z);
    }

    private static Vector3 GridToCubePoint(Vector3Int grid, int resolution)
    {
        float max = resolution - 1;
        return new Vector3(grid.x / max, grid.y / max, grid.z / max);
    }

    private static Vector3 GetFaceDirection(int face)
    {
        switch (face)
        {
            case 0: return Vector3.right;
            case 1: return Vector3.left;
            case 2: return Vector3.up;
            case 3: return Vector3.down;
            case 4: return Vector3.forward;
            case 5: return Vector3.back;
        }
        return Vector3.zero;
    }

    private static void AddTriangle(List<int> triangles, List<Vector3> verts, int i0, int i1, int i2, Vector3 faceDir)
    {
        Vector3 a = verts[i0];
        Vector3 b = verts[i1];
        Vector3 c = verts[i2];

        Vector3 triNormal = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(triNormal, faceDir) < 0f)
        {
            int temp = i1;
            i1 = i2;
            i2 = temp;
        }

        triangles.Add(i0);
        triangles.Add(i1);
        triangles.Add(i2);
    }

    private static Vector2 GetLatLongUV(Vector3 sphereNormal)
    {
        float u = 0.5f + Mathf.Atan2(sphereNormal.z, sphereNormal.x) / (2f * Mathf.PI);
        float v = 0.5f - Mathf.Asin(sphereNormal.y) / Mathf.PI;
        return new Vector2(u, v);
    }

    private void ApplyTexture()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        var tiles = new Texture2D[8] { A1, B1, C1, D1, A2, B2, C2, D2 };
        UpdateAtlasTexture(tiles);

        if (generatedAtlas == null)
        {
            Debug.LogWarning($"{name}: Failed to build atlas. Make sure A1..D2 are assigned and readable.", this);
            return;
        }

        Material material = renderer.sharedMaterial;
        if (material == null || material.shader == null)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError($"{name}: No valid shader found to render atlas.", this);
                return;
            }

            material = new Material(shader);
            renderer.sharedMaterial = material;
        }

        material.mainTexture = generatedAtlas;
        renderer.sharedMaterial = material;
    }

    private void UpdateAtlasTexture(Texture2D[] tiles)
    {
        DestroyGeneratedAtlas();
        generatedAtlas = BuildAtlasTexture(tiles, 4, 2);
    }

    private void DestroyGeneratedAtlas()
    {
        if (generatedAtlas == null)
            return;

        if (Application.isEditor)
            DestroyImmediate(generatedAtlas);
        else
            Destroy(generatedAtlas);

        generatedAtlas = null;
    }

    private static Texture2D BuildAtlasTexture(Texture2D[] tiles, int columns, int rows)
    {
        if (tiles == null || tiles.Length == 0 || columns <= 0 || rows <= 0)
            return null;

        int tileWidth = 0;
        int tileHeight = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
            {
                tileWidth = tiles[i].width;
                tileHeight = tiles[i].height;

                Debug.Log($"Using tile {i} with size {tileWidth}x{tileHeight} for atlas.");

                break;
            }
        }

        if (tileWidth == 0 || tileHeight == 0)
            return null;

        int atlasWidth = tileWidth * columns;
        int atlasHeight = tileHeight * rows;

        RenderTexture tempRT = RenderTexture.GetTemporary(atlasWidth, atlasHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture.active = tempRT;
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, atlasWidth, 0, atlasHeight);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index >= tiles.Length || tiles[index] == null)
                    continue;

                Rect destRect = new Rect(col * tileWidth, (rows - 1 - row) * tileHeight, tileWidth, tileHeight);
                Graphics.DrawTexture(destRect, tiles[index]);
            }
        }

        GL.PopMatrix();

        Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false, false);
        atlas.ReadPixels(new Rect(0, 0, atlasWidth, atlasHeight), 0, 0);
        atlas.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);

        atlas.wrapMode = TextureWrapMode.Clamp;
        atlas.filterMode = FilterMode.Bilinear;
        atlas.name = "EarthAtlas_2x4";

        return atlas;
    }
}
