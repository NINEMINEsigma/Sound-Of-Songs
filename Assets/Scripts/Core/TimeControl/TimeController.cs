using System.Collections.Generic;
using System.IO;
using AD;
using AD.BASE;
using AD.Math;
using AD.UI;
using AD.Utility.Object;
using RhythmGame.Visual;
using RhythmGame.Visual.Note;
using UnityEngine;

namespace RhythmGame.Time
{
    public class TimeController : ADController, IController
    {

        public List<IListenTime> Listeners;

        public MonoBehaviour MonoTarget => this;

        public AudioSourceController MainAudioSource;
        [SerializeField] private MaterialGroup MainMaterialGroup;
        [SerializeField] private ModernUIFillBar TimeFillBar;

        public override void Init()
        {
            if (!new ArithmeticVariable("__refreshRate")) ArithmeticExtension.AddVariable("__refreshRate", new(Screen.currentResolution.refreshRate));
            Application.targetFrameRate = 60;
            if (!new ArithmeticVariable("__targetFrameRate")) ArithmeticExtension.AddVariable("__targetFrameRate", new(Application.targetFrameRate));
            //QualitySettings.vSyncCount = 0;

            Listeners = new();
            //MainAudioSource.Stop();
            //MainAudioSource.CurrentTime = -3;
            //MainAudioSource.Play();
            //App.instance.StartTime = 0;
            //App.instance.EndTime = MainAudioSource.CurrentClip.length;

            Architecture.RegisterModel<TouchLock>();
        }

        private void Start()
        {
            if (!new ArithmeticVariable("delay")) ArithmeticExtension.AddVariable("delay", new(0));
            App.instance.Init();
            App.instance.RegisterController(this);
        }

        private void Update()
        {
            App.CurrentTime = MainAudioSource.CurrentTime;
            if (MainAudioSource.IsPlay)
            {
                TimeFillBar.SetPerecent(MainAudioSource.CurrentTime / MainAudioSource.CurrentClip.length, 0, MainAudioSource.CurrentClip.length);
                foreach (var listener in Listeners)
                {
                    listener.When(MainAudioSource.CurrentTime, MainAudioSource.CurrentClip.length);
                }
            }

            //Update Note Material
            MainMaterialGroup.UpdateTarget("_NearPanel", App.instance.CameraSafeAreaPanel);
            ArithmeticVariable.VariableConstantPairs["__refreshRate"].Value.SetValue(Screen.currentResolution.refreshRate == 0 ? -1 : Screen.currentResolution.refreshRate);
            ArithmeticVariable.VariableConstantPairs["__targetFrameRate"].Value.SetValue(Application.targetFrameRate);
        }

        public void AddListener(IListenTime listener)
        {
            Listeners.Add(listener);
        }

        public void RemoveListener(IListenTime listener)
        {
            Listeners.Remove(listener);
        }

        public void PlaySong()
        {
            MainAudioSource.Play();
        }

        public void PauseSong()
        {
            MainAudioSource.Pause();
        }

        public void StopSong()
        {
            MainAudioSource.Stop();
            foreach (var listener in Listeners)
            {
                if (listener.As<NoteBase>(out var note))
                {
                    note.IsBeenTouchJudge = false;
                }
            }
            Architecture.GetController<RhythmGame.Visual.ScoreBoard>().Init();
        }

        public void ResetSongSetting()
        {
            App.instance.StartTime = 0;
            ArithmeticVariable delay = new("delay");
            Length = App.instance.EndTime = MainAudioSource.CurrentClip.length + (delay ? delay.ReadValue() : 0);
        }

        public float Length;

        public void Replay()
        {
            StopSong();
            MainAudioSource.CurrentTime = -3;
            PlaySong();
            ResetSongSetting();
            foreach (var item in Listeners)
            {
                if (item.As<JudgeEffect>(out var effect))
                {
                    effect.gameObject.SetActive(false);
                }
                else if (item.As<IRebuildHandler>(out var handler))
                {
                    handler.RebuildImmediately();
                }
            }
        }
    }
}
