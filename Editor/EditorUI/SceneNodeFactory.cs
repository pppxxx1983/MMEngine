using System;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class SceneNodeFactory
    {
        private const string SpecialGraphParentName = "Special";

        public static GameObject CreateSceneNode(Type serviceType)
        {
            if (serviceType == null || !typeof(Component).IsAssignableFrom(serviceType))
            {
                return null;
            }

            Root root = UnityEngine.Object.FindObjectOfType<Root>();
            if (root == null)
            {
                GameObject rootObject = new GameObject("Root");
                Undo.RegisterCreatedObjectUndo(rootObject, "Create Root");
                root = Undo.AddComponent<Root>(rootObject);
            }

            ResourceCenter resourceCenter = EnsureComponentChild<ResourceCenter>(root.transform, "ResourceCenter");
            GlobalContext globalContext = EnsureComponentChild<GlobalContext>(root.transform, "GlobalContext");
            GlobalAudioManager audioManager = EnsureComponentChild<GlobalAudioManager>(root.transform, "GlobalAudioManager");

            root.resourceCenter = resourceCenter;
            root.audioManager = audioManager;
            EditorUtility.SetDirty(root);

            Graph graph = root.GetComponentInChildren<Graph>();
            if (graph == null)
            {
                GameObject graphObject = new GameObject("Graph");
                Undo.RegisterCreatedObjectUndo(graphObject, "Create Graph");
                graphObject.transform.SetParent(root.transform, false);
                graph = Undo.AddComponent<Graph>(graphObject);
            }

            GameObject nodeObject = graph.CreateNodeObject();
            if (typeof(ISpecialNode).IsAssignableFrom(serviceType))
            {
                Transform specialParent = EnsureChildTransform(graph.transform, SpecialGraphParentName);
                nodeObject.transform.SetParent(specialParent, false);
            }

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

        private static T EnsureComponentChild<T>(Transform rootTransform, string childName) where T : Component
        {
            Transform childTransform = rootTransform.Find(childName);
            GameObject childObject;

            if (childTransform == null)
            {
                childObject = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
                childObject.transform.SetParent(rootTransform, false);
            }
            else
            {
                childObject = childTransform.gameObject;
            }

            T component = childObject.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(childObject);
            }

            return component;
        }

        private static Transform EnsureChildTransform(Transform parentTransform, string childName)
        {
            Transform childTransform = parentTransform.Find(childName);
            if (childTransform != null)
            {
                return childTransform;
            }

            GameObject childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
            childObject.transform.SetParent(parentTransform, false);
            return childObject.transform;
        }
    }
}
