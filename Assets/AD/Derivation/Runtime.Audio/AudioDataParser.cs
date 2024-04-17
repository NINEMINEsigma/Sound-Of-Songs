using AD.BASE;
using AD.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using AD.Derivation;

namespace AD.Experimental.Runtime
{
    public interface IAudioFilters:IBaseFilter<float>
    {
        //Limit filters
    }

    public class AudioDataParser : MonoBehaviour
    {
        [SerializeField] private AudioSourceController m_Source;
        public AudioSourceController Source
        {
            get { return m_Source = m_Source != null ? m_Source : this.SeekComponent<AudioSourceController>(); }
        }
        [SerializeField] private AudioClip m_Clip;
        public AudioClip TargetClip
        {
            get
            {
                return m_Clip ?? Source.CurrentClip;
            }
        }

        public void Parse(float[][] datas, int start)
        {
            if (TargetClip != null && datas != null)
            {
                for (int i = 0, e = datas.Length; i < e; i++)
                {
                    TargetClip.GetData(datas[i], start + i);
                }
            }
        }

        public void Parse(float[] data, int offset)
        {
            if (TargetClip != null && data != null)
            {
                TargetClip.GetData(data, offset);
            }
        }
    }
}
