using AD;
using AD.BASE;
using RhythmGame.Time;
using RhythmGame.Visual;
using RhythmGame.Visual.Note;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame.Editor
{
    public class RandomManager : ADController, IController
    {
        public MonoBehaviour MonoTarget => this;

        public NoteBase NoteA, NoteB;
        public Transform ParRoot;

        public override void Init()
        {
            int baseTime = 1;
            Architecture.GetController<GuideLine>().Share(out var line);
            line.Vertexs.Clear();
            line.Vertexs.Add(new VertexData(new AD.Utility.MeshExtension.VertexEntry()
            {
                Position = Vector3.zero,
                Normal = Vector3.right,
                Size = 0.3f,
                Type = AD.Utility.MeshExtension.BuildNormalType.JustDirection
            }));
            for (int i = 0, e = (int)App.instance.GetController<TimeController>().MainAudioSource.CurrentClip.length; i < e; i++)
            {
                NoteBase current = (Random.value > 0.5 ? NoteA.PrefabInstantiate() : NoteB.PrefabInstantiate());
                current.JudgeTimeExpression = (baseTime + Random.value + i).ToString();
                if (Random.value > 0.75)
                {
                    NoteBase CurDouble = (Random.value > 0.5 ? NoteA.PrefabInstantiate() : NoteB.PrefabInstantiate());
                    CurDouble.transform.SetParent(ParRoot, false);
                    CurDouble.JudgeTimeExpression = current.JudgeTimeExpression;
                    CurDouble.LocalPostion = new string[2] { "{WorldX(" + ((Random.value - 0.5f) * 12).ToString() + ")}", "{WorldY(" + ((Random.value - 0.5f) * 5).ToString() + ")}" };
                    CurDouble.LocalEulerAngles = new string[3]
                    {  "0","0",((Random.value - 0.5f) * 180).ToString() };
                }
                current.transform.SetParent(ParRoot, false);
                current.LocalPostion = new string[2] { "{WorldX(" + ((Random.value - 0.5f) * 15).ToString() + ")}", "{WorldY(" + ((Random.value - 0.5f) * 7).ToString() + ")}" };
                current.LocalEulerAngles = new string[3]
                {  "0","0",((Random.value - 0.5f) * 180).ToString() };
                if (i % 5 == 0 && i != 0)
                {
                    float x = Random.value * 7 - 3.5f, y = Random.value * 5 - 2.5f;
                    line.Vertexs.Add(new VertexData(new AD.Utility.MeshExtension.VertexEntry()
                    {
                        Position = new Vector3(0, 0, i * 100),
                        Normal = Vector3.right,
                        Size = 0.3f,
                        Type = AD.Utility.MeshExtension.BuildNormalType.JustDirection
                    }));
                    line.Vertexs.Add(new VertexData(new AD.Utility.MeshExtension.VertexEntry()
                    {
                        Position = new Vector3(x, y, i * 100 + 50),
                        Normal = Vector3.right,
                        Size = 0.3f,
                        Type = AD.Utility.MeshExtension.BuildNormalType.JustDirection
                    }));
                    line.Vertexs.Add(new VertexData(new AD.Utility.MeshExtension.VertexEntry()
                    {
                        Position = new Vector3(x, y, (i + 5) * 100 - 250),
                        Normal = Vector3.right,
                        Size = 0.3f,
                        Type = AD.Utility.MeshExtension.BuildNormalType.JustDirection
                    }));
                    line.Vertexs.Add(new VertexData(new AD.Utility.MeshExtension.VertexEntry()
                    {
                        Position = new Vector3(0, 0, (i + 5) * 100 - 200),
                        Normal = Vector3.right,
                        Size = 0.3f,
                        Type = AD.Utility.MeshExtension.BuildNormalType.JustDirection
                    }));
                }
            }
            line.SetDirty();
        }

        private void Start()
        {
            ADGlobalSystem.OpenCoroutine(() => !App.instance.Contains<GuideLine>()||!App.instance.Contains<TimeController>(), () =>
            {
                App.instance.RegisterController(this);
            });
        }
    }
}
