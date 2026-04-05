using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public enum TriggerShapeType
    {
        Sphere = 0,
        Box = 1,
    }

    public class Trigger : MonoBehaviour,IMMVarTarget
    {
        [Tooltip("Trigger shape type")]
        public TriggerShapeType shapeType = TriggerShapeType.Sphere;

        [Tooltip("Sphere radius")]
        public float radius = 1f;

        [Tooltip("Box size in world units")]
        public Vector3 boxSize = Vector3.one;

        public bool IsTrigger(Trigger trigger)
        {
            if (trigger == null || trigger == this)
                return false;

            return IsOverlapping(this, trigger);
        }

        public bool IsTrigger(List<Trigger> trigger)
        {
            if (trigger == null || trigger.Count == 0)
                return false;

            for (int i = 0; i < trigger.Count; i++)
            {
                if (IsTrigger(trigger[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public float GetSafeRadius()
        {
            return Mathf.Max(0f, radius);
        }

        public Vector3 GetHalfExtents()
        {
            return GetSafeBoxSize() * 0.5f;
        }

        public Bounds GetAabbBounds()
        {
            return new Bounds(transform.position, GetSafeBoxSize());
        }

        public Vector3 GetSafeBoxSize()
        {
            return new Vector3(
                Mathf.Max(0f, boxSize.x),
                Mathf.Max(0f, boxSize.y),
                Mathf.Max(0f, boxSize.z));
        }

        private static bool IsOverlapping(Trigger a, Trigger b)
        {
            if (a.shapeType == TriggerShapeType.Sphere)
            {
                if (b.shapeType == TriggerShapeType.Sphere)
                    return CheckSphereSphere(a, b);

                return CheckSphereBox(a, b);
            }

            if (b.shapeType == TriggerShapeType.Sphere)
                return CheckSphereBox(b, a);

            return CheckBoxBox(a, b);
        }

        private static bool CheckSphereSphere(Trigger a, Trigger b)
        {
            float distance = a.GetSafeRadius() + b.GetSafeRadius();
            return TMLMath.FastCompareDistance(a.transform.position, b.transform.position, distance) <= 0;
        }

        private static bool CheckBoxBox(Trigger a, Trigger b)
        {
            return a.GetAabbBounds().Intersects(b.GetAabbBounds());
        }

        private static bool CheckSphereBox(Trigger sphereTrigger, Trigger boxTrigger)
        {
            Bounds boxBounds = boxTrigger.GetAabbBounds();
            Vector3 sphereCenter = sphereTrigger.transform.position;
            Vector3 closestPoint = new Vector3(
                Mathf.Clamp(sphereCenter.x, boxBounds.min.x, boxBounds.max.x),
                Mathf.Clamp(sphereCenter.y, boxBounds.min.y, boxBounds.max.y),
                Mathf.Clamp(sphereCenter.z, boxBounds.min.z, boxBounds.max.z));
            float radiusValue = sphereTrigger.GetSafeRadius();
            return (sphereCenter - closestPoint).sqrMagnitude <= radiusValue * radiusValue;
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0f, radius);
            boxSize = GetSafeBoxSize();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            if (shapeType == TriggerShapeType.Sphere)
            {
                Gizmos.DrawWireSphere(transform.position, GetSafeRadius());
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, GetSafeBoxSize());
            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}

