using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    public interface IVisualBase
    {
        bool IsDirty { get; }
        void SetDirty();
        void Rebuild();
    }
}
