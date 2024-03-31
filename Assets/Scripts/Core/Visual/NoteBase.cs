using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using AD.Utility;
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
            [SerializeField]private bool m_IsDirty;
            public bool IsDirty { get => m_IsDirty; private set => m_IsDirty = value; }

            public MonoBehaviour MonoTarget => this;

            public ADOrderlyEvent RebuildListener = new();

            public Vector2 m_Anchors;
            [RhythmData]
            public Vector2 Anchors
            {
                get => m_Anchors;
                set
                {
                    IsDirty = true;
                    m_Anchors = value;
                }
            }
            public Vector2 m_Postion;
            [RhythmData]
            public Vector2 LocalPostion
            {
                get => m_Postion;
                set
                {
                    IsDirty = true;
                    m_Postion = value;
                }
            }

            public Vector2 Positon
            {
                get => LocalPostion + Anchors;
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
                Rebuild();
            }

            public void SetDirty()
            {
                IsDirty = true;
            }

            public void When(float time, float duration)
            {

            }
        }
    }
}
