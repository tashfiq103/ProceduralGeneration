namespace com.faith.procedural
{
    using UnityEngine;

    [CreateAssetMenu(
        fileName = "Tag Variable",
        menuName = ScriptableObjectAssetMenu.MENU_UNITY_TAG_VARIABLE,
        order = ScriptableObjectAssetMenu.ORDER_UNITY_TAG_VARIABLE)]
    public class TagVariable : ScriptableObject
    {
#if UNITY_EDITOR
        [Multiline]
        public string DeveloperDescription = "";

        [SerializeField] private int _tagIndex;
#endif
        public string Value;

        public void SetValue(string value)
        {
            Value = value;
        }

        public void SetValue(TagVariable value)
        {
            Value = value.Value;
        }
    }
}

