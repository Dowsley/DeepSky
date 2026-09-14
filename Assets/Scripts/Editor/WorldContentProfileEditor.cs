using System;
using DeepSky.World.Population;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor
{
    /// <summary>Edits typed population rules.</summary>
    [CustomEditor(typeof(WorldContentProfile))]
    public sealed class WorldContentProfileEditor : UnityEditor.Editor
    {
        /// <summary>Draws placement settings and controls for adding, ordering and removing typed rules.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mineralSeparation"));
            SerializedProperty rules = serializedObject.FindProperty("populations");
            for (int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string label = rule.FindPropertyRelative("label")?.stringValue ?? "Unconfigured rule";
                    EditorGUILayout.PropertyField(rule, new GUIContent(label), true);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Up") && i > 0)
                        {
                            rules.MoveArrayElement(i, i - 1);
                        }
                        if (GUILayout.Button("Down") && i + 1 < rules.arraySize)
                        {
                            rules.MoveArrayElement(i, i + 1);
                        }
                        if (GUILayout.Button("Remove"))
                        {
                            rules.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }
            if (GUILayout.Button("Add population"))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Mineral"), false, () => AddRule(new MineralPopulationRule()));
                menu.AddItem(new GUIContent("Batched plant"), false, () => AddRule(new PlantPopulationRule()));
                menu.AddItem(new GUIContent("Decoration"), false, () => AddRule(new DecorationPopulationRule()));
                menu.ShowAsContext();
            }
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Validate content"))
            {
                try
                {
                    ((WorldContentProfile)target).ValidateContent();
                    Debug.Log("Population content is valid.", target);
                }
                catch (InvalidOperationException error)
                {
                    Debug.LogError(error.Message, target);
                }
            }
        }

        /// <summary>Adds a rule with an unused stream ID through Unity serialization and Undo.</summary>
        /// <param name="rule">New rule owned by this profile.</param>
        private void AddRule(PopulationRule rule)
        {
            serializedObject.Update();
            SerializedProperty rules = serializedObject.FindProperty("populations");
            var used = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < rules.arraySize; i++)
            {
                SerializedProperty stream = rules.GetArrayElementAtIndex(i).FindPropertyRelative("seedStream");
                if (stream != null)
                {
                    used.Add(stream.intValue);
                }
            }
            int next = 1;
            while (used.Contains(next))
            {
                next++;
            }
            int index = rules.arraySize++;
            SerializedProperty entry = rules.GetArrayElementAtIndex(index);
            entry.managedReferenceValue = rule;
            entry.FindPropertyRelative("seedStream").intValue = next;
            entry.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
