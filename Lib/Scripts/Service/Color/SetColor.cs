using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SP
{
    public class SetColor : Service
    {
        public TransformListVar input;
        public Color targetColor = Color.white;
        private void OnEnable()
        {
            List<Transform> targets = input.Get();
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Transform current = targets[i];
                    if (current == null)
                        continue;

                    ApplyColor(current.gameObject, targetColor);
                }
            }

            Next();
        }

        internal static void ApplyColor(GameObject target, Color color)
        {
            if (target == null)
                return;

            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = color;

            TextMeshPro tm = target.GetComponent<TextMeshPro>();
            if (tm != null)
                tm.color = color;
        }
    }
}






