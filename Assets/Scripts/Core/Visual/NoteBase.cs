using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.Utility;
using AD.Math;
using RhythmGame.Time;
using UnityEngine;
using RhythmGame.ScoreBoard;
using UnityEditor;

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

        public class RebuildException : NoteException
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

        public interface IController : RhythmGame.IController, IListenTime, IListenTouch
        {

        }

        /// <summary>
        /// 由创建者执行<see cref="App.MatchData(RhythmGame.IController)"/>
        /// </summary>
        public class NoteBase : MonoBehaviour, IVisualBase, IController
        {
            public string m_NoteType;
            [RhythmData] public string NoteType { get => m_NoteType; set => m_NoteType = value; }

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
            [SerializeField] private string[] m_LocalEulerAngles = new string[3] { "0", "0", "0" };
            [RhythmData]
            public string[] LocalEulerAngles
            {
                get => m_LocalEulerAngles;
                set
                {
                    IsDirty = true;
                    m_LocalEulerAngles = value;
                }
            }
            [SerializeField] private string m_JudgeTimeExpression = "0";
            [RhythmData]
            public string JudgeTimeExpression
            {
                get => m_JudgeTimeExpression;
                set
                {
                    IsDirty = true;
                    m_JudgeTimeExpression = value;
                }
            }
            private float m_JudgeTime;
            public float JudgeTime => m_JudgeTime;

            private Vector2 m_Position;
            public Vector2 Position
            {
                get
                {
                    return m_Position;
                }
            }
            public Vector3 EulerAngles
            {
                get => LocalEulerAngles.MakeArithmeticVec3Parse();
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
                //Get Anchor Position
                int m_CurrentGuideLineVertexIndex = 0;
                GuideLine guideLine = App.instance.GetController<GuideLine>();
                TimeController timeController = App.instance.GetController<TimeController>();
                CameraCore cameraCore = App.instance.GetController<CameraCore>();
                float depth = cameraCore.GetAnchorDepth(JudgeTime, timeController.MainAudioSource.CurrentClip.length);
                Vector3 AnchorGuiderPosition = guideLine.GetAnchorPoint(depth, ref m_CurrentGuideLineVertexIndex);
                transform.position = new(
                    AnchorGuiderPosition.x + Position.x * App.instance.ViewportWidth,
                    AnchorGuiderPosition.y + Position.y * App.instance.ViewportHeight,
                    AnchorGuiderPosition.z);
                transform.eulerAngles = EulerAngles;
            }

            public void RebuildImmediately()
            {
                m_JudgeTime = JudgeTimeExpression.MakeArithmeticParse();
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

            private JudgeData m_JudgeData;
            [SerializeField] private bool IsJudged = false;
            [SerializeField] private bool IsBeenTouch = false;
            [SerializeField] public bool IsBeenTouchJudge = false;
            [Header("JudgeEffect")]
            public JudgeEffect JudgeEffectPrefab;
            public void When(float time, float duration)
            {
                if (this.gameObject.activeInHierarchy)
                    Rebuild();

                //已被触摸判定
                if (IsBeenTouch)
                {
                    //超过需要判定的时间
                    if (App.CurrentTime > JudgeTime)
                    {
                        //未判定则判定并生成动画
                        if (!IsJudged)
                        {
                            PlayJudgeEffect();
                            gameObject.SetActive(false);
                            IsJudged = true;
                        }
                    }
                    //回归至判定之前的时间,且已经判定过,则进行回正
                    else if (!IsBeenTouchJudge && time < JudgeTime + JudgeType.Bad.ToSecond() && IsJudged)
                    {
                        gameObject.SetActive(true);
                        DistroyDataAndDoRemoveJudgeData();
                        IsJudged = false;
                        IsBeenTouch = false;
                    }
                    //已被触摸判定则直接跳过后续,若是恰好回正则延迟一帧进行新的判定
                    return;
                }

#if SOS_EDITOR
                if (time > JudgeTime)
                {
                    if (!IsJudged)
                    {
                        PlayJudgeEffect();
                        CreateDataAndDoAddJudgeData(0, JudgeType.Best);
                        gameObject.SetActive(false);
                    }
                    IsJudged = true;
                }
                else if (!IsBeenTouch && IsJudged)
                {
                    gameObject.SetActive(true);
                    DistroyDataAndDoRemoveJudgeData();
                    IsJudged = false;
                    IsBeenTouch = false;
                }
#else
                if (time < JudgeTime && !IsBeenTouch && IsJudged)
                {
                    gameObject.SetActive(true);
                    DistroyDataAndDoRemoveJudgeData();
                    IsJudged = false;
                    IsBeenTouch = false;
                }
#endif
                //没有被判定且超出判定区间
                if (!(IsBeenTouch || IsJudged) && (time - JudgeTime > JudgeType.Bad.ToSecond()))
                {
                    CreateDataAndDoAddJudgeData(JudgeType.Bad.ToSecond(), JudgeType.Lost);
                    gameObject.SetActive(false);
                    IsJudged = true;
                }
            }

            private void CreateDataAndDoAddJudgeData(float offset, JudgeType type)
            {
                m_JudgeData = new(JudgeTime, offset, type);
                App.instance.GetController<ScoreBoard>().AddJudgeData(m_JudgeData);
            }
            private void DistroyDataAndDoRemoveJudgeData()
            {
                if (m_JudgeData != null)
                {
                    App.instance.GetController<ScoreBoard>().RemoveJudgeData(m_JudgeData);
                    m_JudgeData = null;
                }
            }
            private void PlayJudgeEffect()
            {
                JudgeEffectPrefab.PrefabInstantiate().transform.position = this.transform.position;
            }

            public void OnCatching(Touch touch)
            {
                if (IsJudged || IsBeenTouch) return;

                if (touch.phase == TouchPhase.Began)
                {
                    float Offset = App.CurrentTime - JudgeTime;
                    if (Mathf.Abs(Offset) < JudgeType.Good.ToSecond())
                    {
                        //尝试判定
                        var lockList = App.instance.GetModel<TouchLock>().TouchLockList;
                        if (lockList.ContainsKey(touch.fingerId))
                        {
                            lockList[touch.fingerId] = this.NoteType;
                        }
                        else
                        {
                            lockList.Add(touch.fingerId, this.NoteType);
                        }
                        //成功的判定
                        if (Mathf.Abs(Offset) < JudgeType.Best.ToSecond())
                        {
                            CreateDataAndDoAddJudgeData(Offset, JudgeType.Best);
                        }
                        else if (Mathf.Abs(Offset) < JudgeType.Perfect.ToSecond())
                        {
                            CreateDataAndDoAddJudgeData(Offset, JudgeType.Perfect);
                        }
                        else
                        {
                            CreateDataAndDoAddJudgeData(Offset, JudgeType.Good);
                        }
                    }
                    else if (Mathf.Abs(Offset) < JudgeType.Bad.ToSecond())
                    {
                        MakeBadJudge(Offset);
                        return;
                    }
                    else return;
                    PlayJudgeEffect();

                    gameObject.SetActive(false);
                    IsJudged = true;
                    IsBeenTouch = true;
                    IsBeenTouchJudge = true;
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    //如果首先被判定为正确,那么后续错误也可以无视
                    if (!IsBeenTouch)
                    {
                        float Offset = App.CurrentTime - JudgeTime;
                        if (Mathf.Abs(Offset) < JudgeType.Good.ToSecond())
                        {
                            //尝试判定
                            var lockList = App.instance.GetModel<TouchLock>().TouchLockList;
                            if (lockList.ContainsKey(touch.fingerId))
                            {
                                if (lockList[touch.fingerId] != this.NoteType)
                                {
                                    MakeBadJudge(Offset);
                                    return;
                                }
                            }
                            else
                            {
                                lockList.Add(touch.fingerId, this.NoteType);
                            }
                            //成功的判定
                            CreateDataAndDoAddJudgeData(JudgeType.Best.ToSecond(), JudgeType.Best);
                            IsBeenTouch = true;
                            IsBeenTouchJudge = true;
                        }
                    }
                }

                void MakeBadJudge(float Offset)
                {
                    CreateDataAndDoAddJudgeData(Offset, JudgeType.Bad);
                    gameObject.SetActive(false);
                    IsBeenTouch = true;
                    IsJudged = true;
                    IsBeenTouchJudge = true;
                    return;
                }
            }
        }
    }
}
