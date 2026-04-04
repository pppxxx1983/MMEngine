using System;
using UnityEngine;

namespace SP
{
    [ExecuteAlways]
    public class Global_Vector2 : GlobalValueService<Vector2>
    {
        public override Type TargetValueType => typeof(Vector2);

        [Output] private Vector2 value;

        protected override Vector2 DefaultValue => Vector2.zero;

        protected override void SetOutputValue(Vector2 nextValue)
        {
            value = nextValue;
        }
    }
}
