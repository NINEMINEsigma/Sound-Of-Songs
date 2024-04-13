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
            Application.targetFrameRate = 90;

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
                //TimeFillBar.SetPerecent(MainAudioSource.CurrentTime / MainAudioSource.CurrentClip.length, 0, MainAudioSource.CurrentClip.length);
                foreach (var listener in Listeners)
                {
                    listener.When(MainAudioSource.CurrentTime, MainAudioSource.CurrentClip.length);
                }
            }
            //else if (App.instance.MaxDepth != 0)
            //{
            //    TimeFillBar.SetPerecent(0, 0, MainAudioSource.CurrentClip.length);
            //    foreach (var listener in Listeners)
            //    {
            //        listener.When(0, MainAudioSource.CurrentClip.length);
            //    }
            //}

            //Update Note Material
            MainMaterialGroup.UpdateTarget("_NearPanel", App.instance.CameraSafeAreaPanel);
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
