namespace com.faith.procedural
{
    using UnityEngine;

    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class TerrainViwer : MonoBehaviour
    {
        #region Public Variables

        public MeshRenderer MeshRendererReference { get => _meshRendererReference; }
        public MeshCollider MeshColliderReference { get => _meshColliderReference; }

        #endregion

        #region Private Variables

#if UNITY_EDITOR

        [SerializeField] private bool _showGizomos;

#endif

        private MeshRenderer _meshRendererReference;
        private MeshCollider _meshColliderReference;

        private const bool GIZMOS_ON_SELECTED = false;

        #endregion

        #region Configuretion

#if UNITY_EDITOR

        private void GizmosGUI()
        {
            if (_showGizomos)
            {
                if (_meshRendererReference != null)
                {
                    Gizmos.DrawWireCube(
                            _meshRendererReference.bounds.center,
                            _meshRendererReference.bounds.size
                        );
                }
            }




        }

#endif


        #endregion

        #region Mono Behaviour

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (GIZMOS_ON_SELECTED)
                GizmosGUI();
        }

        private void OnDrawGizmos()
        {
            if (!GIZMOS_ON_SELECTED)
                GizmosGUI();
        }

        private void OnValidate()
        {

        }

#endif

        private void Awake()
        {
            _meshRendererReference = GetComponent<MeshRenderer>();
            _meshColliderReference = GetComponent<MeshCollider>();


        }



        private void OnEnable()
        {
            _meshRendererReference = GetComponent<MeshRenderer>();
            _meshColliderReference = GetComponent<MeshCollider>();
        }



        #endregion
    }

}
