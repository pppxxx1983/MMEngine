using System.Collections.Generic;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class EditorUIWindow : EditorWindow
    {
        private const string AssetKey = "PlayableFramework.EditorUI.NodePosAsset";

        private bool isSyncingSelection;
        private bool isBoxSelecting;
        private Vector2 boxStart;
        private Vector2 boxCurrent;
        private ObjectField assetField;
        private HelpBox saveTip;
        private NodePosAsset posAsset;

        [MenuItem("Tools/PlayableFramework/Editor UI")]
        private static void OpenWindow()
        {
            UIManager.Instance.EnsureWindow().Show();
        }

        private void OnEnable()
        {
            NodeManager.Instance.Changed += RebuildNodes;
            NodeManager.Instance.SelectionChanged += OnNodeSelectionChanged;
            NodeManager.Instance.PosChanged += SavePos;
        }

        private void OnDisable()
        {
            NodeManager.Instance.Changed -= RebuildNodes;
            NodeManager.Instance.SelectionChanged -= OnNodeSelectionChanged;
            NodeManager.Instance.PosChanged -= SavePos;
            SavePos();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = new Color(0.15f, 0.16f, 0.18f, 1f);
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            LoadAsset();

            VisualElement topBar = new HLayout();
            topBar.style.flexGrow = 0f;
            topBar.style.flexShrink = 0f;
            topBar.style.justifyContent = Justify.FlexStart;
            topBar.style.alignItems = Align.Center;
            topBar.style.paddingLeft = 8f;
            topBar.style.paddingRight = 8f;
            topBar.style.paddingTop = 6f;
            topBar.style.paddingBottom = 6f;

            assetField = new ObjectField();
            assetField.objectType = typeof(NodePosAsset);
            assetField.allowSceneObjects = false;
            assetField.value = posAsset;
            assetField.style.width = 260f;
            assetField.RegisterValueChangedCallback(OnAssetChanged);
            topBar.Add(assetField);

            Button saveButton = new Button(CreatePosAsset);
            saveButton.text = "Save";
            topBar.Add(saveButton);

            saveTip = new HelpBox("Please save asset.", HelpBoxMessageType.Info);
            saveTip.style.marginLeft = 8f;
            topBar.Add(saveTip);

            rootVisualElement.Add(topBar);

            VisualElement canvas = new VisualElement();
            canvas.name = "editor-ui-canvas";
            canvas.focusable = true;
            canvas.style.flexGrow = 1f;
            canvas.style.position = Position.Relative;
            canvas.RegisterCallback<ContextClickEvent>(OnCanvasContextClick);
            canvas.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown, TrickleDown.TrickleDown);
            canvas.RegisterCallback<PointerMoveEvent>(OnCanvasPointerMove, TrickleDown.TrickleDown);
            canvas.RegisterCallback<PointerUpEvent>(OnCanvasPointerUp, TrickleDown.TrickleDown);
            rootVisualElement.Add(canvas);
            UIManager.Instance.SetRoot(rootVisualElement);
            UIManager.Instance.SetCanvas(canvas);
            RefreshAssetTip();

            SyncNodes();
        }

        private void RebuildNodes()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            canvas.Clear();
            Line line = new Line();
            UIManager.Instance.SetCanvas(canvas);
            UIManager.Instance.SetLine(line);

            for (int i = 0; i < NodeManager.Instance.Nodes.Count; i++)
            {
                NodeData nodeData = NodeManager.Instance.Nodes[i];
                if (nodeData != null)
                {
                    UINode node = new UINode(nodeData);
                    canvas.Add(node);
                }
            }

            canvas.Add(line);
            SelectionBox selectionBox = new SelectionBox();
            UIManager.Instance.SetSelectionBox(selectionBox);
            canvas.Add(selectionBox);
        }

        private void OnHierarchyChange()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            SyncNodes();
            SavePos();
        }

        private void RefreshNodes()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            int childCount = canvas.childCount;
            for (int i = 0; i < childCount; i++)
            {
                UINode node = canvas[i] as UINode;
                if (node != null)
                {
                    node.Refresh();
                }
            }
        }

        private void OnSelectionChange()
        {
            if (isSyncingSelection)
            {
                return;
            }

            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            isSyncingSelection = true;
            try
            {
                GameObject[] selectedObjects = Selection.gameObjects;
                if (selectedObjects == null || selectedObjects.Length == 0)
                {
                    NodeManager.Instance.ClearSelection();
                    return;
                }

                List<NodeData> selectedNodes = new List<NodeData>();
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    GameObject selectedObject = selectedObjects[i];
                    if (selectedObject == null)
                    {
                        continue;
                    }

                    string nodeId = SceneNodeFactory.GetSceneNodeId(selectedObject);
                    if (string.IsNullOrEmpty(nodeId))
                    {
                        continue;
                    }

                    NodeData node = NodeManager.Instance.GetNode(nodeId);
                    if (node != null)
                    {
                        selectedNodes.Add(node);
                    }
                }

                NodeManager.Instance.SetSelection(selectedNodes);
            }
            finally
            {
                isSyncingSelection = false;
            }
        }

        private void OnNodeSelectionChanged()
        {
            RefreshNodes();
            SyncSceneSelectionFromNode();
        }

        private void OnCanvasContextClick(ContextClickEvent evt)
        {
            UIManager.Instance.Canvas.Focus();
            Vector2 mousePosition = evt.mousePosition;
            EditorUITypeMenu.ShowCreateMenu(mousePosition, selectedType =>
            {
                if (selectedType == null)
                {
                    return;
                }

                GameObject nodeObject = SceneNodeFactory.CreateSceneNode(selectedType);
                if (nodeObject == null)
                {
                    return;
                }

                string nodeId = SceneNodeFactory.GetSceneNodeId(nodeObject);
                SyncNodes();

                NodeData node = NodeManager.Instance.GetNode(nodeId);
                if (node != null)
                {
                    node.Position = mousePosition;
                }

                SavePos();
                RebuildNodes();
            });
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace)
            {
                return;
            }

            DeleteSelectedNode();
            evt.StopPropagation();
        }

        private void OnAssetChanged(ChangeEvent<Object> evt)
        {
            posAsset = evt.newValue as NodePosAsset;
            if (posAsset != null)
            {
                EditorPrefs.SetString(AssetKey, AssetDatabase.GetAssetPath(posAsset));
            }
            else
            {
                EditorPrefs.DeleteKey(AssetKey);
            }

            RefreshAssetTip();
            SyncNodes();
        }

        private void CreatePosAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Node Pos", "NodePos", "asset", "Save node pos asset");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            NodePosAsset asset = ScriptableObject.CreateInstance<NodePosAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            posAsset = asset;
            EditorPrefs.SetString(AssetKey, path);

            if (assetField != null)
            {
                assetField.value = posAsset;
            }

            RefreshAssetTip();
            SavePos();
        }

        private void LoadAsset()
        {
            string path = EditorPrefs.GetString(AssetKey, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                posAsset = AssetDatabase.LoadAssetAtPath<NodePosAsset>(path);
            }
        }

        private void RefreshAssetTip()
        {
            if (saveTip == null)
            {
                return;
            }

            saveTip.style.display = posAsset == null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SyncNodes()
        {
            List<NodeData> nodes = new List<NodeData>();
            Dictionary<string, Vector2> posMap = BuildPosMap();
            Graph graph = FindSceneGraph();
            if (graph == null)
            {
                NodeManager.Instance.SetNodes(nodes);
                return;
            }

            Service[] services = graph.GetComponentsInChildren<Service>(true);
            HashSet<int> addedObjects = new HashSet<int>();
            int order = 0;
            for (int i = 0; i < services.Length; i++)
            {
                Service service = services[i];
                if (service == null)
                {
                    continue;
                }

                GameObject nodeObject = service.gameObject;
                if (!addedObjects.Add(nodeObject.GetInstanceID()))
                {
                    continue;
                }

                string nodeId = SceneNodeFactory.EnsureSceneNodeId(nodeObject);
                if (string.IsNullOrEmpty(nodeId))
                {
                    continue;
                }

                Vector2 position;
                if (!posMap.TryGetValue(nodeId, out position))
                {
                    position = new Vector2(80f, 80f + order * 90f);
                }

                nodes.Add(new NodeData(position, nodeObject.name, nodeId));
                order++;
            }

            NodeManager.Instance.SetNodes(nodes);
        }

        private Dictionary<string, Vector2> BuildPosMap()
        {
            Dictionary<string, Vector2> posMap = new Dictionary<string, Vector2>();
            if (posAsset == null || posAsset.nodes == null)
            {
                return posMap;
            }

            for (int i = 0; i < posAsset.nodes.Count; i++)
            {
                NodePos node = posAsset.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    continue;
                }

                posMap[node.id] = node.pos;
            }

            return posMap;
        }

        private void SavePos()
        {
            if (posAsset == null)
            {
                RefreshAssetTip();
                return;
            }

            if (posAsset.nodes == null)
            {
                posAsset.nodes = new List<NodePos>();
            }

            posAsset.nodes.Clear();
            for (int i = 0; i < NodeManager.Instance.Nodes.Count; i++)
            {
                NodeData node = NodeManager.Instance.Nodes[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                posAsset.nodes.Add(new NodePos
                {
                    id = node.Id,
                    pos = node.Position
                });
            }

            EditorUtility.SetDirty(posAsset);
            AssetDatabase.SaveAssets();
            RefreshAssetTip();
        }

        private void DeleteSelectedNode()
        {
            NodeData node = NodeManager.Instance.SelectedNode;
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                return;
            }

            GameObject nodeObject;
            if (SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) && nodeObject != null)
            {
                Undo.DestroyObjectImmediate(nodeObject);
            }

            SyncNodes();
            SavePos();
        }

        private void SyncSceneSelectionFromNode()
        {
            if (isSyncingSelection)
            {
                return;
            }

            isSyncingSelection = true;
            try
            {
                List<NodeData> selectedNodes = NodeManager.Instance.GetSelectedNodes();
                if (selectedNodes.Count == 0)
                {
                    if (Selection.activeGameObject != null)
                    {
                        Selection.activeGameObject = null;
                    }

                    return;
                }

                List<GameObject> selectedObjects = new List<GameObject>();
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    NodeData node = selectedNodes[i];
                    if (node == null || string.IsNullOrEmpty(node.Id))
                    {
                        continue;
                    }

                    GameObject nodeObject;
                    if (SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) && nodeObject != null)
                    {
                        selectedObjects.Add(nodeObject);
                    }
                }

                Selection.objects = selectedObjects.ToArray();
            }
            finally
            {
                isSyncingSelection = false;
            }
        }

        private void OnCanvasPointerDown(PointerDownEvent evt)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            if (evt.target != canvas)
            {
                return;
            }

            canvas.Focus();
            boxStart = evt.localPosition;
            boxCurrent = evt.localPosition;
            isBoxSelecting = true;
            canvas.CapturePointer(evt.pointerId);
            UpdateSelectionBox();
            evt.StopPropagation();
        }

        private void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (!isBoxSelecting || !canvas.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            boxCurrent = evt.localPosition;
            UpdateSelectionBox();
            ApplyBoxSelection();
            evt.StopPropagation();
        }

        private void OnCanvasPointerUp(PointerUpEvent evt)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            SelectionBox selectionBox = UIManager.Instance.SelectionBox;
            if (!isBoxSelecting || !canvas.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            boxCurrent = evt.localPosition;
            ApplyBoxSelection();
            isBoxSelecting = false;
            if (selectionBox != null)
            {
                selectionBox.IsVisible = false;
                selectionBox.MarkDirtyRepaint();
            }
            canvas.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void UpdateSelectionBox()
        {
            SelectionBox selectionBox = UIManager.Instance.SelectionBox;
            if (selectionBox == null)
            {
                return;
            }

            float xMin = Mathf.Min(boxStart.x, boxCurrent.x);
            float yMin = Mathf.Min(boxStart.y, boxCurrent.y);
            float xMax = Mathf.Max(boxStart.x, boxCurrent.x);
            float yMax = Mathf.Max(boxStart.y, boxCurrent.y);

            selectionBox.Rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            selectionBox.IsVisible = true;
            selectionBox.MarkDirtyRepaint();
        }

        private void ApplyBoxSelection()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            SelectionBox selectionBox = UIManager.Instance.SelectionBox;
            if (canvas == null || selectionBox == null)
            {
                return;
            }

            Rect boxRect = selectionBox.Rect;
            List<NodeData> selectedNodes = new List<NodeData>();
            int childCount = canvas.childCount;
            for (int i = 0; i < childCount; i++)
            {
                UINode node = canvas[i] as UINode;
                if (node == null)
                {
                    continue;
                }

                Rect nodeRect = node.layout;
                if (boxRect.Overlaps(nodeRect))
                {
                    selectedNodes.Add(node.Data);
                }
            }

            NodeManager.Instance.SetSelection(selectedNodes);
        }

        private static Graph FindSceneGraph()
        {
            Root root = Object.FindObjectOfType<Root>();
            if (root == null)
            {
                return null;
            }

            return root.GetComponentInChildren<Graph>();
        }
    }
}
