using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmalgamGames.Editor
{
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Checks to make sure field value is an object reference 
            if(property.propertyType == SerializedPropertyType.ObjectReference)
            {
                RequireInterfaceAttribute requireAttribute = this.attribute as RequireInterfaceAttribute;

                EditorGUI.BeginProperty(position, label, property);

                property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, requireAttribute.requiredType, true);

                EditorGUI.EndProperty();
            }
            else
            {
                // Show error if not an object reference

                Color previousColour = GUI.color;
                GUI.color = Color.red;
                EditorGUI.LabelField(position, label, new GUIContent("Property is not a reference type"));

                GUI.color = previousColour;
            }
        }
    }
}