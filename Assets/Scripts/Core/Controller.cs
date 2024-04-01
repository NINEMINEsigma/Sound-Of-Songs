using System.Collections;
using System.Collections.Generic;
using AD.BASE;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 通过使用这个特性来指示这个Property是需要保存的数据，并且在初始化时需要使用
    /// <para>之所以这样指定为Property是为了能够更好的规范使用<see cref="IRebuildHandler.IsDirty"/></para>
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class RhythmDataAttribute : System.Attribute
    {

    }

    public static class RhythmCore
    {

    }

    public interface IListenTime
    {
        void When(float time, float duration);
    }

    public interface IController
    {
        MonoBehaviour MonoTarget { get; }
    }

    public interface IRebuildHandler
    {
        bool IsDirty { get; }
        void Rebuild();
        void RebuildImmediately();
        void SetDirty();
    }

    public interface IListenTouch:IADEventSystemHandler
    {
        void OnCatching(Touch touch);
    }

    public class ControllerException:ADException
    {
        public ControllerException(IController controller,string message):base(message)
        {
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = controller.MonoTarget.gameObject;
#endif
        }
    }
}
