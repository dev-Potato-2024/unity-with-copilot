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
        if (resolution < 2)
            resolution = 2;

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = "CubeSphere";
        }
        else
        {
            generatedMesh.Clear();
        }

        var vertexIndexByGrid = new Dictionary<Vector3Int, int>();
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        var uvs = new List<Vector2>();

        for (int face = 0; face < 6; face++)
        {
            for (int iy = 0; iy < resolution; iy++)
            {
                for (int ix = 0; ix < resolution; ix++)
                {
                    Vector3Int grid = GetGridCoords(face, ix, iy, resolution);
                    if (vertexIndexByGrid.ContainsKey(grid))
                        continue;

                    Vector3 cubePoint = GridToCubePoint(grid, resolution);
                    Vector3 spherePoint = cubePoint.normalized * radius;
                    Vector3 sphereNormal = cubePoint.normalized;

                    int index = vertices.Count;
                    vertexIndexByGrid[grid] = index;
                    vertices.Add(spherePoint);
                    normals.Add(sphereNormal);
                    uvs.Add(GetLatLongUV(sphereNormal));
                }
            }
        }

        for (int face = 0; face < 6; face++)
        {
            Vector3 faceDir = GetFaceDirection(face);

            for (int iy = 0; iy < resolution - 1; iy++)
            {
                for (int ix = 0; ix < resolution - 1; ix++)
                {
                    int i00 = vertexIndexByGrid[GetGridCoords(face, ix, iy, resolution)];
                    int i10 = vertexIndexByGrid[GetGridCoords(face, ix + 1, iy, resolution)];
                    int i01 = vertexIndexByGrid[GetGridCoords(face, ix, iy + 1, resolution)];
                    int i11 = vertexIndexByGrid[GetGridCoords(face, ix + 1, iy + 1, resolution)];

                    AddTriangle(triangles, vertices, i00, i01, i10, faceDir);
                    AddTriangle(triangles, vertices, i10, i01, i11, faceDir);
                }
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
