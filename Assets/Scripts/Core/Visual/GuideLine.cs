using System;
using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Utility;
using AD.Utility.Object;
using RhythmGame.Time;
using UnityEngine;

namespace RhythmGame.Visual
{
    /// <summary>
    /// 用于保存的实际类型，通过直接保存表达式来扩展铺面文件的可操作性
    /// </summary>
    [Serializable, EaseSave3]
    public class VertexData
    {
        public VertexData() : this(new MeshExtension.VertexEntry()
        {
            Size = 0.3f,
            Normal = Vector3.right,
            Position = Vector3.zero
        })
        { }
        public VertexData(string[] position, string[] normal, string size)
        {
            this.Position = position;
            this.Normal = normal;
            this.Size = size;
        }
        public VertexData(MeshExtension.VertexEntry entry) : this(
            new string[3] { entry.Position.x.ToString(), entry.Position.y.ToString(), entry.Position.z.ToString() },
            new string[3] { entry.Normal.x.ToString(), entry.Normal.y.ToString(), entry.Normal.z.ToString() },
            entry.Size.ToString()
            )
        { }

        public string[] Position;
        public string[] Normal;
        public string Size;

        public static implicit operator MeshExtension.VertexEntry(VertexData o)
        {
            try
            {
                MeshExtension.VertexEntry result = new()
                {
                    Position = o.Position.MakeArithmeticVec3Parse(),
                    Normal = o.Normal.MakeArithmeticVec3Parse(),
                    Size = o.Size.MakeArithmeticParse(),
                    Type = MeshExtension.BuildNormalType.JustDirection
                };
                return result;
            }
            catch
            {
                return new();
            }
        }
    }

    /// <summary>
    /// 在生成时立即进行一次<see cref="App.MatchData(IController)"/>
    /// </summary>
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    public class GuideLine : ADController, IController, IRuildHandler, IListenTime
    {
        [SerializeField] private List<VertexData> m_Vertexs = new();
        [RhythmData, ADSerialize()]
        public List<VertexData> Vertexs
        {
            get => m_Vertexs;
            set
            {
                IsDirty = true;
                m_Vertexs = value;
            }
        }
        [SerializeField] private List<MeshExtension.VertexEntry> m_RealVertexs = new();
        public List<MeshExtension.VertexEntry> RealVertexs
        {
            get => m_RealVertexs;
            private set => m_RealVertexs = value;
        }

        public MonoBehaviour MonoTarget => this;
        [SerializeField] private MeshFilter m_MeshFilter;
        [SerializeField] private MeshRenderer m_MeshRenderer;

        [SerializeField] private bool m_IsDirty;
        public bool IsDirty { get => m_IsDirty; set => m_IsDirty = value; }

        public override void Init()
        {
            Architecture.GetController<TimeController>().RemoveListener(this);
            Architecture.GetController<TimeController>().AddListener(this);
            m_CurrentGuideLineVertexIndex = 0;
            m_MeshFilter.sharedMesh = new();
            App.instance.MatchData(this);
            if (Vertexs.Count == 0)
            {
                Vertexs.Add(new(new MeshExtension.VertexEntry()
                {
                    Size = 0.3f,
                    Normal = Vector3.right,
                    Position = Vector3.zero,
                    Type = MeshExtension.BuildNormalType.JustDirection
                }));
                Vertexs.Add(new(new MeshExtension.VertexEntry()
                {
                    Size = 0.3f,
                    Normal = Vector3.right,
                    Position = new(0, 0, 100),
                    Type = MeshExtension.BuildNormalType.JustDirection
                }));
            }
            RebuildImmediately();
        }

        public void RebuildImmediately()
        {
            RealVertexs = Vertexs.Contravariance(T => (MeshExtension.VertexEntry)T);
            m_MeshFilter.RebuildMesh(RealVertexs);
            IsDirty = false;
        }

        public void Rebuild()
        {
            if (IsDirty)
            {
                RebuildImmediately();
            }
            else
            {
                return;
            }
        }

        private void LateUpdate()
        {
            Rebuild();
        }

        private void Start()
        {
            Vertexs ??= new();
            ADGlobalSystem.OpenCoroutine(() =>
            {
                return !App.instance.Contains<TimeController>();
            }, () => App.instance.RegisterController(this));
        }

        [SerializeField] private Transform AnchorGuider;
        [SerializeField] private MaterialGroup MainMaterialGroup;
        private int m_CurrentGuideLineVertexIndex = 0;
        public void When(float time, float duration)
        {
            //Get Anchor Position
            while (m_CurrentGuideLineVertexIndex + 1 < this.RealVertexs.Count)
            {
                if (this.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position.z >= App.instance.CameraSafeAreaPanel)
                {
                    App.instance.StartVertex = this.RealVertexs[m_CurrentGuideLineVertexIndex].Position;
                    float start = App.instance.StartVertex.z;
                    App.instance.EndVertex = this.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position;
                    float end = App.instance.EndVertex.z;
                    float Zduration = end - start;
                    float Zt = (App.instance.CameraSafeAreaPanel - start) / (float)Zduration;
                    AnchorGuider.position = Vector3.Lerp(this.RealVertexs[m_CurrentGuideLineVertexIndex].Position, this.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position, Zt);
                    break;
                }
                m_CurrentGuideLineVertexIndex++;
            }
            if (m_CurrentGuideLineVertexIndex + 2 >= this.RealVertexs.Count) m_CurrentGuideLineVertexIndex = 0;
            //Update Anchor Material
            MainMaterialGroup.UpdateTarget("_Offset", App.instance.CameraSafeAreaPanel / 2.0f);
        }
    }
}
