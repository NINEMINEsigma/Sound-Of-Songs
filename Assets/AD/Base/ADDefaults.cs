using UnityEngine;

#if UNITY_EDITOR
#endif

namespace AD.BASE
{
    public class ADDefaults : ScriptableObject
    {
        [SerializeField]
        public ADSerializableSettings settings = new();

        public bool addMgrToSceneAutomatically = false;
        public bool autoUpdateReferences = true;
        public bool addAllPrefabsToManager = true;
        public int collectDependenciesDepth = 4;

        public bool logDebugInfo = false;
        public bool logWarnings = true;
        public bool logErrors = true;
    }
}
