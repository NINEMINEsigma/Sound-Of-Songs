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
            int baseTime = 1;
            for (int i = 0; i < 100; i++)
            {
                NoteBase current = (Random.value > 0.5 ? NoteA.PrefabInstantiate() : NoteB.PrefabInstantiate());
                current.JudgeTimeExpression = (baseTime + Random.value + i).ToString();
                if(Random.value>0.75)
                {
                    NoteBase CurDouble = (Random.value > 0.5 ? NoteA.PrefabInstantiate() : NoteB.PrefabInstantiate());
                    CurDouble.transform.SetParent(ParRoot, false);
                    CurDouble.JudgeTimeExpression = current.JudgeTimeExpression;
                    CurDouble.LocalPostion = new string[2] { "{WorldX(" + ((Random.value - 0.5f) * 12).ToString() + ")}", "{WorldY(" + ((Random.value - 0.5f) * 5).ToString() + ")}" };
                    CurDouble.LocalEulerAngles = new string[3]
                    {  "0","0",((Random.value - 0.5f) * 180).ToString() };
                }
                current.transform.SetParent(ParRoot, false);
                current.LocalPostion = new string[2] { "{WorldX(" + ((Random.value - 0.5f)*15).ToString() + ")}", "{WorldY(" + ((Random.value - 0.5f)*7).ToString() + ")}" };
                current.LocalEulerAngles = new string[3] 
                {  "0","0",((Random.value - 0.5f) * 180).ToString() };
            }
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
