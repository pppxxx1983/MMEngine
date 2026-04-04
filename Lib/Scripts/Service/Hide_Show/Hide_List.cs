using System;
using System.Collections.Generic;
using SP;
using UnityEngine;

public class Hide_List : Service
{
    // [SharedRef("oopTargets")] 
    [Input]
    public GameObjectListVar source;

    public override void Enter()
    {

        if (!source.ValidateAndLog(this))
        {
            NextService();
            return;
        }

        List<GameObject> targets = source.Get();
        if (targets == null || targets.Count == 0)
        {
            NextService();
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null)
                continue;

            target.SetActive(false);
        }

        NextService();
    }
}

