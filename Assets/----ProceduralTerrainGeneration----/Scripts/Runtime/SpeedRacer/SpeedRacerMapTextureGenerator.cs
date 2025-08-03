namespace com.faith.procedural
{
    using UnityEngine;
    using BarsStudio.InternalTool.ProceduralTerrainGeneration;

#if UNITY_EDITOR

    using UnityEditor;
    [CustomEditor(typeof(SpeedRacerMapTextureGenerator))]
    public class SpeedRacerMapTextureGeneratorEditor : Editor
    {
        #region Private Variables

        private SpeedRacerMapTextureGenerator _reference;

        #endregion

        #region Editor

        private void OnEnable()
        {
            _reference = (SpeedRacerMapTextureGenerator)target;

            if (_reference == null)
                return;
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

            if (GUILayout.Button("GenerateMapTexture"))
            {
                float[,] noiseMap;
                Color[] colorMap;
                _reference.GenerateMapTexture(out noiseMap, out colorMap);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    public class SpeedRacerMapTextureGenerator : MonoBehaviour
    {
        #region Custom Variables

        [System.Serializable]
        public struct TerrainType
        {
            public string name;
            [Range(0f, 1f)] public float height;
            public Color color;
            public SpeedRacerTerrainDataAsset[] _terrainAsset;
        }

        #endregion

        #region Public Variables

#if UNITY_EDITOR

        [Header("EditorOnly")]
        public bool autoUpdate;

#endif

        [Header("Debug")]
        [SerializeField] private Renderer _texturePreview;

        [Header("Parameter  :   Texture")]
        public int textureSize = 241;
        [Range(1, 6)]
        public int levelOfDetail = 1;
        public float noiseScale = 1;

        [Header("Parameter  :   InnerRegion")]
        public int seed = 1;
        public Vector2 offset;

        [Space(10f)]
        public int octavesForInnerRegion = 3;
        [Range(0f, 1f)] public float persistanceForInnerRegion = 0.5f;
        public float lacunarityForInnerRegion = 1;

        [Space(10f)]
        public SpeedRacerMapDataAsset terrainData;

        [Header("Parameter  :   OuterRegion")]
        [SerializeField, Range(0.1f, 0.99f)] private float _radius = 0.5f;
        [SerializeField] private Gradient _outerRadiusGradient;

        #endregion

        #region Mono Behaviour

#if UNITY_EDITOR

        private void OnValidate()
        {
        }

#endif

        #endregion

        #region Public Callback

        public Texture2D GenerateMapTexture(out float[,] noiseMap, out Color[] colorMap)
        {
            noiseMap = Noise.GenerateNoiseMap(seed, textureSize, textureSize, noiseScale, octavesForInnerRegion, persistanceForInnerRegion, lacunarityForInnerRegion, offset);

            float midTextureSize = textureSize / 2f;

            colorMap = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float magnitude = (new Vector2(x, y) - new Vector2(midTextureSize, midTextureSize)).magnitude;
                    float interpolatedValue = magnitude / new Vector2(midTextureSize, midTextureSize).magnitude;
                    float clampedInterpolatedValue = Mathf.InverseLerp(0, 0.707f, interpolatedValue);

                    int index = y * textureSize + x;

                    if (clampedInterpolatedValue <= _radius)
                    {
                        float currentHeight = noiseMap[x, y];
                        for (int i = 0; i < terrainData.regions.Length; i++)
                        {
                            if (currentHeight <= terrainData.regions[i].regionInterpolatedHeight)
                            {
                                colorMap[index] = terrainData.regions[i].regionColor;
                                break;
                            }
                        }
                    }
                    else
                    {
                        float modifiedInterpolatedValue = (interpolatedValue - (_radius * 0.707f)) / (1 - (_radius * 0.707f));
                        colorMap[index] = _outerRadiusGradient.Evaluate(modifiedInterpolatedValue);
                    }


                }
            }

            Texture2D colorTexture = TextureGenerator.TextureFromColorMap(colorMap, textureSize, textureSize);

            if (_texturePreview != null)
            {
                int scaleSize = Mathf.Clamp(textureSize, 1, 128);

                _texturePreview.sharedMaterial.mainTexture = colorTexture;
                _texturePreview.transform.localScale = new Vector3(scaleSize, 1, scaleSize);
            }

            return colorTexture;
        }

        #endregion
    }

}
