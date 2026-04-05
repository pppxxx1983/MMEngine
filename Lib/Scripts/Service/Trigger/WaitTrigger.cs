using UnityEngine;

namespace SP
{
    public class WaitTrigger : Service
    {
        [Input]
        public TriggerVar trigger1;
        [Input]
        public TriggerVar trigger2;
        private void OnEnable()
        {
            // if (!trigger1.Validate(this) || !trigger2.Validate(this))
            // {
            //     Next();
            //     return;
            // }
        }

        private void Update()
        {
            if (!trigger1.ValidateAndLog(this) || !trigger2.ValidateAndLog(this))
            {
                return;
            }
            Trigger leftTrigger = trigger1.Get();
            Trigger rightTrigger = trigger2.Get();
            if (leftTrigger == null || rightTrigger == null)
            {
                Next();

                return;
            }

            if (leftTrigger.IsTrigger(rightTrigger))
            {
                Next();
                return;
            }
        }
    }
}






