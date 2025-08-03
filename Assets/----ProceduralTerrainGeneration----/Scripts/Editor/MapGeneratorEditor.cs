namespace BarsStudio.InternalTool.ProceduralTerrainGeneration
{
#if UNITY_EDITOR

    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        #region Private Variables

        private MapGenerator _reference;

        #endregion


        #region Editor

        private void OnEnable()
        {
            _reference = (MapGenerator)target;

            if (_reference == null)
                return;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (DrawDefaultInspector())
            {
                if (_reference.autoUpdate)
                    _reference.GenerateMap();
            }

            if (GUILayout.Button("Generate"))
            {
                _reference.GenerateMap();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif
}

