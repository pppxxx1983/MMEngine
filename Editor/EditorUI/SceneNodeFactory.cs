using System;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class SceneNodeFactory
    {
        private const string GroupGraphParentName = "Group";
        private const string SpecialGraphParentName = "Special";

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

            Transform parentTransform = graph.transform;
            if (typeof(IGroupNode).IsAssignableFrom(serviceType))
            {
                parentTransform = GameObjectOperator.EnsureChildTransform(graph.transform, GroupGraphParentName);
            }
            else if (typeof(ISpecialNode).IsAssignableFrom(serviceType))
            {
                parentTransform = GameObjectOperator.EnsureChildTransform(graph.transform, SpecialGraphParentName);
            }

            GameObject nodeObject = graph.CreateNodeObject();
            nodeObject.transform.SetParent(parentTransform, false);

            Undo.RegisterCreatedObjectUndo(nodeObject, "Create Node");
            Undo.AddComponent<SceneRefObject>(nodeObject);

            Component component = nodeObject.GetComponent(serviceType);
            if (component == null)
            {
                component = Undo.AddComponent(nodeObject, serviceType);
            }

            nodeObject.name = serviceType.Name;
            Selection.activeGameObject = nodeObject;
            return nodeObject;
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
