using System;
using UnityEngine;

namespace SP
{
    [ExecuteAlways]
    public class Global_Int : GlobalValueService<int>
    {
        public override Type TargetValueType => typeof(int);

        [Output] private int value;

        protected override int DefaultValue => 0;

        protected override void SetOutputValue(int nextValue)
        {
            value = nextValue;
        }
    }
}
