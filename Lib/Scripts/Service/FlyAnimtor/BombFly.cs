using System.Collections.Generic;
using DG.Tweening;
using LYLib;
using UnityEngine;

namespace SP
{
    public class BombFly : Service
    {
        private class ChildFlyState
        {
            public Transform Child;
            public Vector3 StartPos;
            public Vector3 ControlPoint;
            public bool HasStarted;
            public bool IsExploding;
            public bool IsOver;
            public float Progress;
        }

        [SharedRef("oopTarget")]
        public TransformListVar flyTarget ;
        public TransformVar targetInput;
        public float ExplosionTime = 4f;
        public float toTargetTime = 4f;

        private readonly List<ChildFlyState> _children = new List<ChildFlyState>();
        private List<Transform> _targets;
        private Transform _target;
        private bool _hasCompleted;
        private void OnEnable()
        {
            // bool isFlyTargetValid = flyTarget.Get();
            // bool isTargetValid = targetInput.Get();
            // if (!isFlyTargetValid || !isTargetValid)
            // {
            //     Debug.LogError($"ChildsBombFly params are invalid. flyTargetValid={isFlyTargetValid}, targetInputValid={isTargetValid}", this);
            //     Next();
            //     return;
            // }

            _targets = flyTarget.Get();
            _target = targetInput.Get();
            if (_targets == null || _targets.Count == 0 || _target == null)
            {
                Debug.LogError("ChildsBombFly targets are invalid.", this);
                Next();
                return;
            }

            _hasCompleted = false;
            InitializeContainers();

            bool hasValidChild = false;
            for (int i = 0; i < _children.Count; i++)
            {
                int index = i;
                ChildFlyState state = _children[index];
                if (state.Child == null)
                {
                    state.IsOver = true;
                    continue;
                }

                hasValidChild = true;
                Vector3 explosionPos = Random.onUnitSphere * 2f + state.Child.position;
                state.Child.DOMove(explosionPos, ExplosionTime).OnComplete(() => MoveToTarget(index));
            }

            if (!hasValidChild)
            {
                _hasCompleted = true;
                Next();
            }
        }

        public void MoveToTarget(int index)
        {
            if (index < 0 || index >= _children.Count)
                return;

            ChildFlyState state = _children[index];
            if (state.Child == null)
            {
                state.IsOver = true;
                state.IsExploding = false;
                return;
            }

            state.HasStarted = true;
            state.IsExploding = true;
            state.IsOver = false;
            state.StartPos = state.Child.position;
            state.ControlPoint = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(2f, 8f),
                Random.Range(-5f, 5f)
            ) + state.StartPos;
            state.Progress = 0f;

        }

        private void Update()
        {
            if (_hasCompleted)
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                ChildFlyState state = _children[i];
                if (!state.IsExploding)
                    continue;

                if (state.Child == null)
                {
                    state.IsOver = true;
                    state.IsExploding = false;
                    continue;
                }

                float duration = Mathf.Max(0.0001f, toTargetTime);
                state.Progress += Time.deltaTime / duration;
                state.Child.position = state.StartPos.BezierCurve(_target.position, state.StartPos, state.Progress);

                if (state.Progress >= 1f)
                {
                    state.Child.position = _target.position;
                    state.IsOver = true;
                    state.IsExploding = false;
                }
            }

            for (int i = 0; i < _children.Count; i++)
            {
                ChildFlyState state = _children[i];
                if (state.Child == null)
                    continue;

                if (!state.HasStarted || !state.IsOver)
                    return;

                if ((state.Child.position - _target.position).sqrMagnitude > 0.0001f)
                    return;
            }

            _hasCompleted = true;
            Next();
        }

        private void InitializeContainers()
        {
            _children.Clear();

            for (int i = 0; i < _targets.Count; i++)
            {
                _children.Add(new ChildFlyState
                {
                    Child = _targets[i],
                    StartPos = Vector3.zero,
                    ControlPoint = Vector3.zero,
                    HasStarted = false,
                    IsExploding = false,
                    IsOver = false,
                    Progress = 0f
                });
            }
        }

    }
}






