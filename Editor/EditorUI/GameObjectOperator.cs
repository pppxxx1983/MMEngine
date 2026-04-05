using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class GameObjectOperator
    {
        public static Root FindRoot()
        {
            return Object.FindObjectOfType<Root>();
        }

        public static Graph FindGraph()
        {
            Root root = FindRoot();
            if (root == null)
            {
                return null;
            }

            return root.GetComponentInChildren<Graph>();
        }

        public static bool TryGetNodeObject(string nodeId, out GameObject nodeObject)
        {
            nodeObject = null;
            if (string.IsNullOrEmpty(nodeId))
            {
                return false;
            }

            return SceneRefManager.Instance.TryGetGameObject(nodeId, out nodeObject) && nodeObject != null;
        }

        public static bool DestroyNodeObject(string nodeId)
        {
            GameObject nodeObject;
            if (!TryGetNodeObject(nodeId, out nodeObject))
            {
                return false;
            }

            Undo.DestroyObjectImmediate(nodeObject);
            return true;
        }

        public static bool MoveNodeToGraph(string nodeId)
        {
            Graph graph = FindGraph();
            GameObject nodeObject;
            if (graph == null || !TryGetNodeObject(nodeId, out nodeObject))
            {
                return false;
            }

            if (nodeObject == null || nodeObject.transform.parent == graph.transform)
            {
                return false;
            }

            Undo.SetTransformParent(nodeObject.transform, graph.transform, "Unlink Editor UI Node");
            return true;
        }

        public static bool MoveNodeToDefaultParent(string nodeId)
        {
            Graph graph = FindGraph();
            Root root = FindRoot();
            GameObject nodeObject;
            if (graph == null || !TryGetNodeObject(nodeId, out nodeObject) || nodeObject == null)
            {
                return false;
            }

            Transform targetParent = graph.transform;
            Service service = nodeObject.GetComponent<Service>();
            if (service is IGroupNode groupNode && root != null && !string.IsNullOrEmpty(groupNode.GroupParentName))
            {
                targetParent = EnsureChildTransform(root.transform, groupNode.GroupParentName);
            }

            if (nodeObject.transform.parent == targetParent)
            {
                return false;
            }

            Undo.SetTransformParent(nodeObject.transform, targetParent, "Move Node To Default Parent");
            return true;
        }

        public static T EnsureComponentChild<T>(Transform parentTransform, string childName) where T : Component
        {
            Transform childTransform = parentTransform.Find(childName);
            GameObject childObject;

            if (childTransform == null)
            {
                childObject = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
                childObject.transform.SetParent(parentTransform, false);
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

        public static Transform EnsureChildTransform(Transform parentTransform, string childName)
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
