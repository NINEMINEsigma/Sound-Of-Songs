using AD.BASE;
using AD.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AD.Derivation.GameEditor
{
    public class PropertiesItemSubListEx : MonoBehaviour
    {
        public Button FolderButton;

        public object target;

        private void Invoke()
        {
            if (target == null) return;
            if (PropertiesExLayout.isFolderObject.TryAdd(target, !FolderButton.IsClick))
                PropertiesExLayout.isFolderObject[target] = !FolderButton.IsClick;
        }

        private void Start()
        {
            FolderButton.AddListener(Invoke, AD.PressType.ThisFramePressed);
            FolderButton.AddListener(Invoke, AD.PressType.ThisFrameReleased);
        }

        public void Unfold()
        {
            Transform curTrans = transform.parent;
            while (curTrans != null)
            {
                if (curTrans.SeekComponent<ListView>() || curTrans.SeekComponent<AreaDetecter>())
                {
                    var Rect = curTrans.As<RectTransform>().rect;
                    Rect.height = Rect.height + 270;
                }
                curTrans = curTrans.parent;
            }
        }
        public void Fold()
        {
            Transform curTrans = transform.parent;
            while (curTrans != null)
            {
                if (curTrans.SeekComponent<ListView>() || curTrans.SeekComponent<AreaDetecter>())
                {
                    var Rect = curTrans.As<RectTransform>().rect;
                    Rect.height = Rect.height - 270;
                }
                curTrans = curTrans.parent;
            }
        }
    }
}
