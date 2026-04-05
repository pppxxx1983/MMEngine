using SP;
using UnityEngine;

public class Hide : Service
{
    [Input]
    public TransformVar source;
        private void OnEnable()
        {
        
        if (!source.ValidateAndLog(this))
        {
            Next();
            return;
        }

        Transform target = source.Get();
        if (target == null)
        {
            Next();
            return;
        }

        target.gameObject.SetActive(false);
        Next();
    }
}




