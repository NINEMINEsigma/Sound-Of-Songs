using System.Collections;
using System.Collections.Generic;
using AD.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AD.Derivation.GameEditor
{
    public class AreaDetecter : ADUI
    {
        private void Start()
        {
            ADUI.Initialize(this);
        }
        private void OnDestroy()
        {
            ADUI.Destroy(this);
        }

        public string Message = "";

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            GameEditorApp.instance.GetController<Information>().Log(Message);
        }
    }
}
