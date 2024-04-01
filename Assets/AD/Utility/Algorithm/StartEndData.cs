using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AD.Math;

namespace AD.Utility
{
    [Serializable]
    public class StartEndData
    {
        public string StartTimeExpression = "0";
        public string EndTimeExpression = "0";
        public string StartValueExpression = "0";
        public string EndValueExpression = "0";
        public string EaseCurveTypeExpression = EaseCurveType.Linear.ToString();

        public float StartTime => StartTimeExpression.MakeArithmeticParse();
        public float EndTime => EndTimeExpression.MakeArithmeticParse();
        public float StartValue=>StartValueExpression.MakeArithmeticParse();
        public float EndValue=>EndValueExpression.MakeArithmeticParse();
        public EaseCurveType CurveType => Enum.Parse<EaseCurveType>(EaseCurveTypeExpression);

        public bool IsMyDuration(float t)
        {
            return t <= EndValue && t >= StartValue;
        }

        public float Evaluate(float t)
        {
            float st = StartTime;
            return Mathf.Lerp(StartValue, EndValue, new EaseCurve().Evaluate((t - st) / (EndTime - st), CurveType, false));
        }
    }
}
