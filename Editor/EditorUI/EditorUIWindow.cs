using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
        private static readonly List<ClipboardNodeData> clipboardNodes = new List<ClipboardNodeData>();
        private static int pasteSequence;

        private bool isSyncingSelection;
        private bool isBoxSelecting;
        private bool isCanvasPanning;
        private int lastNodeStructureSignature;
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
        private readonly Dictionary<string, string> nodeBindingSignatures = new Dictionary<string, string>();
        private readonly Dictionary<string, Vector2> pendingPosMap = new Dictionary<string, Vector2>();
        private Vector2 lastPointerPanelPosition;
        private bool hasLastPointerPanelPosition;

        private sealed class ClipboardNodeData
        {
            public string nodeId;
            public Vector2 position;
            public string parentNodeId;
        }

        [MenuItem("Tools/PlayableFramework/Editor UI")]
        private static void OpenWindow()
        {
            UIManager.Instance.EnsureWindow().Show();
        }

        [Shortcut("PlayableFramework/Save Node Pos", typeof(EditorUIWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void OnSaveNodePosShortcut()
        {
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

        [Shortcut("PlayableFramework/Copy Selected Nodes", typeof(EditorUIWindow), KeyCode.C, ShortcutModifiers.Action)]
        private static void OnCopySelectedNodesShortcut()
        {
            EditorUIWindow window = EditorWindow.focusedWindow as EditorUIWindow;
            if (window == null)
            {
                return;
            }

            window.CopySelection();
        }

        [Shortcut("PlayableFramework/Paste Selected Nodes", typeof(EditorUIWindow), KeyCode.V, ShortcutModifiers.Action)]
        private static void OnPasteSelectedNodesShortcut()
        {
            EditorUIWindow window = EditorWindow.focusedWindow as EditorUIWindow;
            if (window == null)
            {
                return;
            }

            window.PasteSelection();
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
            RefreshChangedSelectedNodes();
            TrySyncNodesIfStructureChanged();
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

        private void TrySyncNodesIfStructureChanged()
        {
            int currentSignature = CalculateNodeStructureSignature();
            if (currentSignature == 0 || currentSignature == lastNodeStructureSignature)
            {
                return;
            }

            lastNodeStructureSignature = currentSignature;
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

        private void RefreshChangedSelectedNodes()
        {
            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            if (selectedNodes == null || selectedNodes.Count == 0)
            {
                return;
            }

            bool anyRefreshed = false;
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                UINode node = selectedNodes[i];
                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                {
                    continue;
                }

                string currentSignature = node.GetBindingSignature();
                if (nodeBindingSignatures.TryGetValue(node.Data.Id, out string previousSignature) &&
                    previousSignature == currentSignature)
                {
                    continue;
                }

                nodeBindingSignatures[node.Data.Id] = currentSignature;
                node.RefreshBindings();
                node.Refresh();
                anyRefreshed = true;
            }

            if (!anyRefreshed)
            {
                return;
            }

            UIManager.Instance.VarLine?.MarkDirtyRepaint();
            UIManager.Instance.Curve?.MarkDirtyRepaint();
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

            UpdateLastPointerPosition(evt.mousePosition);
            Vector2 mousePosition = canvas.WorldToLocal(evt.mousePosition);
            Vector2 mouseScreenPosition = GUIUtility.GUIToScreenPoint(evt.mousePosition);
            EditorUITypeMenu.ShowCreateMenu(mouseScreenPosition, selectedType =>
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
            Graph graph = FindSceneGraph();
            if (graph != null)
            {
                Undo.RecordObject(graph, "Assign Graph NodePosAsset");
                graph.nodePosAsset = posAsset;
                EditorUtility.SetDirty(graph);
            }

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

            Graph graph = FindSceneGraph();
            if (graph != null)
            {
                Undo.RecordObject(graph, "Assign Graph NodePosAsset");
                graph.nodePosAsset = posAsset;
                EditorUtility.SetDirty(graph);
            }

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
            Graph graph = FindSceneGraph();
            if (graph != null && graph.nodePosAsset != null)
            {
                posAsset = graph.nodePosAsset as NodePosAsset;
                string graphAssetPath = AssetDatabase.GetAssetPath(posAsset);
                if (!string.IsNullOrEmpty(graphAssetPath))
                {
                    EditorPrefs.SetString(AssetKey, graphAssetPath);
                }

                return;
            }

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
                lastNodeStructureSignature = 0;
                NodeManager.Instance.SetUINodes(nodes);
                return;
            }

            string graphId = SceneNodeFactory.EnsureSceneNodeId(graph.gameObject);
            Dictionary<string, Vector2> posMap = BuildPosMap(graphId);

            List<Service> services = CollectGraphServices(graph);
            HashSet<int> addedObjects = new HashSet<int>();
            int order = 0;
            for (int i = 0; i < services.Count; i++)
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
            nodeBindingSignatures.Clear();
            lastNodeStructureSignature = CalculateNodeStructureSignature(graph);
            NodeManager.Instance.SetUINodes(nodes);
        }

        private int CalculateNodeStructureSignature()
        {
            return CalculateNodeStructureSignature(FindSceneGraph());
        }

        private static int CalculateNodeStructureSignature(Graph graph)
        {
            if (graph == null)
            {
                return 0;
            }

            List<Service> services = CollectGraphServices(graph);
            if (services == null || services.Count == 0)
            {
                return 1;
            }

            StringBuilder builder = new StringBuilder(services.Count * 64);
            for (int i = 0; i < services.Count; i++)
            {
                Service service = services[i];
                if (service == null)
                {
                    continue;
                }

                builder.Append(service.GetType().AssemblyQualifiedName);
                builder.Append('|');

                FieldInfo[] fields = service.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int j = 0; j < fields.Length; j++)
                {
                    FieldInfo field = fields[j];
                    if (field == null)
                    {
                        continue;
                    }

                    bool isInput = field.IsDefined(typeof(InputAttribute), true);
                    bool isOutput = field.IsDefined(typeof(OutputAttribute), true);
                    if (!isInput && !isOutput)
                    {
                        continue;
                    }

                    builder.Append(isInput ? "I:" : "O:");
                    builder.Append(field.Name);
                    builder.Append(':');
                    builder.Append(field.FieldType.AssemblyQualifiedName);
                    builder.Append(';');
                }

                builder.Append('#');
            }

            return builder.ToString().GetHashCode();
        }

        private static List<Service> CollectGraphServices(Graph graph)
        {
            List<Service> services = new List<Service>();
            if (graph == null)
            {
                return services;
            }

            Service[] childServices = graph.GetComponentsInChildren<Service>(true);
            if (childServices != null)
            {
                for (int i = 0; i < childServices.Length; i++)
                {
                    Service service = childServices[i];
                    if (service != null)
                    {
                        services.Add(service);
                    }
                }
            }

            IReadOnlyList<Transform> groupParents = graph.GroupParents;
            if (groupParents == null)
            {
                return services;
            }

            for (int i = 0; i < groupParents.Count; i++)
            {
                Transform groupParent = groupParents[i];
                if (groupParent == null)
                {
                    continue;
                }

                Service[] groupServices = groupParent.GetComponentsInChildren<Service>(true);
                if (groupServices == null)
                {
                    continue;
                }

                for (int j = 0; j < groupServices.Length; j++)
                {
                    Service service = groupServices[j];
                    if (service != null)
                    {
                        services.Add(service);
                    }
                }
            }

            return services;
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
                VarLine varLine = UIManager.Instance.VarLine;
                if (varLine == null || !varLine.HasSelection())
                {
                    return;
                }

                List<VarLine.SelectedBinding> bindings = varLine.GetSelectedBindings();
                bool anyCleared = false;
                for (int i = 0; i < bindings.Count; i++)
                {
                    VarLine.SelectedBinding binding = bindings[i];
                    if (!ServiceRule.Instance.TryClearInputValue(binding.InputNodeId, binding.InputFieldName))
                    {
                        continue;
                    }

                    anyCleared = true;
                }

                varLine.ClearSelection();
                if (anyCleared)
                {
                    SyncNodes();
                    SavePos();
                }

                return;
            }

            List<string> childIds = curve.GetSelectedChildIds();
            List<Curve.SelectedLink> selectedLinks = curve.GetSelectedLinks();
            if (selectedLinks.Count == 0)
            {
                return;
            }

            bool anyChanged = false;
            for (int i = 0; i < selectedLinks.Count; i++)
            {
                Curve.SelectedLink link = selectedLinks[i];
                if (string.IsNullOrEmpty(link.ParentId) || string.IsNullOrEmpty(link.ChildId))
                {
                    continue;
                }

                if (ServiceRule.Instance.TryClearFlowLink(link.ParentId, link.ChildId))
                {
                    anyChanged = true;
                    continue;
                }

                if (GameObjectOperator.MoveNodeToGraph(link.ChildId))
                {
                    anyChanged = true;
                }
            }

            curve.ClearSelection();
            if (anyChanged)
            {
                SyncNodes();
                SavePos();
            }
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
            VarLine varLine = UIManager.Instance.VarLine;
            if (canvas == null || surface == null)
            {
                return;
            }

            UpdateLastPointerPosition(evt.position);
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
                if (varLine != null)
                {
                    varLine.ClearSelection();
                }
                evt.StopPropagation();
                return;
            }

            if (varLine != null && varLine.TrySelectAt(canvasLocalPosition))
            {
                if (curve != null)
                {
                    curve.ClearSelection();
                }

                evt.StopPropagation();
                return;
            }

            if (curve != null)
            {
                curve.ClearSelection();
            }

            if (varLine != null)
            {
                varLine.ClearSelection();
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

            UpdateLastPointerPosition(evt.position);
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
            VarLine varLine = UIManager.Instance.VarLine;
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

            if (varLine != null)
            {
                varLine.SelectInRect(boxRect);
            }
        }

        private void CopySelection()
        {
            clipboardNodes.Clear();

            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            if (selectedNodes == null || selectedNodes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < selectedNodes.Count; i++)
            {
                UINode node = selectedNodes[i];
                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                {
                    continue;
                }

                clipboardNodes.Add(new ClipboardNodeData
                {
                    nodeId = node.Data.Id,
                    position = node.Data.Position,
                    parentNodeId = node.Data.ParentId
                });
            }

            pasteSequence = 0;
        }

        private void PasteSelection()
        {
            if (clipboardNodes.Count == 0)
            {
                return;
            }

            Graph graph = FindSceneGraph();
            if (graph == null)
            {
                return;
            }

            List<ClipboardNodeData> validClipboardNodes = new List<ClipboardNodeData>();
            Vector2 anchorMin = new Vector2(float.MaxValue, float.MaxValue);
            for (int i = 0; i < clipboardNodes.Count; i++)
            {
                ClipboardNodeData entry = clipboardNodes[i];
                if (entry == null || string.IsNullOrEmpty(entry.nodeId))
                {
                    continue;
                }

                if (!GameObjectOperator.TryGetNodeObject(entry.nodeId, out GameObject sourceNodeObject) || sourceNodeObject == null)
                {
                    continue;
                }

                validClipboardNodes.Add(entry);
                anchorMin.x = Mathf.Min(anchorMin.x, entry.position.x);
                anchorMin.y = Mathf.Min(anchorMin.y, entry.position.y);
            }

            if (validClipboardNodes.Count == 0)
            {
                clipboardNodes.Clear();
                return;
            }

            pasteSequence++;
            Vector2 pasteAnchor = GetPasteAnchorPosition();
            if (!hasLastPointerPanelPosition)
            {
                pasteAnchor += new Vector2(32f * pasteSequence, 32f * pasteSequence);
            }

            Dictionary<string, GameObject> oldIdToNewNode = new Dictionary<string, GameObject>();
            Dictionary<UnityEngine.Object, UnityEngine.Object> objectRemap = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

            for (int i = 0; i < validClipboardNodes.Count; i++)
            {
                ClipboardNodeData entry = validClipboardNodes[i];
                GameObjectOperator.TryGetNodeObject(entry.nodeId, out GameObject sourceNodeObject);
                if (sourceNodeObject == null)
                {
                    continue;
                }

                GameObject clonedNodeObject = Instantiate(sourceNodeObject, sourceNodeObject.transform.parent);
                clonedNodeObject.name = sourceNodeObject.name;
                Undo.RegisterCreatedObjectUndo(clonedNodeObject, "Paste Nodes");
                SceneNodeFactory.EnsureSceneNodeId(clonedNodeObject);

                oldIdToNewNode[entry.nodeId] = clonedNodeObject;
                RegisterCloneMappings(sourceNodeObject, clonedNodeObject, objectRemap);
            }

            for (int i = 0; i < validClipboardNodes.Count; i++)
            {
                ClipboardNodeData entry = validClipboardNodes[i];
                if (!oldIdToNewNode.TryGetValue(entry.nodeId, out GameObject clonedNodeObject) || clonedNodeObject == null)
                {
                    continue;
                }

                Transform targetParent = ResolvePasteParent(graph.transform, entry.parentNodeId, oldIdToNewNode);
                if (clonedNodeObject.transform.parent != targetParent)
                {
                    Undo.SetTransformParent(clonedNodeObject.transform, targetParent, "Paste Nodes");
                }

                RemapObjectReferences(clonedNodeObject, objectRemap);

                string newNodeId = SceneNodeFactory.GetSceneNodeId(clonedNodeObject);
                if (string.IsNullOrEmpty(newNodeId))
                {
                    continue;
                }

                pendingPosMap[newNodeId] = pasteAnchor + (entry.position - anchorMin);
            }

            SyncNodes();

            List<UINode> pastedNodes = new List<UINode>();
            foreach (KeyValuePair<string, GameObject> pair in oldIdToNewNode)
            {
                string newNodeId = SceneNodeFactory.GetSceneNodeId(pair.Value);
                UINode node = NodeManager.Instance.GetUINode(newNodeId);
                if (node != null)
                {
                    pastedNodes.Add(node);
                }
            }

            NodeManager.Instance.SetSelection(pastedNodes);
            SavePos();
        }

        private Vector2 GetPasteAnchorPosition()
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas != null && hasLastPointerPanelPosition)
            {
                return canvas.WorldToLocal(lastPointerPanelPosition);
            }

            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                return selectedNodes[0].Data.Position + new Vector2(32f, 32f);
            }

            return new Vector2(120f, 120f);
        }

        private void UpdateLastPointerPosition(Vector2 panelPosition)
        {
            lastPointerPanelPosition = panelPosition;
            hasLastPointerPanelPosition = true;
        }

        private static Transform ResolvePasteParent(Transform graphTransform, string originalParentNodeId, Dictionary<string, GameObject> oldIdToNewNode)
        {
            if (!string.IsNullOrEmpty(originalParentNodeId) &&
                oldIdToNewNode.TryGetValue(originalParentNodeId, out GameObject remappedParent) &&
                remappedParent != null)
            {
                return remappedParent.transform;
            }

            if (!string.IsNullOrEmpty(originalParentNodeId) &&
                GameObjectOperator.TryGetNodeObject(originalParentNodeId, out GameObject existingParent) &&
                existingParent != null)
            {
                return existingParent.transform;
            }

            return graphTransform;
        }

        private static void RegisterCloneMappings(GameObject sourceRoot, GameObject cloneRoot, Dictionary<UnityEngine.Object, UnityEngine.Object> objectRemap)
        {
            if (sourceRoot == null || cloneRoot == null)
            {
                return;
            }

            Transform[] sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            Transform[] cloneTransforms = cloneRoot.GetComponentsInChildren<Transform>(true);
            int transformCount = Mathf.Min(sourceTransforms.Length, cloneTransforms.Length);
            for (int i = 0; i < transformCount; i++)
            {
                Transform sourceTransform = sourceTransforms[i];
                Transform cloneTransform = cloneTransforms[i];
                if (sourceTransform == null || cloneTransform == null)
                {
                    continue;
                }

                objectRemap[sourceTransform] = cloneTransform;
                objectRemap[sourceTransform.gameObject] = cloneTransform.gameObject;

                Component[] sourceComponents = sourceTransform.GetComponents<Component>();
                Component[] cloneComponents = cloneTransform.GetComponents<Component>();
                int componentCount = Mathf.Min(sourceComponents.Length, cloneComponents.Length);
                for (int j = 0; j < componentCount; j++)
                {
                    Component sourceComponent = sourceComponents[j];
                    Component cloneComponent = cloneComponents[j];
                    if (sourceComponent == null || cloneComponent == null)
                    {
                        continue;
                    }

                    objectRemap[sourceComponent] = cloneComponent;
                }
            }
        }

        private static void RemapObjectReferences(GameObject clonedNodeObject, Dictionary<UnityEngine.Object, UnityEngine.Object> objectRemap)
        {
            if (clonedNodeObject == null || objectRemap == null || objectRemap.Count == 0)
            {
                return;
            }

            Component[] components = clonedNodeObject.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                bool changed = false;
                while (iterator.Next(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    UnityEngine.Object currentReference = iterator.objectReferenceValue;
                    if (currentReference == null || !objectRemap.TryGetValue(currentReference, out UnityEngine.Object remappedReference))
                    {
                        continue;
                    }

                    iterator.objectReferenceValue = remappedReference;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
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
