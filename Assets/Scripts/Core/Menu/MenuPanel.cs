using AD.BASE;
using AD.Math;
using AD.UI;
using RhythmGame.Time;
using RhythmGame.Visual.Note;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmGame.Visual
{
    public class MenuPanel : MonoBehaviour, IController
    {
        public MonoBehaviour MonoTarget => this;

        public Button BackToGame, Replay, BackToPerScene;

        public InputField DelayField;

        private void RebuildAll()
        {
            foreach (var item in App.instance.GetController<TimeController>().Listeners)
            {
                if (item.As<JudgeEffect>(out var effect))
                {
                    effect.gameObject.SetActive(false);
                }
                else if (item.As<IRebuildHandler>(out var handler))
                {
                    handler.RebuildImmediately();
                }
            }
        }

        private void OnEnable()
        {
            DelayField.RemoveAllListener();
            ArithmeticVariable delay = new("delay");
            if (delay)
            {
                DelayField.SetText(delay.ReadValue().ToString());
                DelayField.AddListener(T =>
                {
                    ArithmeticVariable.VariableConstantPairs["delay"].Value.SetValue(T.MakeArithmeticParse());
                    RebuildAll();
                });
            }
            else
            {
                DelayField.SetText("0");
                DelayField.AddListener(T =>
                {
                    ArithmeticExtension.AddVariable("delay", new(T.MakeArithmeticParse()));
                    RebuildAll();
                });
            }
        }
    }
}
