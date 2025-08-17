// Regenerated for runtime-safe generation (adaptive batching is in MapGenerator)
namespace com.faith.procedural
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(MapTextureGenerator))]
    public class MapTextureGeneratorEditor : Editor
    {
        private MapTextureGenerator _reference;

        private void OnEnable()
        {
            _reference = (MapTextureGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (DrawDefaultInspector())
            {
                if (_reference.autoUpdate)
                {
                    float[,] noiseMap;
                    Color[] colorMap;
                    _reference.GenerateMapTexture(out noiseMap, out colorMap);
                }
            }

            if (!_reference.autoUpdate)
            {
                if (GUILayout.Button("Generate Map Texture"))
                {
                    float[,] noiseMap;
                    Color[] colorMap;
                    _reference.GenerateMapTexture(out noiseMap, out colorMap);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public class MapTextureGenerator : MonoBehaviour
    {
        #region Custom Types
        [System.Serializable]
        public struct TerrainType
        {
            public string name;
            [Range(0f, 1f)] public float height;
            public Color color;
            public TerrainDataAsset[] _terrainAsset;
        }
        #endregion

#if UNITY_EDITOR
        [Header("Editor Only")]
        public bool autoUpdate;
#endif

        [Header("Debug")]
        [SerializeField] private Renderer _texturePreview;

        [Header("Texture Settings")]
        [Min(2)] public int textureSize = 241;
        [Range(1, 6)] public int levelOfDetail = 1;
        [Min(0.0001f)] public float noiseScale = 1f;

        [Header("Inner Region Noise")]
        public int seed = 1;
        public Vector2 offset;
        [Min(1)] public int octavesForInnerRegion = 3;
        [Range(0f, 1f)] public float persistanceForInnerRegion = 0.5f;
        [Min(1f)] public float lacunarityForInnerRegion = 1f;

        [Space(6f)]
        public MapDataAsset terrainData;

        [Header("Outer Region Ring")]
        [SerializeField, Range(0.1f, 0.99f)] private float _radius = 0.5f;
        [SerializeField] private Gradient _outerRadiusGradient;

        public Texture2D GenerateMapTexture(out float[,] noiseMap, out Color[] colorMap)
        {
            // Generate noise map
            noiseMap = NoiseGenerator.GenerateNoiseMap(
                seed, textureSize, textureSize, noiseScale,
                octavesForInnerRegion, persistanceForInnerRegion,
                lacunarityForInnerRegion, offset
            );

            // Paint color map using regions + radial falloff
            float mid = textureSize / 2f;
            colorMap = new Color[textureSize * textureSize];

            Vector2 midV = new Vector2(mid, mid);
            float denom = midV.magnitude;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int idx = y * textureSize + x;

                    float magnitude = (new Vector2(x, y) - midV).magnitude;
                    float t = magnitude / denom;
                    float clampedT = Mathf.InverseLerp(0f, 0.707f, t);

                    if (clampedT <= _radius)
                    {
                        float h = noiseMap[x, y];
                        for (int i = 0; i < terrainData.regions.Length; i++)
                        {
                            if (h <= terrainData.regions[i].regionSpreadArea)
                            {
                                colorMap[idx] = terrainData.regions[i].regionColor;
                                break;
                            }
                        }
                    }
                    else
                    {
                        float modT = (t - (_radius * 0.707f)) / (1 - (_radius * 0.707f));
                        colorMap[idx] = _outerRadiusGradient.Evaluate(modT);
                    }
                }
            }

            Texture2D colorTexture = TextureGenerator.TextureFromColorMap(colorMap, textureSize, textureSize);

            if (_texturePreview != null)
            {
                _texturePreview.sharedMaterial.mainTexture = colorTexture;
            }

            return colorTexture;
        }
    }
}
