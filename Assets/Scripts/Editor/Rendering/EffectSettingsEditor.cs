using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor.Rendering
{
    /// <summary>Edits an effect and its shared settings asset together.</summary>
    public abstract class EffectSettingsEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor? settingsEditor;
        private bool settingsExpanded = true;

        protected abstract string TuningHint { get; }

        /// <summary>Draws component settings and the referenced asset's native Inspector with Undo support.</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(TuningHint, MessageType.Info);
            Object? asset = GetSettingsAsset();
            if (!asset)
            {
                ReleaseEditor();
                return;
            }

            settingsExpanded = EditorGUILayout.Foldout(settingsExpanded, "Effect tuning", true);
            if (!settingsExpanded)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("These are shared asset settings. Changes affect every user of this asset, "
                    + "persist after Play mode, and support Undo.", MessageType.Info);
                CreateCachedEditor(asset, null, ref settingsEditor);
                if (settingsEditor)
                {
                    EditorGUI.BeginChangeCheck();
                    if (settingsEditor is MaterialEditor materialEditor)
                    {
                        if (materialEditor.PropertiesGUI())
                        {
                            materialEditor.PropertiesChanged();
                        }
                    }
                    else
                    {
                        settingsEditor.OnInspectorGUI();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        SceneView.RepaintAll();
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
            }
        }

        /// <summary>Releases the nested Inspector when this component Inspector closes.</summary>
        protected virtual void OnDisable()
        {
            ReleaseEditor();
        }

        /// <summary>Resolves the currently assigned settings without copying or modifying them.</summary>
        /// <returns>The shared settings asset, or null when its reference is unassigned.</returns>
        protected abstract Object? GetSettingsAsset();

        /// <summary>Destroys only the owned editor instance, leaving its inspected asset intact.</summary>
        private void ReleaseEditor()
        {
            if (settingsEditor)
            {
                DestroyImmediate(settingsEditor);
            }
            settingsEditor = null;
        }
    }
}
