using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AD.BASE;

[CreateAssetMenu(fileName = "Script Copy Helper", menuName = "AD/ScriptCopyHelper", order = -30104)]
public class ScriptCopyHelper : ScriptableObject
{
    public string replaceStrTarget;
    public string replaceString;

    public string targetPath;

    public string Text;
}

[CustomEditor(typeof(ScriptCopyHelper))]
public class ScriptCopyHelperEditor : AbstractCustomADEditor
{
    ScriptCopyHelper that;

    SerializedProperty replaceStrTarget;
    SerializedProperty replaceString;

    SerializedProperty targetPath;

    protected override void OnEnable()
    {
        base.OnEnable();
        that = target as ScriptCopyHelper;
        replaceStrTarget = serializedObject.FindProperty(nameof(replaceStrTarget));
        replaceString = serializedObject.FindProperty(nameof(replaceString));
        targetPath = serializedObject.FindProperty(nameof(targetPath));
    }

    public override void OnContentGUI()
    {
        this.HorizontalBlockWithBox(() =>
        {
            EditorGUILayout.PropertyField(targetPath);
            if (GUILayout.Button("Find"))
            {
                FileC.SelectFileOnSystem(T => that.targetPath = T, ".cs", "脚本", "cs");
            }
        });
        this.VerticalBlockWithBox(() =>
        {
            EditorGUILayout.PropertyField(replaceStrTarget);
            EditorGUILayout.PropertyField(replaceString);
        });
        this.VerticalBlockWithBox(() =>
        {
            this.HorizontalBlock(() =>
            {
                if (GUILayout.Button("ReRead"))
                {
                    that.Text = new ADFile(that.targetPath, false, false, false).GetString(true, System.Text.Encoding.UTF8);
                }
                if (GUILayout.Button("Replace"))
                {
                    that.Text = that.Text.Replace(that.replaceStrTarget, that.replaceString, System.StringComparison.Ordinal);
                }
            });
            that.Text = EditorGUILayout.TextField(that.Text, GUILayout.Height(650));
        });
    }

    public override void OnResourcesGUI()
    {

    }

    public override void OnSettingsGUI()
    {

    }
}