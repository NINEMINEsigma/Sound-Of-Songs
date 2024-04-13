using System;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.UI;
using RhythmGame.ScoreBoard;
using RhythmGame.Time;
using UnityEngine;
using static AD.ADGlobalSystem;

namespace RhythmGame.ScoreBoard
{
    public enum JudgeType
    {
        Best = 16, Perfect = 50, Good = 80, Bad = 110, Lost = 999
    }

    public static class JudgeTypeHelper
    {
        public static float ToSecond(this JudgeType type)
        {
            return 0.001f * (int)type;
        }
    }

    public class JudgeData
    {
        public JudgeData() { }
        public JudgeData(float time,float offset, JudgeType type)
        {
            Time = time;
            Offset = offset;
            Type = type;
        }

        public float Time;
        public float Offset;
        public JudgeType Type;
    }

    [Serializable]
    public class RealScore : ICanInitialize
    {
        //源数据
        public List<float> S = new();
        //方差
        public List<float> F = new();
        //均值
        public List<float> E = new();

        public void Add(float addition)
        {
            S.Add(addition);
            addition = Mathf.Abs(addition);
            if (E.Count > 0) E.Add(E[^1] + (addition - E[^1]) / (E.Count + 1));
            else E.Add(addition);

            if (E.Count > 1) F.Add(F[^1] + (addition - E[^1]) * (addition - E[^2]));
            else F.Add(0);
        }

        public float GetF()
        {
            return F.Count == 0 ? 0 : F[^1];
        }

        public float GetE()
        {
            return E.Count == 0 ? 0 : E[^1];
        }

        public void Init()
        {
            S = new();
            E = new();
            F = new();
        }
    }
}

namespace RhythmGame.Visual
{
    public class ScoreBoard : ADController, IController, IRebuildHandler
    {
        public MonoBehaviour MonoTarget => this;

        public bool IsDirty { get; private set; }

        public override void Init()
        {
            JudgeEffectCorTimeCounter = 0;
            TotalMainScore.Init();
            TotalPerfectScore.Init();
            TotalGoodScore.Init();
            TotalBadScore.Init();
            TotalLostScore.Init();
            SetDirty();
        }

        private void Start()
        {
            ComboText.SetText("");
            ADGlobalSystem.OpenCoroutine(() => !App.instance.Contains<GuideLine>() || !App.instance.Contains<TimeController>(), () =>
            {
                App.instance.RegisterController(this);
            });
        }

        public List<JudgeData> Datas = new();

        public int ComboValue;
        [SerializeField] private Text ComboText;
        //误差表
        [SerializeField] private Text MainScoreBoard;
        [SerializeField] private Text PerfectScoreBoard;
        [SerializeField] private Text GoodScoreBoard;
        [SerializeField] private Text BadScoreBoard;
        [SerializeField] private Text LostScoreBoard;
        //值表
        [SerializeField] private RealScore TotalMainScore;
        [SerializeField] private RealScore TotalPerfectScore;
        [SerializeField] private RealScore TotalGoodScore;
        [SerializeField] private RealScore TotalBadScore;
        [SerializeField] private RealScore TotalLostScore;
        //
        [SerializeField] private UnityEngine.UI.Image JudgeEffectUI;
        private Color JudgeEffectColor;
        public float JudgeEffectCorTimeCounter = 0;

        private void LateUpdate()
        {
            Rebuild();
            JudgeEffectCorTimeCounter -= UnityEngine.Time.deltaTime;
            if (JudgeEffectCorTimeCounter > 0)
                JudgeEffectUI.color = Color.Lerp(new Color(1, 1, 1, 0), JudgeEffectColor, JudgeEffectCorTimeCounter / JudgeEffectShadowDuratoin);
        }

        private const float JudgeEffectShadowDuratoin = 1;

        public void AddJudgeData(JudgeData data)
        {
            JudgeEffectCorTimeCounter = JudgeEffectShadowDuratoin;
            ComboValue++;
            if (data.Type== JudgeType.Bad||data.Type==JudgeType.Lost)
            {
                ComboValue = 0;
            }
            ComboText.text = ComboValue < 3 ? "" : (ComboValue.ToString() + " Combo");
            //
            IsDirty = true;
            Datas.Add(data);
            TotalMainScore.Add(data.Offset);
            switch (data.Type)
            {
                case JudgeType.Best or JudgeType.Perfect:
                    {
                        TotalPerfectScore.Add(data.Offset);
                        JudgeEffectColor = Color.yellow;
                    }
                    break;
                case JudgeType.Good:
                    {
                        TotalGoodScore.Add(data.Offset);
                        JudgeEffectColor = Color.blue;
                    }
                    break;
                case JudgeType.Bad:
                    {
                        TotalBadScore.Add(data.Offset);
                        JudgeEffectColor = Color.red;
                    }
                    break;
                case JudgeType.Lost:
                    {
                        TotalLostScore.Add(data.Offset);
                        JudgeEffectColor = Color.black;
                    }
                    break;
                default:
                    break;
            }
        }

        public void RemoveJudgeData(JudgeData data)
        {
            ComboValue = 0;
            ComboText.text = "";
            IsDirty = true;
            Datas.Remove(data);
            TotalMainScore.Init();
            TotalPerfectScore.Init();
            TotalGoodScore.Init();
            TotalBadScore.Init();
            TotalLostScore.Init();
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

        public const int FullScore = 1000000;
        public const int ShortFullScore = 1000;

        public void RebuildImmediately()
        {
            MainScoreBoard.SetText($"{GetFormatScore(0, JudgeType.Bad.ToSecond(), FullScore, TotalMainScore.GetE(), 7)}\n{TotalMainScore.S.Count}");
            PerfectScoreBoard.SetText($"Perfect\n{TotalPerfectScore.S.Count}");
            //    $"{TotalPerfectScore.S.Count} P {GetFormatScore(JudgeType.Best.ToSecond(), JudgeType.Good.ToSecond(), ShortFullScore, TotalPerfectScore.GetE() , 4)}");
            GoodScoreBoard.SetText($"Good\n{TotalGoodScore.S.Count}");
            //    $"{TotalGoodScore.S.Count} G {GetFormatScore(JudgeType.Good.ToSecond(), JudgeType.Bad.ToSecond(), ShortFullScore, TotalGoodScore.GetE() , 4)}");
            BadScoreBoard.SetText($"Bad\n{TotalBadScore.S.Count}");
            //    $"{TotalBadScore.S.Count} B {GetFormatScore(JudgeType.Bad.ToSecond(), JudgeType.Lost.ToSecond(), ShortFullScore, TotalBadScore.GetE(), 4)}");
            LostScoreBoard.SetText($"Lost\n{TotalLostScore.S.Count}");
            IsDirty = false;
        }

        public static string GetFormatScore(float min,float max,int fullScore,float value,int length)
        {
            float t = (value - min) / (max - min);
            int finalValue = Mathf.RoundToInt(fullScore * (1 - t));
            string str = finalValue.ToString();
            length -= str.Length;
            while (length-->0)
            {
                str = "0" + str;
            }
            return str;
        }

        public void SetDirty()
        {
            IsDirty = true;
        }
    }
}
