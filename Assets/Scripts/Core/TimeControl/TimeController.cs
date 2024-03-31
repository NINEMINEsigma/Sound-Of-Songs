using System.Collections.Generic;
using AD.BASE;
using AD.UI;
using UnityEngine;

namespace RhythmGame.Time
{
    public class TimeController : ADController,IController
    {
        public List<IListenTime> Listeners;

        public MonoBehaviour MonoTarget => this;

        public AudioSourceController MainAudioSource;

        public override void Init()
        {
            Listeners = new();
            MainAudioSource.Stop();
            MainAudioSource.CurrentTime = -3;
            MainAudioSource.Play();
        }

        private void Start()
        {
            App.instance.RegisterController(this);
        }

        private void Update()
        {
            if (MainAudioSource.IsPlay && MainAudioSource.CurrentTime > 0)
                foreach (var listener in Listeners)
                {
                    listener.When(MainAudioSource.CurrentTime, MainAudioSource.CurrentClip.length);
                }
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
