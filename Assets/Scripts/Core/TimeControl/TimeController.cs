using System.Collections.Generic;
using AD.BASE;
using AD.UI;
using AD.Utility.Object;
using UnityEngine;

namespace RhythmGame.Time
{
    public class TimeController : ADController, IController
    {
        public List<IListenTime> Listeners;

        public MonoBehaviour MonoTarget => this;

        public AudioSourceController MainAudioSource;
        [SerializeField] private MaterialGroup MainMaterialGroup;

        public override void Init()
        {
            Listeners = new();
            MainAudioSource.Stop();
            MainAudioSource.CurrentTime = -3;
            MainAudioSource.Play();
            App.instance.StartTime = 0;
            App.instance.EndTime = MainAudioSource.CurrentClip.length;
        }

        private void Start()
        {
            App.instance.Init();
            App.instance.RegisterController(this);
        }

        private void Update()
        {
            if (MainAudioSource.IsPlay)
                foreach (var listener in Listeners)
                {
                    listener.When(MainAudioSource.CurrentTime, MainAudioSource.CurrentClip.length);
                }
            else
                foreach (var listener in Listeners)
                {
                    listener.When(0, 100);
                }

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
    }
}
