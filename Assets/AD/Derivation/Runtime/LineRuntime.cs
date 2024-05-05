using System.Collections;
using System.Collections.Generic;
using AD.Experimental.Runtime.Internal;
using UnityEngine;

namespace AD.Experimental.Runtime
{
    namespace Internal
    {
        //将源文本解析为可识别语言
        public class LanguageUnit
        {
            
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
            if (lines != null)
            {
                char ch;
                foreach (var line in lines)
                {
                    List<(int,int)> word_views = new();
                    for (int i = 0, e = line.Length; i < e; i++)
                    {

                    }
                }
            }
        }

        private LineTransformer m_transformer;
        private List<LanguageUnit[]> Lines;
    }
}