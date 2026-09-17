using DeepSky.Rendering.Atmosphere;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Exposes authored and running time-of-day controls.</summary>
    [CustomEditor(typeof(DayNightCycle))]
    public sealed class DayNightCycleEditor : UnityEditor.Editor
    {
        /// <summary>Draws the time slider and cycle settings without serializing the running clock.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var cycle = (DayNightCycle)target;
            if (Application.isPlaying)
            {
                EditorGUI.BeginChangeCheck();
                float hour = EditorGUILayout.Slider("Time of day", cycle.TimeOfDay, 0f, 24f);
                if (EditorGUI.EndChangeCheck())
                {
                    cycle.SetTimeOfDay(hour);
                    SceneView.RepaintAll();
                }

                EditorGUILayout.HelpBox("Time changes last for this Play session. Turn off Advance Time to hold the selected hour.", MessageType.Info);
                Repaint();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("startHour"), new GUIContent("Time of day"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("advanceTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dayLengthMinutes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("daylightByHour"));
            if (serializedObject.ApplyModifiedProperties())
            {
                SceneView.RepaintAll();
            }
        }
    }
}
