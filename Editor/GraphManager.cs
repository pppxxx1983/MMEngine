using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    /// <summary>
    /// 全局唯一的节点图管理器。
    /// 负责节点数据、连线关系和位置保存。
    /// </summary>
    internal sealed class GraphManager
    {
        private enum SelectedLineType
        {
            None,
            Parent,
            Data
        }

        private const string AssetPathPrefKey = "PlayableFramework.NodeGraphAssetPath";
        private const float AutoLayoutVerticalSpacing = 150f;
        private const float AutoLayoutHorizontalSpacing = 260f;
        private const BindingFlags ServiceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class SceneNodeInfo
        {
            public string id;
            public string parentId;
            public string title;
            public Type serviceType;
            public GameObject nodeObject;
            public int siblingIndex;
            public int siblingCount;
        }

        private static GraphManager instance;

        private string currentAssetPath;
        private ConnectionPoint draggingConnectionPoint;
        private Vector2 draggingConnectionMousePosition;
        private SelectedLineType selectedLineType;
        private string selectedLineFromNodeId;
        private string selectedLineToNodeId;
        private int selectedLineInputIndex = -1;

        private GraphManager()
        {
            currentAssetPath = EditorPrefs.GetString(AssetPathPrefKey, string.Empty);
        }

        public static GraphManager Instance
        {
            get { return instance ?? (instance = new GraphManager()); }
        }

        public GraphLayoutAsset CurrentAsset { get; private set; }

        public bool HasSelectedLine
        {
            get { return selectedLineType != SelectedLineType.None; }
        }

        public ConnectionPoint DraggingConnectionPoint
        {
            get { return draggingConnectionPoint; }
            set { draggingConnectionPoint = value; }
        }

        public Vector2 DraggingConnectionMousePosition
        {
            get { return draggingConnectionMousePosition; }
            set { draggingConnectionMousePosition = value; }
        }

        public System.Collections.Generic.IReadOnlyList<GraphNode> Nodes
        {
            get { return CurrentAsset != null ? CurrentAsset.nodes : null; }
        }

        private System.Collections.Generic.List<GraphNode> NodeList
        {
            get { return CurrentAsset != null ? CurrentAsset.nodes : null; }
        }

        public GraphNode CreateNode(Type serviceType, Vector2 canvasPosition)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Create node is disabled while in Play Mode.");
                return null;
            }

            if (Nodes == null)
            {
                return null;
            }

            GraphNode node = new GraphNode(canvasPosition,serviceType);
            NodeList.Add(node);
            SyncNodeToScene(node,serviceType);
            return node;
        }

        private void SyncNodeToScene(GraphNode node,Type serviceType)
        {
            Root root = UnityEngine.Object.FindObjectOfType<Root>();
            if (root == null)
            {
                return;
            }

            Graph graph = root.GetComponentInChildren<Graph>();
            if (graph == null)
            {
                GameObject graphObj = new GameObject("Graph");
                graphObj.transform.SetParent(root.transform, false);
                graph = graphObj.AddComponent<Graph>();
                Undo.RegisterCreatedObjectUndo(graphObj, "Create Graph");
            }

            // 创建 GameObject 并挂 SceneRefObject
            // SceneRefObject.OnEnable 会自动注册并确保 ID 唯一
            GameObject nodeObj = graph.CreateNodeObject();
            SceneRefObject sceneRef = nodeObj.AddComponent<SceneRefObject>();

            // 以 SceneRef 生成的唯一 ID 作为 node 的 ID
            node.Id = sceneRef.Id;
            nodeObj.name = serviceType != null ? serviceType.Name : node.Id;

            if (serviceType != null)
            {
                if (!typeof(Component).IsAssignableFrom(serviceType))
                {
                    Debug.LogError("Create node failed: selected type is not a Component. " + serviceType.FullName);
                }
                else
                {
                    Component component = nodeObj.GetComponent(serviceType);
                    if (component == null)
                    {
                        component = Undo.AddComponent(nodeObj, serviceType);
                    }

                    if (component != null)
                    {
                        node.Title = component.GetType().Name;
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(nodeObj, "Create Node");
            EditorUtility.SetDirty(graph);
            node.InitConnectionPoints(nodeObj);
        }

        public void LoadFromAsset(GraphLayoutAsset asset)
        {
            if (asset == null || asset.nodes == null)
            {
                return;
            }

            currentAssetPath = AssetDatabase.GetAssetPath(asset);
            EditorPrefs.SetString(AssetPathPrefKey, currentAssetPath);
            CurrentAsset = asset;
            ClearSelectedNode();
            ClearSelectedLine();

            for (int i = 0; i < asset.nodes.Count; i++)
            {
                GraphNode node = asset.nodes[i];
                if (node != null)
                {
                    GameObject nodeObject;
                    TryGetNodeObject(node, out nodeObject);
                    node.InitConnectionPoints(nodeObject);
                }
            }

            bool changed = SyncNodesFromSceneHierarchy(true);

            if (changed)
            {
                EditorUtility.SetDirty(CurrentAsset);
            }
        }

        public bool SyncGraphFromSceneHierarchy()
        {
            if (CurrentAsset == null)
            {
                return false;
            }

            bool changed = SyncNodesFromSceneHierarchy(true);
            if (changed)
            {
                EditorUtility.SetDirty(CurrentAsset);
            }

            return changed;
        }

        public void LoadGraph()
        {
            if (string.IsNullOrEmpty(currentAssetPath))
            {
                return;
            }

            GraphLayoutAsset asset = AssetDatabase.LoadAssetAtPath<GraphLayoutAsset>(currentAssetPath);
            LoadFromAsset(asset);
        }

        public void SaveGraph()
        {
            if (CurrentAsset == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Node Graph", "NodeGraphLayout", "asset", "选择保存位置");

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                currentAssetPath = path;
                EditorPrefs.SetString(AssetPathPrefKey, currentAssetPath);

                GraphLayoutAsset asset = ScriptableObject.CreateInstance<GraphLayoutAsset>();
                AssetDatabase.CreateAsset(asset, currentAssetPath);
                CurrentAsset = asset;
            }

            EditorUtility.SetDirty(CurrentAsset);
            AssetDatabase.SaveAssets();
        }

        public void RemoveNode(GraphNode node)
        {
            if (NodeList == null || node == null)
            {
                return;
            }

            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode current = NodeList[i];
                if (current == node)
                {
                    continue;
                }

                if (GetMainInputSourceId(current) == node.Id)
                {
                    SetNodeParentInScene(current, null);
                }

                IReadOnlyList<ConnectionPoint> inputPoints = current.DataInputPoints;
                for (int j = 0; j < inputPoints.Count; j++)
                {
                    ConnectionPoint inputPoint = inputPoints[j];
                    if (inputPoint != null)
                    {
                        UnbindInputVarFromService(current, inputPoint, node.Id);
                    }
                }
            }
            RemoveNodeObject(node.Id);
            NodeList.Remove(node);
            if (selectedLineFromNodeId == node.Id || selectedLineToNodeId == node.Id)
            {
                ClearSelectedLine();
            }
        }

        public GraphNode SelectNodeById(string nodeId)
        {
            GraphNode node = GetNodeById(nodeId);
            SelectSingleNode(node);
            return node;
        }

        public void ClearSelectedNode()
        {
            if (NodeList == null)
            {
                return;
            }

            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode node = NodeList[i];
                if (node != null)
                {
                    node.IsSelected = false;
                }
            }
        }

        public void ClearSelectedLine()
        {
            selectedLineType = SelectedLineType.None;
            selectedLineFromNodeId = null;
            selectedLineToNodeId = null;
            selectedLineInputIndex = -1;
        }

        public void SelectSingleNode(GraphNode selected)
        {
            ClearSelectedLine();
            if (NodeList == null)
            {
                return;
            }

            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode node = NodeList[i];
                if (node != null)
                {
                    node.IsSelected = node == selected;
                }
            }
        }

        public bool IsSelectedParentLine(string parentNodeId, string childNodeId)
        {
            return selectedLineType == SelectedLineType.Parent
                && selectedLineFromNodeId == parentNodeId
                && selectedLineToNodeId == childNodeId;
        }

        public bool IsSelectedDataLine(string outputNodeId, string inputNodeId, int inputIndex)
        {
            return selectedLineType == SelectedLineType.Data
                && selectedLineFromNodeId == outputNodeId
                && selectedLineToNodeId == inputNodeId
                && selectedLineInputIndex == inputIndex;
        }

        public bool TrySelectLineAt(Vector2 mousePosition, Vector2 canvasOffset)
        {
            if (Nodes == null)
            {
                ClearSelectedLine();
                return false;
            }

            const float hitThreshold = 8f;
            float bestDistance = float.MaxValue;
            SelectedLineType bestType = SelectedLineType.None;
            string bestFromId = null;
            string bestToId = null;
            int bestInputIndex = -1;

            for (int i = 0; i < Nodes.Count; i++)
            {
                GraphNode childNode = Nodes[i];
                if (childNode == null || childNode.EnterPoint == null || string.IsNullOrEmpty(childNode.EnterPoint.SingleConnectedNodeId))
                {
                    continue;
                }

                GraphNode parentNode = GetNodeById(childNode.EnterPoint.SingleConnectedNodeId);
                if (parentNode == null || parentNode.NextPoint == null)
                {
                    continue;
                }

                float distance = DistanceToConnectionBezier(
                    mousePosition,
                    parentNode.NextPoint.GetCanvasCenter(canvasOffset),
                    childNode.EnterPoint.GetCanvasCenter(canvasOffset));
                if (distance <= hitThreshold && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestType = SelectedLineType.Parent;
                    bestFromId = parentNode.Id;
                    bestToId = childNode.Id;
                    bestInputIndex = -1;
                }
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                GraphNode inputNode = Nodes[i];
                if (inputNode == null)
                {
                    continue;
                }

                IReadOnlyList<ConnectionPoint> inputPoints = inputNode.DataInputPoints;
                for (int j = 0; j < inputPoints.Count; j++)
                {
                    ConnectionPoint inputPoint = inputPoints[j];
                    if (inputPoint == null)
                    {
                        continue;
                    }

                    string sourceNodeId;
                    if (!TryGetBoundSourceNodeIdForInputNode(inputNode, inputPoint, out sourceNodeId))
                    {
                        continue;
                    }

                    GraphNode outputNode = GetNodeById(sourceNodeId);
                    if (outputNode == null || outputNode.DataOutputPoint == null)
                    {
                        continue;
                    }

                    float distance = DistanceToConnectionBezier(
                        mousePosition,
                        outputNode.DataOutputPoint.GetCanvasCenter(canvasOffset),
                        inputPoint.GetCanvasCenter(canvasOffset));
                    if (distance <= hitThreshold && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestType = SelectedLineType.Data;
                        bestFromId = outputNode.Id;
                        bestToId = inputNode.Id;
                        bestInputIndex = j;
                    }
                }
            }

            if (bestType == SelectedLineType.None)
            {
                ClearSelectedLine();
                return false;
            }

            selectedLineType = bestType;
            selectedLineFromNodeId = bestFromId;
            selectedLineToNodeId = bestToId;
            selectedLineInputIndex = bestInputIndex;
            return true;
        }

        public bool DeleteSelectedLine()
        {
            if (selectedLineType == SelectedLineType.None)
            {
                return false;
            }

            bool removed = false;
            if (selectedLineType == SelectedLineType.Parent)
            {
                GraphNode childNode = GetNodeById(selectedLineToNodeId);
                if (childNode != null)
                {
                    removed = SetNodeParentInScene(childNode, null);
                }
            }
            else if (selectedLineType == SelectedLineType.Data)
            {
                GraphNode inputNode = GetNodeById(selectedLineToNodeId);
                if (inputNode != null)
                {
                    IReadOnlyList<ConnectionPoint> inputPoints = inputNode.DataInputPoints;
                    if (selectedLineInputIndex >= 0 && selectedLineInputIndex < inputPoints.Count)
                    {
                        removed = UnbindInputVarFromService(inputNode, inputPoints[selectedLineInputIndex], selectedLineFromNodeId);
                    }
                }
            }

            if (removed && CurrentAsset != null)
            {
                EditorUtility.SetDirty(CurrentAsset);
            }

            ClearSelectedLine();
            return removed;
        }

        private void RemoveNodeObject(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            GameObject nodeObject;
            if (SceneRefManager.Instance.TryGetGameObject(nodeId, out nodeObject) && nodeObject != null)
            {
                Undo.DestroyObjectImmediate(nodeObject);
            }
        }

        public void RemoveConnections(GraphNode node)
        {
            if (NodeList == null || node == null)
            {
                return;
            }

            SetNodeParentInScene(node, null);

            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode current = NodeList[i];
                if (current == null)
                {
                    continue;
                }

                if (current != node && GetMainInputSourceId(current) == node.Id)
                {
                    SetNodeParentInScene(current, null);
                }

                IReadOnlyList<ConnectionPoint> inputPoints = current.DataInputPoints;
                for (int j = 0; j < inputPoints.Count; j++)
                {
                    ConnectionPoint inputPoint = inputPoints[j];
                    if (inputPoint != null)
                    {
                        UnbindInputVarFromService(current, inputPoint, node.Id);
                    }
                }
            }
        }

        public void BringNodeToFront(GraphNode node)
        {
            if (NodeList == null || node == null)
            {
                return;
            }

            if (!NodeList.Remove(node))
            {
                return;
            }

            NodeList.Add(node);
        }

        public GraphNode GetNodeAt(Vector2 mousePosition, Vector2 canvasOffset)
        {
            if (NodeList == null)
            {
                return null;
            }

            for (int i = NodeList.Count - 1; i >= 0; i--)
            {
                if (NodeList[i].GetCanvasRect(canvasOffset).Contains(mousePosition))
                {
                    return NodeList[i];
                }
            }

            return null;
        }

        public GraphNode GetNodeById(string nodeId)
        {
            if (NodeList == null || string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < NodeList.Count; i++)
            {
                if (NodeList[i].Id == nodeId)
                {
                    return NodeList[i];
                }
            }

            return null;
        }

        public bool TryGetNodeObject(GraphNode node, out GameObject nodeObject)
        {
            nodeObject = null;
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                return false;
            }

            return SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) && nodeObject != null;
        }

        public bool TryGetConnectionPointAt(Vector2 mousePosition, Vector2 canvasOffset, out ConnectionPoint point)
        {
            point = null;
            if (NodeList == null)
            {
                return false;
            }

            for (int i = NodeList.Count - 1; i >= 0; i--)
            {
                if (NodeList[i].TryGetPointAt(mousePosition, canvasOffset, out point))
                {
                    return true;
                }
            }

            return false;
        }

        public bool SyncNodeTitlesFromScene()
        {
            if (NodeList == null)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode node = NodeList[i];
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                GameObject nodeObject;
                if (!SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) || nodeObject == null)
                {
                    continue;
                }

                if (node.Title == nodeObject.name)
                {
                    continue;
                }

                node.Title = nodeObject.name;
                changed = true;
            }

            return changed;
        }

        private bool SyncNodesFromSceneHierarchy(bool removeMissingNodes)
        {
            if (NodeList == null)
            {
                return false;
            }

            Root root = UnityEngine.Object.FindObjectOfType<Root>();
            if (root == null)
            {
                return false;
            }

            Graph graph = root.GetComponentInChildren<Graph>();
            if (graph == null)
            {
                return false;
            }

            List<SceneNodeInfo> sceneNodes = CollectSceneNodes(graph.transform);

            Dictionary<string, GraphNode> nodeById = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode existing = NodeList[i];
                if (existing == null || string.IsNullOrEmpty(existing.Id))
                {
                    continue;
                }

                if (!nodeById.ContainsKey(existing.Id))
                {
                    nodeById.Add(existing.Id, existing);
                }
            }

            bool changed = false;
            for (int i = 0; i < sceneNodes.Count; i++)
            {
                SceneNodeInfo info = sceneNodes[i];
                if (nodeById.ContainsKey(info.id))
                {
                    GraphNode existingNode = nodeById[info.id];
                    existingNode.InitConnectionPoints(info.nodeObject);
                    continue;
                }

                GraphNode created = new GraphNode(Vector2.zero, info.serviceType);
                created.Id = info.id;
                created.Title = string.IsNullOrEmpty(info.title)
                    ? (info.serviceType != null ? info.serviceType.Name : "Node")
                    : info.title;
                created.Position = CalculateAutoPosition(info, nodeById);
                created.InitConnectionPoints(info.nodeObject);
                NodeList.Add(created);
                nodeById.Add(created.Id, created);
                changed = true;
            }

            HashSet<string> sceneNodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sceneNodes.Count; i++)
            {
                sceneNodeIds.Add(sceneNodes[i].id);
            }

            if (removeMissingNodes)
            {
                List<GraphNode> removeNodes = new List<GraphNode>();
                for (int i = 0; i < NodeList.Count; i++)
                {
                    GraphNode node = NodeList[i];
                    if (node == null || string.IsNullOrEmpty(node.Id))
                    {
                        removeNodes.Add(node);
                        continue;
                    }

                    if (!sceneNodeIds.Contains(node.Id))
                    {
                        removeNodes.Add(node);
                    }
                }

                if (removeNodes.Count > 0)
                {
                    for (int i = 0; i < removeNodes.Count; i++)
                    {
                        NodeList.Remove(removeNodes[i]);
                    }

                    changed = true;
                }
            }

            nodeById.Clear();
            for (int i = 0; i < NodeList.Count; i++)
            {
                GraphNode existing = NodeList[i];
                if (existing == null || string.IsNullOrEmpty(existing.Id))
                {
                    continue;
                }

                if (!nodeById.ContainsKey(existing.Id))
                {
                    nodeById.Add(existing.Id, existing);
                }
            }

            for (int i = 0; i < sceneNodes.Count; i++)
            {
                SceneNodeInfo info = sceneNodes[i];
                GraphNode node;
                if (!nodeById.TryGetValue(info.id, out node) || node == null || node.EnterPoint == null)
                {
                    continue;
                }

                string currentParentId = node.EnterPoint.SingleConnectedNodeId;
                string expectedParentId = info.parentId;
                if (currentParentId == expectedParentId)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(expectedParentId))
                {
                    node.EnterPoint.ClearConnections();
                }
                else
                {
                    node.EnterPoint.SetSingleConnection(expectedParentId);
                }
                changed = true;
            }

            return changed;
        }

        private List<SceneNodeInfo> CollectSceneNodes(Transform graphTransform)
        {
            List<SceneNodeInfo> result = new List<SceneNodeInfo>();
            if (graphTransform == null)
            {
                return result;
            }

            CollectSceneNodesRecursive(graphTransform, null, result);
            return result;
        }

        private void CollectSceneNodesRecursive(Transform parent, string parentNodeId, List<SceneNodeInfo> output)
        {
            if (parent == null)
            {
                return;
            }

            List<Transform> validChildren = new List<Transform>();
            List<SceneNodeInfo> validInfos = new List<SceneNodeInfo>();
            Dictionary<Transform, string> childNodeIdLookup = new Dictionary<Transform, string>();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                SceneNodeInfo info;
                if (!TryBuildSceneNodeInfo(child, parentNodeId, out info))
                {
                    continue;
                }

                validChildren.Add(child);
                validInfos.Add(info);
                childNodeIdLookup[child] = info.id;
            }

            int siblingCount = validInfos.Count;
            for (int i = 0; i < validInfos.Count; i++)
            {
                validInfos[i].siblingIndex = i;
                validInfos[i].siblingCount = siblingCount;
                output.Add(validInfos[i]);
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string nextParentId = parentNodeId;
                string nodeId;
                if (childNodeIdLookup.TryGetValue(child, out nodeId))
                {
                    nextParentId = nodeId;
                }

                CollectSceneNodesRecursive(child, nextParentId, output);
            }
        }

        private bool TryBuildSceneNodeInfo(Transform nodeTransform, string parentNodeId, out SceneNodeInfo info)
        {
            info = null;
            if (nodeTransform == null)
            {
                return false;
            }

            Service service = GetNodeService(nodeTransform.gameObject);
            if (service == null)
            {
                return false;
            }

            SceneRefObject sceneRef = nodeTransform.GetComponent<SceneRefObject>();
            if (sceneRef == null)
            {
                sceneRef = Undo.AddComponent<SceneRefObject>(nodeTransform.gameObject);
            }

            if (sceneRef == null)
            {
                return false;
            }

            SceneRefManager.Instance.Register(sceneRef);
            if (string.IsNullOrEmpty(sceneRef.Id))
            {
                return false;
            }

            info = new SceneNodeInfo
            {
                id = sceneRef.Id,
                parentId = parentNodeId,
                title = nodeTransform.name,
                serviceType = service.GetType(),
                nodeObject = nodeTransform.gameObject
            };
            return true;
        }

        private Service GetNodeService(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            Service[] services = nodeObject.GetComponents<Service>();
            if (services == null || services.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < services.Length; i++)
            {
                Service service = services[i];
                if (service != null && service.GetType() != typeof(Service))
                {
                    return service;
                }
            }

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] != null)
                {
                    return services[i];
                }
            }

            return null;
        }

        private Vector2 CalculateAutoPosition(SceneNodeInfo info, Dictionary<string, GraphNode> nodeById)
        {
            float offsetIndex = info.siblingIndex - (info.siblingCount - 1) * 0.5f;

            GraphNode parentNode;
            if (!string.IsNullOrEmpty(info.parentId) && nodeById.TryGetValue(info.parentId, out parentNode) && parentNode != null)
            {
                return new Vector2(
                    parentNode.Position.x + offsetIndex * AutoLayoutHorizontalSpacing,
                    parentNode.Position.y + AutoLayoutVerticalSpacing);
            }

            return new Vector2(offsetIndex * AutoLayoutHorizontalSpacing, 20f);
        }

        public bool CanConnect(ConnectionPoint fromPoint, ConnectionPoint toPoint)
        {
            if (fromPoint == null || toPoint == null)
            {
                return false;
            }

            if (fromPoint == toPoint)
            {
                return false;
            }

            if (fromPoint.Node == toPoint.Node)
            {
                return false;
            }

            if (fromPoint.IsInputSide == toPoint.IsInputSide)
            {
                return false;
            }

            ConnectionPoint outputPoint = fromPoint.IsOutputSide ? fromPoint : toPoint;
            ConnectionPoint inputPoint = outputPoint == fromPoint ? toPoint : fromPoint;

            if (outputPoint.PointType == ConnectionPointType.Next && inputPoint.PointType == ConnectionPointType.Enter)
            {
                return true;
            }

            if (outputPoint.PointType == ConnectionPointType.Output && inputPoint.PointType == ConnectionPointType.Input)
            {
                return CanConnectByVariable(outputPoint.Node, inputPoint.Node, inputPoint);
            }

            return false;
        }

        public static string DescribePoint(ConnectionPoint point)
        {
            if (point == null)
            {
                return "null";
            }

            string nodeId = point.Node != null ? point.Node.Id : "null-node";
            return string.Format("{0}[{1}] node={2}", point.PointType, point.Index, nodeId);
        }

        public bool IsPointConnected(ConnectionPoint point)
        {
            if (point == null || point.Node == null)
            {
                return false;
            }

            if (point.PointType == ConnectionPointType.Input)
            {
                string sourceNodeId;
                return TryGetBoundSourceNodeIdForInputNode(point.Node, point, out sourceNodeId);
            }

            if (point.PointType == ConnectionPointType.Output)
            {
                return HasAnyInputBoundToOutputNode(point.Node);
            }

            if (point.PointType == ConnectionPointType.Enter)
            {
                return HasValidParentForNode(point.Node);
            }

            if (point.PointType == ConnectionPointType.Next)
            {
                return HasAnyChildForNode(point.Node);
            }

            return false;
        }

        public bool TryGetDataPointObjectName(ConnectionPoint point, out string objectName)
        {
            objectName = null;
            if (point == null || point.Node == null)
            {
                return false;
            }

            if (point.PointType == ConnectionPointType.Input)
            {
                Service boundService;
                if (!TryGetBoundServiceFromInputNode(point.Node, point, out boundService) || boundService == null)
                {
                    return false;
                }

                if (boundService.gameObject != null)
                {
                    objectName = boundService.gameObject.name;
                    return !string.IsNullOrEmpty(objectName);
                }

                objectName = boundService.name;
                return !string.IsNullOrEmpty(objectName);
            }

            if (point.PointType == ConnectionPointType.Output)
            {
                GameObject outputNodeObject;
                if (TryGetNodeObject(point.Node, out outputNodeObject) && outputNodeObject != null)
                {
                    objectName = outputNodeObject.name;
                    return !string.IsNullOrEmpty(objectName);
                }
            }

            return false;
        }

        private bool UnbindInputVarFromService(GraphNode inputNode, ConnectionPoint inputPoint, string expectedSourceNodeId)
        {
            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            if (!TryGetInputVarBinding(inputNode, inputPoint, out inputService, out inputField, out expectedType, out expectsList)
                || inputService == null
                || inputField == null)
            {
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            MMVar singleVar = inputVarObject as MMVar;
            if (singleVar != null)
            {
                if (singleVar.type != InputType.Service || singleVar.service == null || !IsServiceFromNode(singleVar.service, expectedSourceNodeId))
                {
                    return false;
                }

                Undo.RecordObject(inputService, "Unbind Input Var Service");
                singleVar.type = singleVar.GetFallbackInputType();
                singleVar.service = null;
                inputField.SetValue(inputService, inputVarObject);
                EditorUtility.SetDirty(inputService);
                return true;
            }

            MMListVar listVar = inputVarObject as MMListVar;
            if (listVar != null)
            {
                if (listVar.type != InputType.Service || listVar.service == null || !IsServiceFromNode(listVar.service, expectedSourceNodeId))
                {
                    return false;
                }

                Undo.RecordObject(inputService, "Unbind Input Var Service");
                listVar.type = listVar.GetFallbackInputType();
                listVar.service = null;
                inputField.SetValue(inputService, inputVarObject);
                EditorUtility.SetDirty(inputService);
                return true;
            }

            return false;
        }

        private static bool IsServiceFromNode(Service service, string nodeId)
        {
            if (service == null || string.IsNullOrEmpty(nodeId))
            {
                return false;
            }

            SceneRefObject sceneRef = service.GetComponent<SceneRefObject>();
            return sceneRef != null && sceneRef.Id == nodeId;
        }

        private static float DistanceToConnectionBezier(Vector2 point, Vector2 start, Vector2 end)
        {
            float tangentOffset = Mathf.Max(60f, Mathf.Abs(end.x - start.x) * 0.5f);
            Vector2 startTangent = start + Vector2.right * tangentOffset;
            Vector2 endTangent = end + Vector2.left * tangentOffset;

            const int segments = 24;
            float minDistance = float.MaxValue;
            Vector2 prev = start;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 current = EvaluateBezier(start, startTangent, endTangent, end, t);
                float distance = DistanceToSegment(point, prev, current);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }

                prev = current;
            }

            return minDistance;
        }

        private static Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqr = ab.sqrMagnitude;
            if (sqr <= Mathf.Epsilon)
            {
                return Vector2.Distance(p, a);
            }

            float t = Vector2.Dot(p - a, ab) / sqr;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + ab * t;
            return Vector2.Distance(p, projection);
        }

        public void TryCreateConnection(ConnectionPoint fromPoint, ConnectionPoint toPoint)
        {
            if (fromPoint == null || toPoint == null)
            {
                return;
            }

            if (fromPoint == toPoint)
            {
                return;
            }

            if (fromPoint.Node == null || toPoint.Node == null)
            {
                return;
            }

            if (fromPoint.Node == toPoint.Node)
            {
                return;
            }

            if (fromPoint.IsInputSide == toPoint.IsInputSide)
            {
                return;
            }

            ConnectionPoint outputPoint = fromPoint.IsOutputSide ? fromPoint : toPoint;
            ConnectionPoint inputPoint = outputPoint == fromPoint ? toPoint : fromPoint;
            GraphNode outputNode = outputPoint.Node;
            GraphNode inputNode = inputPoint.Node;

            if (outputPoint.PointType == ConnectionPointType.Next && inputPoint.PointType == ConnectionPointType.Enter)
            {
                bool flowChanged = SetNodeParentInScene(inputNode, outputNode != null ? outputNode.Id : null);
                if (!flowChanged)
                {
                    return;
                }

                if (inputNode != null && inputNode.EnterPoint != null && outputNode != null)
                {
                    inputNode.EnterPoint.SetSingleConnection(outputNode.Id);
                }

                return;
            }

            if (outputPoint.PointType == ConnectionPointType.Output && inputPoint.PointType == ConnectionPointType.Input)
            {
                if (!CanConnectByVariable(outputNode, inputNode, inputPoint))
                {
                    return;
                }

                string bindReason;
                bool bound = BindInputVarToService(inputNode, outputNode, inputPoint, out bindReason);
                if (!bound)
                {
                    return;
                }

                return;
            }

            return;
        }

        private bool CanConnectByVariable(GraphNode outputNode, GraphNode inputNode, ConnectionPoint inputPoint)
        {
            Service outputService;
            FieldInfo outputField;
            Type outputFieldType;
            if (!TryGetOutputBinding(outputNode, out outputService, out outputField, out outputFieldType))
            {
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            if (!TryGetInputVarBinding(inputNode, inputPoint, out inputService, out inputField, out expectedType, out expectsList))
            {
                return false;
            }

            if (expectsList)
            {
                return ServiceOutputUtility.IsListOutputCompatible(outputFieldType, expectedType);
            }

            return ServiceOutputUtility.IsOutputCompatible(outputFieldType, expectedType);
        }

        private bool BindInputVarToService(GraphNode inputNode, GraphNode outputNode, ConnectionPoint inputPoint, out string reason)
        {
            reason = null;
            Service outputService;
            FieldInfo outputField;
            Type outputFieldType;
            if (!TryGetOutputBinding(outputNode, out outputService, out outputField, out outputFieldType) || outputService == null)
            {
                reason = "output binding missing";
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            if (!TryGetInputVarBinding(inputNode, inputPoint, out inputService, out inputField, out expectedType, out expectsList) || inputService == null || inputField == null)
            {
                reason = "input binding missing";
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            if (inputVarObject == null)
            {
                try
                {
                    inputVarObject = Activator.CreateInstance(inputField.FieldType);
                }
                catch
                {
                    inputVarObject = null;
                }
            }

            if (inputVarObject == null)
            {
                reason = "input var instance null";
                return false;
            }

            bool changed = false;
            MMVar singleVar = inputVarObject as MMVar;
            if (singleVar != null)
            {
                changed = singleVar.type != InputType.Service || singleVar.service != outputService;
            }

            MMListVar listVar = inputVarObject as MMListVar;
            if (listVar != null)
            {
                changed = listVar.type != InputType.Service || listVar.service != outputService;
            }

            if (singleVar == null && listVar == null)
            {
                reason = "input field is not MMVar/MMListVar";
                return false;
            }

            if (!changed)
            {
                reason = "already bound";
                return true;
            }

            Undo.RecordObject(inputService, "Bind Input Var Service");
            if (singleVar != null)
            {
                singleVar.type = InputType.Service;
                singleVar.service = outputService;
            }

            if (listVar != null)
            {
                listVar.type = InputType.Service;
                listVar.service = outputService;
            }

            inputField.SetValue(inputService, inputVarObject);
            EditorUtility.SetDirty(inputService);
            reason = "updated";
            return true;
        }

        private bool TryGetOutputBinding(GraphNode outputNode, out Service outputService, out FieldInfo outputField, out Type outputFieldType)
        {
            outputService = null;
            outputField = null;
            outputFieldType = null;
            if (outputNode == null)
            {
                return false;
            }

            GameObject outputNodeObject;
            if (!TryGetNodeObject(outputNode, out outputNodeObject) || outputNodeObject == null)
            {
                return false;
            }

            outputService = GetNodeService(outputNodeObject);
            if (outputService == null)
            {
                return false;
            }

            string error;
            if (!ServiceOutputUtility.TryGetOutputField(outputService.GetType(), out outputField, out error) || outputField == null)
            {
                return false;
            }

            outputFieldType = outputField.FieldType;
            return outputFieldType != null;
        }

        private bool TryGetInputVarBinding(GraphNode inputNode, ConnectionPoint inputPoint, out Service inputService, out FieldInfo inputField, out Type expectedType, out bool expectsList)
        {
            inputService = null;
            inputField = null;
            expectedType = null;
            expectsList = false;
            if (inputNode == null)
            {
                return false;
            }

            GameObject inputNodeObject;
            if (!TryGetNodeObject(inputNode, out inputNodeObject) || inputNodeObject == null)
            {
                return false;
            }

            inputService = GetNodeService(inputNodeObject);
            if (inputService == null)
            {
                return false;
            }

            List<FieldInfo> inputFields = FindTaggedFields(inputService.GetType(), typeof(InputAttribute));
            if (inputFields.Count == 0)
            {
                return false;
            }

            int inputIndex = 0;
            if (inputPoint != null)
            {
                int resolvedIndex = inputNode.GetInputPointIndex(inputPoint);
                if (resolvedIndex < 0 || resolvedIndex >= inputFields.Count)
                {
                    return false;
                }

                inputIndex = resolvedIndex;
            }

            inputField = inputFields[inputIndex];
            return TryGetVarValueType(inputField.FieldType, out expectedType, out expectsList);
        }

        private static FieldInfo FindTaggedField(Type serviceType, Type attributeType)
        {
            if (serviceType == null || attributeType == null)
            {
                return null;
            }

            FieldInfo[] fields = serviceType.GetFields(ServiceFieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field != null && field.IsDefined(attributeType, true))
                {
                    return field;
                }
            }

            return null;
        }

        private static List<FieldInfo> FindTaggedFields(Type serviceType, Type attributeType)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            if (serviceType == null || attributeType == null)
            {
                return result;
            }

            FieldInfo[] fields = serviceType.GetFields(ServiceFieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field != null && field.IsDefined(attributeType, true))
                {
                    result.Add(field);
                }
            }

            result.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            return result;
        }

        private static bool TryGetVarValueType(Type varFieldType, out Type valueType, out bool isList)
        {
            valueType = null;
            isList = false;
            if (TryResolveValueTypeFromGetMethod(varFieldType, out Type methodValueType, out bool methodIsList))
            {
                valueType = methodValueType;
                isList = methodIsList;
                return valueType != null;
            }

            Type current = varFieldType;
            while (current != null)
            {
                if (current.IsGenericType)
                {
                    Type genericTypeDefinition = current.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(MMVar<>))
                    {
                        Type[] arguments = current.GetGenericArguments();
                        valueType = arguments != null && arguments.Length == 1 ? arguments[0] : null;
                        isList = false;
                        return valueType != null;
                    }

                    if (genericTypeDefinition == typeof(MMListVar<>))
                    {
                        Type[] arguments = current.GetGenericArguments();
                        valueType = arguments != null && arguments.Length == 1 ? arguments[0] : null;
                        isList = true;
                        return valueType != null;
                    }
                }

                current = current.BaseType;
            }

            return false;
        }

        private static bool TryResolveValueTypeFromGetMethod(Type type, out Type valueType, out bool isListType)
        {
            valueType = null;
            isListType = false;
            if (type == null)
            {
                return false;
            }

            MethodInfo getter = type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (getter == null)
            {
                return false;
            }

            Type returnType = getter.ReturnType;
            if (returnType == null || returnType == typeof(void))
            {
                return false;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type[] genericArguments = returnType.GetGenericArguments();
                if (genericArguments != null && genericArguments.Length == 1)
                {
                    valueType = genericArguments[0];
                    isListType = true;
                    return true;
                }

                return false;
            }

            valueType = returnType;
            return true;
        }

        private void SyncNodeParentToScene(GraphNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                return;
            }

            GameObject nodeObject;
            if (!SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) || nodeObject == null)
            {
                return;
            }

            Transform targetParent = ResolveNodeParentTransform(node);
            if (targetParent == null || nodeObject.transform.parent == targetParent)
            {
                return;
            }

            if (targetParent.IsChildOf(nodeObject.transform))
            {
                return;
            }

            Undo.SetTransformParent(nodeObject.transform, targetParent, "Set Node Parent");
            nodeObject.transform.localPosition = Vector3.zero;
            nodeObject.transform.localRotation = Quaternion.identity;
            nodeObject.transform.localScale = Vector3.one;
        }

        private bool SetNodeParentInScene(GraphNode childNode, string parentNodeId)
        {
            if (childNode == null || string.IsNullOrEmpty(childNode.Id))
            {
                return false;
            }

            GameObject childObject;
            if (!SceneRefManager.Instance.TryGetGameObject(childNode.Id, out childObject) || childObject == null)
            {
                return false;
            }

            Transform targetParent = null;
            if (!string.IsNullOrEmpty(parentNodeId))
            {
                GameObject parentObject;
                if (!SceneRefManager.Instance.TryGetGameObject(parentNodeId, out parentObject) || parentObject == null)
                {
                    return false;
                }

                targetParent = parentObject.transform;
                if (targetParent == childObject.transform || targetParent.IsChildOf(childObject.transform))
                {
                    return false;
                }
            }
            else
            {
                Graph graph = UnityEngine.Object.FindObjectOfType<Graph>();
                if (graph == null)
                {
                    return false;
                }

                targetParent = graph.transform;
            }

            if (childObject.transform.parent == targetParent)
            {
                return false;
            }

            Undo.SetTransformParent(childObject.transform, targetParent, "Set Node Parent");
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;
            return true;
        }

        private static string GetMainInputSourceId(GraphNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                return null;
            }

            GameObject nodeObject;
            if (!SceneRefManager.Instance.TryGetGameObject(node.Id, out nodeObject) || nodeObject == null)
            {
                return null;
            }

            Transform parent = nodeObject.transform.parent;
            if (parent == null)
            {
                return null;
            }

            SceneRefObject parentRef = parent.GetComponent<SceneRefObject>();
            return parentRef != null ? parentRef.Id : null;
        }

        private bool HasAnyInputBoundToOutputNode(GraphNode outputNode)
        {
            if (outputNode == null || Nodes == null)
            {
                return false;
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                GraphNode candidate = Nodes[i];
                if (candidate == null)
                {
                    continue;
                }

                IReadOnlyList<ConnectionPoint> inputPoints = candidate.DataInputPoints;
                for (int j = 0; j < inputPoints.Count; j++)
                {
                    string sourceNodeId;
                    if (TryGetBoundSourceNodeIdForInputNode(candidate, inputPoints[j], out sourceNodeId) && sourceNodeId == outputNode.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasValidParentForNode(GraphNode childNode)
        {
            if (childNode == null || childNode.EnterPoint == null)
            {
                return false;
            }

            string parentId = GetMainInputSourceId(childNode);
            if (string.IsNullOrEmpty(parentId))
            {
                return false;
            }

            GraphNode parentNode = GetNodeById(parentId);
            return parentNode != null && parentNode.NextPoint != null;
        }

        private bool HasAnyChildForNode(GraphNode parentNode)
        {
            if (parentNode == null || Nodes == null || parentNode.NextPoint == null)
            {
                return false;
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                GraphNode childNode = Nodes[i];
                if (childNode == null || childNode.EnterPoint == null)
                {
                    continue;
                }

                if (GetMainInputSourceId(childNode) == parentNode.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetBoundSourceNodeIdForInputNode(GraphNode inputNode, ConnectionPoint inputPoint, out string sourceNodeId)
        {
            sourceNodeId = null;
            if (inputNode == null || inputPoint == null)
            {
                return false;
            }

            Service boundService;
            if (!TryGetBoundServiceFromInputNode(inputNode, inputPoint, out boundService) || boundService == null)
            {
                return false;
            }

            SceneRefObject sourceRef = boundService.GetComponent<SceneRefObject>();
            if (sourceRef == null || string.IsNullOrEmpty(sourceRef.Id))
            {
                return false;
            }

            GraphNode sourceNode = GetNodeById(sourceRef.Id);
            if (sourceNode == null || sourceNode.DataOutputPoint == null)
            {
                return false;
            }

            sourceNodeId = sourceRef.Id;
            return true;
        }

        private bool TryGetBoundServiceFromInputNode(GraphNode inputNode, ConnectionPoint inputPoint, out Service boundService)
        {
            boundService = null;
            if (inputNode == null)
            {
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            if (!TryGetInputVarBinding(inputNode, inputPoint, out inputService, out inputField, out expectedType, out expectsList) || inputField == null)
            {
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            MMVar singleVar = inputVarObject as MMVar;
            if (singleVar != null)
            {
                if (singleVar.type == InputType.Service && singleVar.service != null)
                {
                    boundService = singleVar.service;
                    return true;
                }

                return false;
            }

            MMListVar listVar = inputVarObject as MMListVar;
            if (listVar != null)
            {
                if (listVar.type == InputType.Service && listVar.service != null)
                {
                    boundService = listVar.service;
                    return true;
                }

                return false;
            }

            return false;
        }

        private Transform ResolveNodeParentTransform(GraphNode node)
        {
            if (node == null)
            {
                return null;
            }

            string mainInputSourceId = GetMainInputSourceId(node);
            if (!string.IsNullOrEmpty(mainInputSourceId))
            {
                GameObject parentObject;
                if (SceneRefManager.Instance.TryGetGameObject(mainInputSourceId, out parentObject) && parentObject != null)
                {
                    return parentObject.transform;
                }
            }

            Graph graph = UnityEngine.Object.FindObjectOfType<Graph>();
            return graph != null ? graph.transform : null;
        }
    }
}
