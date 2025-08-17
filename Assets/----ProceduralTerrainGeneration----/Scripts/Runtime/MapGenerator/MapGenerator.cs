// Runtime-safe, adaptively batched generator with per-layer activation gating.
// Region spawning order: highest index to lowest (e.g., 4-3-2-1)
namespace com.faith.procedural
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine.Events;

#if UNITY_EDITOR
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        private MapGenerator _reference;

        private void OnEnable()
        {
            _reference = (MapGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map (Editor/Play)"))
            {
                _reference.GenerateMap();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public class MapGenerator : MonoBehaviour
    {
        private struct TerrainRegionHierarchy
        {
            public Transform regionParentTransform;
            public TerrainRegionLayerHierarchy[] terrainRegionLayerHierarchy;
        }

        private struct TerrainRegionLayerHierarchy
        {
            public Transform regionLayerParentTransform;
            public GameObject[] terrainAssetPrefab;
            public List<MeshRenderer> listOfTerrainMeshRenderer;
        }

        [Header("External References")]
        public MapTextureGenerator mapTextureGenerator;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        [HideInInspector] public float heightMultiplierForInnerRegion = 0f;
        [HideInInspector] public AnimationCurve meshHeightCurveForInnerRegion =
            new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        [Header("Runtime Generation Controls")]
        public bool generateOnStart = true;

        [Tooltip("Approximate frame time to spend on placement per frame (in milliseconds). Higher = faster generation, lower = smoother frame time.")]
        [Min(1f)] public float frameBudgetMs = 5f;

        [Tooltip("Optional progress event [0..1] invoked as rows are processed (per layer).")]
        public UnityEvent<float> onProgress;

        private List<GameObject> _listOfTerrain;

        private void Start()
        {
            if (generateOnStart)
                GenerateMap();
        }

        private int GetTerrainAsset(
            float heightMap, float regionMinHeight, float regionMaxHeight, float regionDensity,
            int numberOfTerrainAsset, TerrainDataAsset[] terrainAssets)
        {
            float interpolatedRandomPoint = Mathf.Lerp(regionMinHeight, regionMaxHeight, Random.Range(0f, 1f));
            List<int> listOfTerrainIndex = new List<int>();

            for (int terrainAssetIndex = 0; terrainAssetIndex < numberOfTerrainAsset; terrainAssetIndex++)
            {
                float interpolatedDensity = Mathf.Lerp(regionMinHeight, regionMaxHeight, terrainAssets[terrainAssetIndex].terrainDensity * regionDensity);

                if (interpolatedDensity >= interpolatedRandomPoint)
                {
                    float interpolatedHeightMap = Mathf.Lerp(regionMinHeight, regionMaxHeight, heightMap);
                    float interpolatedHeightBiasness = Mathf.Lerp(regionMinHeight, regionMaxHeight, terrainAssets[terrainAssetIndex].heightMapBiasness);

                    if (interpolatedHeightMap >= interpolatedHeightBiasness)
                        listOfTerrainIndex.Add(terrainAssetIndex);
                }
            }

            if (listOfTerrainIndex.Count > 0)
                return listOfTerrainIndex[Random.Range(0, listOfTerrainIndex.Count)];
            else
                return -1;
        }

        private void DestroyAllChildren(Transform parent)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in parent) toDestroy.Add(child.gameObject);
            for (int i = 0; i < toDestroy.Count; i++) Destroy(toDestroy[i]);
        }

        private IEnumerator GenerateTerrain(float[,] noiseMap, Texture2D mapTexture, MeshData meshData)
        {
            // Cleanup previous terrain (children of the meshRenderer only)
            DestroyAllChildren(meshRenderer.transform);

            // Data init
            _listOfTerrain = new List<GameObject>();

            MapDataAsset terrainData = mapTextureGenerator.terrainData;
            int numberOfRegion = terrainData.regions.Length;

            int textureWidth = noiseMap.GetLength(0);
            int textureHeight = noiseMap.GetLength(1);
            int meshWidth = meshData.meshWidth;
            int meshHeight = meshData.meshHeight;

            Color[] colorMap = mapTexture.GetPixels();

            // Build region/layer parents and blueprint prefabs
            TerrainRegionHierarchy[] terrainRegionHeirarchy = new TerrainRegionHierarchy[numberOfRegion];
            for (int i = 0; i < numberOfRegion; i++)
            {
                int numberOfRegionLayer = terrainData.regions[i].regionLayers.Length;
                string regionName = $"Region - {terrainData.regions[i].regionName}";
                Debug.Log(regionName);
                Transform regionParent = new GameObject(regionName).transform;

                regionParent.SetParent(meshRenderer.transform, false);

                terrainRegionHeirarchy[i] = new TerrainRegionHierarchy
                {
                    regionParentTransform = regionParent,
                    terrainRegionLayerHierarchy = new TerrainRegionLayerHierarchy[numberOfRegionLayer]
                };

                for (int j = 0; j < numberOfRegionLayer; j++)
                {
                    string regionLayerName = $"RegionLayer - {terrainData.regions[i].regionLayers[j].regionLayerName}";
                    Debug.Log(regionLayerName);
                    Transform regionLayerParent = new GameObject(regionLayerName).transform;
                    regionLayerParent.SetParent(regionParent, false);

                    int numberOfTerrainAsset = terrainData.regions[i].regionLayers[j]._terrainAsset.Length;
                    GameObject[] blueprints = new GameObject[numberOfTerrainAsset];

                    for (int k = 0; k < numberOfTerrainAsset; k++)
                    {
                        GameObject bp = Instantiate(terrainData.regions[i].regionLayers[j]._terrainAsset[k].terrainPrefab, meshRenderer.transform);
                        bp.name = $"BluePrint - {terrainData.regions[i].regionLayers[j]._terrainAsset[k].terrainPrefab.name}";
                        //bp.SetActive(false); // keep inactive until placement
                        blueprints[k] = bp;
                    }

                    terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j] = new TerrainRegionLayerHierarchy
                    {
                        regionLayerParentTransform = regionLayerParent,
                        terrainAssetPrefab = blueprints,
                        listOfTerrainMeshRenderer = new List<MeshRenderer>()
                    };
                }
            }

            // Precompute normalized region colors
            Vector3[] normalizedRegionColors = new Vector3[numberOfRegion];
            for (int r = 0; r < numberOfRegion; r++)
            {
                Color rc = terrainData.regions[r].regionColor;
                normalizedRegionColors[r] = new Vector3(rc.r, rc.g, rc.b).normalized;
            }

            // Adaptive batching across frames (no exposed min/max rows)
            float frameBudget = Mathf.Max(0.001f, frameBudgetMs / 1000f); // seconds
            int batchRows = 1;                // start conservative
            int hardMaxRows = 1024;           // internal safety cap

            // === Reverse region order: highest index to lowest ===
            for (int regionIndex = 0; regionIndex < numberOfRegion; regionIndex++)
            {
                int numberOfRegionLayer = terrainData.regions[regionIndex].regionLayers.Length;
                Vector3 normalizedRegion = normalizedRegionColors[regionIndex];
                Debug.Log($"Generating Region : {terrainData.regions[regionIndex].regionName}");
                for (int regionLayerIndex = 0; regionLayerIndex < numberOfRegionLayer; regionLayerIndex++)
                {

                    Debug.Log($"Generating Region : {terrainData.regions[regionIndex].regionLayers[regionLayerIndex].regionLayerName}");
                    // Collect spawned objects per layer; they remain inactive until layer completes
                    List<GameObject> spawnedForLayer = new List<GameObject>();

                    int numberOfTerrainAssetInRegionLayer = terrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset.Length;

                    int processedRows = 0;
                    for (int meshRow = 0; meshRow < meshHeight - 1; )
                    {
                        float frameStart = Time.realtimeSinceStartup;

                        int rowsThisFrame = Mathf.Clamp(batchRows, 1, Mathf.Min(hardMaxRows, (meshHeight - 1) - meshRow));
                        for (int rr = 0; rr < rowsThisFrame; rr++, meshRow++)
                        {
                            int row = meshRow;
                            for (int meshColumn = 0; meshColumn < meshWidth - 1; meshColumn++)
                            {
                                int meshIndex = (row * meshWidth) + meshColumn;

                                int textureColumn = (int)(textureWidth * (meshColumn / (meshWidth * 1f)));
                                int textureRow = (int)(textureHeight * (row / (meshHeight * 1f)));
                                int textureIndex = (textureRow * textureWidth) + textureColumn;

                                float noiseMapValue = noiseMap[textureColumn, textureRow];

                                Color colorMapColor = colorMap[textureIndex];
                                Vector3 colorMapVector = new Vector3(colorMapColor.r, colorMapColor.g, colorMapColor.b).normalized;
                                float matchedValue = Vector3.Dot(normalizedRegion, colorMapVector);

                                Vector3 sourcePosition = meshFilter.transform.position +  meshData.vertices[meshIndex] + new Vector3(0.5f, 500f, 0.5f);
                                int terrainIndex = GetTerrainAsset(
                                    noiseMapValue,
                                    regionIndex == 0 ? 0 : terrainData.regions[regionIndex - 1].regionSpreadArea,
                                    terrainData.regions[regionIndex].regionSpreadArea,
                                    terrainData.regions[regionIndex].regionLayers[regionLayerIndex].terrainDensityOnRegionLayer,
                                    numberOfTerrainAssetInRegionLayer,
                                    terrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset
                                );

                                if (terrainIndex == -1) continue;

                                TerrainDataAsset terrainAsset = terrainData.regions[regionIndex].regionLayers[regionLayerIndex]._terrainAsset[terrainIndex];
                                float probability = Random.Range(0f, 1f);

                                if (matchedValue >= .99f &&
                                    probability <= terrainAsset.terrainDensity * terrainData.regions[regionIndex].regionLayers[regionLayerIndex].terrainDensityOnRegionLayer)
                                {
                                    if (Physics.Raycast(new Ray(sourcePosition, Vector3.down), out RaycastHit raycastHit, 510f))
                                    {
                                        TagReference[] filteredTagReferences = terrainData.regions[regionIndex].regionLayers[regionLayerIndex].tagsForPlacingTerrain;
                                        float boundCompromizedValue = 1f - terrainData.regions[regionIndex].regionLayers[regionLayerIndex].boundCompromization;

                                        for (int filteredTagIndex = 0; filteredTagIndex < filteredTagReferences.Length; filteredTagIndex++)
                                        {
                                            if (filteredTagReferences[filteredTagIndex].Value.Equals(raycastHit.collider.tag))
                                            {
                                                bool generateTerrain = true;
                                                Vector3 newTerrainScale = Vector3.Lerp(terrainAsset.lowerScaleBound, terrainAsset.randomScaleUpperBound, Random.Range(0f, 1f));

                                                if (terrainData.regions[regionIndex].regionLayers[regionLayerIndex].checkBoundingBox)
                                                {
                                                    MeshRenderer meshRendererReference =
                                                        terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex]
                                                        .terrainAssetPrefab[terrainIndex].GetComponent<TerrainViwer>().MeshRendererReference;

                                                    meshRendererReference.transform.localScale = newTerrainScale * boundCompromizedValue;
                                                    meshRendererReference.transform.position = raycastHit.point;

                                                    var existing = terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].listOfTerrainMeshRenderer;
                                                    int n = existing.Count;
                                                    for (int e = 0; e < n; e++)
                                                    {
                                                        MeshRenderer other = existing[e];
                                                        Vector3 originalScale = other.transform.localScale;
                                                        other.transform.localScale = originalScale * boundCompromizedValue;

                                                        if (meshRendererReference.bounds.Intersects(other.bounds))
                                                        {
                                                            generateTerrain = false;
                                                            other.transform.localScale = originalScale;
                                                            break;
                                                        }

                                                        other.transform.localScale = originalScale;
                                                    }

                                                    meshRendererReference.transform.localScale = Vector3.one;
                                                }

                                                if (generateTerrain)
                                                {
                                                    GameObject terrainGO = Instantiate(
                                                        terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].terrainAssetPrefab[terrainIndex],
                                                        raycastHit.point,
                                                        Quaternion.Euler(Vector3.up * Random.Range(0f, 360f))
                                                    );
                                                    terrainGO.transform.localScale = newTerrainScale;
                                                    terrainGO.name = terrainAsset.terrainPrefab.name;
                                                    terrainGO.transform.SetParent(terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex].regionLayerParentTransform, true);

                                                    // keep disabled until the whole layer is finished
                                                    //terrainGO.SetActive(false);
                                                    spawnedForLayer.Add(terrainGO);

                                                    // Track for bounding checks
                                                    var viewer = terrainGO.GetComponent<TerrainViwer>();
                                                    if (viewer != null)
                                                    {
                                                        var mr = viewer.MeshRendererReference;
                                                        if (mr != null)
                                                        {
                                                            terrainRegionHeirarchy[regionIndex].terrainRegionLayerHierarchy[regionLayerIndex]
                                                                .listOfTerrainMeshRenderer.Add(mr);
                                                        }
                                                    }
                                                }

                                                break; // proceed to next cell after tag decision
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        processedRows += rowsThisFrame;
                        if (onProgress != null)
                        {
                            float progress = (float)processedRows / (meshHeight - 1);
                            onProgress.Invoke(progress);
                        }

                        Physics.SyncTransforms();

                        // Always yield to keep frame pacing smooth
                        yield return null;

                        // Adaptive adjustment of rows
                        float elapsed = Time.realtimeSinceStartup - frameStart;
                        if (elapsed > frameBudget * 1.25f)
                        {
                            batchRows = Mathf.Max(1, batchRows / 2);
                        }
                        else if (elapsed < frameBudget * 0.75f)
                        {
                            batchRows = Mathf.Min(hardMaxRows, batchRows + Mathf.Max(1, batchRows / 2));
                        }
                    }

                    // Layer finished → enable all spawned objects for this layer in one go
                    for (int i = 0; i < spawnedForLayer.Count; i++)
                    {
                        if (spawnedForLayer[i] != null)
                            spawnedForLayer[i].SetActive(true);
                    }
                }
            }

            // Cleanup blueprint prefabs
            for (int i = 0; i < numberOfRegion; i++)
            {
                int numberOfRegionLayer = terrainRegionHeirarchy[i].terrainRegionLayerHierarchy.Length;
                for (int j = 0; j < numberOfRegionLayer; j++)
                {
                    foreach (GameObject terrainPrefabBlueprint in terrainRegionHeirarchy[i].terrainRegionLayerHierarchy[j].terrainAssetPrefab)
                        Destroy(terrainPrefabBlueprint);
                }
            }
        }

        public void GenerateMap()
        {
            float[,] noiseMap;
            Color[] colorMap;
            Texture2D mapTexture = mapTextureGenerator.GenerateMapTexture(out noiseMap, out colorMap);
            MeshData meshData = MapMeshGenerator.GenerateTerrainMesh(
                noiseMap, heightMultiplierForInnerRegion, meshHeightCurveForInnerRegion, mapTextureGenerator.levelOfDetail
            );

            meshFilter.sharedMesh = meshData.CreateMesh();
            meshRenderer.sharedMaterial.mainTexture = mapTexture;

            var collider = meshRenderer.GetComponent<MeshCollider>();
            if (collider != null) collider.sharedMesh = meshFilter.sharedMesh;

            StartCoroutine(GenerateTerrain(noiseMap, mapTexture, meshData));
        }
    }
}
