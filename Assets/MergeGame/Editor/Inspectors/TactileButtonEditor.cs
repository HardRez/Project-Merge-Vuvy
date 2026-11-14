using MergeGame.View.Common;
using UnityEditor;
using UnityEditor.UI;

namespace MergeGame.Editor.Inspectors
{
    /// <summary>The stock button inspector hides fields of subclasses; this adds the tactile settings below it.</summary>
    [CustomEditor(typeof(TactileButton))]
    [CanEditMultipleObjects]
    internal sealed class TactileButtonEditor : ButtonEditor
    {
        private static readonly string[] TactileFields =
        {
            "background", "enabledColor", "disabledColor", "content", "disabledContentAlpha", "pressedScale", "pressSeconds", "releaseSeconds"
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tactile", EditorStyles.boldLabel);
            foreach (var field in TactileFields)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
