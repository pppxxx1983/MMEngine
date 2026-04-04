using UnityEngine;

namespace SP
{
    public class CreateOneItem : Service
    {

        [ResourceName(ResourceCategory.Prefab)]
        public string itemType;


        public bool isCreateOnly = false;
        private bool isCreate;

        [Output] private Transform outTransform;

        public override void Enter()
        {
            if (isCreateOnly && isCreate)
            {

                NextService();
                return;
            }

            GameObject item = Root.Instance.resourceCenter.SpawnPrefab(itemType);
            outTransform = item != null ? item.transform : null;


            isCreate = true;
            NextService();
        }
    }
}

