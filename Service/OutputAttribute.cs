using System;
using UnityEngine;

namespace SP
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class OutputAttribute : PropertyAttribute
    {
    }
}
