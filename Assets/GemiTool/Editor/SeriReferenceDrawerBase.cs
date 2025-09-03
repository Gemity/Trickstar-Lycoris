using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class SeriReferenceDrawerBase<T> : PropertyDrawer
{
    private List<Type> _childClasses = new();
    private const string NotSet = "Not set";

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (_childClasses.Count == 0)
        {
            _childClasses = Assembly.GetAssembly(typeof(T))
                                .GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && typeof(T).IsAssignableFrom(t))
                                .ToList();
        }

        if (_childClasses.Count == 0)
            return;

        Type type = property.managedReferenceValue?.GetType();
        string typeName;
        if (type == null)
            typeName = NotSet;
        else
            typeName = type.Name;

        Rect dropdownRect = position;
        dropdownRect.x += EditorGUIUtility.labelWidth + 2;
        dropdownRect.width -= EditorGUIUtility.labelWidth + 2;
        dropdownRect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.BeginProperty(position, label, property);

        if (EditorGUI.DropdownButton(dropdownRect, new(typeName), FocusType.Keyboard))
        {
            GenericMenu _menu = new();

            _menu.AddItem(new GUIContent(NotSet), property.managedReferenceValue == null, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            foreach (var child in _childClasses)
            {
                _menu.AddItem(new GUIContent(child.Name), typeName == child.Name, () =>
                {
                    object obj = Activator.CreateInstance(child);
                    property.managedReferenceValue = obj;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            _menu.ShowAsContext();
        }

        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }
}