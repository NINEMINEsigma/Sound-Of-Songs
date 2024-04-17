using AD.Experimental.Runtime.Internal;
using AD.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AD.Experimental.Runtime
{
    public interface IBaseFilter<_Type>
    {
        _Type[][] Filtrate(_Type[][] source, int width, int length);
        void FiltrateSource(_Type[][] source, int width, int length);
    }

    public interface ITreeFilter<_Type, _NextFilter> : IBaseFilter<_Type> where _NextFilter : IBaseFilter<_Type>
    {
        _Type[][] Filtrate(_Type[][] source, int width, int length, _NextFilter next);
    }

    namespace Internal
    {
        public abstract class BaseFilter : IBaseFilter<float>
        {
            protected abstract void DoFiltrate(float[][] source, float[][] result, int width, int length);

            public float[][] Filtrate(float[][] source, int width, int length)
            {
                float[][] result = new float[width][];
                for (int i = 0; i < width; i++)
                {
                    result[i] = new float[length];
                }
                DoFiltrate(source, result, width, length);
                return result;
            }

            public void FiltrateSource(float[][] source, int width, int length)
            {
                DoFiltrate(source, source, width, length);
            }
        }

        public abstract class BaseColorFilter : IBaseFilter<Color>
        {
            protected abstract void DoFiltrate(Color[][] source, Color[][] result, int width, int length);

            public Color[][] Filtrate(Color[][] source, int width, int length)
            {
                Color[][] result = new Color[width][];
                for (int i = 0; i < width; i++)
                {
                    result[i] = new Color[length];
                }
                DoFiltrate(source, result, width, length);
                return result;
            }

            public void FiltrateSource(Color[][] source, int width, int length)
            {
                DoFiltrate(source, source, width, length);
            }
        }
    }

    [Serializable]
    public class LimitFilter : BaseFilter
    {
        [SerializeField] public float min { get; private set; }
        [SerializeField] public float max { get; private set; }
        [SerializeField] public float maxEnhance { get; private set; }
        [SerializeField] public float maxWeakened { get; private set; }
        [SerializeField] public int bufferSize { get; private set; }

        private float[][] buffer;
        private int header = 0;

        public LimitFilter(float min, float max, float maxEnhance, float maxWeakened, int bufferSize)
        {
            this.min = min;
            this.max = max;
            this.maxEnhance = maxEnhance;
            this.maxWeakened = maxWeakened;
            this.bufferSize = bufferSize;
        }

        protected override void DoFiltrate(float[][] source, float[][] result, int width, int length)
        {
            InitBuffer(source, result, width, length);
            for (int i = 0; i < width; i++)
            {
                ReadLine(source, result, i, width, length);
                BuildLine(source, result, i, width, length);
            }
            ClearBuffer();
        }

        protected virtual void InitBuffer(float[][] source, float[][] result, int width, int length)
        {
            buffer ??= new float[bufferSize][];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = new float[length];
                if (i < width)
                    for (int j = 0; j < length; j++)
                    {
                        buffer[i][j] = source[i][j];
                    }
            }
        }

        protected virtual void ClearBuffer()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = null;
            }
        }

        protected virtual void ReadLine(float[][] source, float[][] result, int offset, int width, int length)
        {
            //Move data to buffer
            for (int i = 0, e = length; i < e; i++)
            {
                buffer[header][i] = source[offset][i];
            }
            //Push buffer header
            header = (header + 1) % bufferSize;
        }

        protected virtual void BuildLine(float[][] source, float[][] result, int offset, int width, int length)
        {
            int tempHeader = header;
            float[] line = new float[length];
            do
            {
                //Mark data
                for (int i = 0; i < length; i++)
                {
                    float value = buffer[tempHeader][i];
                    float lastValue = buffer[(tempHeader - 1) % bufferSize][i];
                    if (value - lastValue > maxEnhance)
                    {
                        line[i] += lastValue + maxEnhance;
                    }
                    else if (lastValue - value > maxWeakened)
                    {
                        line[i] += lastValue - maxWeakened;
                    }
                    else if (value > max)
                    {
                        line[i] += max;
                    }
                    else if (value < min)
                    {
                        line[i] += min;
                    }
                    else
                    {
                        line[i] += value;
                    }
                    line[i] = line[i] / 2.0f;
                }
                //Push buffer header
                tempHeader = (tempHeader + 1) % bufferSize;
            }
            while (tempHeader != header);
            for (int i = 0; i < length; i++)
            {
                result[offset][i] = line[i];
            }
        }
    }

    [Serializable]
    public class ColorLimitFilter : BaseColorFilter
    {
        [SerializeField] public Color min { get; private set; }
        [SerializeField] public Color max { get; private set; }
        [SerializeField] public Color maxEnhance { get; private set; }
        [SerializeField] public Color maxWeakened { get; private set; }

        public ColorLimitFilter(Color min, Color max, Color maxEnhance, Color maxWeakened)
        {
            this.min = min;
            this.max = max;
            this.maxEnhance = maxEnhance;
            this.maxWeakened = maxWeakened;
        }

        protected override void DoFiltrate(Color[][] source, Color[][] result, int width, int length)
        {
            for (int i = 0; i < width; i++)
            {
                BuildLine(source, result, i, width, length);
            }
        }

        protected virtual void BuildLine(Color[][] source, Color[][] result, int offset, int width, int length)
        {
            Color[] line = new Color[length];
            //Mark data
            for (int i = 0; i < length; i++)
            {
                Color value = source[(offset - 1) % source.Length][i];
                Color lastValue = source[offset][i];
                line[i] = new(BuildColorR(value, lastValue, line, i),
                              BuildColorG(value, lastValue, line, i),
                              BuildColorB(value, lastValue, line, i),
                              BuildColorA(value, lastValue, line, i));
            }
            for (int i = 0; i < length; i++)
            {
                result[offset][i] = line[i];
            }
        }

        protected float BuildColorA(Color value, Color lastValue, Color[] line, int i)
        {
            float a = 0;
            if (value.a - lastValue.a > maxEnhance.a)
            {
                a = line[i].a + lastValue.a + maxEnhance.a;
            }
            else if (lastValue.a - value.a > maxWeakened.a)
            {
                a = line[i].a + lastValue.a - maxWeakened.a;
            }
            else if (value.a > max.a)
            {
                a = line[i].a + max.a;
            }
            else if (value.a < min.a)
            {
               a = line[i].a - min.a;
            }
            return a/2.0f;
        }

        protected float BuildColorR(Color value, Color lastValue, Color[] line, int i)
        {
            float r = 0;
            if (value.  r - lastValue.r > maxEnhance.r)
            {
                r = line[i].r + lastValue.r + maxEnhance.r;
            }
            else if (lastValue.r - value.r > maxWeakened.r)
            {
                r = line[i].r + lastValue.r - maxWeakened.r;
            }
            else if (value.r > max.r)
            {
                r = line[i].r + max.r;
            }
            else if (value.r < min.r)
            {
                r = line[i].r - min.r;
            }
            return r/2.0f;
        }

        protected float BuildColorG(Color value, Color lastValue, Color[] line, int i)
        {
            float g = 0;
            if (value.g - lastValue.g > maxEnhance.g)
            {
                g = line[i].g + lastValue.g + maxEnhance.g;
            }
            else if (lastValue.g - value.g > maxWeakened.g)
            {
                g = line[i].g + lastValue.g - maxWeakened.g;
            }
            else if (value.g > max.g)
            {
                g = line[i].g + max.g;
            }
            else if (value.g < min.g)
            {
                g = line[i].g - min.g;
            }
            return g/2.0f;
        }

        protected float BuildColorB(Color value, Color lastValue, Color[] line, int i)
        {
            float b = 0;
            if (value.b - lastValue.b > maxEnhance.b)
            {
                b = line[i].b + lastValue.b + maxEnhance.b;
            }
            else if (lastValue.b - value.b > maxWeakened.b)
            {
                b = line[i].b + lastValue.b - maxWeakened.b;
            }
            else if (value.b > max.b)
            {
                b = line[i].b + max.b;
            }
            else if (value.b < min.b)
            {
                b = line[i].b - min.b;
            }
            return b/2.0f;
        }
    }
}
