using AD.BASE;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AD.UI
{
    [RequireComponent(typeof(Camera))]
    public class Projection : MonoBehaviour
    {
        public List<UnityEngine.UI.RawImage> Options = new();

        public RenderTexture Source;
        public Vector3 Rect;

        private void OnEnable()
        {
            Source = new((int)Rect.x,(int)Rect.y,(int)Rect.z);
            foreach (var item in Options)
            {
                item.texture = Source;
            }
            this.GetComponent<Camera>().targetTexture = Source;
        }
    }
}