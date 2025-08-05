namespace com.faith.procedural
{
    using UnityEngine;

#if UNITY_EDITOR

    using UnityEditor;

#endif


    [CreateAssetMenu(fileName = "MapDataAsset", menuName = "ProceduralTerrain/MapDataAsset")]
    public class MapDataAsset : ScriptableObject
    {
        #region Custom Variables

        [System.Serializable]
        public class Region
        {
            #region Public Variables

            public string regionName;
            [Range(0f, 1f)] public float regionSpreadArea;
            public Color regionColor;

            [Space(10f)]
            public RegionLayer[] regionLayers;

            #endregion
        }

        [System.Serializable]
        public class RegionLayer
        {
            public string regionLayerName;

            [Header("Filter")]
            public bool checkBoundingBox = false;
            [Range(0f, 1f)] public float boundCompromization = 0f;
            public TagReference[] tagsForPlacingTerrain;

            [Header("Terrain")]
            [Range(0f, 1f)] public float terrainDensityOnRegionLayer = 1;
            public TerrainDataAsset[] _terrainAsset;
        }

        #endregion

        #region Public Variables

        public Region[] regions;

        #endregion

        #region Public Callback

        public int GetRegionIndex(float interpolatedHeightMap)
        {
            int result = -1;
            interpolatedHeightMap = Mathf.Clamp01(interpolatedHeightMap);

            int numberOfLegion = regions.Length;
            for (int i = 0; i < numberOfLegion; i++)
            {
                if (interpolatedHeightMap <= regions[i].regionSpreadArea)
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        #endregion

    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MapDataAsset))]
    public class SpeedRacerTerrainDataEditor : Editor
    {
        #region Private Variables

        private MapDataAsset _reference;

        #endregion

        #region Mono Behaviour

        private void OnEnable()
        {
            _reference = (MapDataAsset)target;

            if (_reference == null)
                return;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (DrawDefaultInspector())
            {
                int numberOfRegions = _reference.regions.Length;
                for (int i = 1; i < numberOfRegions - 1; i++)
                {
                    _reference.regions[i].regionSpreadArea = Mathf.Clamp(
                            _reference.regions[i].regionSpreadArea,
                            _reference.regions[i - 1].regionSpreadArea,
                            _reference.regions[i + 1].regionSpreadArea
                        );

                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

}
