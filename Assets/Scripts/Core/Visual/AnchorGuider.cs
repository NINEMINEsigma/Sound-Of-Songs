using System;
using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using AD.Utility;
using UnityEngine;

namespace RhythmGame.Visual
{
    public class AnchorGuider : ADController, IController
    {
        public MonoBehaviour MonoTarget => this;
        public Camera MainCamera;
        public float TouchMoveSpeed = 0.01f;

        private void Start()
        {
            App.instance.RegisterController(this);
        }

        public override void Init()
        {

        }

        public Touch Current;
        public bool IsCatching = false;

        private void LateUpdate()
        {
            for (int i = 0, e = Input.touchCount; i < e; i++)
            {
                var current = Input.GetTouch(i);
                //if (current.phase != TouchPhase.Ended && current.phase != TouchPhase.Canceled)
                if (current.phase == TouchPhase.Began)
                {
                    Ray ray = MainCamera.ScreenPointToRay(current.position);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (hit.collider.gameObject == this.gameObject)
                        {
                            Current = current;
                            IsCatching = true;
                            break;
                        }
                    }
                }
                if (IsCatching && current.fingerId == current.fingerId)
                {
                    Current = current;
                    IsCatching = Current.phase != TouchPhase.Ended && Current.phase != TouchPhase.Canceled;
                }
            }


            if (IsCatching && Current.phase == TouchPhase.Moved)
            {
                Architecture.GetController<CameraCore>().transform.Translate(Current.deltaPosition * TouchMoveSpeed, Space.World);
            }
        }
    }
}
