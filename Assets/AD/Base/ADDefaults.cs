using UnityEngine;

#if UNITY_EDITOR
#endif

namespace AD.BASE
{
    public class ADDefaults : ScriptableObject
    {
        public ADSerializableSettings settings = new();

        public bool logDebugInfo = false;
        public bool logWarnings = true;
        public bool logErrors = true;
    }
}
