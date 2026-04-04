using System;
using UnityEditor;
using UnityEngine;
using SP.SceneRefs;

namespace PlayableFramework.Editor
{
    /// <summary>
    /// 节点编辑器窗口入口。
    /// </summary>
    public sealed class GraphEditorWindow : EditorWindow
    {
        private const float ToolbarHeight = 22f;

        private GraphRenderer renderer;
        private EditorInputHandler inputHandler;

        private GraphRenderer Renderer
        {
            get
            {
                if (renderer == null)
                {
                    renderer = new GraphRenderer();
                }

                return renderer;
            }
        }

        private EditorInputHandler InputHandler
        {
            get
            {
                if (inputHandler == null)
                {
                    inputHandler = new EditorInputHandler(ShowNotification);
                }

                return inputHandler;
            }
        }

        [MenuItem("Tools/PlayableFramework/Node Editor")]
        private static void OpenWindow()
        {
            GraphEditorWindow window = GetWindow<GraphEditorWindow>("Playable Node Editor");
            window.minSize = new Vector2(920f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            GraphManager.Instance.LoadGraph();
            Selection.selectionChanged += OnUnitySelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnUnitySelectionChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnGUI()
        {
            TrySyncFromScene();
            DrawToolbar();

            Rect canvasRect = new Rect(0, ToolbarHeight, position.width, position.height - ToolbarHeight);

            InputHandler.ProcessEvents(Event.current);
            Renderer.Render(canvasRect, InputHandler);
            HandleCanvasDrop(canvasRect);

            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void OnUnitySelectionChanged()
        {
            string selectedNodeId = null;
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                SceneRefObject sceneRefObject = selectedObject.GetComponent<SceneRefObject>();
                if (sceneRefObject != null && !string.IsNullOrEmpty(sceneRefObject.Id))
                {
                    selectedNodeId = sceneRefObject.Id;
                }
            }

            if (string.IsNullOrEmpty(selectedNodeId))
            {
                GraphManager.Instance.ClearSelectedNode();
            }
            else
            {
                GraphManager.Instance.SelectNodeById(selectedNodeId);
            }

            Repaint();
        }

        private void OnHierarchyChanged()
        {
            TrySyncFromScene();
        }

        private void OnUndoRedoPerformed()
        {
            TrySyncFromScene();
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            TrySyncFromScene();
            Repaint();
        }

        private void TrySyncFromScene()
        {
            bool graphChanged = GraphManager.Instance.SyncGraphFromSceneHierarchy();
            bool titleChanged = GraphManager.Instance.SyncNodeTitlesFromScene();
            if (!graphChanged && !titleChanged)
            {
                return;
            }

            if (GraphManager.Instance.CurrentAsset != null)
            {
                EditorUtility.SetDirty(GraphManager.Instance.CurrentAsset);
            }

            Repaint();
        }

        private void HandleCanvasDrop(Rect canvasRect)
        {
            Event e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
            {
                return;
            }

            if (!canvasRect.Contains(e.mousePosition))
            {
                return;
            }

            GraphLayoutAsset dropped = null;
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                dropped = obj as GraphLayoutAsset;
                if (dropped != null)
                {
                    break;
                }
            }

            if (dropped == null)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                GraphManager.Instance.LoadFromAsset(dropped);
                inputHandler = null;
                Repaint();
            }

            e.Use();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width, ToolbarHeight), EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            GUILayout.Label("Graph", EditorStyles.toolbarButton, GUILayout.Width(50f));

            EditorGUI.BeginChangeCheck();
            GraphLayoutAsset asset = (GraphLayoutAsset)EditorGUILayout.ObjectField(
                GraphManager.Instance.CurrentAsset,
                typeof(GraphLayoutAsset),
                false,
                GUILayout.Width(200f));

            if (EditorGUI.EndChangeCheck())
            {
                GraphManager.Instance.LoadFromAsset(asset);
                inputHandler = null; // 重置 canvas offset
                Repaint();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
