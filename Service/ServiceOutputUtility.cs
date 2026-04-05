using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEngine;


public static class OutputUtility
{
    private const BindingFlags OutputFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static bool TryGetOutputField(Type serviceType, out FieldInfo outputField, out string error)
    {
        outputField = null;
        error = null;
        if (serviceType == null)
        {
            error = "Service type is null.";
            return false;
        }

        FieldInfo[] fields = serviceType.GetFields(OutputFieldFlags);
        int outputCount = 0;
        int i;
        for (i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field == null || !field.IsDefined(typeof(OutputAttribute), true))
            {
                continue;
            }

            outputCount++;
            outputField = field;
        }

        if (outputCount == 1 && outputField != null)
        {
            return true;
        }

        if (outputCount == 0)
        {
            error = serviceType.Name + " has no [Output] field.";
            return false;
        }

        error = serviceType.Name + " has multiple [Output] fields. Only one is allowed.";
        outputField = null;
        return false;
    }

    public static bool TryGetCompatibleOutputField(Type serviceType, Type targetType, out FieldInfo outputField, out string error)
    {
        outputField = null;
        if (!TryGetOutputField(serviceType, out outputField, out error))
        {
            return false;
        }

        if (targetType == null)
        {
            return true;
        }

        if (IsOutputCompatible(outputField.FieldType, targetType))
        {
            return true;
        }

        error = serviceType.Name + " output type " + outputField.FieldType.Name + " is not compatible with " + targetType.Name + ".";
        outputField = null;
        return false;
    }

    public static bool TryGetOutputValue<T>(MonoBehaviour outputProvider, out T value, out string error) where T : UnityEngine.Object
    {
        value = null;
        error = null;
        if (outputProvider == null)
        {
            error = "Output provider is null.";
            return false;
        }

        FieldInfo outputField;
        if (!TryGetCompatibleOutputField(outputProvider.GetType(), typeof(T), out outputField, out error))
        {
            return false;
        }

        object rawValue;
        if (!TryGetResolvedOutputValue(outputProvider, outputField, typeof(T), false, out rawValue, out error))
        {
            return false;
        }

        if (rawValue == null)
        {
            error = outputProvider.GetType().Name + " output value is null.";
            return false;
        }

        UnityEngine.Object unityObject = rawValue as UnityEngine.Object;
        if (unityObject == null)
        {
            error = outputProvider.GetType().Name + " output value is not a Unity object.";
            return false;
        }

        object convertedValue;
        if (!GlobalTypeUtility.TryConvertObject(unityObject, outputField.FieldType, typeof(T), out convertedValue))
        {
            error = outputProvider.GetType().Name + " output value cannot be converted to " + typeof(T).Name + ".";
            return false;
        }

        value = convertedValue as T;
        if (value != null)
        {
            return true;
        }

        error = outputProvider.GetType().Name + " output value cannot be converted to " + typeof(T).Name + ".";
        return false;
    }

    public static bool TryValidateOutputProvider(MonoBehaviour outputProvider, Type targetType, out string error)
    {
        error = null;
        if (outputProvider == null)
        {
            return true;
        }

        FieldInfo outputField;
        return TryGetCompatibleOutputField(outputProvider.GetType(), targetType, out outputField, out error);
    }

    public static bool TryGetOutputValue(MonoBehaviour outputProvider, Type targetType, out object value, out string error)
    {
        value = null;
        error = null;
        if (outputProvider == null)
        {
            error = "Output provider is null.";
            return false;
        }

        FieldInfo outputField;
        if (!TryGetCompatibleOutputField(outputProvider.GetType(), targetType, out outputField, out error))
        {
            return false;
        }

        object rawValue;
        if (!TryGetResolvedOutputValue(outputProvider, outputField, targetType, false, out rawValue, out error))
        {
            return false;
        }

        if (rawValue == null)
        {
            if (targetType == null || !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
            {
                return true;
            }

            error = outputProvider.GetType().Name + " output value is null.";
            return false;
        }

        if (targetType == null || targetType.IsInstanceOfType(rawValue))
        {
            value = rawValue;
            return true;
        }

        UnityEngine.Object unityObject = rawValue as UnityEngine.Object;
        if (unityObject != null)
        {
            object convertedObject;
            if (GlobalTypeUtility.TryConvertObject(unityObject, outputField.FieldType, targetType, out convertedObject))
            {
                value = convertedObject;
                return true;
            }
        }

        object convertedPrimitive;
        if (GlobalTypeUtility.TryConvertPrimitive(rawValue, targetType, out convertedPrimitive))
        {
            value = convertedPrimitive;
            return true;
        }

        error = outputProvider.GetType().Name + " output value cannot be converted to " + targetType.Name + ".";
        return false;
    }

    public static bool TryGetOutputListValue<T>(MonoBehaviour outputProvider, out List<T> values, out string error) where T : UnityEngine.Object
    {
        values = new List<T>();
        error = null;
        if (outputProvider == null)
        {
            error = "Output provider is null.";
            return false;
        }

        FieldInfo outputField;
        if (!TryGetOutputField(outputProvider.GetType(), out outputField, out error))
        {
            return false;
        }

        if (!IsListOutputCompatible(outputField.FieldType, typeof(T)))
        {
            error = outputProvider.GetType().Name + " output type " + outputField.FieldType.Name + " is not compatible with " + typeof(T).Name + " list.";
            return false;
        }

        object rawValue;
        if (!TryGetResolvedOutputValue(outputProvider, outputField, typeof(T), true, out rawValue, out error))
        {
            return false;
        }

        if (rawValue == null)
        {
            return true;
        }

        IEnumerable enumerable = rawValue as IEnumerable;
        if (enumerable == null)
        {
            error = outputProvider.GetType().Name + " output value is not a list.";
            return false;
        }

        Type elementType = GetListElementType(outputField.FieldType);
        foreach (object item in enumerable)
        {
            UnityEngine.Object unityObject = item as UnityEngine.Object;
            if (unityObject == null)
            {
                continue;
            }

            object convertedValue;
            if (!GlobalTypeUtility.TryConvertObject(unityObject, elementType, typeof(T), out convertedValue))
            {
                continue;
            }

            T convertedObject = convertedValue as T;
            if (convertedObject != null)
            {
                values.Add(convertedObject);
            }
        }

        return true;
    }

    public static bool IsOutputCompatible(Type outputType, Type targetType)
    {
        if (outputType == null || targetType == null)
        {
            return false;
        }

        if (GlobalTypeUtility.IsGameObjectTransformCompatible(outputType, targetType))
        {
            return true;
        }

        if (targetType == typeof(GameObject))
        {
            return outputType == typeof(GameObject) || typeof(Component).IsAssignableFrom(outputType);
        }

        if (targetType == typeof(Transform))
        {
            return outputType == typeof(GameObject) || typeof(Component).IsAssignableFrom(outputType);
        }

        if (typeof(Component).IsAssignableFrom(targetType))
        {
            return outputType == typeof(GameObject) || targetType.IsAssignableFrom(outputType);
        }

        return targetType.IsAssignableFrom(outputType);
    }

    public static bool IsListOutputCompatible(Type outputType, Type targetType)
    {
        Type elementType = GetListElementType(outputType);
        if (elementType == null)
        {
            return false;
        }

        return IsOutputCompatible(elementType, targetType);
    }

    private static bool TryGetResolvedOutputValue(MonoBehaviour outputProvider, FieldInfo outputField, Type targetType, bool expectsList, out object value, out string error)
    {
        value = null;
        error = null;
        if (outputProvider == null)
        {
            error = "Output provider is null.";
            return false;
        }

        if (outputField == null)
        {
            error = outputProvider.GetType().Name + " has no [Output] field.";
            return false;
        }

        value = outputField.GetValue(outputProvider);
        return true;
    }

    private static Type GetListElementType(Type outputType)
    {
        if (outputType == null)
        {
            return null;
        }

        if (outputType.IsArray)
        {
            return outputType.GetElementType();
        }

        if (outputType.IsGenericType && outputType.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type[] genericArguments = outputType.GetGenericArguments();
            if (genericArguments != null && genericArguments.Length == 1)
            {
                return genericArguments[0];
            }
        }

        return null;
    }
}

