using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.Utility;
using RhythmGame.Time;
using UnityEngine;

namespace RhythmGame.Visual
{
    namespace Note
    {
        public class NoteException : ControllerException
        {
            public NoteException(NoteBase note, string message) : base(note, message)
            {
            }
        }

        public class RebuildException: NoteException
        {
            public RebuildException(NoteBase note, params object[] args)
                : base(note, "Failed On Rebuild , Args is :\n" + args.Contravariance(T =>
                {
                    if (T == null) return null;
                    string str = T.ToString();
                    if (str.Length < 20) return str;
                    else return str[..20] + " ...";
                }).LinkAndInsert(","))
            { }
            public RebuildException(NoteBase note) : base(note, "Failed On Rebuild") { }
        }

        public interface IController : RhythmGame.IController, RhythmGame.IListenTime
        {

        }

        /// <summary>
        /// 由创建者执行<see cref="App.MatchData(RhythmGame.IController)"/>
        /// </summary>
        public class NoteBase : MonoBehaviour, IVisualBase, IController
        {
            [SerializeField] private bool m_IsDirty;
            public bool IsDirty { get => m_IsDirty; private set => m_IsDirty = value; }

            public MonoBehaviour MonoTarget => this;

            public ADOrderlyEvent RebuildListener = new();

            private void Start()
            {
                LocalPostion ??= new string[2] { "0", "0" };
                if (Application.isPlaying)
                    ADGlobalSystem.OpenCoroutine(() => !App.instance.Contains<TimeController>() || !App.instance.Contains<CameraCore>(), () =>
                    {
                        App.instance.GetController<TimeController>().AddListener(this);
                        SetDirty();
                    });
            }

            private void OnDestroy()
            {
                if (Application.isPlaying && ADGlobalSystem.instance)
                    App.instance.GetController<TimeController>().RemoveListener(this);
            }

            [SerializeField] private string[] m_LocalPostion = new string[2] { "0", "0" };
            [RhythmData]
            public string[] LocalPostion
            {
                get => m_LocalPostion;
                set
                {
                    IsDirty = true;
                    m_LocalPostion = value;
                }
            }
            [SerializeField] private string m_JudgeTime = "0";
            [RhythmData]
            public string JudgeTime
            {
                get => m_JudgeTime;
                set
                {
                    IsDirty = true;
                    m_JudgeTime = value;
                }
            }

            private Vector2 m_Position;
            public Vector2 Position
            {
                get
                {
                    return m_Position;
                }
            }


            /// <summary>
            /// 重建Mesh将发生在<see cref="TransformRebuild"/>之后
            /// </summary>
            protected virtual void MeshRebuild()
            {

            }

            /// <summary>
            /// 重设变换将发生在<see cref="MeshRebuild"/>之前
            /// </summary>
            protected virtual void TransformRebuild()
            {
                //Local Position
                m_Position = LocalPostion.MakeArithmeticVec2Parse();
                m_Position.x *= App.instance.ViewportWidth;
                m_Position.y *= App.instance.ViewportHeight;
                //Get Anchor Position
                int m_CurrentGuideLineVertexIndex = 0;
                GuideLine guideLine = App.instance.GetController<GuideLine>();
                TimeController timeController = App.instance.GetController<TimeController>();
                Vector3 AnchorGuiderPosition = Vector2.zero;
                float DurDepth =
                    (JudgeTime.MakeArithmeticParse() / timeController.MainAudioSource.CurrentClip.length) *
                    (App.instance.MaxDepth - App.instance.MinDepth) + App.instance.MinDepth;
                while (m_CurrentGuideLineVertexIndex + 1 < guideLine.RealVertexs.Count)
                {
                    if (guideLine.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position.z >= DurDepth)
                    {
                        float start = guideLine.RealVertexs[m_CurrentGuideLineVertexIndex].Position.z;
                        float end = guideLine.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position.z;
                        float Zduration = end - start;
                        //判定时间/总时长获得百分比,乘以总长度获得从MinDepth开始的距离,加上MinDepth获得世界坐标下的深度
                        //据此深度坐标,套入公式获得这个区间内的百分比
                        float Zt = (DurDepth - start) / (float)Zduration;
                        AnchorGuiderPosition
                            = Vector3.Lerp(guideLine.RealVertexs[m_CurrentGuideLineVertexIndex].Position, guideLine.RealVertexs[m_CurrentGuideLineVertexIndex + 1].Position, Zt);
                        break;
                    }
                    m_CurrentGuideLineVertexIndex++;
                }
                //Final
                transform.position = new(
                    AnchorGuiderPosition.x + Position.x * App.instance.ViewportWidth,
                    AnchorGuiderPosition.y + Position.y * App.instance.ViewportHeight,
                    AnchorGuiderPosition.z);
            }

            public void RebuildImmediately()
            {
                RebuildListener.Invoke();
                TransformRebuild();
                MeshRebuild();
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

            }

            public void SetDirty()
            {
                IsDirty = true;
            }

            public void When(float time, float duration)
            {
                if (this.gameObject.activeInHierarchy)
                    Rebuild();

            }
        }
    }
}
