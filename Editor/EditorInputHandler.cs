using System;
using System.Collections.Generic;
using System.IO;
using SP;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    /// <summary>
    /// 节点编辑器输入处理器。
    /// 负责所有鼠标事件与快捷键事件，不参与任何绘制。
    /// </summary>
    internal sealed class EditorInputHandler
    {
        private const string ServiceLibRootPath = "Assets/PlayableFramework/Lib/Scripts/Service";
        private const string DataLibRootPath = "Assets/PlayableFramework/Lib/Scripts/Data";
        private static readonly Color FolderMenuBackgroundColor = new Color(0.95f, 0.73f, 0.33f, 1f);

        private sealed class ServiceMenuEntry
        {
            public string MenuPath;
            public Type ServiceType;
        }

        private sealed class ServiceMenuFolderNode
        {
            public readonly SortedDictionary<string, ServiceMenuFolderNode> Folders =
                new SortedDictionary<string, ServiceMenuFolderNode>(StringComparer.OrdinalIgnoreCase);

            public readonly List<ServiceMenuEntry> Services = new List<ServiceMenuEntry>();
        }

        private sealed class ServiceMenuPopupContent : PopupWindowContent
        {
            private readonly ServiceMenuFolderNode root = new ServiceMenuFolderNode();
            private readonly string rootLabel;
            private readonly Action<Type> onSelectService;
            private readonly List<string> currentPath = new List<string>();
            private Vector2 scrollPosition;

            public ServiceMenuPopupContent(string rootLabel, List<ServiceMenuEntry> entries, Action<Type> onSelectService)
            {
                this.rootLabel = rootLabel;
                this.onSelectService = onSelectService;
                BuildTree(entries);
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(320f, 360f);
            }

            public override void OnGUI(Rect rect)
            {
                DrawBreadcrumb();
                EditorGUILayout.Space(4f);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawParentFolderRow();
                DrawFolderRows();
                DrawServiceRows();
                EditorGUILayout.EndScrollView();
            }

            private void DrawBreadcrumb()
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(rootLabel, EditorStyles.miniButtonLeft))
                {
                    currentPath.Clear();
                }

                for (int i = 0; i < currentPath.Count; i++)
                {
                    GUILayout.Label(">", GUILayout.Width(10f));
                    int index = i;
                    if (GUILayout.Button(currentPath[i], EditorStyles.miniButtonMid))
                    {
                        currentPath.RemoveRange(index + 1, currentPath.Count - (index + 1));
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            private void DrawParentFolderRow()
            {
                if (currentPath.Count == 0)
                {
                    return;
                }

                if (GUILayout.Button("..", GUILayout.Height(22f)))
                {
                    currentPath.RemoveAt(currentPath.Count - 1);
                }
            }

            private void DrawFolderRows()
            {
                ServiceMenuFolderNode node = GetCurrentNode();
                if (node == null || node.Folders.Count == 0)
                {
                    return;
                }

                Color originalBackground = GUI.backgroundColor;
                GUI.backgroundColor = FolderMenuBackgroundColor;
                foreach (KeyValuePair<string, ServiceMenuFolderNode> folder in node.Folders)
                {
                    string folderName = folder.Key;
                    if (GUILayout.Button(folderName + "  >", GUILayout.Height(22f)))
                    {
                        currentPath.Add(folderName);
                    }
                }

                GUI.backgroundColor = originalBackground;
            }

            private void DrawServiceRows()
            {
                ServiceMenuFolderNode node = GetCurrentNode();
                if (node == null || node.Services.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < node.Services.Count; i++)
                {
                    ServiceMenuEntry entry = node.Services[i];
                    string displayName = GetDisplayName(entry.MenuPath);
                    if (!GUILayout.Button(displayName, GUILayout.Height(22f)))
                    {
                        continue;
                    }

                    onSelectService?.Invoke(entry.ServiceType);
                    editorWindow?.Close();
                }
            }

            private void BuildTree(List<ServiceMenuEntry> entries)
            {
                if (entries == null)
                {
                    return;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    ServiceMenuEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.MenuPath) || entry.ServiceType == null)
                    {
                        continue;
                    }

                    string[] parts = entry.MenuPath.Split('/');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    ServiceMenuFolderNode node = root;
                    for (int p = 1; p < parts.Length - 1; p++)
                    {
                        string folder = parts[p];
                        if (string.IsNullOrEmpty(folder))
                        {
                            continue;
                        }

                        ServiceMenuFolderNode child;
                        if (!node.Folders.TryGetValue(folder, out child))
                        {
                            child = new ServiceMenuFolderNode();
                            node.Folders.Add(folder, child);
                        }

                        node = child;
                    }

                    node.Services.Add(entry);
                }

                SortServicesRecursive(root);
            }

            private static void SortServicesRecursive(ServiceMenuFolderNode node)
            {
                node.Services.Sort((a, b) =>
                    string.Compare(GetDisplayName(a.MenuPath), GetDisplayName(b.MenuPath), StringComparison.OrdinalIgnoreCase));

                foreach (KeyValuePair<string, ServiceMenuFolderNode> folder in node.Folders)
                {
                    SortServicesRecursive(folder.Value);
                }
            }

            private ServiceMenuFolderNode GetCurrentNode()
            {
                ServiceMenuFolderNode node = root;
                for (int i = 0; i < currentPath.Count; i++)
                {
                    ServiceMenuFolderNode child;
                    if (!node.Folders.TryGetValue(currentPath[i], out child))
                    {
                        return null;
                    }

                    node = child;
                }

                return node;
            }

            private static string GetDisplayName(string menuPath)
            {
                if (string.IsNullOrEmpty(menuPath))
                {
                    return string.Empty;
                }

                int lastSlashIndex = menuPath.LastIndexOf('/');
                return lastSlashIndex >= 0 ? menuPath.Substring(lastSlashIndex + 1) : menuPath;
            }
        }

        private readonly Action<GUIContent> showNotification;

        private Vector2 canvasDragStart;
        private bool isDraggingCanvas;

        private GraphNode draggingNode;
        private Vector2 nodeDragOffset;

        // 渲染器绘制预览连线时需要读取
        public Vector2 CanvasOffset { get; private set; }

        public EditorInputHandler(Action<GUIContent> showNotification)
        {
            this.showNotification = showNotification;
        }

        public void ProcessEvents(Event e)
        {
            if (e == null)
            {
                return;
            }

            ProcessShortcuts(e);
            ProcessCanvasEvents(e);
            ProcessConnectionEvents(e);
            ProcessNodeEvents(e);
            ProcessContextMenu(e);
        }

        private void ProcessShortcuts(Event e)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (e.type != EventType.KeyDown)
            {
                return;
            }

            GraphNode selectedNode = GetSelectedNodeFromManagerQueue();
            if ((e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) && selectedNode != null)
            {
                manager.RemoveNode(selectedNode);
                manager.ClearSelectedNode();
                manager.ClearSelectedLine();
                GUI.changed = true;
                e.Use();
                return;
            }

            if ((e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) && manager.HasSelectedLine)
            {
                if (manager.DeleteSelectedLine())
                {
                    GUI.changed = true;
                }
                e.Use();
                return;
            }

            if (!e.control || e.keyCode != KeyCode.S)
            {
                return;
            }

            manager.SaveGraph();
            showNotification?.Invoke(new GUIContent("Node graph saved"));
            e.Use();
        }

        private void ProcessCanvasEvents(Event e)
        {
            if (e.button != 2)
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    isDraggingCanvas = true;
                    canvasDragStart = e.mousePosition;
                    GUI.FocusControl(null);
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (!isDraggingCanvas)
                    {
                        return;
                    }

                    CanvasOffset += e.mousePosition - canvasDragStart;
                    canvasDragStart = e.mousePosition;
                    GUI.changed = true;
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (!isDraggingCanvas)
                    {
                        return;
                    }

                    isDraggingCanvas = false;
                    e.Use();
                    break;
            }
        }

        private void ProcessConnectionEvents(Event e)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (isDraggingCanvas)
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != 0)
                    {
                        return;
                    }

                    ConnectionPoint downPoint;
                    if (!manager.TryGetConnectionPointAt(e.mousePosition, CanvasOffset, out downPoint))
                    {
                        return;
                    }

                    manager.ClearSelectedLine();
                    manager.DraggingConnectionPoint = downPoint;
                    manager.DraggingConnectionMousePosition = e.mousePosition;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (e.button != 0 || manager.DraggingConnectionPoint == null)
                    {
                        return;
                    }

                    manager.DraggingConnectionMousePosition = e.mousePosition;
                    GUI.changed = true;
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (e.button != 0 || manager.DraggingConnectionPoint == null)
                    {
                        return;
                    }

                    ConnectionPoint dragStartPoint = manager.DraggingConnectionPoint;
                    ConnectionPoint upPoint;
                    if (manager.TryGetConnectionPointAt(e.mousePosition, CanvasOffset, out upPoint))
                    {
                        manager.TryCreateConnection(dragStartPoint, upPoint);
                    }

                    manager.DraggingConnectionPoint = null;
                    GUI.changed = true;
                    e.Use();
                    break;
            }
        }

        private void ProcessNodeEvents(Event e)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (isDraggingCanvas || manager.DraggingConnectionPoint != null)
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != 0)
                    {
                        return;
                    }

                    draggingNode = GetNodeAtFromManagerQueue(e.mousePosition);
                    if (draggingNode == null)
                    {
                        manager.ClearSelectedNode();
                        if (manager.TrySelectLineAt(e.mousePosition, CanvasOffset))
                        {
                            GUI.changed = true;
                            e.Use();
                            return;
                        }

                        manager.ClearSelectedLine();
                        return;
                    }

                    manager.SelectSingleNode(draggingNode);
                    manager.ClearSelectedLine();
                    manager.BringNodeToFront(draggingNode);
                    SelectGameObjectFromNode(draggingNode);
                    nodeDragOffset = e.mousePosition - draggingNode.GetCanvasRect(CanvasOffset).position;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (e.button != 0 || draggingNode == null)
                    {
                        return;
                    }

                    draggingNode.Position = e.mousePosition - nodeDragOffset - CanvasOffset;
                    GUI.changed = true;
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (draggingNode == null)
                    {
                        return;
                    }

                    draggingNode = null;
                    manager.SaveGraph();
                    e.Use();
                    break;
            }
        }

        private void ProcessContextMenu(Event e)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (e.type != EventType.ContextClick)
            {
                return;
            }

            Vector2 mousePosition = e.mousePosition;
            GraphNode clickedNode = GetNodeAtFromManagerQueue(mousePosition);

            GenericMenu menu = new GenericMenu();

            if (clickedNode != null)
            {
                menu.AddDisabledItem(new GUIContent("Node"));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Delete Node"), false, () =>
                {
                    manager.RemoveNode(clickedNode);
                    GUI.changed = true;
                });
                menu.AddItem(new GUIContent("Delete Links"), false, () =>
                {
                    manager.RemoveConnections(clickedNode);
                    GUI.changed = true;
                });
                menu.AddSeparator(string.Empty);
            }

            // menu.AddItem(new GUIContent("Create/Empty Node"), false, () =>
            // {
            //     manager.CreateNode("Node", null, mousePosition - CanvasOffset);
            //     GUI.changed = true;
            // });
            AddServiceCreateItems(menu, mousePosition);

            menu.ShowAsContext();
            e.Use();
        }

        private void SelectGameObjectFromNode(GraphNode node)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return;
            }

            GameObject nodeObject;
            if (!manager.TryGetNodeObject(node, out nodeObject) || nodeObject == null)
            {
                return;
            }

            if (Selection.activeGameObject == nodeObject)
            {
                return;
            }

            Selection.activeGameObject = nodeObject;
        }

        private GraphNode GetNodeAtFromManagerQueue(Vector2 mousePosition)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var nodes = manager.Nodes;
            if (nodes == null)
            {
                return null;
            }

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                GraphNode node = nodes[i];
                if (node != null && node.GetCanvasRect(CanvasOffset).Contains(mousePosition))
                {
                    return node;
                }
            }

            return null;
        }

        private GraphNode GetSelectedNodeFromManagerQueue()
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var nodes = manager.Nodes;
            if (nodes == null)
            {
                return null;
            }

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                GraphNode node = nodes[i];
                if (node != null && node.IsSelected)
                {
                    return node;
                }
            }

            return null;
        }

        private void AddServiceCreateItems(GenericMenu menu, Vector2 mousePosition)
        {
            bool hasAnyEntry = false;

            List<ServiceMenuEntry> serviceEntries = BuildTypeMenuEntries(ServiceLibRootPath, "Service");
            if (serviceEntries.Count > 0)
            {
                hasAnyEntry = true;
                menu.AddItem(new GUIContent("Service"), false, () => ShowTypeCreatePopup("Service", serviceEntries, mousePosition));
            }

            List<ServiceMenuEntry> dataEntries = BuildTypeMenuEntries(DataLibRootPath, "Data");
            if (dataEntries.Count > 0)
            {
                hasAnyEntry = true;
                menu.AddItem(new GUIContent("Data"), false, () => ShowTypeCreatePopup("Data", dataEntries, mousePosition));
            }

            if (!hasAnyEntry)
            {
                menu.AddDisabledItem(new GUIContent("Service/Data (No Entry Found)"));
            }
        }

        private void ShowTypeCreatePopup(string rootLabel, List<ServiceMenuEntry> entries, Vector2 mousePosition)
        {
            Vector2 screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);
            Rect popupAnchor = new Rect(screenPosition.x, screenPosition.y, 1f, 1f);
            PopupWindow.Show(popupAnchor, new ServiceMenuPopupContent(rootLabel, entries, serviceType =>
            {
                GraphManager manager = GraphManager.Instance;
                if (manager == null)
                {
                    return;
                }

                manager.CreateNode(serviceType, mousePosition - CanvasOffset);
                GUI.changed = true;
            }));
        }

        private static List<ServiceMenuEntry> BuildTypeMenuEntries(string rootPath, string rootLabel)
        {
            List<ServiceMenuEntry> entries = new List<ServiceMenuEntry>();
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { rootPath });

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script == null)
                {
                    continue;
                }

                Type type = script.GetClass();
                if (type == null || type.IsAbstract || !typeof(Service).IsAssignableFrom(type) || type == typeof(Service))
                {
                    continue;
                }

                string normalizedPath = assetPath.Replace('\\', '/');
                string normalizedRoot = rootPath.Replace('\\', '/');
                if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = normalizedPath.Substring(normalizedRoot.Length).TrimStart('/');
                string relativeDirectory = Path.GetDirectoryName(relativePath);
                if (string.IsNullOrEmpty(relativeDirectory))
                {
                    entries.Add(new ServiceMenuEntry
                    {
                        MenuPath = rootLabel + "/" + type.Name,
                        ServiceType = type
                    });
                    continue;
                }

                relativeDirectory = relativeDirectory.Replace('\\', '/');
                entries.Add(new ServiceMenuEntry
                {
                    MenuPath = rootLabel + "/" + relativeDirectory + "/" + type.Name,
                    ServiceType = type
                });
            }

            entries.Sort((a, b) => string.Compare(a.MenuPath, b.MenuPath, StringComparison.OrdinalIgnoreCase));
            return entries;
        }
    }
}
