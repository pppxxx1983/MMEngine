using UnityEditor;
using UnityEngine;
using SP;
using System.Reflection;

namespace PlayableFramework.Editor
{
    internal static class ConnectionPointDrawer
    {
        private static readonly Color LabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color PortOuterRingColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color PortInnerFillColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        public static void DrawFlowRow(
            ConnectionPoint point,
            Rect rowRect,
            Vector2 canvasOffset,
            ConnectionPoint draggingPoint)
        {
            if (point == null)
            {
                return;
            }

            Rect portRect = point.GetCanvasRect(canvasOffset);
            DrawPort(portRect, point.GetColor(draggingPoint));
            if (point.DrawOnLeft)
            {
                DrawLabel(BuildFlowLeftLabelRect(rowRect, portRect), point.Label, true);
                return;
            }

            DrawLabel(BuildFlowRightLabelRect(rowRect, portRect), point.Label, false);
        }

        public static void DrawDataRow(
            ConnectionPoint point,
            Rect rowRect,
            Vector2 canvasOffset,
            ConnectionPoint draggingPoint)
        {
            if (point == null)
            {
                return;
            }

            Rect portRect = point.GetCanvasRect(canvasOffset);
            DrawPort(portRect, point.GetColor(draggingPoint));
            if (point.PointType == ConnectionPointType.Output)
            {
                DrawDataOutputLabelOnly(rowRect, portRect, point.Label, point.DrawOnLeft);
                return;
            }
            string fieldName = GetPointFieldName(point);
            if (string.IsNullOrEmpty(fieldName)) fieldName = "Var";
            
            string variableName = string.IsNullOrEmpty(point.Label) ? "Var" : point.Label;
            string valueName = GetDataValueName(point);
            string slotText = valueName + "(" + variableName + ")";
            if (point.DrawOnLeft)
            {
                DrawLabelAndSlotLeft(rowRect, portRect, fieldName, slotText);
            }
            else
            {
                DrawLabelAndSlotRight(rowRect, portRect, fieldName, slotText);
            }
        }

        private static void DrawLabelAndSlotLeft(Rect rowRect, Rect portRect, string variableName, string slotText)
        {
            GUIStyle labelStyle = BuildLabelStyle(true);
            float labelWidth = Mathf.Clamp(labelStyle.CalcSize(new GUIContent(variableName)).x + 4f, 24f, Mathf.Max(24f, rowRect.width * 0.45f));
            float x = portRect.xMax + 6f;
            Rect labelRect = new Rect(x, rowRect.y, labelWidth, rowRect.height);
            DrawLabel(labelRect, variableName, true);

            float slotX = labelRect.xMax + 4f;
            float slotWidth = Mathf.Max(24f, rowRect.xMax - slotX - 2f);
            Rect slotRect = new Rect(slotX, rowRect.y + 2f, slotWidth, rowRect.height - 4f);
            DrawSlot(slotRect, slotText, true);
        }

        private static void DrawLabelAndSlotRight(Rect rowRect, Rect portRect, string variableName, string slotText)
        {
            GUIStyle labelStyle = BuildLabelStyle(false);
            float labelWidth = Mathf.Clamp(labelStyle.CalcSize(new GUIContent(variableName)).x + 4f, 24f, Mathf.Max(24f, rowRect.width * 0.45f));
            float left = rowRect.x + 2f;
            float rightLimit = portRect.xMin - 6f;
            Rect labelRect = new Rect(left, rowRect.y, Mathf.Min(labelWidth, Mathf.Max(24f, rightLimit - left - 28f)), rowRect.height);
            DrawLabel(labelRect, variableName, false);

            float slotX = labelRect.xMax + 4f;
            float slotWidth = Mathf.Max(24f, rightLimit - slotX);
            Rect slotRect = new Rect(slotX, rowRect.y + 2f, slotWidth, rowRect.height - 4f);
            DrawSlot(slotRect, slotText, false);
        }

        private static void DrawDataOutputLabelOnly(Rect rowRect, Rect portRect, string label, bool drawOnLeft)
        {
            string text = string.IsNullOrEmpty(label) ? "Output" : label;
            if (drawOnLeft)
            {
                DrawLabel(BuildFlowLeftLabelRect(rowRect, portRect), text, true);
                return;
            }

            DrawLabel(BuildFlowRightLabelRect(rowRect, portRect), text, false);
        }

        private static Rect BuildFlowLeftLabelRect(Rect rowRect, Rect portRect)
        {
            return new Rect(
                portRect.xMax + 6f,
                rowRect.y,
                Mathf.Max(16f, rowRect.width - GraphNode.PortSize - 10f),
                rowRect.height);
        }

        private static Rect BuildFlowRightLabelRect(Rect rowRect, Rect portRect)
        {
            float width = Mathf.Max(16f, portRect.xMin - rowRect.x - 6f);
            return new Rect(rowRect.x + 2f, rowRect.y, width, rowRect.height);
        }

        private static string GetDataValueName(ConnectionPoint point)
        {
            if (point == null)
            {
                return "None";
            }

            if (TryGetInputSourceDisplayText(point, out string sourceDisplayText))
            {
                return sourceDisplayText;
            }

            GraphManager manager = GraphManager.Instance;
            string managerResolved;
            if (manager != null && manager.TryGetDataPointObjectName(point, out managerResolved) && !string.IsNullOrEmpty(managerResolved))
            {
                return managerResolved;
            }

            if (point.Node == null)
            {
                return "None";
            }

            string resolved = point.Node.GetDataPointObjectName(point);
            return string.IsNullOrEmpty(resolved) ? "None" : resolved;
        }

        private static bool TryGetInputSourceDisplayText(ConnectionPoint point, out string displayText)
        {
            displayText = null;
            if (point == null || point.PointType != ConnectionPointType.Input || point.Node == null)
            {
                return false;
            }

            GraphManager manager = GraphManager.Instance;
            if (manager == null)
            {
                return false;
            }

            if (!manager.TryGetNodeObject(point.Node, out GameObject nodeObject) || nodeObject == null)
            {
                return false;
            }

            Service inputService = ResolveServiceInstance(nodeObject);
            if (inputService == null)
            {
                return false;
            }

            int inputIndex = point.Node.GetInputPointIndex(point);
            if (inputIndex < 0)
            {
                return false;
            }

            string inputFieldName = point.Node.GetInputFieldNameByIndex(inputIndex);
            if (string.IsNullOrEmpty(inputFieldName))
            {
                return false;
            }

            FieldInfo inputField = GetFieldFromTypeHierarchy(inputService.GetType(), inputFieldName);
            if (inputField == null)
            {
                return false;
            }

            object raw = inputField.GetValue(inputService);
            InputType inputType;
            string globalKey = null;
            if (raw is MMVar singleVar)
            {
                inputType = singleVar.GetResolvedInputType();
                if (inputType == InputType.Output)
                {
                    displayText = BuildOutputDisplayText(singleVar.service);
                    return true;
                }

                if (inputType == InputType.Default)
                {
                    displayText = BuildDefaultDisplayText(singleVar.obj);
                    return true;
                }

                if (inputType != InputType.Global)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(singleVar.global))
                {
                    displayText = "Global<None>";
                    return true;
                }

                globalKey = singleVar.global;
            }
            else if (raw is MMListVar listVar)
            {
                inputType = listVar.GetResolvedInputType();
                if (inputType == InputType.Output)
                {
                    displayText = BuildOutputDisplayText(listVar.service);
                    return true;
                }

                if (inputType == InputType.Default)
                {
                    GameObject firstObject = listVar.objs != null && listVar.objs.Count > 0 ? listVar.objs[0] : null;
                    displayText = BuildDefaultDisplayText(firstObject);
                    return true;
                }

                if (inputType != InputType.Global)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(listVar.global))
                {
                    displayText = "Global<None>";
                    return true;
                }

                globalKey = listVar.global;
            }
            else
            {
                return false;
            }

            if (!TryGetGlobalEntryInfo(globalKey, out GlobalValueType valueType, out MonoBehaviour provider))
            {
                return false;
            }

            if (valueType == GlobalValueType.OutputProvider && provider != null)
            {
                string gameObjectName = provider.gameObject != null ? provider.gameObject.name : "None";
                displayText = gameObjectName + "(" + provider.GetType().Name + ")";
                return true;
            }

            if (valueType == GlobalValueType.GameObject)
            {
                if (TryBuildGlobalGameObjectDisplayText(globalKey, out string gameObjectDisplay))
                {
                    displayText = gameObjectDisplay;
                    return true;
                }
            }

            displayText = "Global<" + valueType + ">";
            return true;
        }

        private static string BuildOutputDisplayText(MonoBehaviour service)
        {
            if (service == null)
            {
                return "Output<None>";
            }

            string gameObjectName = service.gameObject != null ? service.gameObject.name : service.name;
            return gameObjectName + "(Output)";
        }

        private static string BuildDefaultDisplayText(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "Default<None>";
            }

            return gameObject.name + "(Default)";
        }

        private static bool TryBuildGlobalGameObjectDisplayText(string globalKey, out string displayText)
        {
            displayText = null;
            if (string.IsNullOrEmpty(globalKey))
            {
                return false;
            }

            GlobalContext global = FindGlobalInstance();
            if (global == null)
            {
                return false;
            }

            if (!global.TryGetValue(globalKey, typeof(GameObject), out object rawValue) || rawValue == null)
            {
                return false;
            }

            GameObject gameObject = rawValue as GameObject;
            if (gameObject != null)
            {
                displayText = gameObject.name + "(GameObject)";
                return true;
            }

            Component component = rawValue as Component;
            if (component != null)
            {
                string ownerName = component.gameObject != null ? component.gameObject.name : component.name;
                displayText = ownerName + "(" + component.GetType().Name + ")";
                return true;
            }

            return false;
        }

        private static bool TryGetGlobalEntryInfo(string key, out GlobalValueType valueType, out MonoBehaviour outputProvider)
        {
            valueType = GlobalValueType.GameObject;
            outputProvider = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            GlobalContext global = FindGlobalInstance();
            if (global == null)
            {
                return false;
            }

            SerializedObject globalObject = new SerializedObject(global);
            SerializedProperty entriesProperty = globalObject.FindProperty("entries");
            if (entriesProperty == null || !entriesProperty.isArray)
            {
                return false;
            }

            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
                if (entry == null)
                {
                    continue;
                }

                SerializedProperty keyProperty = entry.FindPropertyRelative("key");
                if (keyProperty == null || !string.Equals(keyProperty.stringValue, key, System.StringComparison.Ordinal))
                {
                    continue;
                }

                SerializedProperty valueTypeProperty = entry.FindPropertyRelative("valueType");
                if (valueTypeProperty != null)
                {
                    valueType = (GlobalValueType)valueTypeProperty.enumValueIndex;
                }

                SerializedProperty providerProperty = entry.FindPropertyRelative("outputProvider");
                if (providerProperty != null)
                {
                    outputProvider = providerProperty.objectReferenceValue as MonoBehaviour;
                }

                return true;
            }

            return false;
        }

        private static GlobalContext FindGlobalInstance()
        {
            if (GlobalContext.ins != null)
            {
                return GlobalContext.ins;
            }

            GlobalContext[] globals = Resources.FindObjectsOfTypeAll<GlobalContext>();
            if (globals == null || globals.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < globals.Length; i++)
            {
                GlobalContext global = globals[i];
                if (global == null)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(global))
                {
                    continue;
                }

                return global;
            }

            return globals[0];
        }

        private static Service ResolveServiceInstance(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            Service[] services = nodeObject.GetComponents<Service>();
            for (int i = 0; i < services.Length; i++)
            {
                Service service = services[i];
                if (service != null && service.GetType() != typeof(Service))
                {
                    return service;
                }
            }

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] != null)
                {
                    return services[i];
                }
            }

            return null;
        }

        private static FieldInfo GetFieldFromTypeHierarchy(System.Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo match = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (match != null)
                {
                    return match;
                }

                type = type.BaseType;
            }

            return null;
        }
        private static string GetPointFieldName(ConnectionPoint point)
        {
            if (point == null || point.Node == null) return string.Empty;

            if (point.PointType == ConnectionPointType.Input)
            {
                int idx = point.Node.GetInputPointIndex(point);
                return point.Node.GetInputFieldNameByIndex(idx); // 杩欓噷杩斿洖 dataSource
            }

            if (point.PointType == ConnectionPointType.Output)
            {
                return point.Node.GetOutputFieldName();
            }

            return string.Empty;
        }
        private static GUIStyle BuildLabelStyle(bool isInputSide)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = LabelColor;
            labelStyle.clipping = TextClipping.Clip;
            labelStyle.fontSize = 10;
            labelStyle.alignment = isInputSide ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            return labelStyle;
        }

        private static void DrawLabel(Rect labelRect, string label, bool isInputSide)
        {
            GUIStyle labelStyle = BuildLabelStyle(isInputSide);
            GUI.Label(labelRect, label ?? "point", labelStyle);
        }

        private static void DrawSlot(Rect slotRect, string text, bool alignLeft)
        {
            GUI.Box(slotRect, GUIContent.none, EditorStyles.objectField);
            GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.alignment = alignLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            valueStyle.clipping = TextClipping.Clip;
            valueStyle.fontSize = 10;
            Rect textRect = new Rect(slotRect.x + 3f, slotRect.y, slotRect.width - 6f, slotRect.height);
            GUI.Label(textRect, text, valueStyle);
        }

        private static void DrawPort(Rect portRect, Color stateColor)
        {
            Vector2 center = portRect.center;
            float outerRadius = Mathf.Max(2f, portRect.width * 0.5f);
            float innerRadius = Mathf.Max(1f, outerRadius - 2f);
            float dotRadius = Mathf.Max(1.5f, outerRadius * 0.35f);

            Handles.BeginGUI();
            Handles.color = PortOuterRingColor;
            Handles.DrawSolidDisc(center, Vector3.forward, outerRadius);
            Handles.color = PortInnerFillColor;
            Handles.DrawSolidDisc(center, Vector3.forward, innerRadius);
            Handles.color = stateColor;
            Handles.DrawSolidDisc(center, Vector3.forward, dotRadius);
            Handles.EndGUI();
        }
    }
}

