namespace BarsStudio.InternalTool.ProceduralTerrainGeneration
{
    using UnityEngine;

    public class MapDisplay : MonoBehaviour
    {
        #region Public Variables

        public Renderer textureRenderer;

        [Space(5f)]
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        #endregion

        #region Public Callback

        public void DrawTexture(Texture2D texture)
        {
            textureRenderer.sharedMaterial.mainTexture = texture;
            textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }

        public void DrawMesh(MeshData meshData, Texture2D texture)
        {
            meshFilter.sharedMesh = meshData.CreateMesh();
            meshRenderer.sharedMaterial.mainTexture = texture;
        }

        #endregion
    }
}

