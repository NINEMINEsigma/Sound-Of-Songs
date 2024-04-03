using AD.BASE;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame.Visual.Note
{
    public class TouchLock:ADModel
    {
        public Dictionary<int, string> TouchLockList;

        public override void Init()
        {
            TouchLockList = new();
        }
    }
}
