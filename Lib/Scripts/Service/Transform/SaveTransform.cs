using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class TransformState:IMMVarTarget
    {
        public Transform Child;
        public Vector3 Position;
        public Quaternion  Rotation;
        public Vector3 Scale;
    }
    public class SaveTransform : Service
    {
        public TransformListVar source;

        [Output] public List<TransformState> outList = new List<TransformState>();
        private void OnEnable()
        {
            // if (!source.Validate(this) || !output.Validate(this))
            // {
            //     Next();
            //     return;
            // }

            List<Transform> targets = source.Get();
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning("SaveTransform source returned no targets.", this);
                Next();
                return;
            }

            outList.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null)
                    continue;

                outList.Add(new TransformState
                {
                    Child = target,
                    Position = target.position,
                    Rotation = target.rotation,
                    Scale = target.localScale
                });
            }


            Next();
        }
    }
}






