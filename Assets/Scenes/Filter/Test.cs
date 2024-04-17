using AD.Experimental.Runtime;
using AD.UI;
using System.Linq;
using UnityEngine;

namespace T3T1
{
    public class Test : MonoBehaviour
    {
        //public RawImage A, B;
        //
        //public Texture2D source;
        //
        //public Color min, max, maxEnhange, maxW;
        public float min, max, maxEnhange, maxW;
        public int bufferSize;

        public float[] values;


        public void RendererB()
        {
            LimitFilter limitFilter = new(min, max, maxEnhange, maxW, bufferSize);
            float[][] valuess=new float[1][];
            valuess[0] = values;
            limitFilter.FiltrateSource(valuess, 1, values.Length);

            //A.MainTex = source;
            //Texture2D newTex = new(source.width, source.height);
            //Color[] cols = source.GetPixels(0);//new Color[source.width * source.height]; 
            //ColorLimitFilter limitFilter = new(min, max * 4, maxEnhange, maxW);
            //
            //Color[][] colstemp=new Color[1][];
            //colstemp[0] = cols;
            //colstemp=limitFilter.Filtrate(colstemp,1, source.width*source.height);
            //
            //newTex.SetPixels(cols, 0);
            //newTex.Apply();
            //B.MainTex = newTex;
        }

    }
}
