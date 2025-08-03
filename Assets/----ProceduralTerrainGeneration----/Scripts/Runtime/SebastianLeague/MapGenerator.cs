namespace BarsStudio.InternalTool.ProceduralTerrainGeneration
{
    using UnityEngine;

    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }

    public class MapGenerator : MonoBehaviour
    {
        #region Custom Variables

        public enum DrawMode
        { 
            NoiseMap,
            ColorMap,
            Mesh
        }

        #endregion

        #region Public Variables

        [Header("External Reference")]
        public MapDisplay mapDisplay;

        [Header("Parameter")]

        public DrawMode drawMode;

        [Space(5f)]
        [Range(1, 6)]
        public int levelOfDetail = 1;
        public float noiseScale = 1;

        [Space(5f)]
        public int octaves = 3;
        [Range(0f, 1f)]public float persistance = 0.5f;
        public float lacunarity = 1;

        [Space(5f)]
        public int seed = 1;
        public Vector2 offset;

        public float heightMultiplier = 10;
        public AnimationCurve meshHeightCurve;

        [Space(5f)]
        public bool autoUpdate;

        [Space(5f)]
        public TerrainType[] regions;

        #endregion

        #region Private Variables

        const int mapChunkSize = 241;

        #endregion

        #region Mono Behaviour

#if UNITY_EDITOR

        private void OnValidate()
        {

            if (lacunarity < 1)
                lacunarity = 1;

            if (octaves < 1)
                octaves = 1;
        }

#endif

        #endregion

        #region Public Callback

        public void GenerateMap()
        {
            float[,] noiseMap = Noise.GenerateNoiseMap(seed, mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, offset);


            Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
            for (int y = 0; y < mapChunkSize; y++)
            {
                for (int x = 0; x < mapChunkSize; x++)
                {
                    float currentHeight = noiseMap[x, y];
                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (currentHeight <= regions[i].height)
                        {
                            int index = y * mapChunkSize + x;
                            colorMap[index] = regions[i].color;
                            break;
                        }
                    }
                }
            }

            switch (drawMode)
            {
                case DrawMode.NoiseMap:

                    mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));

                    break;

                case DrawMode.ColorMap:

                    mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));

                    break;

                case DrawMode.Mesh:

                    mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));

                    break;
            }

            
        }

        #endregion
    }
}

