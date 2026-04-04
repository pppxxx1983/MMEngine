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
            NextService();
            return;
        }

        Transform target = source.Get();
        if (target == null)
        {
            NextService();
            return;
        }

        target.gameObject.SetActive(false);
        NextService();
    }
}
