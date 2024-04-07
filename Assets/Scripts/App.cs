using System;
using System.Collections.Generic;
using System.IO;
using AD;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Gadget.Malody;
using AD.Math;
using AD.Reflection;
using AD.UI;
using AD.Utility;
using RhythmGame.Time;
using RhythmGame.Visual;
using RhythmGame.Visual.Note;
using UnityEngine;
using static AD.Reflection.ReflectionExtension;
using static UnityEngine.GraphicsBuffer;

namespace RhythmGame
{
    public sealed class App : ADArchitecture<App>
    {
        public static float CurrentTime;

        public const string ActualSongTime2Percentage = "SongTime";
        public float Internal_ActualSongTime2Percentage(float t)
        {
            return (t - StartTime) / (EndTime - StartTime);
        }
        public const string ViewportPositionX2WorldPositionX = "WorldX";
        public float Internal_ViewportPositionX2WorldPositionX(float x)
        {
            return x / ViewportWidth;
        }
        public const string ViewportPositionY2WorldPositionY = "WorldY";
        public float Internal_ViewportPositionY2WorldPositionY(float x)
        {
            return x / ViewportHeight;
        }

        public float CameraSafeAreaPanel = 0;
        public float MinDepth, MaxDepth;
        public Vector3 StartVertex, EndVertex;
        public float StartTime, EndTime;
        public float ViewportWidth, ViewportHeight;

        public NoteBase NoteA, NoteB;
        public Transform ParRoot;

        public float DepthMul
        {
            get
            {
                ArithmeticVariable demul = new("_DepthMul");
                if (demul) return demul.ReadValue();
                else return 50;
            }
        }


        public void MatchData(IController controller)
        {

        }

        public override void Init()
        {
            base.Init();

            MinDepth = Mathf.Infinity;
            MaxDepth = 0;
            StartVertex = EndVertex = Vector3.zero;
            StartTime = EndTime = 0;

            ArithmeticExtension.AddFunction(ActualSongTime2Percentage, new(ADReflectedMethod.Temp<float, float>(Internal_ActualSongTime2Percentage), this));
            ArithmeticExtension.AddFunction(ViewportPositionX2WorldPositionX, new(ADReflectedMethod.Temp<float, float>(Internal_ViewportPositionX2WorldPositionX), this));
            ArithmeticExtension.AddFunction(ViewportPositionY2WorldPositionY, new(ADReflectedMethod.Temp<float, float>(Internal_ViewportPositionY2WorldPositionY), this));

            ArithmeticExtension.AddFunction(Second2BeatCounter, new(ADReflectedMethod.Temp<float, float, float, float>(Internal_Second2BeatCounter), this));

            RegisterModel<JudgeEffectStack>()
                ;
        }

        public const string Second2BeatCounter = "Beat";
        public float Internal_Second2BeatCounter(float measure, float beat, float beat_p)
        {
            ArithmeticVariable bpm = new("bpm");
            if (bpm)
            {
                float second;
                float SongBpm = bpm.ReadValue();
                float SecondPerBar = 60 / SongBpm;
                second = SecondPerBar * (measure + beat / beat_p);
                return second;
            }
            return 0;
        }
    }

    [Serializable]
    public class StartEndData : AD.Utility.StartEndData
    {
        public void OnSerialize()
        {
            PropertiesLayout.Label(nameof(this.StartTime));
            var stip = PropertiesLayout.InputField(StartTime.ToString(), nameof(this.StartTime));
            stip.source.onSelect.AddListener(_ => stip.SetTextWithoutNotify(StartTimeExpression));
            stip.source.onEndEdit.AddListener(T =>
            {
                StartTimeExpression = T;
                stip.SetTextWithoutNotify(StartTime.ToString());
            });

            PropertiesLayout.Label(nameof(this.EndTime));
            var etip = PropertiesLayout.InputField(EndTime.ToString(), nameof(this.EndTime));
            etip.source.onSelect.AddListener(_ => etip.SetTextWithoutNotify(EndTimeExpression));
            etip.source.onEndEdit.AddListener(T =>
            {
                EndTimeExpression = T;
                etip.SetTextWithoutNotify(EndTime.ToString());
            });

            PropertiesLayout.Label(nameof(this.StartValue));
            var svip = PropertiesLayout.InputField(StartValue.ToString(), nameof(this.StartValue));
            svip.source.onSelect.AddListener(_ => svip.SetTextWithoutNotify(StartValueExpression));
            svip.source.onEndEdit.AddListener(T =>
            {
                StartValueExpression = T;
                svip.SetTextWithoutNotify(StartValue.ToString());
            });

            PropertiesLayout.Label(nameof(this.EndValue));
            var evip = PropertiesLayout.InputField(EndValue.ToString(), nameof(this.EndValue));
            evip.source.onSelect.AddListener(_ => evip.SetTextWithoutNotify(EndValueExpression));
            evip.source.onEndEdit.AddListener(T =>
            {
                EndValueExpression = T;
                evip.SetTextWithoutNotify(EndValue.ToString());
            });
        }
    }

    public static class RhythmGameCommandScript
    {
        public static bool IsEnableScriptReading = false;
        private static object commander = new RhythmGame.Load.Script();
        private static Type commanderType = typeof(RhythmGame.Load.Script);

        private static object GetArg(string T)
        {
            if (T.Contains('"'))
            {
                string result = T[(T.IndexOf('"') + 1)..T.LastIndexOf('"')];
                DebugExtension.LogMessage("GetArg " + result);
                return result;
            }
            else
            {
                if (ArithmeticExtension.TryParse(T, out var arithmeticInfo))
                {
                    float result = arithmeticInfo.ReadValue();
                    DebugExtension.LogMessage("GetArg " + result.ToString());
                    return result;
                }
                else
                {
                    DebugExtension.LogMessage("GetArg " + T + " is failed parse");
                    return 0;
                }
            }
        }

        public static void Read(string[] lines)
        {
            foreach (var lineSingle in lines)
            {
                if (string.IsNullOrEmpty(lineSingle)) continue;
                if (lineSingle.StartsWith("//")) continue;

                string line = lineSingle.Replace('\n', ' ').Trim(' ');
                string[] strs;
                if (line[0] == '{')
                {
                    strs = new string[1] { line };
                }
                else
                {
                    if (line.Contains('|')) strs = line.Trim().Split('|');
                    else strs = line.Trim(' ').Split(' ');
                }
                for (int i = 0; i < strs.Length; i++)
                {
                    strs[i] = strs[i].Trim(' ');
                }
                DebugExtension.LogMessage("source [ length = " + strs.Length + " ] = " + strs.LinkAndInsert(' '));
                if (strs.Length > 1)
                    Parse(strs[0], strs[1..]);
                else
                    Parse(strs[0]);
            }
        }

        private static void Parse(string commandHeader, params string[] args)
        {
            IsEnableScriptReading = true;
            {
                if (commandHeader == "import")
                {
                    args.CheckLength(out string typename);
                    ImportCommandType(typename);
                }
                else if (commandHeader == "build")
                {
                    BuildCommandInstance(args);
                }
                else if (commandHeader == "while")
                {
                    args.CheckLength(out string init, out string indexName, out string step, out string max, out string func);
                    WhileRun(init, indexName, step, max, func);
                }
                else if (commandHeader == "if")
                {
                    args.CheckLength(out string indexName, out string func);
                    IfRun(indexName, func);
                }
                else
                {
                    //DebugExtension.LogMessage(commandHeader);
                    object _ = commander.RunMethodByName(commandHeader, ReflectionExtension.DefaultBindingFlags, args.Contravariance(GetArg).ToArray());
                }
            }
            IsEnableScriptReading = false;
        }

        private static float Parse(string excption)
        {
            IsEnableScriptReading = true;

            DebugExtension.LogMessage(excption);
            float endResult = ArithmeticExtension.TryParse(excption, out var result) ? result.ReadValue() : -1;

            IsEnableScriptReading = false;
            return endResult;
        }

        private static void DoParse(string func)
        {
            try
            {
                object obj = GetArg(func);
                if (obj is string line)
                {
                    if (!line.StartsWith("//"))
                    {
                        string[] strs;
                        if (line.Contains('|')) strs = line.Trim(' ').Split('|');
                        else strs = line.Trim(' ').Split(' ');
                        for (int i = 0; i < strs.Length; i++)
                        {
                            strs[i] = strs[i].Trim(' ');
                        }
                        if (strs.Length > 0)
                            Parse(strs[0], strs[1..]);
                        else
                            Parse(strs[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void ImportCommandType(string name)
        {
            name = name.Trim();
            commanderType = ADGlobalSystem.FinalCheck(ReflectionExtension.Typen(name), $"type {name} is not find");
            DebugExtension.LogMessage("import " + commanderType.FullName);
        }

        private static void BuildCommandInstance(string[] args)
        {
            if (args[0] == "void")
                commander = ReflectionExtension.CreateInstance(commanderType);
            else
                commander = ReflectionExtension.CreateInstance(commanderType, args.Contravariance(GetArg).ToArray());
            DebugExtension.LogMessage("build " + commander.ToString());
        }

        private static void WhileRun(string init, string indexName, string step, string max, string func)
        {
            float initResult = ArithmeticExtension.TryParse(init, out var initResultInfo) ? initResultInfo.ReadValue() : -1;
            DebugExtension.LogMessage("init Result->" + initResult.ToString());
            int loopCounter = 0;
            string index = "{" + indexName + "}";
            while (index.MakeArithmeticParse() < max.MakeArithmeticParse())
            {
                DebugExtension.LogMessage("loopCounter->" + (loopCounter++).ToString());
                DoParse(func);
                ArithmeticVariable.VariableConstantPairs[indexName].Value.SetValue(index.MakeArithmeticParse() + step.MakeArithmeticParse());
            }
        }

        private static void IfRun(string indexName, string func)
        {
            string index = "{" + indexName + "}";
            if (index.MakeArithmeticParse() != 0)
            {
                DoParse(func);
            }
        }

        public static void Write(Stream stream, params string[] lines)
        {
            using var writer = new StreamWriter(stream);
            for (int i = 0, e = lines.Length; i < e; i++)
            {
                writer.WriteLine(lines[i]);
            }
        }
    }

    namespace Load
    {
        public class Script
        {
            public Script() : this("", "") { }
            public Script(string projectName, string creater)
            {
                ProjectName = projectName;
                CreaterName = creater;
                //ArithmeticExtension.AddFunction(nameof(LoadSong), new(new(this.GetType().GetMethod(nameof(LoadSong)), 1), this));
                //ArithmeticExtension.AddFunction(nameof(Vertex), new(new(this.GetType().GetMethod(nameof(Vertex)), 7), this));
                //ArithmeticExtension.AddFunction(nameof(Note), new(new(this.GetType().GetMethod(nameof(Note)), 5), this));
            }

            public string ProjectName;
            public string CreaterName;

            public float LoadSong(string path)
            {
                if (!RhythmGameCommandScript.IsEnableScriptReading) return -1;

                path = path.Replace("...StreamingAssets", Application.streamingAssetsPath);
                path = path.Replace("...PersistentData", Application.persistentDataPath);

                Debug.LogWarning("LoadSong " + path);

                App.instance.GetController<TimeController>().Share(out var tc).MainAudioSource.LoadOnUrl(path, AudioSourceController.GetAudioType(path), true);
                ADGlobalSystem.OpenCoroutine(() =>
                {
                    bool result = tc.MainAudioSource.CurrentClip == null;
                    if (!result)
                        Debug.LogWarning(tc.MainAudioSource.CurrentSourcePair.CilpName);
                    return result;
                }, () =>
                {
                    tc.ResetSongSetting();

                    var MainGuideLine = App.instance.GetController<GuideLine>();
                    MainGuideLine.RebuildImmediately();
                    for (int i = 0, e = MainGuideLine.RealVertexs.Count; i < e; i++)
                    {
                        var current = MainGuideLine.RealVertexs[i];
                        if (current.Position.z < App.instance.MinDepth) App.instance.MinDepth = current.Position.z;
                        if (current.Position.z > App.instance.MaxDepth) App.instance.MaxDepth = current.Position.z;
                    }

                    tc.Replay();

                    App.instance.GetController<CameraCore>().min = App.instance.MinDepth;
                    App.instance.GetController<CameraCore>().max = App.instance.MaxDepth;

                    App.instance.GetController<CameraCore>().SetDirty();
                });

                return 0;
            }

            public float LoadMelodyBM(string path)
            {
                if (!RhythmGameCommandScript.IsEnableScriptReading) return -1;

                if (AD.ADGlobalSystem.Input<MalodyBeatMapBM>(path, out object obj))
                {
                    MalodyBeatMapBM bm = obj as MalodyBeatMapBM;
                    MalodyBeatMapBMTimeMode bmT = bm.ToTimeMode();
                    Dictionary<float, int> coms = new();
                    foreach (var note in bmT.note)
                    {
                        string x = (Mathf.Cos(note.column / (float)bm.extra.test.divide * Mathf.PI * 2) * 0.2f).ToString();
                        string y = (Mathf.Sin(note.column / (float)bm.extra.test.divide * Mathf.PI * 2 + Mathf.PI) * 0.2f).ToString();
                        float judgeTime = note.keyTime;
                        Note((coms.TryGetValue(judgeTime, out int num) ? num : 0) % 2, judgeTime.ToString(), x, y, (judgeTime * 223.65 / 360.0f).ToString());
                        coms.TryAdd(judgeTime, 0);
                        coms[judgeTime] = coms[judgeTime] + 1;
                    }
                    return 0;
                }
                else return -1;
            }

            public float Vertex(string posX, string posY, string depth, string nomX, string nomY, string nomZ, string size)
            {
                if (!RhythmGameCommandScript.IsEnableScriptReading) return -1;

                var line = App.instance.GetController<GuideLine>();
                line.Vertexs.Add(new VertexData(
                    new string[3] { posX, posY, depth },
                    new string[3] { nomX, nomY, nomZ },
                    size
                    ));
                line.SetDirty();

                return 0;
            }

            public float Vertex_N(string posX, string posY, string depth, string nomX, string nomY, string nomZ, string size)
            {
                return Vertex(
                    posX.MakeArithmeticParse().ToString(),
                    posY.MakeArithmeticParse().ToString(),
                    depth.MakeArithmeticParse().ToString(),
                    nomX.MakeArithmeticParse().ToString(),
                    nomY.MakeArithmeticParse().ToString(),
                    nomZ.MakeArithmeticParse().ToString(),
                    size.MakeArithmeticParse().ToString()
                    );
            }

            //public float Track(string name,string curve,string posX,string posY,string depth)
            //{
            //
            //}

            public float Timing(string startTime, string endTime, string startValue, string endValue, string easeCurve)
            {
                if (!RhythmGameCommandScript.IsEnableScriptReading) return -1;
                App.instance.GetController<CameraCore>().TimingPairs.Add(new()
                {
                    StartTimeExpression = startTime,
                    EndTimeExpression = endTime,
                    StartValueExpression = startValue,
                    EndValueExpression = endValue,
                    EaseCurveTypeExpression = easeCurve
                });
                return 0;
            }

            public float Note(float noteType, string judgeTime, string posX, string poxY, string eulerZ)
            {
                if (!RhythmGameCommandScript.IsEnableScriptReading) return -1;
                int _noteType = (int)noteType;

                NoteBase _note;
                if (_noteType == 0) _note = App.instance.NoteA.PrefabInstantiate();
                else if (_noteType == 1) _note = App.instance.NoteB.PrefabInstantiate();
                else return -1;

                _note.transform.SetParent(App.instance.ParRoot);

                _note.JudgeTimeExpression = judgeTime;
                ArithmeticVariable delay = new("delay");
                if (delay) _note.JudgeTimeExpression += "+(" + delay.ReadValue().ToString() + ")";
                _note.LocalPostion = new string[2] { posX, poxY };
                _note.LocalEulerAngles = new string[3] { "0", "0", eulerZ };
                _note.SetDirty();

                return 0;
            }

            public float Note_N(float noteType, string judgeTime, string posX, string poxY, string eulerZ)
            {
                return Note(
                    noteType,
                    judgeTime.MakeArithmeticParse().ToString(),
                    posX.MakeArithmeticParse().ToString(),
                    poxY.MakeArithmeticParse().ToString(),
                    eulerZ.MakeArithmeticParse().ToString());
            }

            public float Set(string name, float value)
            {
                ArithmeticExtension.AddVariable(name, new(value));
                return 0;
            }
        }
    }
}
