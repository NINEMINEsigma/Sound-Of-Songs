using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.Utility;
using AD.Utility.Object;
using RhythmGame.Time;
using RhythmGame.Visual;
using UnityEngine;

namespace RhythmGame
{
    public class CameraCore : ADController, IController, IListenTime
    {
        public const float CameraOffsetZ = -10;

        [SerializeField] private GuideLine MainGuideLine;
        [SerializeField] private Material LineCilper;
        [SerializeField] private float MinZ, MaxZ;

        public MonoBehaviour MonoTarget => this;

        public override void Init()
        {
            Architecture.GetController<TimeController>().RemoveListener(this);
            Architecture.GetController<TimeController>().AddListener(this);
            MainGuideLine = Architecture.GetController<GuideLine>();
            for (int i = 0, e = MainGuideLine.RealVertexs.Count; i < e; i++)
            {
                var current = MainGuideLine.RealVertexs[i];
                if (current.Position.z < MinZ) MinZ = current.Position.z;
                if (current.Position.z > MaxZ) MaxZ = current.Position.z;
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

        public void When(float time, float duration)
        {
            float t = time / duration;
            float depth = Mathf.Lerp(MinZ, MaxZ, t);
            this.transform.localPosition = this.transform.localPosition.SetZ(CameraOffsetZ + depth);
            LineCilper.SetFloat("_NearPanel", this.transform.position.z - CameraOffsetZ);
            App.instance.CameraSafeAreaPanel = this.transform.position.z - CameraOffsetZ;
        }

        private void Start()
        {
            ADGlobalSystem.OpenCoroutine(() =>
            {
                return !App.instance.Contains<GuideLine>() || !App.instance.Contains<TimeController>();
            }, () => App.instance.RegisterController(this));
        }
    }
}
