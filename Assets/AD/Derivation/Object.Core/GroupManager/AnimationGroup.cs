using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AD.Utility.Object
{
    public class AnimationGroup : MonoBehaviour
    {
        public ADSerializableDictionary<Animator, string> boolList = new();

        public void Switch(Animator animator)
        {
            if (!boolList.ContainsKey(animator)) return;
            animator.SetBool(boolList[animator], !animator.GetBool(boolList[animator]));
        }
    }
}
