using AD.BASE;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AD.Derivation.GameEditor
{
    public class PropertiesItemSubListEx : MonoBehaviour
    {
        public void Unfold()
        {
            transform.parent.As<RectTransform>().Share(out var rectTransform).sizeDelta = new Vector2(rectTransform.sizeDelta.x, 300);
        }
        public void Fold()
        {
            transform.parent.As<RectTransform>().Share(out var rectTransform).sizeDelta = new Vector2(rectTransform.sizeDelta.x, 30);
        }
    }
}
