using System;
using System.Collections;
using System.Collections.Generic;
using AD;
using AD.BASE;
using AD.UI;
using RhythmGame.ScoreBoard;
using RhythmGame.Time;
using UnityEngine;
using UnityEngine.UIElements;

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

        private void LateUpdate()
        {
            Rebuild();
        }

        public void AddJudgeData(JudgeData data)
        {
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
                    }
                    break;
                case JudgeType.Good:
                    {
                        TotalGoodScore.Add(data.Offset);
                    }
                    break;
                case JudgeType.Bad:
                    {
                        TotalBadScore.Add(data.Offset);
                    }
                    break;
                case JudgeType.Lost:
                    {
                        TotalLostScore.Add(data.Offset);
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

        public void RebuildImmediately()
        {
            MainScoreBoard.SetText($"{TotalMainScore.S.Count} X {(int)(TotalMainScore.GetF() * 1000)}");
            PerfectScoreBoard.SetText($"{TotalPerfectScore.S.Count} P {(int)(TotalPerfectScore.GetF() * 1000)}");
            GoodScoreBoard.SetText($"{TotalGoodScore.S.Count} G {(int)(TotalGoodScore.GetF() * 1000)}");
            BadScoreBoard.SetText($"{TotalBadScore.S.Count} B {(int)(TotalBadScore.GetF() * 1000)}");
            LostScoreBoard.SetText($"Lost {TotalLostScore.S.Count}");
            IsDirty = false;
        }

        public void SetDirty()
        {
            IsDirty = true;
        }
    }
}
