using System;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Math;
using RhythmGame.Visual.Note;
using UnityEngine;
using static AD.Reflection.ReflectionExtension;

namespace RhythmGame
{
    public sealed class App : ADArchitecture<App>
    {
        public static float CurrentTime;

        public const string ActualSongTime2Percentage = "SongTime";
        public float Internal_ActualSongTime2Percentage(float t)
        {
            return (t - StartTime) / (EndTime - StartTime);
        }
        public const string ViewportPositionX2WorldPositionX = "WorldX";
        public float Internal_ViewportPositionX2WorldPositionX(float x)
        {
            return x / ViewportWidth;
        }
        public const string ViewportPositionY2WorldPositionY = "WorldY";
        public float Internal_ViewportPositionY2WorldPositionY(float x)
        {
            return x / ViewportHeight;
        }

        public float CameraSafeAreaPanel = 0;
        public float MinDepth, MaxDepth;
        public Vector3 StartVertex, EndVertex;
        public float StartTime, EndTime;
        public float ViewportWidth, ViewportHeight;


        public void MatchData(IController controller)
        {

        }

        public override void Init()
        {
            base.Init();
            ArithmeticExtension.AddFunction(ActualSongTime2Percentage, new(ADReflectedMethod.Temp<float, float>(Internal_ActualSongTime2Percentage), this));
            ArithmeticExtension.AddFunction(ViewportPositionX2WorldPositionX, new(ADReflectedMethod.Temp<float, float>(Internal_ViewportPositionX2WorldPositionX), this));
            ArithmeticExtension.AddFunction(ViewportPositionY2WorldPositionY, new(ADReflectedMethod.Temp<float, float>(Internal_ViewportPositionY2WorldPositionY), this));

            RegisterModel<JudgeEffectStack>()
                ;
        }
    }

    [Serializable]
    public class StartEndData : AD.Utility.StartEndData
    {
        public void OnSerialize()
        {
            PropertiesLayout.Label(nameof(this.StartTime));
            var stip = PropertiesLayout.InputField(StartTime.ToString(), nameof(this.StartTime));
            stip.source.onSelect.AddListener(_ => stip.SetTextWithoutNotify(StartTimeExpression));
            stip.source.onEndEdit.AddListener(T =>
            {
                StartTimeExpression = T;
                stip.SetTextWithoutNotify(StartTime.ToString());
            });

            PropertiesLayout.Label(nameof(this.EndTime));
            var etip = PropertiesLayout.InputField(EndTime.ToString(), nameof(this.EndTime));
            etip.source.onSelect.AddListener(_ => etip.SetTextWithoutNotify(EndTimeExpression));
            etip.source.onEndEdit.AddListener(T =>
            {
                EndTimeExpression = T;
                etip.SetTextWithoutNotify(EndTime.ToString());
            });

            PropertiesLayout.Label(nameof(this.StartValue));
            var svip = PropertiesLayout.InputField(StartValue.ToString(), nameof(this.StartValue));
            svip.source.onSelect.AddListener(_ => svip.SetTextWithoutNotify(StartValueExpression));
            svip.source.onEndEdit.AddListener(T =>
            {
                StartValueExpression = T;
                svip.SetTextWithoutNotify(StartValue.ToString());
            });

            PropertiesLayout.Label(nameof(this.EndValue));
            var evip = PropertiesLayout.InputField(EndValue.ToString(), nameof(this.EndValue));
            evip.source.onSelect.AddListener(_ => evip.SetTextWithoutNotify(EndValueExpression));
            evip.source.onEndEdit.AddListener(T =>
            {
                EndValueExpression = T;
                evip.SetTextWithoutNotify(EndValue.ToString());
            });
        }
    }
}
