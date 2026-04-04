using System.Collections.Generic;
using UnityEngine;

#if UNITY_LUNA
using Bridge;
#endif

namespace SP
{
    public static class Tools
    {
        private static Dictionary<int, string> numToString = new Dictionary<int, string>();

        /// <summary>
        /// 做一个存储，避免多次toString
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string ToS(this int num)
        {
            string s = string.Empty;
            if (numToString.ContainsKey(num))
                s = numToString[num];
            else
            {
                s = num.ToString();
                numToString.Add(num, s);
            }
            return s;
        }

        /// <summary>
        /// 绘制贝塞尔曲线
        /// </summary>
        /// <param name="t"></param>
        /// <param name="target"></param>
        /// <param name="otherPos"></param>
        /// <returns></returns>
#if UNITY_LUNA
        public static Vector3 BezierCurve([Ref] this Vector3 start, Transform target, [Ref] Vector3 otherPos, float lerpT)
#else
        public static Vector3 BezierCurve(this Vector3 start, Transform target, Vector3 otherPos, float lerpT)
#endif
        {
            return start.BezierCurve(target.position, otherPos, lerpT);
        }

        /// <summary>
        /// 绘制贝塞尔曲线
        /// </summary>
        /// <param name="t">需要移动的组件</param>
        /// <param name="target">目标</param>
        /// <param name="otherPos">辅助点</param>
        /// <param name="lerpT">进度</param>
        /// <returns></returns>
#if UNITY_LUNA
        public static Vector3 BezierCurve([Ref] this Vector3 start,[Ref] Vector3 target,[Ref] Vector3 otherPos, float lerpT)
#else
        public static Vector3 BezierCurve(this Vector3 start, Vector3 target, Vector3 otherPos, float lerpT)
#endif
        {
            Vector3 pos1 = Vector3.Lerp(start, otherPos, lerpT);
            Vector3 pos2 = Vector3.Lerp(otherPos, target, lerpT);
            return Vector3.Lerp(pos1, pos2, lerpT);
        }

        /// <summary>
        /// 检测两个位置的距离是否在指定范围内，使用平方距离进行计算，传入的距离会被平方，避免开方运算
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
#if UNITY_LUNA
        public static bool CheckDistanceSquareDis([Ref] this Vector3 pos1,[Ref] Vector3 pos2, float dis)
#else
        public static bool CheckDistanceSquareDis(this Vector3 pos1, Vector3 pos2, float dis)
#endif
        {
            return (pos1 - pos2).sqrMagnitude <= dis * dis;
        }

        /// <summary>
        /// 检测两个位置的距离是否在指定范围内，使用平方距离进行计算，传入的距离请确保是平方后的距离，避免开方运算
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
#if UNITY_LUNA
        public static bool CheckDistance([Ref] this Vector3 pos1,[Ref] Vector3 pos2, float dis)
#else
        public static bool CheckDistance(this Vector3 pos1, Vector3 pos2, float dis)
#endif
        {
            return (pos1 - pos2).sqrMagnitude <= dis;
        }

        public static bool IsTirgger(List<Trigger> trigger1, List<Trigger> trigger2)
        {
            if (trigger1 == null || trigger2 == null || trigger1.Count == 0 || trigger2.Count == 0)
                return false;

            for (int i = 0; i < trigger1.Count; i++)
            {
                Trigger currentTrigger = trigger1[i];
                if (currentTrigger == null)
                    continue;

                if (currentTrigger.IsTrigger(trigger2))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
