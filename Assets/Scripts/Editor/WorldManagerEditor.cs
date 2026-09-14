using DeepSky.World;
using DeepSky.World.Generation;
using DeepSky.World.Mapping;
using UnityEditor;
using UnityEngine;

namespace DeepSky.Editor
{
    /// <summary>Whole-world depth overview and transient local chunk preview using the runtime generator.</summary>
    [CustomEditor(typeof(WorldManager))]
    public sealed class WorldManagerEditor : UnityEditor.Editor
    {
        private const int MapResolution = 256;
        private Texture2D? depthMap;
        private Vector2 selectedPosition = Vector2.zero;
        private bool selected = false;
        private bool showSettings = true;
        private UnityEditor.Editor? settingsEditor;
        private string settingsSignature = string.Empty;
        private WorldData? mapWorld;

        /// <summary>Releases the inspector-owned depth texture and cached settings editor.</summary>
        private void OnDisable()
        {
            if (depthMap)
            {
                DestroyImmediate(depthMap);
            }

            if (settingsEditor)
            {
                DestroyImmediate(settingsEditor);
            }
        }

        /// <summary>Draws world settings, the selectable depth chart and mode-appropriate preview or travel actions.</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (WorldManager)target;
            if (!manager.Settings)
            {
                return;
            }

            showSettings = EditorGUILayout.InspectorTitlebar(showSettings, manager.Settings);
            if (showSettings)
            {
                CreateCachedEditor(manager.Settings, null, ref settingsEditor);
                settingsEditor!.OnInspectorGUI();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This chart covers the whole world. Generate opens the Scene view and creates a local 3D patch at the selected point, not every chunk. Previews are temporary and clear on entering Play or recompiling.", MessageType.Info);
            string signature = EditorJsonUtility.ToJson(manager.Settings);
            if (mapWorld == null || signature != settingsSignature)
            {
                try
                {
                    mapWorld = manager.Settings.CreateWorld();
                }
                catch (System.InvalidOperationException error)
                {
                    EditorGUILayout.HelpBox(error.Message, MessageType.Error);
                    return;
                }
            }
            WorldData world = mapWorld;
            if (!selected)
            {
                selectedPosition = world.Spawn;
                selected = true;
            }

            if (GUILayout.Button("Refresh depth map") || !depthMap || signature != settingsSignature)
            {
                DrawMap(world);
                settingsSignature = signature;
            }

            Rect rect = GUILayoutUtility.GetAspectRect(1f);
            GUI.DrawTexture(rect, depthMap!, ScaleMode.ScaleToFit);
            Vector2 marker = new Vector2(rect.x + (selectedPosition.x / world.Size + 0.5f) * rect.width,
                rect.y + (0.5f - selectedPosition.y / world.Size) * rect.height);
            EditorGUI.DrawRect(new Rect(marker.x - 3f, marker.y - 3f, 6f, 6f), Color.white);
            Vector2 spawnUV = WorldMapTexture.ToUV(world, world.Spawn);
            EditorGUI.DrawRect(new Rect(rect.x + spawnUV.x * rect.width - 2f,
                rect.y + (1f - spawnUV.y) * rect.height - 2f, 4f, 4f), Color.yellow);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Vector2 mouse = Event.current.mousePosition;
                selectedPosition = new Vector2((mouse.x - rect.x) / rect.width - 0.5f,
                    0.5f - (mouse.y - rect.y) / rect.height) * world.Size;
                Event.current.Use();
                Repaint();
            }

            float height = world.Height(selectedPosition.x, selectedPosition.y);
            EditorGUILayout.LabelField($"{world.Size:0} m square | {world.ChunksPerSide} x {world.ChunksPerSide} chunks");
            EditorGUILayout.LabelField($"Selected XZ {selectedPosition:F1} | depth {-height:0.0} m | stage {world.Stage(height) + 1}");
            EditorGUILayout.LabelField($"Loaded: {manager.LoadedChunkCount} | {(manager.IsGenerating ? "Generating" : "Idle")}");
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Generate 3D preview at selection"))
                {
                    WorldPreviewSession.Show(manager, selectedPosition);
                    Frame(selectedPosition, height);
                }

                if (GUILayout.Button("Preview here (Scene view)"))
                {
                    SceneView scene = SceneView.lastActiveSceneView;
                    if (scene)
                    {
                        selectedPosition = new Vector2(scene.pivot.x, scene.pivot.z);
                        WorldPreviewSession.Show(manager, selectedPosition);
                    }
                }

                if (GUILayout.Button("Clear preview"))
                {
                    WorldPreviewSession.Clear();
                }
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Travel to selection"))
                {
                    manager.TravelTo(selectedPosition);
                }

                if (GUILayout.Button("Regenerate world"))
                {
                    manager.Regenerate();
                }
            }
        }

        /// <summary>Replaces the inspector-owned chart texture with a map of the supplied snapshot.</summary>
        /// <param name="world">Immutable generation snapshot to visualize.</param>
        private void DrawMap(WorldData world)
        {
            if (depthMap)
            {
                DestroyImmediate(depthMap);
            }

            depthMap = WorldMapTexture.Create(world, MapResolution,
                new Color(0.2f, 0.8f, 0.72f), new Color(0.035f, 0.08f, 0.25f), 25f);
        }

        /// <summary>Focuses the Scene view on the selected terrain location.</summary>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <param name="height">Terrain world Y in metres at that position.</param>
        private static void Frame(Vector2 position, float height)
        {
            SceneView scene = EditorWindow.GetWindow<SceneView>();
            scene.Focus();
            scene.LookAt(new Vector3(position.x, height, position.y), Quaternion.Euler(35f, 35f, 0f), 55f);
            scene.Repaint();
        }
    }

    [InitializeOnLoad]
    internal static class WorldPreviewSession
    {
        private static WorldManager? _active;

        /// <summary>Registers preview ticking and cleanup before reload, shutdown or entering Play.</summary>
        static WorldPreviewSession()
        {
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    Clear();
                }
            };
        }

        /// <summary>Clears the current preview and begins a local preview owned by the supplied manager.</summary>
        /// <param name="manager">Scene manager whose settings, content profiles and prefabs generate the preview.</param>
        /// <param name="position">Preview center in world XZ metres.</param>
        internal static void Show(WorldManager manager, Vector2 position)
        {
            Clear();
            _active = manager;
            manager.BeginPreview(position);
        }

        /// <summary>Stops the active preview and releases its generated objects, if its manager still exists.</summary>
        internal static void Clear()
        {
            if (_active)
            {
                _active!.ClearGenerated();
            }

            _active = null;
        }

        /// <summary>Advances the active edit-mode preview and schedules repaints while generation is pending.</summary>
        private static void Tick()
        {
            if (!Application.isPlaying && _active)
            {
                _active!.TickPreview();
                SceneView.RepaintAll();
                if (_active.IsGenerating)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }
    }
}
