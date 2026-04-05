using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SP
{
    public static class MMVarValidationUtility
    {
        public static bool TryValidate(MMVar variable, Type expectedValueType, out string error)
        {
            error = null;
            if (variable == null)
            {
                return true;
            }

            InputType resolvedType = variable.GetResolvedInputType();
            return TryValidateSingle(resolvedType, variable.obj, variable.service, variable.global, expectedValueType, out error);
        }

        public static bool ValidateAndLog(MMVar variable, Type expectedValueType, UnityEngine.Object context = null)
        {
            string error;
            if (TryValidate(variable, expectedValueType, out error))
            {
                return true;
            }

            Debug.LogError(BuildErrorMessage(variable != null ? variable.tag : "MMVar", variable != null ? variable.GetResolvedInputType() : InputType.Default, expectedValueType, false, error), context);
            return false;
        }

        public static bool TryValidate(MMListVar variable, Type expectedValueType, out string error)
        {
            error = null;
            if (variable == null)
            {
                return true;
            }

            InputType resolvedType = variable.GetResolvedInputType();
            return TryValidateList(resolvedType, variable.objs, variable.service, variable.global, expectedValueType, out error);
        }

        public static bool ValidateAndLog(MMListVar variable, Type expectedValueType, UnityEngine.Object context = null)
        {
            string error;
            if (TryValidate(variable, expectedValueType, out error))
            {
                return true;
            }

            Debug.LogError(BuildErrorMessage(variable != null ? variable.tag : "MMListVar", variable != null ? variable.GetResolvedInputType() : InputType.Default, expectedValueType, true, error), context);
            return false;
        }

        public static bool TryValidateSingle(InputType inputType, GameObject obj, MonoBehaviour service, string globalKey, Type expectedValueType, out string error)
        {
            error = null;
            switch (inputType)
            {
                case InputType.Output:
                    return TryValidateService(service, expectedValueType, false, out error);
                case InputType.Global:
                    return TryValidateGlobal(globalKey, expectedValueType, false, out error);
                default:
                    return TryValidateDefaultObject(obj, expectedValueType, out error);
            }
        }

        public static bool TryValidateList(InputType inputType, List<GameObject> objs, MonoBehaviour service, string globalKey, Type expectedValueType, out string error)
        {
            error = null;
            switch (inputType)
            {
                case InputType.Output:
                    return TryValidateService(service, expectedValueType, true, out error);
                case InputType.Global:
                    return TryValidateGlobal(globalKey, expectedValueType, true, out error);
                default:
                    return TryValidateDefaultObjects(objs, expectedValueType, out error);
            }
        }

        public static bool TryValidateService(MonoBehaviour service, Type expectedValueType, bool expectsList, out string error)
        {
            error = null;
            if (service == null)
            {
                return true;
            }

            if (!expectsList)
            {
                return OutputUtility.TryValidateOutputProvider(service, expectedValueType, out error);
            }

            if (expectedValueType == null)
            {
                return true;
            }

            Type outputType = GetServiceOutputType(service);
            if (outputType == null)
            {
                error = service.GetType().Name + " has no valid [Output] field.";
                return false;
            }

            if (OutputUtility.IsListOutputCompatible(outputType, expectedValueType))
            {
                return true;
            }

            error = service.GetType().Name + " output type " + outputType.Name + " is not compatible with List<" + expectedValueType.Name + ">.";
            return false;
        }

        public static bool TryValidateGlobal(string globalKey, Type expectedValueType, bool expectsList, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(globalKey))
            {
                return true;
            }

            if (GlobalContext.ins == null)
            {
                error = "Global instance is missing.";
                return false;
            }

            string[] keys = GlobalContext.ins.GetKeys(expectedValueType, expectsList);
            if (Array.IndexOf(keys, globalKey) >= 0)
            {
                return true;
            }

            error = expectsList
                ? "Global key " + globalKey + " is not compatible with List<" + GetTypeName(expectedValueType) + ">."
                : "Global key " + globalKey + " is not compatible with " + GetTypeName(expectedValueType) + ".";
            return false;
        }

        public static bool TryValidateDefaultObject(GameObject obj, Type expectedValueType, out string error)
        {
            error = null;
            if (obj == null || expectedValueType == null || expectedValueType == typeof(GameObject))
            {
                return true;
            }

            if (expectedValueType == typeof(Transform))
            {
                return true;
            }

            if (!typeof(Component).IsAssignableFrom(expectedValueType))
            {
                return true;
            }

            if (obj.GetComponent(expectedValueType) != null)
            {
                return true;
            }

            error = obj.name + " is missing component " + expectedValueType.Name + ".";
            return false;
        }

        public static bool TryValidateDefaultObjects(List<GameObject> objs, Type expectedValueType, out string error)
        {
            error = null;
            if (objs == null || expectedValueType == null || expectedValueType == typeof(GameObject) || expectedValueType == typeof(Transform))
            {
                return true;
            }

            if (!typeof(Component).IsAssignableFrom(expectedValueType))
            {
                return true;
            }

            int i;
            for (i = 0; i < objs.Count; i++)
            {
                GameObject obj = objs[i];
                if (obj == null)
                {
                    continue;
                }

                if (obj.GetComponent(expectedValueType) != null)
                {
                    continue;
                }

                error = obj.name + " is missing component " + expectedValueType.Name + ".";
                return false;
            }

            return true;
        }

        private static Type GetServiceOutputType(MonoBehaviour service)
        {
            if (service == null)
            {
                return null;
            }

            FieldInfo outputField;
            string error;
            if (!OutputUtility.TryGetOutputField(service.GetType(), out outputField, out error))
            {
                return null;
            }

            return outputField != null ? outputField.FieldType : null;
        }

        private static string GetTypeName(Type type)
        {
            return type != null ? type.Name : "value";
        }

        private static string BuildErrorMessage(string tag, InputType inputType, Type expectedValueType, bool expectsList, string error)
        {
            string expectedTypeName = expectsList ? "List<" + GetTypeName(expectedValueType) + ">" : GetTypeName(expectedValueType);
            return tag + " validation failed. InputType=" + inputType + ", ExpectedType=" + expectedTypeName + ". " + error;
        }
    }
}


