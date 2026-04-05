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
        private void OnEnable()
        {
            if (isCreateOnly && isCreate)
            {

                Next();
                return;
            }

            GameObject item = Root.Instance.resourceCenter.SpawnPrefab(itemType);
            outTransform = item != null ? item.transform : null;


            isCreate = true;
            Next();
        }
    }
}






