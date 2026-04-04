using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
public sealed class ShowIfAttribute : PropertyAttribute
{
    public string ConditionFieldName { get; private set; }
    public int ExpectedValue { get; private set; }

    public ShowIfAttribute(string conditionFieldName, int expectedValue)
    {
        ConditionFieldName = conditionFieldName;
        ExpectedValue = expectedValue;
    }
}
