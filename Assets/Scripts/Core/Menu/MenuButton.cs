using AD.BASE;
using AD.UI;
using RhythmGame.Time;
using RhythmGame.Visual;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    public class MenuButton : ADController, IController
    {
        public MonoBehaviour MonoTarget => this;

        public MenuPanel MyPanel;

        public Button ThisButton;

        public void BackToGame()
        {
            MyPanel.gameObject.SetActive(false);
            Architecture.GetController<TimeController>().PlaySong();
            ThisButton.animator.Play("Out");
        }

        public void Replay()
        {
            MyPanel.gameObject.SetActive(false);
            Architecture.GetController<TimeController>().Replay();
            ThisButton.animator.Play("Out");
        }

        public override void Init()
        {
            MyPanel.BackToGame.RemoveAllListeners();
            MyPanel.BackToGame.AddListener(BackToGame);
            MyPanel.Replay.RemoveAllListeners();
            MyPanel.Replay.AddListener(Replay);
            //
        }

        public void MakeStop()
        {
            MyPanel.gameObject.SetActive(true);
            Architecture.GetController<TimeController>().PauseSong();
        }

        private void Start()
        {
            App.instance.RegisterController(this);
        }
    }
}
