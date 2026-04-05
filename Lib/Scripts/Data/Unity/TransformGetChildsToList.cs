using System;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class TransformGetChildsToList : Service, IFlowPort
    {
        public bool HasEnterPort => false;
        public bool HasNextPort => false;

        [Input]
        public TransformVar childParent;

        [Output]
        private List<Transform> childs = new List<Transform>();

        public override void Init()
        {
            Logic();
        }
        private void OnEnable()
        {
            Logic();
            Next();
        }

        public void Logic()
        {
            childs.Clear();
            if (childParent == null)
            {
                Debug.LogError("TransformGetChildsToList: childParent is null.", this);
                return;
            }

            string validateError;
            if (!childParent.TryValidate(out validateError))
            {
                Debug.LogError("TransformGetChildsToList: childParent validation failed. " + validateError, this);
                return;
            }

            Transform child = childParent.Get();
            if (child == null)
            {
                string sourceInfo = "InputType=" + childParent.GetResolvedInputType();
                if (childParent.GetResolvedInputType() == InputType.Global)
                {
                    sourceInfo += ", GlobalKey=" + (string.IsNullOrEmpty(childParent.global) ? "<empty>" : childParent.global);
                }

                Debug.LogError("TransformGetChildsToList: resolved childParent is null. " + sourceInfo, this);
                return;
            }

            for (int i = 0; i < child.childCount; i++)
            {
                childs.Add(child.GetChild(i));
            }
        }

    }
}






