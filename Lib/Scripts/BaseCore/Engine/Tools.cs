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
        /// 鍋氫竴涓瓨鍌紝閬垮厤澶氭toString
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
        /// 缁樺埗璐濆灏旀洸绾?
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
        /// 缁樺埗璐濆灏旀洸绾?
        /// </summary>
        /// <param name="t">闇€瑕佺Щ鍔ㄧ殑缁勪欢</param>
        /// <param name="target">鐩爣</param>
        /// <param name="otherPos">杈呭姪鐐?/param>
        /// <param name="lerpT">杩涘害</param>
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
        /// 妫€娴嬩袱涓綅缃殑璺濈鏄惁鍦ㄦ寚瀹氳寖鍥村唴锛屼娇鐢ㄥ钩鏂硅窛绂昏繘琛岃绠楋紝浼犲叆鐨勮窛绂讳細琚钩鏂癸紝閬垮厤寮€鏂硅繍绠?
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
        /// 妫€娴嬩袱涓綅缃殑璺濈鏄惁鍦ㄦ寚瀹氳寖鍥村唴锛屼娇鐢ㄥ钩鏂硅窛绂昏繘琛岃绠楋紝浼犲叆鐨勮窛绂昏纭繚鏄钩鏂瑰悗鐨勮窛绂伙紝閬垮厤寮€鏂硅繍绠?
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

