using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class ResetTransform : Service
    {
        [Input]
        public SaveTransform saveTransform ;
        public bool setPosition;
        public UnityEngine.Vector3 position;
        public bool setRotation;
        public UnityEngine.Vector3 rotation;
        public bool setScale;
        public UnityEngine.Vector3 scale = UnityEngine.Vector3.one;

        public override void Enter()
        {
            if (saveTransform == null)
            {
                NextService();
                return;
            }

            List<TransformState> states = saveTransform.outList;
            if (states == null || states.Count == 0)
            {
                UnityEngine.Debug.LogWarning("ResetTransform input returned no states.", this);
                NextService();
                return;
            }

            for (int i = 0; i < states.Count; i++)
            {
                TransformState state = states[i];
                if (state == null || state.Child == null)
                    continue;
                
                state.Child.position = setPosition ? position : state.Position;
                state.Child.rotation = setRotation ? UnityEngine.Quaternion.Euler(rotation) : state.Rotation;
                state.Child.localScale = setScale ? scale : state.Scale;
            }

            NextService();
        }
    }
}

