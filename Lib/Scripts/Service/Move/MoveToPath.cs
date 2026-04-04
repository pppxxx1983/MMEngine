using UnityEngine;

namespace SP
{
    public class MoveToPath : Service
    {
        public Transform moveObject;
        public Transform[] _paths;
        public float time = 0.3f;
        // public bool keepOriginalZ = true;

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

        public override void Init()
        {
            _elapsedTime = 0f;
            _isMoving = false;

            if (moveObject == null)
            {
                Debug.LogWarning($"[{GetType().Name}] moveObject is not set.", this);
                return;
            }

            if (_paths == null || _paths.Length < 2)
            {
                Debug.LogWarning($"[{GetType().Name}] At least 2 path points are required.", this);
                return;
            }

            _startZValue = moveObject.position.z;
            BuildPath();

            if (_totalPathLength <= 0f || time <= 0f)
            {
                ApplyProgress(1f);
                return;
            }

            _isMoving = true;
        }

        public override void Enter()
        {
            if (!_isMoving)
            {
                NextService();
            }
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
            _segmentLengths = _paths != null && _paths.Length >= 2 ? new float[_paths.Length - 1] : new float[0];
            _pathPositions = _paths != null ? new Vector3[_paths.Length] : new Vector3[0];
            _totalPathLength = 0f;

            for (int i = 0; i < _pathPositions.Length; i++)
            {
                Transform path = _paths[i];
                _pathPositions[i] = path != null ? path.position : Vector3.zero;
            }

            for (int i = 0; i < _paths.Length - 1; i++)
            {
                Transform from = _paths[i];
                Transform to = _paths[i + 1];
                if (from == null || to == null)
                {
                    _segmentLengths[i] = 0f;
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
            // if (keepOriginalZ)
            // {
            //     newPosition.z = _startZValue;
            // }

            moveObject.position = newPosition;
        }

        private Vector3 GetPositionByProgress(float progress)
        {
            if (_paths == null || _paths.Length == 0)
            {
                return Vector3.zero;
            }

            if (_paths.Length == 1 || _totalPathLength <= 0f)
            {
                return _pathPositions.Length > 0 ? _pathPositions[_pathPositions.Length - 1] : Vector3.zero;
            }

            float targetDistance = _totalPathLength * Mathf.Clamp01(progress);
            float currentDistance = 0f;

            for (int i = 0; i < _segmentLengths.Length; i++)
            {
                Transform from = _paths[i];
                Transform to = _paths[i + 1];
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
            if (_paths == null || _paths.Length < 2)
            {
                return;
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < _paths.Length - 1; i++)
            {
                Transform from = _paths[i];
                Transform to = _paths[i + 1];
                if (from == null || to == null)
                {
                    continue;
                }

                Gizmos.DrawLine(from.position, to.position);
            }
        }
#endif
    }
}
