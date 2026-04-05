using System;
using System.Collections.Generic;
using System.IO;
using SP;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class EditorUITypeMenu
    {
        private const string ServiceLibRootPath = "Assets/PlayableFramework/Lib/Scripts/Service";
        private const string DataLibRootPath = "Assets/PlayableFramework/Lib/Scripts/Data";
        private static readonly Color FolderMenuBackgroundColor = new Color(0.95f, 0.73f, 0.33f, 1f);

        private sealed class MenuEntry
        {
            public string MenuPath;
            public Type TargetType;
        }

        private sealed class MenuFolderNode
        {
            public readonly SortedDictionary<string, MenuFolderNode> Folders =
                new SortedDictionary<string, MenuFolderNode>(StringComparer.OrdinalIgnoreCase);

            public readonly List<MenuEntry> Entries = new List<MenuEntry>();
        }

        private sealed class TypeMenuPopupContent : PopupWindowContent
        {
            private readonly MenuFolderNode root = new MenuFolderNode();
            private readonly string rootLabel;
            private readonly Action<Type> onSelectType;
            private readonly List<string> currentPath = new List<string>();
            private Vector2 scrollPosition;

            public TypeMenuPopupContent(string rootLabel, List<MenuEntry> entries, Action<Type> onSelectType)
            {
                this.rootLabel = rootLabel;
                this.onSelectType = onSelectType;
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
                DrawEntryRows();
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
                MenuFolderNode node = GetCurrentNode();
                if (node == null || node.Folders.Count == 0)
                {
                    return;
                }

                Color originalBackground = GUI.backgroundColor;
                GUI.backgroundColor = FolderMenuBackgroundColor;

                foreach (KeyValuePair<string, MenuFolderNode> folder in node.Folders)
                {
                    if (GUILayout.Button(folder.Key + "  >", GUILayout.Height(22f)))
                    {
                        currentPath.Add(folder.Key);
                    }
                }

                GUI.backgroundColor = originalBackground;
            }

            private void DrawEntryRows()
            {
                MenuFolderNode node = GetCurrentNode();
                if (node == null || node.Entries.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < node.Entries.Count; i++)
                {
                    MenuEntry entry = node.Entries[i];
                    string displayName = GetDisplayName(entry.MenuPath);

                    if (!GUILayout.Button(displayName, GUILayout.Height(22f)))
                    {
                        continue;
                    }

                    onSelectType?.Invoke(entry.TargetType);
                    editorWindow?.Close();
                }
            }

            private void BuildTree(List<MenuEntry> entries)
            {
                if (entries == null)
                {
                    return;
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    MenuEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.MenuPath) || entry.TargetType == null)
                    {
                        continue;
                    }

                    string[] parts = entry.MenuPath.Split('/');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    MenuFolderNode node = root;

                    for (int p = 1; p < parts.Length - 1; p++)
                    {
                        string folder = parts[p];
                        if (string.IsNullOrEmpty(folder))
                        {
                            continue;
                        }

                        MenuFolderNode child;
                        if (!node.Folders.TryGetValue(folder, out child))
                        {
                            child = new MenuFolderNode();
                            node.Folders.Add(folder, child);
                        }

                        node = child;
                    }

                    node.Entries.Add(entry);
                }

                SortEntriesRecursive(root);
            }

            private static void SortEntriesRecursive(MenuFolderNode node)
            {
                node.Entries.Sort((a, b) =>
                    string.Compare(GetDisplayName(a.MenuPath), GetDisplayName(b.MenuPath), StringComparison.OrdinalIgnoreCase));

                foreach (KeyValuePair<string, MenuFolderNode> folder in node.Folders)
                {
                    SortEntriesRecursive(folder.Value);
                }
            }

            private MenuFolderNode GetCurrentNode()
            {
                MenuFolderNode node = root;

                for (int i = 0; i < currentPath.Count; i++)
                {
                    MenuFolderNode child;
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

        public static void ShowCreateMenu(Vector2 mouseScreenPosition, Action<Type> onSelectType)
        {
            GenericMenu menu = new GenericMenu();
            bool hasAnyEntry = false;

            List<MenuEntry> serviceEntries = BuildTypeMenuEntries(ServiceLibRootPath, "Service");
            if (serviceEntries.Count > 0)
            {
                hasAnyEntry = true;
                menu.AddItem(new GUIContent("Service"), false, () => ShowTypePopup("Service", serviceEntries, mouseScreenPosition, onSelectType));
            }

            List<MenuEntry> dataEntries = BuildTypeMenuEntries(DataLibRootPath, "Data");
            if (dataEntries.Count > 0)
            {
                hasAnyEntry = true;
                menu.AddItem(new GUIContent("Data"), false, () => ShowTypePopup("Data", dataEntries, mouseScreenPosition, onSelectType));
            }

            if (!hasAnyEntry)
            {
                menu.AddDisabledItem(new GUIContent("Service/Data (No Entry Found)"));
            }

            menu.ShowAsContext();
        }

        private static void ShowTypePopup(string rootLabel, List<MenuEntry> entries, Vector2 mouseScreenPosition, Action<Type> onSelectType)
        {
            Rect popupAnchor = new Rect(mouseScreenPosition.x, mouseScreenPosition.y, 1f, 1f);
            PopupWindow.Show(popupAnchor, new TypeMenuPopupContent(rootLabel, entries, onSelectType));
        }

        private static List<MenuEntry> BuildTypeMenuEntries(string rootPath, string rootLabel)
        {
            List<MenuEntry> entries = new List<MenuEntry>();
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
                    entries.Add(new MenuEntry
                    {
                        MenuPath = rootLabel + "/" + type.Name,
                        TargetType = type
                    });
                }
                else
                {
                    relativeDirectory = relativeDirectory.Replace('\\', '/');
                    entries.Add(new MenuEntry
                    {
                        MenuPath = rootLabel + "/" + relativeDirectory + "/" + type.Name,
                        TargetType = type
                    });
                }
            }

            entries.Sort((a, b) => string.Compare(a.MenuPath, b.MenuPath, StringComparison.OrdinalIgnoreCase));
            return entries;
        }
    }
}
