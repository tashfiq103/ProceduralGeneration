using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.faith.core;
#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(SpeedRacerMapGenerator))]
public class SpeedRacerMapGeneratorEditor : Editor
{
    #region Private Variables

    private SpeedRacerMapGenerator _reference;

    #endregion

    #region Editor

    private void OnEnable()
    {
        _reference = (SpeedRacerMapGenerator)target;

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
                _reference.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate Map"))
        {
            _reference.GenerateMap();
        }

        if (GUILayout.Button("Generate Map Chunk"))
        {
            _reference.GenerateMapChunk();
        }

        serializedObject.ApplyModifiedProperties();
    }

    #endregion
}

#endif

public class SpeedRacerMapGenerator : MonoBehaviour
{

    #region Custom Variables

    private struct TerrainRegionHierarchy
    {
        public Transform regionParentTransform;
        public TerrainRegionLayerHierarchy[] terrainRegionLayerHierarchy;
    }

    private struct TerrainRegionLayerHierarchy
    {
        public Transform            regionLayerParentTransform;
        public GameObject[]         terrainAssetPrefab;
        public List<MeshRenderer>   listOfTerrainMeshRenderer;
    }

    #endregion

    #region Public Variables

#if UNITY_EDITOR

    [Header("Editor")]
    public bool autoUpdate = false;

#endif

    [Header("External Reference")]
    public SpeedRacerMapTextureGenerator speedRacerMapTextureGenerator;

    [Space(10f)]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Header("ChunkParameter")]
    public GameObject mapChunkPrefab;
    public int mapScale = 100;
    public int gridSize = 3;

    [Header("Parameter  :   InnerRegion")]
    [Range(0.1f, 1f)]public float levelOfDetail = .1f;
    public float heightMultiplierForInnerRegion = 10;
    public AnimationCurve meshHeightCurveForInnerRegion;

    private List<GameObject> _listOfTerrain;

    #endregion

    #region Configuretion

    private int GetTerrainAsset(float heightMap, float regionMinHeight, float regionMaxHeight, float regionDensity, int numberOfTerrainAsset, SpeedRacerTerrainDataAsset[] terrainAssets)
    {
        float interpolatedRandomPoint = Mathf.Lerp(regionMinHeight, regionMaxHeight, Random.Range(0f, 1f));
        float minDiffValue = 1;
        List<int> listOfTerrainIndex = new List<int>();

        for (int terrainAssetIndex = 0; terrainAssetIndex < numberOfTerrainAsset; terrainAssetIndex++)
        {
            float interpolatedDensity = Mathf.Lerp(regionMinHeight, regionMaxHeight, terrainAssets[terrainAssetIndex].terrainDensity * regionDensity);

            //Filter By Density
            if (interpolatedDensity >= interpolatedRandomPoint)
            {
                float interpolatedHeightMap = Mathf.Lerp(regionMinHeight, regionMaxHeight, heightMap);
                float interpolatedHeightBiasness = Mathf.Lerp(regionMinHeight, regionMaxHeight, terrainAssets[terrainAssetIndex].heightMapBiasness);

                if (interpolatedHeightMap >= interpolatedHeightBiasness)
                {
                    listOfTerrainIndex.Add(terrainAssetIndex);
                }

                //Filtered By HeightBiasness
                //float interpolatedTerrainHeight = Mathf.Lerp(regionMinHeight, regionMaxHeight, noiseHeight);
                //float diff = Mathf.Abs(interpolatedRandomPoint - interpolatedTerrainHeight);
                ////Debug.Log($"{terrainAssets[terrainAssetIndex].terrainPrefab.name} : Remap(TerrainHeight) = {terrainAssets[terrainAssetIndex].heightMapBiasness} -> ({regionMinHeight}, {regionMaxHeight}) = {interpolatedTerrainHeight} : diff = {diff}");
                //if (diff < minDiffValue && noiseHeight > terrainAssets[terrainAssetIndex].heightMapBiasness)
                //{
                //    minDiffValue = diff;

                //}
            }
        }

        if (listOfTerrainIndex.Count > 0)
            return listOfTerrainIndex[Random.Range(0, listOfTerrainIndex.Count)];
        else
            return - 1;
    }

    private IEnumerator GenerateTerrain(float[,] noiseMap, Texture2D mapTexture, MeshData meshData)
    {

        //Clearing Previous If Any
        //------------------------
#if UNITY_EDITOR
        Transform[] destroyingObject = meshRenderer.GetComponentsInChildren<Transform>();
        int numberOfObjectToBeDestroyed = destroyingObject.Length;
        for (int i = 1; i < numberOfObjectToBeDestroyed; i++)
        {
            if (destroyingObject[i] != null)
            {
                if (!EditorApplication.isPlaying)
                    DestroyImmediate(destroyingObject[i].gameObject);
                else
                    Destroy(destroyingObject[i].gameObject);
            }
        }
#endif

       

        //Data Initialization
        //------------------------
        _listOfTerrain = new List<GameObject>();

        SpeedRacerMapDataAsset speedRacerTerrainData = speedRacerMapTextureGenerator.terrainData;
        int numberOfRegion = speedRacerTerrainData.regions.Length;

        int textureWidth = noiseMap.GetLength(0);
        int textureHeight= noiseMap.GetLength(1);

        int meshWidth = meshData.meshWidth;
        int meshHeight= meshData.meshHeight;

        Color[] colorMap = mapTexture.GetPixels();

        //Creating Terrain Parent
        //------------------------
        TerrainRegionHierarchy[] terrainRegionHeirarchy = new TerrainRegionHierarchy[numberOfRegion];
        for (int i = 0; i < numberOfRegion; i++)
        {
            int numberOfRegionLayer = speedRacerTerrainData.regions[i].regionLayers.Length;

            Transform regionParent = new GameObject($"Region - {speedRacerTerrainData.regions[i].regionName}").transform;
            regionParent.SetParent(meshRenderer.transform);
            regionParent.localPosition = Vector3.zero;

            terrainRegionHeirarchy[i] = new TerrainRegionHierarchy() { regionParentTransform = regionParent, terrainRegionLayerHierarchy = new TerrainRegionLayerHierarchy[numberOfRegionLayer] };
            for (int j = 0; j < numberOfRegionLayer; j++)
            {
                Transform regionLayerParent = new GameObject($"RegionLayer - {speedRacerTerrainData.regions[i].regionLayers[j].regionLayerName}").transform;
                regionLayerParent.SetParent(regionParent);
                regionLayerParent.localPosition = Vector3.zero;

                terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].regionLayerParentTransform = regionLayerParent;

                int numberOfTerrainAsset = speedRacerTerrainData.regions[i].regionLayers[j]._terrainAsset.Length;
                terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].terrainAssetPrefab = new GameObject[numberOfTerrainAsset];
                for (int k = 0; k < numberOfTerrainAsset; k++)
                {
                    terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].terrainAssetPrefab[k] = Instantiate(
                            speedRacerTerrainData.regions[i].regionLayers[j]._terrainAsset[k].terrainPrefab,
                            meshRenderer.transform
                        );
                    terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].terrainAssetPrefab[k].name = $"BluePrint - {speedRacerTerrainData.regions[i].regionLayers[j]._terrainAsset[k].terrainPrefab}";

                }

                terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].listOfTerrainMeshRenderer = new List<MeshRenderer>();
            }
        }

        //Data Processing
        //------------------------
        for (int regionIndex = 0; regionIndex < numberOfRegion; regionIndex++)
        {
            Color regionColor = speedRacerTerrainData.regions[regionIndex].regionColor;

            int numberOfRegionLayer = speedRacerTerrainData.regions[regionIndex].regionLayers.Length;
            for (int regionLayerIndex = 0; regionLayerIndex < numberOfRegionLayer; regionLayerIndex++)
            {
                int numberOfTerrainAssetInRegionLayer = speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset.Length;

                for (int meshRow = 0; meshRow < meshHeight - 1; meshRow++)
                {
                    for (int meshColumn = 0; meshColumn < meshWidth - 1; meshColumn++)
                    {
                        int meshIndex = (meshRow * meshWidth) + meshColumn;

                        int textureColumn = (int)(textureWidth * (meshColumn / (meshWidth * 1f)));
                        int textureRow = (int)(textureHeight * (meshRow / (meshHeight * 1f)));
                        int textureIndex = (textureRow * textureWidth) + textureColumn;

                        float noiseMapValue = noiseMap[textureColumn, textureRow];

                        Color colorMapColor     = colorMap[textureIndex];
                        Vector3 colorMapVector  = new Vector3(colorMapColor.r, colorMapColor.g, colorMapColor.b).normalized;
                        float matchedValue = Vector3.Dot(
                                new Vector3(regionColor.r, regionColor.g, regionColor.b).normalized,
                                colorMapVector
                            );


                        Vector3 sourcePosition  = meshData.vertices[meshIndex] + new Vector3(0.5f, 500f, 0.5f);
                        int terrainIndex = GetTerrainAsset(
                            noiseMapValue,
                            regionIndex == 0 ? 0 : speedRacerTerrainData.regions[regionIndex - 1].regionInterpolatedHeight,
                            speedRacerTerrainData.regions[regionIndex].regionInterpolatedHeight,
                            speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex].terrainDensityOnRegionLayer,
                            numberOfTerrainAssetInRegionLayer, 
                            speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset);

                        if (terrainIndex != -1)
                        {
                            SpeedRacerTerrainDataAsset terrainAsset = speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset[terrainIndex];

                            float probability = Random.Range(0f, 1f);

                            if (matchedValue >= .99f
                            && probability <= terrainAsset.terrainDensity * speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex].terrainDensityOnRegionLayer)
                            {
                                //Debug.DrawRay(
                                //        sourcePosition,
                                //        Vector3.down * 510,
                                //        speedRacerTerrainData.regions[regionIndex].regionColor,
                                //        1f
                                //    );
                                if (Physics.Raycast(new Ray(sourcePosition, Vector3.down), out RaycastHit raycastHit, 510))
                                {

                                    TagReference[] filteredTagReferences = speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex].tagsForPlacingTerrain;
                                    int numberOfFilteredTagReferences = filteredTagReferences.Length;
                                    float boundCompromizedValue = 1 - speedRacerMapTextureGenerator.terrainData.regions[regionIndex].regionLayers[regionLayerIndex].boundCompromization;

                                    for (int filteredTagIndex = 0; filteredTagIndex < numberOfFilteredTagReferences; filteredTagIndex++)
                                    {
                                        if (filteredTagReferences[filteredTagIndex].Value.Equals(raycastHit.collider.tag))
                                        {
                                            bool generateTerrain = true;
                                            Vector3 newTerrainScale = Vector3.Lerp(terrainAsset.lowerScaleBound, terrainAsset.randomScaleUpperBound, Random.Range(0f, 1f));
                                            if (speedRacerTerrainData.regions[regionIndex].regionLayers[regionLayerIndex].checkBoundingBox)
                                            {
                                                MeshRenderer meshRendererReference          = terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].terrainAssetPrefab[terrainIndex].GetComponent<SpeedRacerTerrain>().MeshRendererReference;
                                                meshRendererReference.transform.localScale  = newTerrainScale * boundCompromizedValue;

                                                int numberOfTerrainOfRegionLayer = terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer.Count;
                                                for (int filteredTerrainIndex = 0; filteredTerrainIndex < numberOfTerrainOfRegionLayer; filteredTerrainIndex++)
                                                {
                                                    meshRendererReference.transform.position = raycastHit.point;

                                                    Vector3 terrainCurrentScale = terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer[filteredTerrainIndex].transform.localScale;
                                                    terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer[filteredTerrainIndex].transform.localScale = terrainCurrentScale * boundCompromizedValue;

                                                    bool hasIntersected = meshRendererReference.bounds.Intersects(terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer[filteredTerrainIndex].bounds);
                                                    terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer[filteredTerrainIndex].transform.localScale = terrainCurrentScale;
                                                    
                                                    if(hasIntersected){
                                                        generateTerrain = false;
                                                        break;
                                                    }

                                                    
                                                }

                                                meshRendererReference.transform.localScale = Vector3.one;

                                            }

                                            if (generateTerrain)
                                            {
                                                GameObject terrain = Instantiate(
                                                    terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].terrainAssetPrefab[terrainIndex],
                                                    raycastHit.point,
                                                    Quaternion.Euler(Vector3.up * Random.Range(0, 360))
                                                );
                                                terrain.transform.localScale = newTerrainScale;
                                                terrain.name = terrainAsset.terrainPrefab.name;

                                                terrain.transform.SetParent(terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].regionLayerParentTransform);
                                                terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer.Add(terrain.GetComponent<SpeedRacerTerrain>().MeshRendererReference);
                                            }

                                            break;
                                        }
                                    }
                                }

                            }


                        }


                    }
                }
                Physics.SyncTransforms();
                yield return new WaitForSeconds(Time.fixedDeltaTime * 2f);
            }
        }

        //Data Cleanup
        //------------------------
        for (int i = 0; i < numberOfRegion; i++)
        {
            int numberOfRegionLayer = terrainRegionHeirarchy[i].terrainRegionLayerHierarchy.Length;
            for (int j = 0; j < numberOfRegionLayer; j++)
            {
                foreach(GameObject terrainPrefabBlueprint in terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].terrainAssetPrefab)
                {


#if UNITY_EDITOR
                    if (!EditorApplication.isPlaying)
                        DestroyImmediate(terrainPrefabBlueprint);
                    else
                        Destroy(terrainPrefabBlueprint);
#else
                Destroy(terrainPrefabBlueprint);
#endif


                }
            }
        }

    }

#endregion

#region Public Callback

    public void GenerateMap()
    {
        float[,] noiseMap;
        Color[] colorMap;
        Texture2D mapTexture = speedRacerMapTextureGenerator.GenerateMapTexture(out noiseMap, out colorMap);
        MeshData meshData = SpeedRacerMapMeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplierForInnerRegion, meshHeightCurveForInnerRegion, speedRacerMapTextureGenerator.levelOfDetail);

        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = mapTexture;
        meshRenderer.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;

        //meshRenderer.transform.localScale = Vector3.one * mapScale;

        StartCoroutine(GenerateTerrain(noiseMap, mapTexture, meshData));
    }

    public void GenerateMapChunk()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
            Destroy(meshRenderer.gameObject);

        int absoluteLevelOfDetail = Mathf.CeilToInt(levelOfDetail * 6);

        float[,] noiseMap;
        Color[] colorMap;
        Texture2D mapTexture = speedRacerMapTextureGenerator.GenerateMapTexture(out noiseMap, out colorMap);

        float startRow      = -((gridSize * SpeedRacerMapMeshGenerator.MESH_CHUNK_SIZE) / 2f) - (gridSize %2 == 0? (SpeedRacerMapMeshGenerator.MESH_CHUNK_SIZE / 2f) : 0);
        float startColumn   = startRow;

        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                int index = row * gridSize + column;
                MeshData meshData = SpeedRacerMapMeshGenerator.GenerateTerrainMeshChunk(row, column, gridSize, noiseMap, heightMultiplierForInnerRegion, meshHeightCurveForInnerRegion, absoluteLevelOfDetail);
                
                SpeedRacerMapChunk mapChunk = Instantiate(mapChunkPrefab, transform).GetComponent<SpeedRacerMapChunk>();
                mapChunk.name       = $"MapChunk({row},{column})|({index})";

                mapChunk.meshFilter.mesh = meshData.CreateMesh();
                mapChunk.meshRenderer.material.mainTexture = mapTexture;

                mapChunk.transform.localPosition = new Vector3(startColumn + (column * SpeedRacerMapMeshGenerator.MESH_CHUNK_SIZE), 1, startRow + ((gridSize - row + 1) * SpeedRacerMapMeshGenerator.MESH_CHUNK_SIZE));
            }
        }
    }

#endregion
}
