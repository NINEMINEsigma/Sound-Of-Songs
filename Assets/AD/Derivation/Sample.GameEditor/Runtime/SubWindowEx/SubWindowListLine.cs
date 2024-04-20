using System;
using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using AD.UI;
using UnityEngine;
using UnityEngine.Events;

namespace AD.Derivation.GameEditor
{
    public class SubWindowListLine : ListViewItem
    {
        public ViewController Icon;
        [SerializeField] private Sprite IconDefaultSprite;

        [SerializeField] private Transform ItemLayerRoot;
        [SerializeField] private Button Prefab;

        private List<GameObject> childs = new();

        public override ListViewItem Init()
        {
            foreach (var child in childs)
            {
                GameObject.Destroy(child);
            }
            childs.Clear();
            Icon.SourcePairs.Clear();
            Icon.SourcePairs.Add(new()
            {
                Name = "Default",
                SpriteName = "Default",
                SpriteSource = IconDefaultSprite
            });
            return this;
        }

        public class DataEntry
        {
            public UnityAction<bool> callback;
            public UnityAction callbackNobool;
            public Sprite icon;
            public string message;

            public DataEntry(UnityAction<bool> callback) : this(callback, null, null) { }

            public DataEntry(UnityAction<bool> callback, string message) : this(callback, null, message) { }

            public DataEntry(UnityAction<bool> callback, Sprite icon) : this(callback, icon, "") { }

            public DataEntry(UnityAction<bool> callback, Sprite icon, string message)
            {
                this.callback = callback;
                this.icon = icon;
                this.message = message ?? "";
            }

            public DataEntry(UnityAction callback) : this(callback, null, null) { }

            public DataEntry(UnityAction callback, string message) : this(callback, null, message) { }

            public DataEntry(UnityAction callback, Sprite icon) : this(callback, icon, "") { }

            public DataEntry(UnityAction callback, Sprite icon, string message)
            {
                this.callbackNobool = callback;
                this.icon = icon;
                this.message = message ?? "";
            }
        }

        public void Setup(IEnumerable<DataEntry> datas)
        {
            foreach (var data in datas)
            {
                var cat = Prefab.PrefabInstantiate();
                cat.transform.SetParent(ItemLayerRoot, false);
                cat.transform.localScale = Vector3.one;
                cat.RemoveAllListeners();
                if (data.callback != null)
                {
                    cat.AddListener(()=>data.callback.Invoke(true), PressType.ThisFramePressed);
                    cat.AddListener(() => data.callback.Invoke(false), PressType.ThisFrameReleased);
                    cat.IsKeepState = true;
                }
                else if(data.callbackNobool!=null)
                {
                    cat.AddListener(data.callbackNobool, PressType.ThisFramePressed);
                    cat.IsKeepState = false;
                }
                else cat.IsKeepState = false;
                if (data.icon != null)
                    cat.SetView(data.icon);
                cat.SetTitle(data.message);
                childs.Add(cat.gameObject);
            }
        }
    }
}
