using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SP
{
    
    public class SetColorTirgger : Service
    {
        
        [Header("Trigger Settings")]
        public List<Trigger> trigger1=new List<Trigger>();
        public List<Trigger> trigger2=new List<Trigger>();

        public List<GameObject> targets;
        public Color noCollider_color=Color.white;
        public Color collider_color=new Color(0,225,255);
        
        
        public override void Enter()
        {
            if (trigger1.Count == 0 || trigger2.Count == 0 || targets.Count ==0 )
            {
                Debug.LogError($"[{GetType().Name}] Missing required references.");
                NextService();
                return;
            }
        }

        public void SetColor(Color color)
        {
            
            for (int k = 0; k < targets.Count; k++)
            {
                var target = targets[k];                
                var sr = target.GetComponent<SpriteRenderer>();
                if(sr)
                    sr.color = color;
                
                var tm = target.GetComponent<TextMeshPro>();
                if(tm)
                    tm.color = color;
            }
        }
        public override void Update()
        {
            bool isEnter = false;
            for (int i = 0; i < trigger1.Count; i++)
            {
                for (int j = 0; j < trigger2.Count; j++)
                {
                    if(trigger1[i] ==  trigger2[j])
                        continue;

                    isEnter = trigger1[i].IsTrigger(trigger2[j]);
                    if (isEnter)
                        break;
                }

                if (isEnter)
                    break;
            }
            
            SetColor(isEnter?collider_color:noCollider_color);
            if (isEnter)
            {
                NextService();
                return;
            }
        }
    }
}

