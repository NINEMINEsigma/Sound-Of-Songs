using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.Utility;
using RhythmGame.Time;
using RhythmGame.Visual;
using UnityEngine;

namespace RhythmGame
{
    public class CameraCore : ADController, IController, IListenTime
    {
        public const float CameraOffsetZ = -10;

        [SerializeField] private GuideLine MainGuideLine;
        [SerializeField] private float MinZ, MaxZ;

        public MonoBehaviour MonoTarget => this;

        public override void Init()
        {
            Architecture.GetController<TimeController>().AddListener(this);
            MainGuideLine = Architecture.GetController<GuideLine>();
            for (int i = 0, e = MainGuideLine.Vertexs.Count; i < e; i++)
            {
                var current = MainGuideLine.Vertexs[i];
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
            int indexCounter = (int)(t * MainGuideLine.Vertexs.Count);
            this.transform.localPosition =
                Vector3.Lerp(MainGuideLine.Vertexs[indexCounter].Position, MainGuideLine.Vertexs[indexCounter + 1].Position, t * MainGuideLine.Vertexs.Count - indexCounter)
                .SetZ(CameraOffsetZ + Mathf.Lerp(t, MinZ, MaxZ)).AddY(1);
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
