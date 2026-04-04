using UnityEngine;

namespace SP
{
    public class MoveToTarget : Service
    {
        public Transform moveObject;
        public Transform[] _moveItems;
        public float time = 0.3f;
        public bool keepOriginalZ = true;

#if UNITY_EDITOR
        [Space]
        [Tooltip("是否始终显示路径 Gizmos")]
        public bool alwaysDisplay = false;
#endif

        private float[] _segmentLengths = new float[0];
        private Vector3[] _pathPositions = new Vector3[0];
        private float _elapsedTime;
        private bool _isMoving;
        private float _startZValue;
        private float _totalPathLength;

        public override void Enter()
        {
            _elapsedTime = 0f;
            _isMoving = false;

            if (moveObject == null)
            {
                Debug.LogWarning($"[{GetType().Name}] moveObject is not set.", this);
                NextService();
                return;
            }

            if (_moveItems == null || _moveItems.Length < 2)
            {
                Debug.LogWarning($"[{GetType().Name}] At least 2 path points are required.", this);
                NextService();
                return;
            }

            _startZValue = moveObject.position.z;
            BuildPath();
            if (_totalPathLength <= 0f)
            {
                ApplyProgress(1f);
                NextService();
                return;
            }

            if (time <= 0f)
            {
                ApplyProgress(1f);
                NextService();
                return;
            }

            _isMoving = true;
        }

        public override void Update()
        {
            if (!_isMoving)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / time);
            ApplyProgress(progress);

            if (progress >= 1f)
            {
                _isMoving = false;
                NextService();
            }
        }

        private void BuildPath()
        {
            _segmentLengths = _moveItems != null && _moveItems.Length >= 2 ? new float[_moveItems.Length - 1] : new float[0];
            _pathPositions = _moveItems != null ? new Vector3[_moveItems.Length] : new Vector3[0];
            _totalPathLength = 0f;

            for (int i = 0; i < _pathPositions.Length; i++)
            {
                Transform path = _moveItems[i];
                _pathPositions[i] = path != null ? path.position : Vector3.zero;
            }

            for (int i = 0; i < _moveItems.Length - 1; i++)
            {
                Transform from = _moveItems[i];
                Transform to = _moveItems[i + 1];
                if (from == null || to == null)
                {
                    _segmentLengths[i] =0f;
                    continue;
                }

                float length = Vector3.Distance(_pathPositions[i], _pathPositions[i + 1]);
                _segmentLengths[i] = length;
                _totalPathLength += length;
            }
        }

        private void ApplyProgress(float progress)
        {
            if (moveObject == null)
            {
                return;
            }

            Vector3 newPosition = GetPositionByProgress(progress);
            if (keepOriginalZ)
            {
                newPosition.z = _startZValue;
            }

            moveObject.position = newPosition;
        }

        private Vector3 GetPositionByProgress(float progress)
        {
            if (_moveItems == null || _moveItems.Length == 0)
            {
                return Vector3.zero;
            }

            if (_moveItems.Length == 1 || _totalPathLength <= 0f)
            {
                return _pathPositions.Length > 0 ? _pathPositions[_pathPositions.Length - 1] : Vector3.zero;
            }

            float targetDistance = _totalPathLength * Mathf.Clamp01(progress);
            float currentDistance = 0f;

            for (int i = 0; i < _segmentLengths.Length; i++)
            {
                Transform from = _moveItems[i];
                Transform to = _moveItems[i + 1];
                float segmentLength = _segmentLengths[i];

                if (from == null || to == null)
                {
                    continue;
                }

                if (segmentLength <= 0f)
                {
                    if (targetDistance <= currentDistance)
                    {
                        return to.position;
                    }

                    continue;
                }

                if (targetDistance <= currentDistance + segmentLength)
                {
                    float segmentProgress = (targetDistance - currentDistance) / segmentLength;
                    return Vector3.Lerp(_pathPositions[i], _pathPositions[i + 1], segmentProgress);
                }

                currentDistance += segmentLength;
            }

            return _pathPositions.Length > 0 ? _pathPositions[_pathPositions.Length - 1] : Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (alwaysDisplay)
            {
                DrawAllLine();
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawAllLine();
        }

        private void DrawAllLine()
        {
            if (_moveItems == null || _moveItems.Length < 2)
            {
                return;
            }

            for (int i = 0; i < _moveItems.Length - 1; i++)
            {
                Transform from = _moveItems[i];
                Transform to = _moveItems[i + 1];
                if (from == null || to == null)
                {
                    continue;
                }

                DrawLine(from, to);
            }
        }

        private void DrawLine(Transform from, Transform to)
        {
            Gizmos.DrawLine(from.position, to.position);
        }
#endif
    }
}
