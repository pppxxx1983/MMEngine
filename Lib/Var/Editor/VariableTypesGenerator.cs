#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace SP.Editor
{
    public static class VariableTypesGenerator
    {
        private static readonly Type[] BuiltinTypes =
        {
            typeof(UnityEngine.Transform),
            typeof(UnityEngine.GameObject),
            typeof(UnityEngine.Camera),
            typeof(UnityEngine.Animator),
        };

        private const string OutputPath = "Assets/PlayableFramework/Lib/Var/VariableTypes.cs";

        [MenuItem("Tools/PlayableFramework/Generate Variable Types")]
        public static void GenerateFromMenu()
        {
            Generate();
        }

        public static void Generate()
        {
            List<Type> types = CollectTypes();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace SP");
            builder.AppendLine("{");

            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                string typeName = type.Name;
                string fullTypeName = GetTypeReference(type);

                builder.AppendLine("    [System.Serializable]");
                builder.AppendLine("    public class " + typeName + "Var : MMVar<" + fullTypeName + ">");
                builder.AppendLine("    {");
                builder.AppendLine("        public " + typeName + "Var() : base(\"" + typeName + "\") {}");
                builder.AppendLine("    }");
                builder.AppendLine();
                builder.AppendLine("    [System.Serializable]");
                builder.AppendLine("    public class " + typeName + "ListVar : MMListVar<" + fullTypeName + ">");
                builder.AppendLine("    {");
                builder.AppendLine("        public " + typeName + "ListVar() : base(\"" + typeName + "\") {}");
                builder.AppendLine("    }");
                if (i < types.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine("}");

            string absoluteOutputPath = Path.GetFullPath(OutputPath);
            string generatedCode = builder.ToString();
            if (File.Exists(absoluteOutputPath))
            {
                string currentCode = File.ReadAllText(absoluteOutputPath, Encoding.UTF8);
                if (string.Equals(currentCode, generatedCode, StringComparison.Ordinal))
                {
                    return;
                }
            }

            string outputDirectory = Path.GetDirectoryName(absoluteOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(absoluteOutputPath, generatedCode, Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static List<Type> CollectTypes()
        {
            List<Type> types = new List<Type>(BuiltinTypes);
            TypeCache.TypeCollection discoveredTypes = TypeCache.GetTypesDerivedFrom<SP.IMMVarTarget>();

            for (int i = 0; i < discoveredTypes.Count; i++)
            {
                Type type = discoveredTypes[i];
                if (type == null || type.IsAbstract || type.IsGenericType)
                {
                    continue;
                }

                if (!typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    continue;
                }

                if (types.Contains(type))
                {
                    continue;
                }

                types.Add(type);
            }

            types.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            return types;
        }

        private static string GetTypeReference(Type type)
        {
            if (type == null)
            {
                return "UnityEngine.Object";
            }

            if (string.IsNullOrEmpty(type.Namespace))
            {
                return "global::" + type.Name;
            }

            return "global::" + type.FullName.Replace('+', '.');
        }
    }
}
#endif
