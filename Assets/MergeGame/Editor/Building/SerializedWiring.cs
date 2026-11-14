using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Assigns private <c>[SerializeField]</c> fields by name, exactly as the inspector would, so components keep
    /// their fields private. A misspelt or renamed field fails the build instead of leaving a silent null.
    /// </summary>
    internal static class SerializedWiring
    {
        public static T Wire<T>(this T target, string field, Object value) where T : Object =>
            Modify(target, field, property =>
            {
                // A destroyed or unloaded object compares equal to null and would be stored as a missing reference.
                if (value is object && value == null)
                    throw new InvalidOperationException($"{target.GetType().Name}.{field}: the object to assign was destroyed or unloaded.");
                property.objectReferenceValue = value;
                if (property.objectReferenceValue != value)
                    throw new InvalidOperationException($"{target.GetType().Name}.{field} does not accept {value}.");
            });

        public static T Wire<T>(this T target, string field, params Object[] values) where T : Object =>
            Modify(target, field, property =>
            {
                property.arraySize = values.Length;
                for (var i = 0; i < values.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        public static T Wire<T>(this T target, string field, string value) where T : Object =>
            Modify(target, field, property => property.stringValue = value);

        public static T Wire<T>(this T target, string field, float value) where T : Object =>
            Modify(target, field, property => property.floatValue = value);

        public static T Wire<T>(this T target, string field, int value) where T : Object =>
            Modify(target, field, property => property.intValue = value);

        public static T Wire<T>(this T target, string field, UnityEngine.Color value) where T : Object =>
            Modify(target, field, property => property.colorValue = value);

        private static T Modify<T>(T target, string field, Action<SerializedProperty> assign) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field)
                           ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            assign(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return target;
        }
    }
}
