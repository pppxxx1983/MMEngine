using UnityEngine;
using System.Reflection;

namespace SP
{
    public class GetComponent_Trigger : Service
    {
        [Input] public GameObjectVar inputObj;
        [Output]
        public Trigger trigger;
        private void OnEnable()
        {
            if (!inputObj.ValidateAndLog(this))
            {
                return;
            }

            var obj = inputObj.Get();
            if (obj != null)
            {
                trigger = obj.GetComponent<Trigger>();
            }

            Next();
        }

    }
}






