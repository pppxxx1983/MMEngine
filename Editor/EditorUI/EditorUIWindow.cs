using System.Collections.Generic;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class EditorUIWindow : EditorWindow
    {
        private const string AssetKey = "PlayableFramework.EditorUI.NodePosAsset";
        private static bool suppressHierarchySync;

        private bool isSyncingSelection;
        private bool isBoxSelecting;
        private bool isCanvasPanning;
        private Vector2 boxStart;
        private Vector2 boxCurrent;
        private Vector2 panStartMouse;
        private Vector2 panStartOffset;
        private Vector2 canvasOffset;
        private float canvasScale = 1f;
        private ObjectField assetField;
        private HelpBox saveTip;
        private VisualElement viewport;
        private NodePosAsset posAsset;
        private readonly Dictionary<string, Vector2> pendingPosMap = new Dictionary<string, Vector2>();

        [MenuItem("Tools/PlayableFramework/Editor UI")]
        private static void OpenWindow()
        {
            UIManager.Instance.EnsureWindow().Show();
        }

        [Shortcut("PlayableFramework/Save Node Pos", typeof(EditorUIWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void OnSaveNodePosShortcut()
        {
            Debug.Log("EditorUI window Ctrl+S shortcut triggered.");

            EditorUIWindow window = EditorWindow.focusedWindow as EditorUIWindow;
            if (window == null)
            {
                return;
            }

            window.SaveNodePosWithAsset();
        }

        [Shortcut("PlayableFramework/Delete Selected Nodes", typeof(EditorUIWindow), KeyCode.Delete)]
        private static void OnDeleteSelectedNodesShortcut()
        {
            EditorUIWindow window = EditorWindow.focusedWindow as EditorUIWindow;
            if (window == null)
            {
                return;
            }

            window.DeleteSelection();
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

        private void OnInspectorUpdate()
        {
            UIManager.Instance.VarLine?.MarkDirtyRepaint();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = new Color(0.15f, 0.16f, 0.18f, 1f);
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

            viewport = new VisualElement();
            viewport.name = "editor-ui-viewport";
            viewport.focusable = true;
            viewport.style.flexGrow = 1f;
            viewport.style.position = Position.Relative;
            viewport.style.overflow = Overflow.Hidden;
            viewport.RegisterCallback<ContextClickEvent>(OnCanvasContextClick);
            viewport.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerMoveEvent>(OnCanvasPointerMove, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerUpEvent>(OnCanvasPointerUp, TrickleDown.TrickleDown);
            viewport.RegisterCallback<WheelEvent>(OnCanvasWheel, TrickleDown.TrickleDown);
            rootVisualElement.Add(viewport);

            VisualElement canvas = new VisualElement();
            canvas.name = "editor-ui-canvas";
            canvas.pickingMode = PickingMode.Position;
            canvas.style.position = Position.Absolute;
            canvas.style.left = 0f;
            canvas.style.top = 0f;
            canvas.style.width = 12000f;
            canvas.style.height = 12000f;
            canvas.style.transformOrigin = new TransformOrigin(0f, 0f, 0f);
            viewport.Add(canvas);
            UIManager.Instance.SetRoot(rootVisualElement);
            UIManager.Instance.SetCanvas(canvas);
            ApplyCanvasOffset();
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
            Curve curve = UIManager.Instance.Curve;
            if (curve == null)
            {
                curve = new Curve();
            }

            VarLine varLine = UIManager.Instance.VarLine;
            if (varLine == null)
            {
                varLine = new VarLine();
            }

            Line line = new Line();
            UIManager.Instance.SetCanvas(canvas);
            UIManager.Instance.SetCurve(curve);
            UIManager.Instance.SetVarLine(varLine);
            UIManager.Instance.SetLine(line);

            for (int i = 0; i < NodeManager.Instance.UINodes.Count; i++)
            {
                UINode node = NodeManager.Instance.UINodes[i];
                if (node != null)
                {
                    canvas.Add(node);
                }
            }

            canvas.Add(curve);
            canvas.Add(varLine);
            canvas.Add(line);
            SelectionBox selectionBox = new SelectionBox();
            UIManager.Instance.SetSelectionBox(selectionBox);
            canvas.Add(selectionBox);
        }

        private void OnHierarchyChange()
        {
            if (suppressHierarchySync)
            {
                return;
            }

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

            Curve curve = UIManager.Instance.Curve;
            if (curve != null)
            {
                curve.MarkDirtyRepaint();
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

                List<UINode> selectedNodes = new List<UINode>();
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

                    UINode node = NodeManager.Instance.GetUINode(nodeId);
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
            VisualElement surface = evt.currentTarget as VisualElement;
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            if (surface != null)
            {
                surface.Focus();
            }

            Vector2 mousePosition = canvas.WorldToLocal(evt.mousePosition);
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
                if (!string.IsNullOrEmpty(nodeId))
                {
                    pendingPosMap[nodeId] = mousePosition;
                }

                SyncNodes();
                SavePos();
            });
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

        private void SaveNodePosWithAsset()
        {
            if (posAsset == null)
            {
                CreatePosAsset();
                return;
            }

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
            List<UINode> nodes = new List<UINode>();
            Graph graph = FindSceneGraph();
            if (graph == null)
            {
                NodeManager.Instance.SetUINodes(nodes);
                return;
            }

            string graphId = SceneNodeFactory.EnsureSceneNodeId(graph.gameObject);
            Dictionary<string, Vector2> posMap = BuildPosMap(graphId);

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

                Vector2 pendingPosition;
                if (pendingPosMap.TryGetValue(nodeId, out pendingPosition))
                {
                    position = pendingPosition;
                }

                string parentId = SceneNodeFactory.GetSceneNodeId(nodeObject.transform.parent != null ? nodeObject.transform.parent.gameObject : null);
                NodeData nodeData = new NodeData(position, nodeObject.name, nodeId, parentId);
                nodes.Add(new UINode(nodeData));
                order++;
            }

            pendingPosMap.Clear();
            NodeManager.Instance.SetUINodes(nodes);
        }

        private Dictionary<string, Vector2> BuildPosMap(string graphId)
        {
            Dictionary<string, Vector2> posMap = new Dictionary<string, Vector2>();
            if (posAsset == null || posAsset.nodes == null)
            {
                return posMap;
            }

            if (string.IsNullOrEmpty(graphId) || posAsset.graphId != graphId)
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

            Graph graph = FindSceneGraph();
            if (graph == null)
            {
                posAsset.graphId = null;
                if (posAsset.nodes != null)
                {
                    posAsset.nodes.Clear();
                }

                EditorUtility.SetDirty(posAsset);
                AssetDatabase.SaveAssets();
                RefreshAssetTip();
                return;
            }

            if (posAsset.nodes == null)
            {
                posAsset.nodes = new List<NodePos>();
            }

            posAsset.graphId = SceneNodeFactory.EnsureSceneNodeId(graph.gameObject);
            posAsset.nodes.Clear();
            for (int i = 0; i < NodeManager.Instance.UINodes.Count; i++)
            {
                UINode node = NodeManager.Instance.UINodes[i];
                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                {
                    continue;
                }

                posAsset.nodes.Add(new NodePos
                {
                    id = node.Data.Id,
                    pos = node.Data.Position
                });
            }

            EditorUtility.SetDirty(posAsset);
            AssetDatabase.SaveAssets();
            RefreshAssetTip();
        }

        private void DeleteSelection()
        {
            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            if (selectedNodes.Count > 0)
            {
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    UINode node = selectedNodes[i];
                    if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                    {
                        continue;
                    }

                    GameObjectOperator.DestroyNodeObject(node.Data.Id);
                }

                SyncNodes();
                SavePos();
                return;
            }

            Curve curve = UIManager.Instance.Curve;
            if (curve == null || !curve.HasSelection())
            {
                return;
            }

            List<string> childIds = curve.GetSelectedChildIds();
            if (childIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < childIds.Count; i++)
            {
                string childId = childIds[i];
                if (string.IsNullOrEmpty(childId))
                {
                    continue;
                }

                GameObjectOperator.MoveNodeToGraph(childId);
            }

            curve.ClearSelection();
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
                List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
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
                    UINode node = selectedNodes[i];
                    if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                    {
                        continue;
                    }

                    GameObject nodeObject;
                    if (GameObjectOperator.TryGetNodeObject(node.Data.Id, out nodeObject))
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
            VisualElement surface = evt.currentTarget as VisualElement;
            VisualElement canvas = UIManager.Instance.Canvas;
            Curve curve = UIManager.Instance.Curve;
            if (canvas == null || surface == null)
            {
                return;
            }

            if (evt.target != surface && evt.target != canvas)
            {
                return;
            }

            if (evt.button == (int)MouseButton.MiddleMouse)
            {
                surface.Focus();
                isCanvasPanning = true;
                panStartMouse = new Vector2(evt.position.x, evt.position.y);
                panStartOffset = canvasOffset;
                surface.CapturePointer(evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            surface.Focus();
            Vector2 pointerPosition = new Vector2(evt.position.x, evt.position.y);
            Vector2 canvasLocalPosition = canvas.WorldToLocal(pointerPosition);
            if (curve != null && curve.TrySelectAt(canvasLocalPosition))
            {
                evt.StopPropagation();
                return;
            }

            if (curve != null)
            {
                curve.ClearSelection();
            }

            boxStart = canvasLocalPosition;
            boxCurrent = canvasLocalPosition;
            isBoxSelecting = true;
            surface.CapturePointer(evt.pointerId);
            UpdateSelectionBox();
            evt.StopPropagation();
        }

        private void OnCanvasPointerMove(PointerMoveEvent evt)
        {
            VisualElement surface = evt.currentTarget as VisualElement;
            if (surface == null)
            {
                return;
            }

            if (isCanvasPanning && surface.HasPointerCapture(evt.pointerId))
            {
                Vector2 currentMouse = new Vector2(evt.position.x, evt.position.y);
                canvasOffset = panStartOffset + (currentMouse - panStartMouse);
                ApplyCanvasOffset();
                evt.StopPropagation();
                return;
            }

            if (!isBoxSelecting || !surface.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            Vector2 pointerPosition = new Vector2(evt.position.x, evt.position.y);
            boxCurrent = canvas.WorldToLocal(pointerPosition);
            UpdateSelectionBox();
            ApplyBoxSelection();
            evt.StopPropagation();
        }

        private void OnCanvasPointerUp(PointerUpEvent evt)
        {
            VisualElement surface = evt.currentTarget as VisualElement;
            SelectionBox selectionBox = UIManager.Instance.SelectionBox;
            if (surface == null)
            {
                return;
            }

            if (isCanvasPanning && surface.HasPointerCapture(evt.pointerId))
            {
                isCanvasPanning = false;
                surface.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (!isBoxSelecting || !surface.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            Vector2 pointerPosition = new Vector2(evt.position.x, evt.position.y);
            boxCurrent = canvas.WorldToLocal(pointerPosition);
            ApplyBoxSelection();
            isBoxSelecting = false;
            if (selectionBox != null)
            {
                selectionBox.IsVisible = false;
                selectionBox.MarkDirtyRepaint();
            }
            surface.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void ApplyCanvasOffset()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            canvas.transform.position = new Vector3(canvasOffset.x, canvasOffset.y, 0f);
            canvas.transform.scale = new Vector3(canvasScale, canvasScale, 1f);
        }

        private void OnCanvasWheel(WheelEvent evt)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            float previousScale = canvasScale;
            float nextScale = Mathf.Clamp(previousScale * (evt.delta.y > 0f ? 0.9f : 1.1f), 0.5f, 2f);
            if (Mathf.Approximately(previousScale, nextScale))
            {
                return;
            }

            Vector2 mousePanelPosition = evt.mousePosition;
            Vector2 contentPositionBeforeZoom = (mousePanelPosition - canvasOffset) / previousScale;
            canvasScale = nextScale;
            canvasOffset = mousePanelPosition - contentPositionBeforeZoom * canvasScale;
            ApplyCanvasOffset();
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
            Curve curve = UIManager.Instance.Curve;
            if (canvas == null || selectionBox == null)
            {
                return;
            }

            Rect boxRect = selectionBox.Rect;
            List<UINode> selectedNodes = new List<UINode>();
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
                    selectedNodes.Add(node);
                }
            }

            NodeManager.Instance.SetSelection(selectedNodes);
            if (curve != null)
            {
                curve.SelectInRect(boxRect);
            }
        }

        private static Graph FindSceneGraph()
        {
            return GameObjectOperator.FindGraph();
        }

        public static void SuppressHierarchySyncOnce()
        {
            suppressHierarchySync = true;
            EditorApplication.delayCall -= ClearHierarchySyncSuppression;
            EditorApplication.delayCall += ClearHierarchySyncSuppression;
        }

        private static void ClearHierarchySyncSuppression()
        {
            suppressHierarchySync = false;
            EditorApplication.delayCall -= ClearHierarchySyncSuppression;
        }
    }
}
