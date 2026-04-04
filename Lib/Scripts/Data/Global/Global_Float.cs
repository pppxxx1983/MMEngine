using System;
using UnityEngine;

namespace SP
{
    [ExecuteAlways]
    public class Global_Float : GlobalValueService<float>
    {
        public override Type TargetValueType => typeof(float);

        [Output] private float value;

        protected override float DefaultValue => 0f;

        protected override void SetOutputValue(float nextValue)
        {
            value = nextValue;
        }
    }
}
