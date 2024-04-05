using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AD.BASE;
using System;
using System.Linq;

[Serializable]
public class InternalString2Pair
{
    public string key; public string value;
}


[CreateAssetMenu(fileName = "Script Copy Helper", menuName = "AD/ScriptCopyHelper", order = -30104)]
public class ScriptCopyHelper : ScriptableObject
{
    public List<InternalString2Pair> ReplacePairs = new();

    public string targetPath;
    public string savePath;

    public string Text;

    public bool isNeedClearUsingAndNamespaceStruct = true;
}

[CustomEditor(typeof(ScriptCopyHelper))]
public class ScriptCopyHelperEditor : AbstractCustomADEditor
{
    ScriptCopyHelper that;

    SerializedProperty ReplacePairs;

    SerializedProperty targetPath;
    SerializedProperty savePath;

    SerializedProperty isNeedClearUsingAndNamespaceStruct;

    protected override void OnEnable()
    {
        base.OnEnable();
        that = target as ScriptCopyHelper;
        ReplacePairs = serializedObject.FindProperty(nameof(ReplacePairs));
        targetPath = serializedObject.FindProperty(nameof(targetPath));
        savePath = serializedObject.FindProperty(nameof(savePath));
        isNeedClearUsingAndNamespaceStruct = serializedObject.FindProperty(nameof(isNeedClearUsingAndNamespaceStruct));
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
        this.HorizontalBlockWithBox(() =>
        {
            EditorGUILayout.PropertyField(savePath);
            if (GUILayout.Button("Find"))
            {
                FileC.SelectFileOnSystem(T => that.savePath = T, ".cs", "脚本", "cs");
            }
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
                    foreach (var pair in that.ReplacePairs)
                    {

                        that.Text = that.Text.Replace(pair.key, pair.value, System.StringComparison.Ordinal);
                    }
                }
            });
            this.HorizontalBlock(() =>
            {
                if (GUILayout.Button("Trim"))
                {
                    that.Text = that.Text.Trim();
                    if (that.isNeedClearUsingAndNamespaceStruct)
                    {
                        var lines = that.Text.Split('\n');
                        that.Text = "";
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("using")) continue;
                            if (line.StartsWith("namespace{"))
                            {
                                that.Text += "{\n";
                                continue;
                            }
                            if (line.StartsWith("namespace")) continue;
                            that.Text += line.Trim() + "\n";
                        }
                        that.Text = that.Text[(that.Text.IndexOf('{') + 1)..that.Text.LastIndexOf('}')];
                    }
                    else
                    {

                    }
                }
                if (GUILayout.Button("Save"))
                {
                    ADFile file = new ADFile(that.savePath == null ? that.targetPath : that.savePath, true, false, false);
                    file.ReplaceAllData(System.Text.Encoding.UTF8.GetBytes(that.Text));
                    file.SaveFileData();
                }
            });
            that.Text = EditorGUILayout.TextField(that.Text, GUILayout.Height(that.Text.Count(T => T == '\n') * 15.05f));
        });
    }

    public override void OnResourcesGUI()
    {

    }

    public override void OnSettingsGUI()
    {
        EditorGUILayout.PropertyField(isNeedClearUsingAndNamespaceStruct);
        EditorGUILayout.PropertyField(ReplacePairs);
    }
}
