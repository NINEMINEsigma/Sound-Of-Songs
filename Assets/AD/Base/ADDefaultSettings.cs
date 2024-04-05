using UnityEngine;

#if UNITY_EDITOR
#endif

namespace AD.BASE
{
    public abstract class ADDefaultSettings : MonoBehaviour
    {
        [SerializeField]
        public ADSerializableSettings settings = null;

        public bool autoUpdateReferences = true;
    }
}
