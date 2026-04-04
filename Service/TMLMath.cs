using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP {
    public static class TMLMath {
        public static Vector3 ClosestPointOnCube(Vector3 centerPos, Vector3 cubeSize, Vector3 testPoint) {
            Vector3 halfSize = cubeSize * 0.5f;
            Vector3 min = centerPos - halfSize;
            Vector3 max = centerPos + halfSize;
            float x = Mathf.Clamp(testPoint.x, min.x, max.x);
            float y = Mathf.Clamp(testPoint.y, min.y, max.y);
            float z = Mathf.Clamp(testPoint.z, min.z, max.z);
            return new Vector3(x, y, z);
        }
        /// <summary>
        /// 判断点是否在四方体内
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="cubeSize"></param>
        /// <param name="testPoint"></param>
        /// <returns></returns>
        public static bool InCube(Vector3 centerPos, Vector3 cubeSize, Vector3 testPoint) {
            Vector3 halfSize = cubeSize * 0.5f;
            Vector3 min = centerPos - halfSize;
            Vector3 max = centerPos + halfSize;
            return (testPoint.x >= min.x && testPoint.x <= max.x) &&
                (testPoint.y >= min.y && testPoint.y <= max.y) &&
                (testPoint.z >= min.z && testPoint.z <= max.z);
            // return true;
            // vec2 delta = abs(point) - boxRect;
            // return length(max(delta, 0.0)) + min(max(delta.x, delta.y), 0.0);
        }
        /// <summary>
        /// 返回线段ab距离testPoint最近的点
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="testPoint"></param>
        /// <returns></returns>
        public static Vector3 ClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 testPoint) {
            Vector3 ab = b - a;
            float t = Vector3.Dot(testPoint - a, ab) / Vector3.Dot(ab, ab);
            return a + Mathf.Min(Mathf.Max(t, 0f), 1f) * ab;
        }
        /// <summary>
        /// 判断点是否在圆锥体内，输入2维向量则是判断扇形区域
        /// </summary>
        /// <param name="startPos">圆锥体的起始位置，尖部</param>
        /// <param name="dir">圆锥体的喇叭朝向</param>
        /// <param name="fovAngle">圆锥体的角度，角度要小于180度</param>
        /// <param name="testPoint">测试点</param>
        /// <returns></returns>
        public static bool InCone(Vector3 startPos, Vector3 dir, float fovAngle, Vector3 testPoint) {
            Vector3 toPoint = (testPoint - startPos);
            // float angle = Vector3.Angle(dir, toPoint);
            // return angle <= fovAngle * 0.5f;
            float cosAngle = Vector3.Dot(dir, toPoint) / (dir.magnitude * toPoint.magnitude); ////避免反余弦和归一化操作，节省算力
            return cosAngle >= Mathf.Cos(fovAngle * 0.5f * Mathf.Deg2Rad);
        }
        /// <summary>
        /// 在XY平面，判断点是否在扇形内
        /// </summary>
        /// <param name="startPos">扇形的起始位置</param>
        /// <param name="dir">扇形的正向向量</param>
        /// <param name="fovAngle">扇形的视角，角度要小于180度</param>
        /// <param name="testPoint">测试点</param>
        /// <returns>是否在扇形内</returns>
        [System.Obsolete("此函数已废弃， 请使用InCone代替")]
        public static bool InFan_XYPlane(Vector3 startPos, Vector3 dir, float fovAngle, Vector3 testPoint) {
            Vector3 t = testPoint - startPos;
            if (Vector3.Dot(t, dir) < 0f) {
                return false;
            }
            Vector3 a = Quaternion.Euler(0f, 0f, fovAngle / 2f) * dir;
            Vector3 b = Quaternion.Euler(0f, 0f, -fovAngle / 2f) * dir;
            return Vector3.Cross(t, a).z * Vector3.Cross(t, b).z < 0f;
        }
        /// <summary>
        /// 在XZ平面，判断点是否在扇形内
        /// </summary>
        /// <param name="startPos">扇形的起始位置</param>
        /// <param name="dir">扇形的正向向量</param>
        /// <param name="fovAngle">扇形的视角，角度要小于180度</param>
        /// <param name="testPoint">测试点</param>
        /// <returns>是否在扇形内</returns>
        [System.Obsolete("此函数已废弃， 请使用InCone代替")]
        public static bool InFan_XZPlane(Vector3 startPos, Vector3 dir, float fovAngle, Vector3 testPoint) {
            Vector3 t = testPoint - startPos;
            if (Vector3.Dot(t, dir) < 0f) {
                return false;
            }
            Vector3 a = Quaternion.Euler(0f, fovAngle / 2f, 0f) * dir;
            Vector3 b = Quaternion.Euler(0f, -fovAngle / 2f, 0f) * dir;
            return Vector3.Cross(t, a).y * Vector3.Cross(t, b).y < 0f;
        }
        /// <summary>
        /// 判断testPoint是否在以startPos为起点，方向为dir，长度为length，半径为radius的胶囊体中
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="dir"></param>
        /// <param name="length"></param>
        /// <param name="radius"></param>
        /// <param name="testPoint"></param>
        /// <returns></returns>
        public static bool InCapsule(Vector3 startPos, Vector3 dir, float length, float radius, Vector3 testPoint) {
            Vector3 segStart = startPos;
            Vector3 segEnd = startPos + dir.normalized * length;
            // 计算testPoint到线段的最近点
            Vector3 segDir = segEnd - segStart;
            float segLen = segDir.magnitude;
            if (segLen > 1e-6f) {
                segDir /= segLen;
            } else {
                segDir = Vector3.zero;
            }
            float t = Vector3.Dot(testPoint - segStart, segDir);
            t = Mathf.Clamp(t, 0f, segLen);
            Vector3 closest = segStart + segDir * t;
            return (testPoint - closest).sqrMagnitude <= radius * radius;
        }
        /// <summary>
        /// 使用射线法判断点是否在多边形内，此方法支持凹多边形
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="testPoint"></param>
        /// <returns>是否在多边形内</returns>
        public static bool InPolygon(Vector2[] polygon, Vector2 testPoint) {
            if (polygon == null || polygon.Length < 3) {
                return false;
            }
            int i, j;
            bool isInside = false;
            for (i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++) {
                if ((polygon[i].y > testPoint.y) != (polygon[j].y > testPoint.y) &&
                    (testPoint.x < (polygon[j].x - polygon[i].x) * (testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)) {
                    isInside = !isInside;
                }
            }
            return isInside;
        }
        /// <summary>
        /// 在XY平面，判断点是否在线段的左边
        /// </summary>
        /// <param name="s">线段起点</param>
        /// <param name="e">线段终点</param>
        /// <param name="testPoint">要被测试的点</param>
        /// <returns></returns>
        public static bool OnLineLeft(Vector2 s, Vector2 e, Vector2 testPoint) {
            // 引入行列式
            // |s.x s.y 1|
            // |e.x e.y 1|
            // |p.x p.y 1|
            return s.x * e.y + s.y * testPoint.x + e.x * testPoint.y - e.y * testPoint.x - s.y * e.x - testPoint.y * s.x > 0;
        }
        /// <summary>
        /// 在XY平面，判断点是否在线段的右边边
        /// </summary>
        /// <param name="s">线段起点</param>
        /// <param name="e">线段终点</param>
        /// <param name="testPoint">要被测试的点</param>
        /// <returns></returns>
        public static bool OnLineRight(Vector2 s, Vector2 e, Vector2 testPoint) {
            // 引入行列式
            // |s.x s.y 1|
            // |e.x e.y 1|
            // |p.x p.y 1|
            return s.x * e.y + s.y * testPoint.x + e.x * testPoint.y - e.y * testPoint.x - s.y * e.x - testPoint.y * s.x < 0;
        }
        /// <summary>
        /// 判断a与b之间的距离是否大于distance
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="distance"></param>
        /// <returns>1：a与b间距大于distance，0：a与b间距等于distance，-1：a与b间距小于distance</returns>
        public static int FastCompareDistance(Vector3 a, Vector3 b, float distance) {
            // 计算平方距离，避免开方运算
            float sqrDistance = (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
            if (sqrDistance < distance * distance) {
                return -1;
            } else if (sqrDistance > distance * distance) {
                return 1;
            }
            return 0;
        }
        public static bool InFrustum(Vector3 testPos, Transform viewFrustumApex, float fov, float aspect, float minRange, float maxRange) {
            // 绘制Gizmo
            // Gizmos.color = Color.yellow;
            // Gizmos.matrix = Matrix4x4.TRS(viewFrustumApex.position, viewFrustumApex.rotation, Vector3.one);
            // Gizmos.DrawFrustum(Vector3.zero, fov, maxRange, minRange, aspect);
            //
            //由于视锥体矩阵使用的是右手坐标系，Unity物体使用的是左手坐标系，所以这里要将z值反转
            Vector3 p = viewFrustumApex.InverseTransformPoint(testPos);
            Vector4 s = Matrix4x4.Perspective(fov, aspect, minRange, maxRange) * new Vector4(p.x, p.y, -p.z, 1f);
            return (s.x > -s.w && s.x < s.w &&
                s.y > -s.w && s.y < s.w &&
                s.z > -s.w && s.z < s.w);
        }
        public static Vector3 BezierPathQuad(float t, Vector3 p0, Vector3 controlPoint, Vector3 p1) {
            float u = 1.0f - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0;
            p += 2f * u * t * controlPoint;
            p += tt * p1;
            return p;
        }
        public static Vector3 BezierPathCubic(float t, Vector3 p0, Vector3 controlPoint0, Vector3 controlPoint1, Vector3 p1) {
            float u = 1.0f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            Vector3 p = uuu * p0;
            p += 3f * uu * t * controlPoint0;
            p += 3f * u * tt * controlPoint1;
            p += ttt * p1;
            return p;
        }
        public static bool BoundsIsIntersected(Bounds left, Bounds right, float tolerant = 0.0f) {
            left.Expand(new Vector3(tolerant, tolerant, 0.0f));
            right.Expand(new Vector3(tolerant, tolerant, 0.0f));
            return left.Intersects(right);
        }
    }
}
