using System;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class SceneNodeFactory
    {
        public static GameObject CreateSceneNode(Type serviceType)
        {
            if (serviceType == null || !typeof(Component).IsAssignableFrom(serviceType))
            {
                return null;
            }

            Root root = GameObjectOperator.FindRoot();
            if (root == null)
            {
                GameObject rootObject = new GameObject("Root");
                Undo.RegisterCreatedObjectUndo(rootObject, "Create Root");
                root = Undo.AddComponent<Root>(rootObject);
            }

            ResourceCenter resourceCenter = GameObjectOperator.EnsureComponentChild<ResourceCenter>(root.transform, "ResourceCenter");
            GlobalContext globalContext = GameObjectOperator.EnsureComponentChild<GlobalContext>(root.transform, "GlobalContext");
            GlobalAudioManager audioManager = GameObjectOperator.EnsureComponentChild<GlobalAudioManager>(root.transform, "GlobalAudioManager");

            root.resourceCenter = resourceCenter;
            root.audioManager = audioManager;
            EditorUtility.SetDirty(root);

            Graph graph = GameObjectOperator.FindGraph();
            if (graph == null)
            {
                GameObject graphObject = new GameObject("Graph");
                Undo.RegisterCreatedObjectUndo(graphObject, "Create Graph");
                graphObject.transform.SetParent(root.transform, false);
                graph = Undo.AddComponent<Graph>(graphObject);
            }

            EnsureSceneNodeId(graph.gameObject);

            GameObject nodeObject = graph.CreateNodeObject();

            Undo.RegisterCreatedObjectUndo(nodeObject, "Create Node");
            Undo.AddComponent<SceneRefObject>(nodeObject);

            Component component = nodeObject.GetComponent(serviceType);
            if (component == null)
            {
                component = Undo.AddComponent(nodeObject, serviceType);
            }

            Transform parentTransform = ResolveParentTransform(root.transform, graph.transform, component);
            nodeObject.transform.SetParent(parentTransform, false);

            if (component is IGroupNode && parentTransform != null && parentTransform != graph.transform)
            {
                Undo.RecordObject(graph, "Register Group Parent");
                graph.RegisterGroupParent(parentTransform);
                EditorUtility.SetDirty(graph);
            }

            nodeObject.name = serviceType.Name;
            Selection.activeGameObject = nodeObject;
            return nodeObject;
        }

        private static Transform ResolveParentTransform(Transform rootTransform, Transform graphTransform, Component component)
        {
            if (rootTransform == null || graphTransform == null)
            {
                return null;
            }

            if (component is IGroupNode groupNode)
            {
                string parentName = string.IsNullOrWhiteSpace(groupNode.GroupParentName)
                    ? null
                    : groupNode.GroupParentName.Trim();
                if (string.IsNullOrEmpty(parentName))
                {
                    return graphTransform;
                }

                return GameObjectOperator.EnsureChildTransform(rootTransform, parentName);
            }

            return graphTransform;
        }

        public static string GetSceneNodeId(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            SceneRefObject sceneRefObject = nodeObject.GetComponent<SceneRefObject>();
            return sceneRefObject != null ? sceneRefObject.Id : null;
        }

        public static string EnsureSceneNodeId(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            SceneRefObject sceneRefObject = nodeObject.GetComponent<SceneRefObject>();
            if (sceneRefObject == null)
            {
                sceneRefObject = Undo.AddComponent<SceneRefObject>(nodeObject);
            }

            return sceneRefObject != null ? sceneRefObject.Id : null;
        }
    }
}
