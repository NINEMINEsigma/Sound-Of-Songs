using AD.BASE;
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
            this.transform.localPosition = new Vector3(0, 0, -CameraCore.CameraOffsetZ);
        }

        public Touch Current;
        public bool IsCatching = false;
        public float SafeTimeCounter = 0;

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

            if (IsCatching)
            {
                if (Current.phase == TouchPhase.Moved)
                {
                    if (SafeTimeCounter < 0.05f)
                    {
                        SafeTimeCounter += UnityEngine.Time.deltaTime;
                    }
                    else
                    {
                        Architecture.GetController<CameraCore>().transform.Translate(-1 * TouchMoveSpeed * Current.deltaPosition, Space.World);
                    }
                }
            }
            else
            {
                SafeTimeCounter = 0;
            }
        }
    }
}
