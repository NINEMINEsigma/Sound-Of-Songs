using System;
using System.Collections.Generic;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Derivation.Performance;
using AD.Sample.Texter.Data;
using AD.Sample.Texter.Internal;
using AD.Sample.Texter.Project;
using AD.Sample.Texter.Scene;
using AD.UI;
using AD.Utility;
using AD.Utility.Object;
using UnityEngine;

namespace AD.Sample.Texter
{
    [Serializable]
    public class ProjectTextData : ProjectItemData
    {
        public string text;
        public string description;

        public ProjectTextData() { }

        public ProjectTextData(ProjectTextField projectItem, string text = "", string description = "") : base(projectItem)
        {
            this.text = text;
            this.description = description;
        }
    }
}

namespace AD.Sample.Texter.Project
{
    public class ProjectTextField : MonoBehaviour, IProjectItemWhereNeedInitData, IUpdateOnChange, ICatchCameraRayUpdate, ISetupSingleMenuOnClickRight
    {
        public class ProjectTextFieldBlock : ProjectItemBlock
        {
            public ProjectTextFieldBlock(ProjectTextField target) : base(target)
            {
                that = target;
            }

            public ProjectTextField that;

            protected override void HowSerialize()
            {
                var data = that.ProjectTextSourceData;
                this.MatchItem.SetTitle("Texter");

                DisplayProjectID(data);

                var inputT = PropertiesLayout.InputField(data.text, "Text");
                inputT.AddListener(T => data.text = T);
                PropertiesLayout.Button("Text", "Enter Text On A Bigger Field", () =>
                {
                    var temp = GameEditorApp.instance.GetSystem<GameEditorWindowGenerator>().ObtainElement(new Vector2(1600, 800)).SetTitle("Text ProjectItem".Translate());
                    var mif = ADGlobalSystem.GenerateElement<ModernUIInputField>().PrefabInstantiate();
                    mif.transform.As<RectTransform>().sizeDelta = new Vector2(1550, 750);
                    mif.SetTitle("Description".Translate());
                    temp
                    .SetADUIOnWindow<ModernUIInputField>("Field", mif)
                    .SetText(data.text)
                    .AddListener(T =>
                    {
                        data.text = T;
                        inputT.text = T;
                    });
                });

                var inputD = PropertiesLayout.InputField(data.description, "Text Description");
                inputD.AddListener(T => data.description = T);
                PropertiesLayout.Button("Description", "Enter Description On A Bigger Field", () =>
                {
                    var temp = GameEditorApp.instance.GetSystem<GameEditorWindowGenerator>().ObtainElement(new Vector2(1600, 800)).SetTitle("Text ProjectItem".Translate());
                    var mif = ADGlobalSystem.GenerateElement<ModernUIInputField>().PrefabInstantiate();
                    mif.transform.As<RectTransform>().sizeDelta = new Vector2(1550, 750);
                    mif.SetTitle("Description".Translate());
                    temp
                    .SetADUIOnWindow<ModernUIInputField>("Field", mif)
                    .SetText(data.description)
                    .AddListener(T =>
                    {
                        data.description = T;
                        inputD.text = T;
                    });
                });

                PropertiesLayout.Button("Open", "Open Sub Project Scene", () =>
                {
                    App.instance.CurrentProjectItemData = that.ProjectTextSourceData;
                    App.instance.GetController<MainSceneLoader>().Load<EasyTextManager>(nameof(ProjectTextField));
                });
            }
        }

        public bool IsSetupProjectTextSourceData = false;
        public ProjectTextData ProjectTextSourceData;
        public ProjectItemData SourceData { get => ProjectTextSourceData; set => ProjectTextSourceData = ADGlobalSystem.FinalCheck(value as ProjectTextData); }
        public ISerializeHierarchyEditor MatchHierarchyEditor { get; set; }
        public List<ISerializePropertiesEditor> MatchPropertiesEditors { get; set; }
        public ICanSerializeOnCustomEditor ParentTarget { get; set; }

        public int SerializeIndex { get; set; }

        public List<GameObject> SubProjectItemPrefab => App.instance.GetModel<PrefabModel>().SubProjectItemPrefabs[nameof(ProjectTextField)];

        private void Start()
        {
            StartCoroutine(App.WaitForInit(this));
        }

        public void Init()
        {
            MyEditGroup.OnEnter.AddListener(EnterMe);
            if (IsSetupProjectTextSourceData)
            {
                transform.localPosition = App.GetOriginPosition(ProjectTextSourceData.ProjectItemPosition);
                this.SetParent(ADGlobalSystem.FinalCheckWithThrow(ProjectTextSourceData.Parent.MatchProjectItem));
            }
            else
            {
                ProjectTextSourceData = new(this);
            }
            MatchHierarchyEditor = new HierarchyBlock<ProjectTextField>(this, () => this.SourceData.ProjectItemID);
            MatchPropertiesEditors = new List<ISerializePropertiesEditor>()
            {
                new ProjectTextFieldBlock(this),
                new ProjectItemGeneraterBlock(this,App.Get(SubProjectItemPrefab,false),new(SetupChild))
            };
            App.instance.AddMessage($"Project Item(Text) {ProjectTextSourceData.ProjectItemID} Setup");
            OnMenu = new()
            {
                [0] = new Dictionary<string, ADEvent>()
                {
                    {"Delete",new(()=>
                        {
                            GameObject.Destroy(gameObject);
                            GameEditorApp.instance.GetSystem<SinglePanelGenerator>().Current.BackPool();
                        })
                    }
                }
            };
        }

        private void OnEnable()
        {
            OnChange();
        }

        private void OnDestroy()
        {
            if (ADGlobalSystem.instance == null) return;
            GameEditorApp.instance.GetController<Hierarchy>().RemoveOnTop(this.MatchHierarchyEditor);
            this.ProjectTextSourceData = null;
            this.MatchHierarchyEditor = null;
            this.MatchPropertiesEditors.Clear();
            this.SetParent(null);
        }

        public void SetupChild(IProjectItem child)
        {
            child.As<MonoBehaviour>().transform.position = this.transform.position + new Vector3(2.5f, 0, 0);
        }

        public string ProjectItemBindKey => this.SourceData.ProjectItemID;

        public void ClickOnLeft()
        {

        }

        public Dictionary<int, Dictionary<string, ADEvent>> OnMenu { get; set; }
        public string OnMenuTitle => "Text Menu";

        public void ClickOnRight()
        {

        }

        public List<ICanSerializeOnCustomEditor> Childs = new();

        public List<ICanSerializeOnCustomEditor> GetChilds() => Childs;

        public bool IsAbleDisplayedOnHierarchyAtTheSameTime(Type type)
        {
            return true;
        }

        //EditGroup

        [SerializeField, Header("EditGroup")] private EditGroup m_EditGroup;
        public EditGroup MyEditGroup => m_EditGroup;

        public LineRenderer MyLineRenderer;
        public const int PointCount = 100;
        public const float PointCountM = 1 / (float)PointCount;

        private void EnterMe()
        {
            InternalUtility.InternalDefault_EnterMe(this);
        }

        public void ReDrawLine()
        {
            InternalUtility.InternalDefault_ReDrawLine(this, MyLineRenderer, PointCount, PointCountM, this.ParentTarget as IProjectItem);
        }

        public void OnChange()
        {
            if (this.ParentTarget == null)
            {
                MyLineRenderer.positionCount = 0;
            }
            else
            {
                ReDrawLine();
            }

            foreach (var item in Childs.GetSubList<ICanSerializeOnCustomEditor, IUpdateOnChange>())
            {
                item.As<IProjectItem>().ReDrawLine();
            }
        }

        public void OnRayCatching()
        {
            OnChange();
        }

        public void ExecuteBeforeSave()
        {
            SourceData.MatchProjectItem = this;
            SourceData.ProjectItemPosition = transform.localPosition.ToVector2(VectorExtension.Vec32Vec2IgnoreType.y);
        }
    }
}
