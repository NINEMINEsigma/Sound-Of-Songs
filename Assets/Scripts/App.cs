using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using UnityEngine;

namespace RhythmGame
{
    public sealed class App : ADArchitecture<App>
    {
        public float CameraSafeAreaPanel = 0;
        public float MinDepth, MaxDepth;
        public Vector3 StartVertex, EndVertex;
        public float ViewportWidth, ViewportHeight;
        

        public void MatchData(IController controller)
        {

        }
    }
}
