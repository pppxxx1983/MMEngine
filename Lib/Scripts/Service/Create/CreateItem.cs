using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SP
{
    
    public class CreateItem : Service
    {
        [Header("Settings")]
        [ResourceName(ResourceCategory.Prefab)]
        public string itemType;
        public int itemCount;

        // public TransformListVar output;

        [Output] private List<Transform> outList=new List<Transform>();
        private void OnEnable()
        {
            outList.Clear();
            for (int i = 0; i < itemCount; i++)
            {
                var item = Root.Instance.resourceCenter.SpawnPrefab(itemType);
                if (item == null)
                    continue;

                outList.Add(item.transform);
            }
            Next();
        }


    }
}






