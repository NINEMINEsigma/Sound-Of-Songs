using System;
using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using AD.UI;
using UnityEngine;

namespace AD.Derivation.GameEditor
{
    public class SubWindowEx : ADController
    {
        public class SubEntry
        {
            public Rect rect;
            public RectTransform transform;
        }
        private void Start()
        {
            GameEditorApp.instance.RegisterController<SubWindowEx>(this);
        }

        private List<SubEntry> RectAreas = new();

        [SerializeField] private RectTransform ButtomRoot;
        [SerializeField] private RectTransform ObjectLayerRoot;

        [SerializeField] private Text Title;
        [SerializeField] private ListView MyListView;
        public int ListViewCount => MyListView.Childs.Count;

        public override void Init()
        {
            InitAllRectArea();
        }

        public void InitAllRectArea()
        {
            if (RectAreas == null) RectAreas = new();
            else
            {
                foreach (var area in RectAreas)
                {
                    GameObject.Destroy(area.transform.gameObject);
                }
                RectAreas.Clear();
            }
        }

        public void GetRectArea(Rect rect, int[] buffer)
        {
            int head = 0;
            for (int i = 0, e = RectAreas.Count, e2 = buffer.Length; i < e && head < e2; i++)
            {
                SubEntry area = RectAreas[i];
                if (rect.Overlaps(area.rect))
                {
                    buffer[head++] = i;
                }
            }
            while (head < buffer.Length)
            {
                buffer[head] = -1;
                head++;
            }
        }

        public void GetRectArea(Rect rect, out int result)
        {
            for (int i = 0, e = RectAreas.Count; i < e; i++)
            {
                SubEntry area = RectAreas[i];
                if (rect.Overlaps(area.rect))
                {
                    result = i;
                    return;
                }
            }
            result = -1;
        }

        public int[] GetRectArea(Rect rect)
        {
            List<int> buffer = new();
            for (int i = 0, e = RectAreas.Count; i < e; i++)
            {
                SubEntry area = RectAreas[i];
                if (rect.Overlaps(area.rect))
                {
                    buffer.Add(i);
                }
            }
            return buffer.ToArray();
        }

        public T SeekComponent<T>(params int[] ids) where T : Component
        {
            foreach (var id in ids)
            {
                if (id < RectAreas.Count)
                {
                    T result = RectAreas[id].transform.gameObject.SeekComponent<T>();
                    if (result != null) return result;
                }
            }
            return null;
        }

        public bool Move(int id, Rect rect)
        {
            if (id < RectAreas.Count)
            {
                SubEntry entry = RectAreas[id];
                entry.transform.offsetMax = Vector2.one;
                entry.transform.offsetMin = Vector2.zero;
                entry.rect = rect;
                entry.transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, rect.x, rect.width);
                entry.transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, rect.y, rect.height);
                return true;
            }
            return false;
        }

        public int Insert(Rect rect, RectTransform target)
        {
            SubEntry entry = new()
            {
                rect = rect,
                transform = target
            };
            target.SetParent(ObjectLayerRoot);
            entry.transform.offsetMax = Vector2.one;
            entry.transform.offsetMin = Vector2.zero;
            entry.transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, rect.x, rect.width);
            entry.transform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, rect.y, rect.height);
            RectAreas.Add(entry);
            return RectAreas.Count - 1;
        }

        public void BreakRect(Rect rect)
        {
            int i = 0;
            while (i<RectAreas.Count)
            {
                if (RectAreas[i].rect.Overlaps(rect))
                {
                    BreakAt(i);
                }
                else i++;
            }
        }

        public void BreakAt(int id)
        {
            RectAreas.RemoveAt(id);
        }

        public class ListLineInfo
        {
            public ListLineInfo(ListView listView, SubWindowListLine target)
            {
                this.listView = listView;
                this.target = target;
            }

            private ListView listView;

            public SubWindowListLine target;

            public void Unregister()
            {
                listView.Remove(target.gameObject);
            }
        }

        protected ListLineInfo BuildListLine()
        {
            return new ListLineInfo(MyListView, MyListView.GenerateItem() as SubWindowListLine);
        }

        public void SetTitle(string title)
        {
            Title.SetText(title);
        }

        public void ClearLines()
        {
            MyListView.Clear();
        }

        public SubWindowListLine AddLine(Sprite sprite)
        {
            var result= MyListView.GenerateItem() as SubWindowListLine;
            result.Icon.CurrentImagePair = new()
            {
                SpriteSource = sprite,
            };
            result.Icon.Refresh();
            return result;
        }

        public SubWindowListLine AddLine()
        {
            var result = MyListView.GenerateItem() as SubWindowListLine;
            return result;
        }
    }
}
