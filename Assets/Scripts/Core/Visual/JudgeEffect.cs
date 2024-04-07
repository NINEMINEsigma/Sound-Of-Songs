using AD;
using AD.BASE;
using AD.Utility;
using RhythmGame.Time;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame.Visual.Note
{

    public class JudgeEffectStack : ADModel
    {
        public Queue<JudgeEffect> Objects = new();

        public override void Init()
        {
            foreach (var item in Objects)
            {
                GameObject.Destroy(item.gameObject);
            }
            Objects.Clear();
        }

        public bool TryObtain(out JudgeEffect result)
        {
            result = null;
            if (Objects.Count == 0) return false;
            result = Objects.Dequeue();
            return true;
        }
    }

    public class JudgeEffect : MonoBehaviour, RhythmGame.IController, IListenTime
    {


        public MonoBehaviour MonoTarget => this;

        public float TimeCounter = 0;
        public Material m_Material;
        public SpriteRenderer m_SpriteRenderer;

        public void When(float time, float duration)
        {
            if (TimeCounter == 0) TimeCounter = time;
            m_Material.SetFloat("_CurrentFramesCount", Mathf.Clamp((time - TimeCounter) * 50, 0, 16));
            this.transform.position = this.transform.position.SetZ(App.instance.CameraSafeAreaPanel);
            if (time - TimeCounter > 0.32f)
            {
                this.gameObject.SetActive(false);
            }
        }

        public bool IsInit = false;

        private void LateUpdate()
        {
            if (!IsInit)
            {
                App.instance.GetController<TimeController>().AddListener(this);
                m_Material = new Material(m_Material);
                m_SpriteRenderer.sharedMaterial = m_Material;
                IsInit = true;
            }
        }

        private void OnEnable()
        {
            IsInit = false;
            TimeCounter = 0;
        }

        private void OnDisable()
        {
            if (ADGlobalSystem.instance)
            {
                App.instance.GetModel<JudgeEffectStack>().Objects.Enqueue(this);
            }
        }

        public JudgeEffect PrefabInstantiate()
        {
            if (App.instance.GetModel<JudgeEffectStack>().TryObtain(out var result))
            {
                result.gameObject.SetActive(true);
                return result;
            }
            else
                return GameObject.Instantiate(this.gameObject).GetComponent<JudgeEffect>();
        }
    }
}
