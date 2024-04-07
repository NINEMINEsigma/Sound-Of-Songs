using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AD;
using AD.BASE;
using AD.Math;
using AD.UI;
using AD.Utility;
using RhythmGame.Time;
using RhythmGame.Visual;
using RhythmGame.Visual.Note;
using UnityEngine;

namespace RhythmGame
{
    public class CameraCore : ADController, IController, IListenTime, IRebuildHandler, IInvariant<IRebuildHandler>
    {
        public const float CameraOffsetZ = -13;


        public float min, max;

        [SerializeField] private Camera MainCamera;
        [SerializeField] private GuideLine MainGuideLine;
        [SerializeField] private Material LineCilper;
        [SerializeField] private RectTransform Viewport;

        [SerializeField] private List<StartEndData> m_TimingPairs = new();
        [RhythmData]
        public List<StartEndData> TimingPairs
        {
            get => m_TimingPairs;
            set
            {
                IsDirty = true;
                m_TimingPairs = value;
            }
        }

        public MonoBehaviour MonoTarget => this;

        [SerializeField] private bool m_IsDirty;
        public bool IsDirty { get => m_IsDirty; set => m_IsDirty = value; }


        [SerializeField] private NoteBase NoteA, NoteB;
        [SerializeField] private Transform ParRoot;

        public ADFile TargetFile;
        public override void Init()
        {
            Architecture.GetController<TimeController>().RemoveListener(this);
            Architecture.GetController<TimeController>().AddListener(this);
            MainGuideLine = Architecture.GetController<GuideLine>();

            App.instance.NoteA = this.NoteA;
            App.instance.NoteB = this.NoteB;
            App.instance.ParRoot = this.ParRoot;

            //Viewport
            var vipo = Viewport.GetRect();
            App.instance.ViewportWidth = (vipo[2].x - vipo[0].x);
            App.instance.ViewportHeight = (vipo[2].y - vipo[0].y);
            Debug.Log($"{App.instance.ViewportWidth},{App.instance.ViewportHeight}");

            //TODO
            try
            {
                ADSettings settings = new("testline.txt", ADStreamEnum.Location.Resources);
                TargetFile = new ADFile(settings);
                TargetFile.UpdateFileData();
                RhythmGameCommandScript.Read(TargetFile.GetString(true, System.Text.Encoding.UTF8).Split('\n'));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        protected override void OnDestroy()
        {
            if (ADGlobalSystem.instance)
            {
                Architecture.GetController<TimeController>().RemoveListener(this);
            }
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            Rebuild();
            if (Architecture == null || !Architecture.Contains<TouchLock>()) return;
            var lockList = Architecture.GetModel<TouchLock>().TouchLockList;
            for (int i = 0, e = Input.touchCount; i < e; i++)
            {
                var current = Input.GetTouch(i);
                if ((current.phase == TouchPhase.Ended || current.phase == TouchPhase.Canceled) && lockList.ContainsKey(current.fingerId))
                {
                    lockList.Remove(current.fingerId);
                }
                else
                {
                    Ray ray = MainCamera.ScreenPointToRay(current.position);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        ADEventSystemExtension.Execute<App, IListenTouch>(App.instance, hit.collider.gameObject, null, (T, _) => T.OnCatching(current));
                    }
                }
            }
        }

        public void When(float time, float duration)
        {
            float depth = GetAnchorDepth(time, duration);
            this.transform.localPosition = this.transform.localPosition.SetZ(CameraOffsetZ + depth);
            LineCilper.SetFloat("_NearPanel", this.transform.position.z - CameraOffsetZ);
            App.instance.CameraSafeAreaPanel = this.transform.position.z - CameraOffsetZ;
        }

        public float GetAnchorDepth(float time, float duration)
        {
            float t = time / duration;
            StartEndData target = TimingPairs.Count == 0 ? null : TimingPairs.Last(T => T.StartTime <= t);
            float depth = Mathf.Lerp(App.instance.MinDepth, App.instance.MaxDepth, target == null ? t : target.Evaluate(t));
            return depth;
        }

        private void Start()
        {
            ADGlobalSystem.OpenCoroutine(() =>
            {
                return !App.instance.Contains<GuideLine>() || !App.instance.Contains<TimeController>();
            }, () => App.instance.RegisterController(this));
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

        public void RebuildImmediately()
        {
            foreach (var listener in Architecture.GetController<TimeController>().Listeners)
            {
                if (listener.As<IRebuildHandler>(out var handler))
                {
                    handler.SetDirty();
                }
            }
            IsDirty = false;
        }

        public void SetDirty()
        {
            IsDirty = true;
        }
    }
}
