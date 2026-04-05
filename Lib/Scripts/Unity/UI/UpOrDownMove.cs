using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    /// <summary>
    /// 涓婁笅寰幆绉诲姩鍔ㄧ敾鍣?
    /// 鐢?Executor 鎵ц鐨勬湇鍔″瀷閫昏緫缁勪欢
    /// 鐢ㄤ簬璁╀竴缁勭墿浣撹繘琛岃繛缁殑涓婁笅寰€澶嶅惊鐜繍鍔?
    /// </summary>
    public class UpOrDownLoopAnimator : Service
    {
        [Tooltip("闇€瑕佷笂涓嬬Щ鍔ㄧ殑鐗╀綋Transform鍒楄〃")]
        public List<Transform> objTranforms = new List<Transform>();

        [Tooltip("Maximum upward offset.")]
        public float maxHeight = 0.5f;

        [Tooltip("鍚戜笅绉诲姩鐨勬渶浣庨珮搴︼紙鐩稿浜庡師濮嬩綅缃級")]
        public float minHeight = 0.5f;

        [Tooltip("涓婁笅绉诲姩鐨勯€熷害")]
        public float moveSpeed = 5f;

        [Tooltip("Move all targets together.")]
        public bool moveTogether = true;

        private readonly List<Transform> _runtimeTargets = new List<Transform>(8);
        private readonly List<Vector3> _originLocalPositions = new List<Vector3>(8);

        private float _elapsedDistance;
        private bool _hasCachedTargets;
        private void OnEnable()
        {
            EnsureTargetsCached();

            if (_runtimeTargets.Count == 0)
            {
                Debug.LogError($"[{GetType().Name}] Missing target objects.");
                Next();
            }
        }

        private void LateUpdate()
        {
            EnsureTargetsCached();

            if (_runtimeTargets.Count == 0)
            {
                Next();
                return;
            }

            float speed = Mathf.Max(0f, moveSpeed);
            if (speed <= 0f)
            {
                Next();
                return;
            }

            float upDistance = Mathf.Abs(maxHeight);
            float downDistance = Mathf.Abs(minHeight);
            float cycleLength = 2f * (upDistance + downDistance);

            if (cycleLength <= Mathf.Epsilon)
            {
                Next();
                return;
            }

            _elapsedDistance += Time.deltaTime * speed;

            int targetCount = _runtimeTargets.Count;
            for (int i = 0; i < targetCount; i++)
            {
                Transform target = _runtimeTargets[i];
                if (target == null)
                    continue;

                float phaseOffset = 0f;
                if (!moveTogether && targetCount > 1)
                {
                    phaseOffset = cycleLength * i / targetCount;
                }

                float offsetY = EvaluateOffset(_elapsedDistance + phaseOffset, upDistance, downDistance, cycleLength);
                Vector3 originLocalPos = _originLocalPositions[i];

                // 璁＄畻鐩爣浣嶇疆骞惰祴鍊硷紙浣跨敤鏈湴鍧愭爣锛岄伩鍏嶇埗鐗╀綋浣嶇Щ褰卞搷锛?
                target.localPosition = new Vector3(
                    originLocalPos.x,
                    originLocalPos.y + offsetY,
                    originLocalPos.z
                );
            }

            Next();
        }

        private void OnDisable()
        {
            CleanupNullTargets();
        }

        /// <summary>
        /// 纭繚鐩爣鍒楄〃宸茬紦瀛?
        /// </summary>
        private void EnsureTargetsCached()
        {
            if (_hasCachedTargets && !IsTargetListChanged())
                return;

            RebuildTargetCache();
        }

        /// <summary>
        /// 妫€鏌ョ洰鏍囧垪琛ㄦ槸鍚﹀彂鐢熷彉鍖?
        /// </summary>
        private bool IsTargetListChanged()
        {
            if (objTranforms == null)
                return _runtimeTargets.Count != 0;

            int validCount = 0;
            for (int i = 0; i < objTranforms.Count; i++)
            {
                if (objTranforms[i] != null)
                    validCount++;
            }

            if (validCount != _runtimeTargets.Count)
                return true;

            int runtimeIndex = 0;
            for (int i = 0; i < objTranforms.Count; i++)
            {
                Transform target = objTranforms[i];
                if (target == null)
                    continue;

                if (runtimeIndex >= _runtimeTargets.Count)
                    return true;

                if (_runtimeTargets[runtimeIndex] != target)
                    return true;

                runtimeIndex++;
            }

            return false;
        }

        /// <summary>
        /// 閲嶆柊鏋勫缓鐩爣缂撳瓨鍒楄〃
        /// </summary>
        private void RebuildTargetCache()
        {
            _runtimeTargets.Clear();
            _originLocalPositions.Clear();

            if (objTranforms == null)
            {
                _hasCachedTargets = true;
                return;
            }

            for (int i = 0; i < objTranforms.Count; i++)
            {
                Transform target = objTranforms[i];
                if (target == null)
                    continue;

                _runtimeTargets.Add(target);
                _originLocalPositions.Add(target.localPosition);
            }

            _hasCachedTargets = true;
        }

        /// <summary>
        /// 娓呯悊绌哄紩鐢ㄧ洰鏍?
        /// </summary>
        private void CleanupNullTargets()
        {
            for (int i = _runtimeTargets.Count - 1; i >= 0; i--)
            {
                if (_runtimeTargets[i] != null)
                    continue;

                _runtimeTargets.RemoveAt(i);
                _originLocalPositions.RemoveAt(i);
            }
        }

        /// <summary>
        /// 璁＄畻褰撳墠甯х殑Y杞村亸绉婚噺锛堜笂涓嬪惊鐜牳蹇冪畻娉曪級
        /// </summary>
        private static float EvaluateOffset(float distance, float upDistance, float downDistance, float cycleLength)
        {
            float loopDistance = Mathf.Repeat(distance, cycleLength);

            if (loopDistance <= upDistance)
            {
                return loopDistance;
            }

            float middleSectionEnd = 2f * upDistance + downDistance;
            if (loopDistance <= middleSectionEnd)
            {
                return 2f * upDistance - loopDistance;
            }

            return loopDistance - cycleLength;
        }
    }
}





