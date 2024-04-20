using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AD.Utility
{
    public static class RectExtension
    {
        public static bool OverlapsNoAngle(this Rect self,Rect target)
        {
            return self.Overlaps(target);
        }
    }
}
