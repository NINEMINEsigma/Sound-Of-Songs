using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AD.BASE;
using AD.Experimental.Runtime.Internal;
using AD.Utility;
using AD.Math;
using AD.Reflection;
using UnityEngine;

namespace AD.Experimental.Runtime
{

    [System.Serializable]
    public class LineLanguageException : ADException
    {
        public LineLanguageException() { }
        public LineLanguageException(string message) : base(message) { }
    }

    public class GrammaticalException:LineLanguageException
    {
        public GrammaticalException() { }
        public GrammaticalException(params LanguageUnit[] units)  { Units = units; }

        public LanguageUnit[] Units;
    }

    namespace Internal
    {
        public static class KeyWordSet
        {
            public static List<string> KeyWords = new()
            {
            //命令头：创建
            "build",
            //命令头：作用于
            "execute",
            //用于表明随后的指示名是一个类型
            "typename",
            //用于声明随后的指示名是一个表达式
            "expression",
            //用于标记一个区域的结束
            "end",
            //用于标记一个区域的开始
            "begin",
            //用于声明目标是一个块
            "block",
            //用于声明目标是一个分割区域
            "area"
            };

            public class NotConstConstant:ArithmeticConstant
            {
                public NotConstConstant(string expression) { Expression = expression; }
                public string Expression;

                public override float ReadValue()
                {
                    return Expression.MakeArithmeticParse();
                }
            }

            public class BuildHeaderUnit : SemanticUnit
            {
                public BuildHeaderUnit() { }
                public BuildHeaderUnit(bool isP):base(isP) { }  

                public bool IsBuildMainType;
                public bool IsBuildExpression;

                private string Typename;
                private string TypeMainName;

                private string VariableName;
                private string VariableExpression;

                private void BuildMainType(LineStructure structure)
                {
                    structure.ObjectPool[TypeMainName] = ReflectionExtension.Typen(Typename).CreateInstance();
                }
                private void BuildVariable(LineStructure structure)
                {
                    if (IsBuildExpression)
                        ArithmeticExtension.AddVariable(VariableName, new NotConstConstant(VariableExpression));
                    else
                        ArithmeticExtension.AddVariable(VariableName, new(VariableExpression.MakeArithmeticParse()));
                }

                public override SemanticUnit BuildUnit(List<LanguageUnit> units)
                {
                    int i = 0;
                    SemanticUnit result= null;
                    foreach (LanguageUnit unit in units)
                    {
                        //TODO
                        i++;
                    }
                    if (i != -1)
                    {
                        result = new BuildHeaderUnit(false)
                        {
                            IsBuildMainType = IsBuildMainType,
                            Typename = Typename,
                            TypeMainName = TypeMainName,
                            VariableName = VariableName,
                            VariableExpression = VariableExpression
                        };
                    }
                    return result;
                }

                public override void Execute(LineStructure structure)
                {
                    if (IsBuildMainType)
                        BuildMainType(structure);
                    else
                        BuildVariable(structure);
                }
            }
        }

        #region Unit

        //将源文本解析为可识别语言
        public sealed class LanguageUnit
        {
            public LanguageUnit(string origin)
            {
                Origin = origin;
                IsKeyWord = KeyWordSet.KeyWords.Contains(origin);
                if (origin[0] == '\"' && origin[^1] == '\"' && origin.Length > 1)
                    IsText = true;
                else
                    IsText = false;
                IsIndicationName = !IsText && !IsKeyWord;
                InternalOrigin = null;
            }
            public LanguageUnit(string translation, LanguageUnit origin) : this(translation)
            {
                InternalOrigin = origin;
            }

            public LanguageUnit InternalOrigin;
            public GrammaticalUnit GrammaticalInstruction;
            public string Origin;
            /// <summary>
            /// 是否为关键字
            /// </summary>
            public readonly bool IsKeyWord;
            /// <summary>
            /// 是否为指示名
            /// </summary>
            public readonly bool IsIndicationName;
            /// <summary>
            /// 是否为带有双引号的文本参数
            /// </summary>
            public readonly bool IsText;
        }

        //对语言的规范进行扩展,补充或修饰
        public abstract class GrammaticalUnit
        {
            public GrammaticalUnit(bool isKeyWord, bool isIndicationName)
            {
                IsKeyWord = isKeyWord;
                IsIndicationName = isIndicationName;
                if (isKeyWord && isIndicationName)
                {
                    throw new System.ArgumentException("It is not possible to bootstrap two different morphemes at the same time");
                }
            }

            /// <summary>
            /// 是否由关键字引导
            /// </summary>
            public readonly bool IsKeyWord;
            /// <summary>
            /// 是否由指示名引导
            /// </summary>
            public readonly bool IsIndicationName;

            public abstract ADException DetectAndRedefine(LanguageUnit unit);
        }

        //将语言翻译为实际行为
        public abstract class SemanticUnit
        {
            public bool IsParentUnit;
            public SemanticUnit()
            {
                IsParentUnit = false;
            }
            public SemanticUnit(bool isParentUnit)
            {
                IsParentUnit = isParentUnit;
            }

            public abstract SemanticUnit BuildUnit(List<LanguageUnit> units);

            public abstract void Execute(LineStructure structure);
        }

        #endregion

        //翻译器
        public abstract class LineTransformer
        {
            public abstract void Init();
            public abstract LanguageUnit[] Translate(string[] origins);
            public abstract GrammaticalUnit[] Detect(LanguageUnit unit);
            public abstract (int, GrammaticalUnit)[] ObsoleteOverRangeGrammar(LanguageUnit unit);
            public virtual SemanticUnit GetSemanticUnit(List<LanguageUnit> units)
            {
                var header = units.First();
                if (header.IsKeyWord)
                    return LineLanguage.GetSemanticUnit(header.Origin).BuildUnit(units);
                else 
                    return LineLanguage.GetSemanticUnit(LineLanguage.NotKeyWordSystemSemanticUnitName).BuildUnit(units);
            }
        }
    }

    public static class LineLanguage
    {
        private readonly static Dictionary<int, LineTransformer> LanguageTransformers;
        private readonly static LineStructure BadStructure;
        private readonly static Dictionary<string, GrammaticalUnit> GrammaticalUnits = new();
        private readonly static Dictionary<string, SemanticUnit> SemanticUnits;
        static LineLanguage()
        {
            LanguageTransformers = new();
            BadStructure = new LineStructure();
            SemanticUnits = new();
        }

        public static int AddLanguage(LineTransformer transformer)
        {
            int hash = transformer.GetHashCode();
            while (LanguageTransformers.ContainsKey(hash))
            {
                hash++;
            }
            LanguageTransformers.Add(hash, transformer);
            return hash;
        }
        public static LineTransformer GetLanguage(int id)
        {
            return LanguageTransformers[id];
        }
        public static bool TryGetLanguage(int id,out LineTransformer transformer)
        {
            return LanguageTransformers.TryGetValue(id, out transformer);
        }
        public static bool RemoveLanguage(int id)
        {
            return LanguageTransformers.Remove(id);
        }
        public static LineStructure BuildStructure(int languageId,int commandMode,params string[] strs)
        {
            if (commandMode == BreakCommandMode)
                return null;
            if(commandMode==CheckLanguageExist)
            {
                return LanguageTransformers.ContainsKey(languageId) ? BadStructure : null;
            }
            LineStructure structure = null;
            if(false)
            {
                //languageId
            }
            if(false)
            {
                //strs
            }
            if((AutoParseMode&commandMode)!=0)
            {
                if (!LanguageTransformers.ContainsKey(languageId))
                    return null;
                structure = new(LanguageTransformers[languageId], strs);
            }
            return structure;
        }

        public const int BreakCommandMode = -1;
        public const int CheckLanguageExist = 0;
        public const int AutoParseMode = 1 << 0;

        public static bool AddGrammar(string strName, GrammaticalUnit unit)
        {
            return GrammaticalUnits.TryAdd(strName, unit);
        }
        public static bool Contains(string strName)
        {
            return GrammaticalUnits.ContainsKey(strName);
        }
        public static bool TryGet(string strName, out GrammaticalUnit unit)
        {
            return GrammaticalUnits.TryGetValue(strName, out unit);
        }

        public static bool RegisterSemanticUnit(string id,SemanticUnit unit)
        {
            return SemanticUnits.TryAdd(id, unit);
        }
        public static SemanticUnit GetSemanticUnit(string id)
        {
            return SemanticUnits[id];
        }
        public static bool TryGetSemanticUnit(string id,out SemanticUnit unit)
        {
            return SemanticUnits.TryGetValue(id, out unit);
        }
        public static bool RemoveSemanticUnit(string id)
        {
            return SemanticUnits.Remove(id);
        }

        public const string NotKeyWordSystemSemanticUnitName = "___SystemUnit";
    }

    public class LineStructure
    {
        public Dictionary<string, object> ObjectPool;

        public LineStructure()
        {

        }
        public LineStructure(LineTransformer transformer, string[] lines)
        {
            m_transformer = transformer;
            Lines = new();
            try
            {
                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        List<StringView> views = new()
                        {
                            new(line)
                        };
                        DoBuildSingleLineStringViews(line, views);
                        List<LanguageUnit> currentLine = new();
                        Lines.Add(currentLine);
                        DoAddSingleLineLanguageUnitToOneLine(views, currentLine);
                    }
                }
            }
            catch
            {
                Lines = null;
                throw;
            }
        }

        private void DoAddSingleLineLanguageUnitToOneLine(List<StringView> views, List<LanguageUnit> currentLine)
        {
            if (m_transformer == null)
            {
                for (int i = 0, e = views.Count; i < e; i++)
                {
                    LanguageUnit unit = new(views[i].ToString());
                    currentLine.Add(unit);
                }
            }
            else
            {
                currentLine.AddRange(m_transformer.Translate(views.Contravariance(T => T.ToString()).ToArray()));
            }
        }

        private void DoBuildSingleLineStringViews(string line, List<StringView> views)
        {
            char ch;
            StringView current = views.Last();
            for (int i = 0, e = line.Length; i < e; i++)
            {
                current.Right = i;
                ch = line[i];
                if (ch == ' ')
                {
                    while (i < e && line[i] == ' ')
                    {
                        i++;
                    }
                    if (i < e)
                    {
                        current = new(line);
                        views.Add(current);
                        current.Left = i;
                    }
                }
                else if (ch == '\"')
                {
                    do
                    {
                        i++;
                    } while (i < e && line[i] == '\"');
                }
            }
        }

        private LineTransformer m_transformer;
        private List<List<LanguageUnit>> Lines;

        public void BuildExecutePipe()
        {
            Dictionary<int, List<GrammaticalUnit>> grammaticalUnits = new();
            foreach (var line in Lines)
            {
                grammaticalUnits.Remove(TemporaryTimeGrammatical);
                if (m_transformer != null)
                {
                    var Obsoleters = m_transformer.ObsoleteOverRangeGrammar(line[0]);
                    foreach (var obsoleter in Obsoleters)
                    {
                        grammaticalUnits.TryGetValue(obsoleter.Item1, out var grammaticalSet);
                        grammaticalSet.Remove(obsoleter.Item2);
                    }
                }
                foreach (var languageUnit in line)
                {
                    DoDetectAndRedefineLanguageUnit(grammaticalUnits, languageUnit);
                }
            }
        }

        private static void DoDetectAndRedefineLanguageUnit(Dictionary<int, List<GrammaticalUnit>> grammaticalUnits, LanguageUnit languageUnit)
        {
            if(grammaticalUnits.TryGetValue(SystemFirstGrammatical,out var systemUnits))
            {
                foreach (var grammaticalUnit in systemUnits)
                {
                    grammaticalUnit.DetectAndRedefine(languageUnit);
                }
            }
            foreach (var grammarItem in grammaticalUnits)
            {
                if (grammarItem.Key == SystemFirstGrammatical) continue;
                foreach (var grammaticalUnit in grammarItem.Value)
                {
                    grammaticalUnit.DetectAndRedefine(languageUnit);
                }
            }
        }

        public const int TemporaryTimeGrammatical = 0;
        public const int SystemFirstGrammatical = 31;

        public void ReleaseSemanticPipe()
        {
            semanticUnits = new();
            ObjectPool = new();
            foreach (var line in Lines)
            {
                semanticUnits.Add(GetSemanticUnit(line));
            }
        }

        public SemanticUnit GetSemanticUnit(List<LanguageUnit> units)
        {
            if (m_transformer!=null)
                return m_transformer.GetSemanticUnit(units);
            else
                return LineLanguage.GetSemanticUnit(LineLanguage.NotKeyWordSystemSemanticUnitName).BuildUnit(units);
        }

        private List<SemanticUnit> semanticUnits;

        public void Execute()
        {
            foreach (var unit in semanticUnits)
            {
                unit.Execute(this);
            }
        }
    }
}
