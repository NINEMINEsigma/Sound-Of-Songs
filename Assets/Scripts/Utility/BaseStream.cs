using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD.BASE;

namespace RhythmGame.IO
{
    public abstract class BaseStream
    {
        private ADFile m_TargetFile;
        public ADFile TargetFile { get => m_TargetFile; private set => m_TargetFile = value; }

        public BaseStream(ADFile file)
        {
            TargetFile = file;
        }

        protected abstract void PreProcessing();
        protected abstract void PostProcessing();
        protected abstract void EachParagraph(params object[] args);
    }
}
