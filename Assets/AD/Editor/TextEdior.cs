using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AD.UI.Text)), CanEditMultipleObjects]
public class TextEdior : ADUIEditor
{
    private AD.UI.Text that = null;

    SerializedProperty m_VariableInitName;

    protected override void OnEnable()
    {
        base.OnEnable();
        that = target as AD.UI.Text;
        m_VariableInitName = serializedObject.FindProperty(nameof(m_VariableInitName));
    }

    public override void OnContentGUI()
    {
        EditorGUI.BeginChangeCheck();
        string str = EditorGUILayout.TextField(new GUIContent("Text"), that.text);
        if (EditorGUI.EndChangeCheck()) that.text = str;
    }

    public override void OnResourcesGUI()
    {
    }

    public override void OnSettingsGUI()
    {
        EditorGUILayout.PropertyField(m_VariableInitName);
    }
}
