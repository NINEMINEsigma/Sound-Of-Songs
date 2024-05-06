using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AD.Experimental.Runtime.Internal;
using AD.Utility;
using UnityEngine;

namespace AD.Experimental.Runtime
{

    namespace Internal
    {
        public static class KeyWordSet
        {
            public readonly static string[] KeyWords = new[]
            {
            //用于创建对象
            "build",
            //用于表明随后的指示名是一个类型
            "typename"
            };
        }

        //将源文本解析为可识别语言
        public class LanguageUnit
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
            }

            public string Origin;
            //build     typename    MainType        Name
            //KeyWord   KeyWord     IndicationName  IndicationName
            //该语句build一个typename为MainType的对象并命名为Name
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
        public class GrammaticalUnit
        {
            
        }

        //将语言翻译为实际行为
        public class SemanticUnit
        {

        }

        //翻译器
        public class LineTransformer
        {
            
        }
    }

    public static class LineLanguage
    {
        private readonly static Dictionary<int, LineTransformer> LanguageTransformers;
        private readonly static LineStructure BadStructure;
        static LineLanguage()
        {
            LanguageTransformers = new();
            BadStructure = new LineStructure();
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
    }

    public class LineStructure
    {
        public LineStructure()
        {

        }
        public LineStructure(LineTransformer transformer, string[] lines)
        {
            m_transformer = transformer;
            Lines = new();
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

        private static void DoAddSingleLineLanguageUnitToOneLine(List<StringView> views, List<LanguageUnit> currentLine)
        {
            for (int i = 0, e = views.Count; i < e; i++)
            {
                LanguageUnit unit = new(views[i].ToString());
                currentLine.Add(unit);
            }
        }

        private static void DoBuildSingleLineStringViews(string line, List<StringView> views)
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
    }
}