using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class PutTake : MonoBehaviour
    {
        public virtual int Count()
        {
            return 0;
        }


        public virtual Transform Take() { return null; }
        public virtual void Put(Transform t){}
    }
}
    

