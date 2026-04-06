using UnityEngine;
using UnityEngine.Serialization;

namespace SP
{
    /// <summary>
    /// Third-person camera follow service.
    /// </summary>
    public class ThirdPersonCamera : Service, IFlowPort
    {
        public bool HasEnterPort => false;
        public bool HasNextPort => false;
        [HideInInspector][SerializeField] private bool isMirror;
        public bool IsMirror
        {
            get => isMirror;
            set => isMirror = value;
        }
        
        
        [FormerlySerializedAs("targetCamera")]
        [Input]
        public CameraVar cameraInput = new CameraVar();
        [FormerlySerializedAs("target")]
        [Input]
        public TransformVar targetInput = new TransformVar();

        [Header("Settings")]
        public bool useAwakeOffset = true;
        [Tooltip("Whether the camera follows the target.")]
        public bool followTarget = true;
        [Tooltip("Whether the camera looks at the target.")]
        public bool lookAtTarget;
        [Tooltip("Keep original Z position.")]
        public bool keepZ;
        [Tooltip("Enable smooth follow.")]
        public bool enableSmoothDamp;
        [Tooltip("Smooth follow time.")]
        public float smoothTime = 0.15f;

        private float _originalZ;
        private Camera _camera;
        private Vector3 _diff;
        private Vector3 _currentVelocity;
        private Vector3 _targetPosition;
        private float _shakeTime = 1.5f;
        private float _shakeAmplitude = 0.01f;
        private bool _isShaking;

        public override void Init()
        {
            _camera = cameraInput != null ? cameraInput.Get() : null;
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;

            Transform target = ResolveTarget();
            if (target != null && useAwakeOffset)
                _diff = _camera.transform.position - target.position;

            _originalZ = _camera.transform.position.z;
        }
        private void OnEnable()
        {
            if (_camera == null)
                _camera = cameraInput != null ? cameraInput.Get() : null;

            if (_camera == null)
                _camera = Camera.main;

            Transform target = ResolveTarget();
            if (_camera == null || target == null)
                return;

            _diff = _camera.transform.position - target.position;
        }

        public void Update()
        {
            if (_camera == null)
            {
                _camera = cameraInput != null ? cameraInput.Get() : null;
                if (_camera == null)
                    _camera = Camera.main;
            }

            if (_camera == null)
            {
                // Next();
                return;
            }

            Transform target = ResolveTarget();
            if (target == null)
            {
                // Next();
                return;
            }

            _targetPosition = target.position + _diff;
            if (followTarget)
            {
                Vector3 resultPosition = _targetPosition;
                if (enableSmoothDamp)
                {
                    resultPosition = Vector3.SmoothDamp(
                        _camera.transform.position,
                        _targetPosition,
                        ref _currentVelocity,
                        smoothTime);
                }

                if (keepZ)
                    resultPosition.z = _originalZ;

                _camera.transform.position = resultPosition;
            }

            if (lookAtTarget)
                _camera.transform.LookAt(target.position, Vector3.up);

            if (_isShaking)
            {
                if (_shakeTime > 0f)
                {
                    _camera.transform.localPosition += Random.onUnitSphere * _shakeAmplitude;
                    _shakeTime -= Time.deltaTime;
                }
                else
                {
                    _shakeTime = 0f;
                    _isShaking = false;
                }
            }

            // Next();
        }



        public void ResetAt(Vector3 position)
        {
            Transform target = ResolveTarget();
            if (_camera == null || target == null)
                return;

            _camera.transform.position = position;
            _diff = _camera.transform.position - target.position;
            _targetPosition = target.position + _diff;
            _originalZ = _camera.transform.position.z;
        }

        public Vector3 Offset
        {
            set => _diff = value;
        }

        private Transform ResolveTarget()
        {
            return targetInput != null ? targetInput.Get() : null;
        }

    }
}




