namespace com.faith.procedural
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "TerrainDataAsset", menuName = "ProceduralTerrain/TerrainDataAsset")]
    public class TerrainDataAsset : ScriptableObject
    {
        #region Public Variables

        public GameObject terrainPrefab;
        public Vector3 lowerScaleBound = Vector3.one;
        public Vector3 randomScaleUpperBound = Vector3.one;
        [Range(0f, 1f)] public float terrainDensity = 0.5f;
        [Range(0f, 1f)] public float heightMapBiasness = .5f;

        #endregion


    }
}

