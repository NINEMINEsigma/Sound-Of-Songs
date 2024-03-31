using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using AD.Utility;
using UnityEngine;

namespace RhythmGame.Visual
{
    /// <summary>
    /// 在生成时立即进行一次<see cref="App.MatchData(IController)"/>
    /// </summary>
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    public class GuideLine : ADController, IController, IRuildHandler, IListenTime
    {
        [SerializeField] private List<MeshExtension.VertexEntry> m_Vertexs = new();
        [RhythmData]
        public List<MeshExtension.VertexEntry> Vertexs
        {
            get => m_Vertexs;
            set
            {
                IsDirty = true;
                m_Vertexs = value;
            }
        }

        public MonoBehaviour MonoTarget => this;
        [SerializeField] private MeshFilter m_MeshFilter;
        [SerializeField] private MeshRenderer m_MeshRenderer;

        [SerializeField] private bool m_IsDirty;
        public bool IsDirty { get => m_IsDirty; set => m_IsDirty = value; }

        public override void Init()
        {
            if (Vertexs == null) Vertexs = new();
            if (Vertexs.Count == 0)
            {
                Vertexs.Add(new()
                {
                    Size = 0.7f,
                    Normal = Vector3.right,
                    Position = Vector3.zero,
                    Type = MeshExtension.BuildNormalType.JustDirection
                });
                Vertexs.Add(new()
                {
                    Size = 0.7f,
                    Normal = Vector3.right,
                    Position = new(0, 0, 100),
                    Type = MeshExtension.BuildNormalType.JustDirection
                });
            }
            RebuildImmediately();
        }

        public void RebuildImmediately()
        {
            m_MeshFilter.RebuildMesh(Vertexs);
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
            m_MeshFilter.sharedMesh = new();
            App.instance.MatchData(this);
            App.instance.RegisterController(this);
        }

        public void When(float time, float duration)
        {

        }
    }
}
