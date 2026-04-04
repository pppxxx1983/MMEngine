using System;

namespace SP
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class EnterAttribute : Attribute
    {
    }
}
