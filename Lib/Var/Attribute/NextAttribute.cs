using System;

namespace SP
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NextAttribute : Attribute
    {
    }
}
