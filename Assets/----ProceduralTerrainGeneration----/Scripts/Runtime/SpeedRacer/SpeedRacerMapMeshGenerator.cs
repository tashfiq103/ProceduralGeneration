using UnityEngine;

public class MeshData
{
    public int meshWidth;
    public int meshHeight;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        this.meshWidth = meshWidth;
        this.meshHeight = meshHeight;

        vertices = new Vector3[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        uvs = new Vector2[meshWidth * meshHeight];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}

public static class SpeedRacerMapMeshGenerator
{
    public const int MESH_CHUNK_SIZE = 241;

    public static MeshData GenerateTerrainMeshChunk(int gridRow, int gridColumn, int gridSize, float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int levelOfDetail)
    {
        float topLeftX                  = (MESH_CHUNK_SIZE - 1) / (-2f);
        float topLeftZ                  = (MESH_CHUNK_SIZE - 1) / 2f;

        int meshSimplificationIncrement = levelOfDetail * 2;
        int verticesPerLine             = (MESH_CHUNK_SIZE - 1) / meshSimplificationIncrement + 1;

        MeshData meshData               =  new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex                 = 0;

        float minUVx = gridRow / (float)gridSize;
        float maxUVx = (gridRow + 1) / (float)gridSize;

        float minUVy = (gridSize - gridColumn) / (float)gridSize;
        float maxUVy = (gridSize - (gridColumn + 1)) / (float)gridSize;

        Debug.Log($"(gridRow, gridColumn) = ({gridRow}, {gridColumn}). (minUVx, maxUVx) = ({minUVx}, {maxUVx}). (minUVy, maxUVy) = ({minUVy}, {maxUVy}).");

        for (int row = 0; row < MESH_CHUNK_SIZE; row += meshSimplificationIncrement)
        {
            for (int column = 0; column < MESH_CHUNK_SIZE; column += meshSimplificationIncrement)
            {
                float heightMapValue = heightMap[row, column];
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + column, heightMapValue * meshHeightCurve.Evaluate(heightMapValue) * heightMultiplier, topLeftZ - row);
                //meshData.uvs[vertexIndex] = new Vector2(
                //    Mathf.Lerp(1f, 0f, column / ((float)MESH_CHUNK_SIZE)),
                //    Mathf.Lerp(0f, 1f, row / ((float)MESH_CHUNK_SIZE))
                    
                //    );
                meshData.uvs[vertexIndex] = new Vector2(
                    Mathf.Lerp(minUVy, maxUVy, column / ((float)MESH_CHUNK_SIZE)),
                    Mathf.Lerp(minUVx, maxUVx, row / ((float)MESH_CHUNK_SIZE))

                    );
                if (row < MESH_CHUNK_SIZE - 1 && column < MESH_CHUNK_SIZE - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

            return meshData;
    }

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);


        float topLeftX = (width /*- 1*/) / (-2f);
        float topLeftZ = (height /*- 1*/) / 2f;

        int meshSimplificationIncrement = levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(width, height/*verticesPerLine, verticesPerLine*/);
        int vertexIndex = 0;

        for (int y = 0; y < height; y +=1 /*meshSimplificationIncrement*/)
        {
            for (int x = 0; x < width; x +=1 /*meshSimplificationIncrement*/)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightMap[x, y] * meshHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / ((float)width), y / ((float)height));
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width /*verticesPerLine*/ + 1, vertexIndex + width /*verticesPerLine*/);
                    meshData.AddTriangle(vertexIndex + width /*verticesPerLine*/ + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}
