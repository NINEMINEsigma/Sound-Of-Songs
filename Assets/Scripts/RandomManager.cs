using AD;
using AD.BASE;
using RhythmGame.Time;
using RhythmGame.Visual;
using RhythmGame.Visual.Note;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame.Editor
{
    public class RandomManager : ADController, IController
    {
        public MonoBehaviour MonoTarget => this;

        public NoteBase NoteA, NoteB;
        public Transform ParRoot;

        public override void Init()
        {
            Debug.LogWarning("1/3");
            int baseTime = 1;
            Debug.LogWarning("2/3");
            for (int i = 0; i < 50; i++)
            {
                NoteBase current = (Random.value > 0.5 ? NoteA.PrefabInstantiate() : NoteB.PrefabInstantiate());
                current.transform.SetParent(ParRoot, false);
                current.JudgeTimeExpression = (baseTime + Random.value + i*2).ToString();
                current.LocalPostion = new string[2] { "{WorldX(" + ((Random.value - 0.5f)*12).ToString() + ")}", "{WorldY(" + ((Random.value - 0.5f)*5).ToString() + ")}" };
                current.LocalEulerAngles = new string[3] 
                {  "0","0",((Random.value - 0.5f) * 180).ToString() };
            }
            Debug.LogWarning("3/3");
        }

        private void Start()
        {
            ADGlobalSystem.OpenCoroutine(() => !App.instance.Contains<GuideLine>()||!App.instance.Contains<TimeController>(), () =>
            {
                App.instance.RegisterController(this);
            });
        }
    }
}
