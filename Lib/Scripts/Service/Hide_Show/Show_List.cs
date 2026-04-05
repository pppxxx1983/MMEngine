using System.Collections.Generic;
using SP;
using UnityEngine;

public class Show_List : Service
{
    // [SharedRef("oopTargets")] 
    [Input]
    public GameObjectListVar source;
        private void OnEnable()
        {
        if (!source.ValidateAndLog(this))
        {
            Next();
            return;
        }

        List<GameObject> targets = source.Get();
        if (targets == null || targets.Count == 0)
        {
            Next();
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null)
                continue;

            target.SetActive(true);
        }

        Next();
    }
}




