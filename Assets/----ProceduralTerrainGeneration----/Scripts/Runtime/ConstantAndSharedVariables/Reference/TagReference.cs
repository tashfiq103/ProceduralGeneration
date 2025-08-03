namespace com.faith.procedural
{
    using UnityEngine;

    [System.Serializable]
    public class TagReference
    {

        #region Private Variables

#if UNITY_EDITOR

        [SerializeField] private int _tagIndex;
#endif

        #endregion

        #region Public Variables

        public bool UseConstant = true;

        public string ConstantValue;
        public TagVariable Variable;

        #endregion

        #region Public Callback

        public TagReference(string value)
        {
            UseConstant = true;
            ConstantValue = value;
        }

        [UnityEngine.SerializeField]
        public string Value
        {
            get
            {
                if (UseConstant)
                    return ConstantValue;
                else
                {
                    if (Variable != null)
                        return Variable.Value;
                    else
                    {
                        Debug.LogWarning("Variable (ScriptableObject) not assigned, returning 'ConstantValue'.");
                        return ConstantValue;
                    }
                }
            }
        }

        public static implicit operator string(TagReference reference)
        {
            return reference.Value;
        }

        #endregion
    }
}



