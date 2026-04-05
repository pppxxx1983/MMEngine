using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SP {
    public class DirectionalMovement : Service, IFlowPort
    {
        public bool HasEnterPort => false;
        public bool HasNextPort => false;
        [Header("方向")] 
        // public JoystickUIVar joystick;
        // private JoystickUI _joystick;
        [Input] public Vector2Var Direction;
        
        // [Tooltip("手柄容器")]
        // public GameObject joystickContainer;
        [Header("控制对象")]
        [Input]
        public TransformVar moveTarget;
        private Transform _moveTarget;
        public float colliderRadius=0.4f;
        public float colliderHeight=1.77f;
        
        [Header("移动控制")]
        public float movementSpeed = 6f;
        [Tooltip("开启后：摇杆拉得越远移动越快\n关闭后：只要拉动就是固定最大速度")]
        public bool variableSpeed = true; // 【新增】控制是否根据拉动距离改变速度的开关
        
        [Header("动画控制")]
        [Input]
        public AnimatorVar anim;
        [Tooltip("移动和待机切换")]
        public String runName="Run";
        
        [Header("摄像机")]
        private Camera playerCamera;
        
        private Rigidbody _rb;
        private CapsuleCollider _collider;
        
        
        private bool _wasMoving;
        private float _facingRotation;
        private float _rotationVelocity = 0f;

        public override void Init()
        {
            if (!moveTarget.ValidateAndLog(this) || !Direction.ValidateAndLog(this))
            {
                return;
            }


            _moveTarget = moveTarget.Get();
            if ( _moveTarget == null)
            {
                return;
            }
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (moveTarget != null)
            {
                
                _rb = _moveTarget.GetComponent<Rigidbody>();
                if (_rb == null)
                {
                    // 3. 没有则添加，并设置基础参数
                    _rb = _moveTarget.gameObject.AddComponent<Rigidbody>();
                }

                _rb.useGravity = false; // 平面移动，不受重力影响
                _rb.isKinematic = false;
                // 保持平面移动：锁Y轴位置，锁X/Z旋转，允许Y旋转用于朝向
                _rb.constraints &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ |
                                     RigidbodyConstraints.FreezeRotationY);
                _rb.constraints |= RigidbodyConstraints.FreezePositionY
                                   | RigidbodyConstraints.FreezeRotationX
                                   | RigidbodyConstraints.FreezeRotationZ;
                _collider = _moveTarget.GetComponent<CapsuleCollider>();
                if (_collider == null)
                {
                    _collider = _moveTarget.gameObject.AddComponent<CapsuleCollider>();
                    _collider.radius = colliderRadius;
                    _collider.height = colliderHeight;
                }
            }
            else{

                Debug.LogError("滑杆控件没有设置操作对象!");
                return;
            }

        }


        
        public void StartMove() {
            if (anim.Get())
            {
                anim.Get().SetBool("Checkout", false);
                anim.Get().SetBool(runName,true);
                
            }
        }

        public void StopMove() {
            
            if (anim.Get())
            {
                anim.Get().SetBool(runName,false);
            }
            
            // --- 加上下面这段代码，踩死物理刹车 ---
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;        // 清除滑动惯性
                _rb.angularVelocity = Vector3.zero; // 清除旋转惯性
            }
            // --------------------------------------
        }
        
        private void Move() {
            if (_rb == null ) {
                return;
            }
            
            Vector2 inputDirection = Vector2.ClampMagnitude(Direction.Get(), 1f);
            bool isMoving = inputDirection.sqrMagnitude > 0;
            if (isMoving && !_wasMoving) {
                StartMove();
            } else if (!isMoving && _wasMoving) {
                StopMove();
            }
            _wasMoving = isMoving;
        
            float moveSpeed = 0f;
            if (isMoving) {
                // 【核心修改】：根据开关决定速度计算方式
                if (variableSpeed) {
                    moveSpeed = inputDirection.magnitude * movementSpeed;
                } else {
                    moveSpeed = movementSpeed;
                }
            }
        
            float cameraEulerY = playerCamera == null ? 0f : playerCamera.transform.eulerAngles.y;
            // float cameraEulerY = 0f;
            if (isMoving) {
                _facingRotation = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg + cameraEulerY;
                float interpolatedRotation = Mathf.SmoothDampAngle(_moveTarget.transform.eulerAngles.y, _facingRotation,
                    ref _rotationVelocity, 0.1f);
                _rb.MoveRotation(Quaternion.Euler(0.0f, interpolatedRotation, 0.0f));
            }
        
            Vector3 facingDirection = _moveTarget.transform.rotation * Vector3.forward;
            _rb.MovePosition(_rb.position + new Vector3(facingDirection.x * moveSpeed * Time.fixedDeltaTime, 0f, facingDirection.z * moveSpeed * Time.fixedDeltaTime));
            // if (movementType == RigidbodyMovementType.MovePosition) {
            //     _rb.MovePosition(transform.position + new Vector3(facingDirection.x * moveSpeed * Time.fixedDeltaTime, 0f, facingDirection.z * moveSpeed * Time.fixedDeltaTime));
            // } else if (movementType == RigidbodyMovementType.Velocity) {
            //     _rb.velocity = new Vector3(facingDirection.x * moveSpeed, _rb.velocity.y, facingDirection.z * moveSpeed);
            // }
        }

        private void FixedUpdate()
        {
            Move();
        }
    }
}

