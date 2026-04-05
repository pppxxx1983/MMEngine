using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SP
{
    public class ChildModelOptimizer : MonoBehaviour
    {
        [Header("Source")]
        public Transform sourceRoot;
        public bool includeInactiveChildren = true;

        [Header("Output")]
        public Transform optimizedModel;
        public string optimizedObjectName = "OptimizedModel";
        public bool optimizeOnEnable = true;
        public bool debugLogs = false;

        [Header("Renderer")]
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
        public bool receiveShadows = false;
        public LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
        public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.Off;

        private class MaterialMeshBuilder
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector4> tangents = new List<Vector4>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<Color> colors = new List<Color>();
            public readonly List<int> indices = new List<int>();
            public bool hasNormals = true;
            public bool hasTangents = true;
            public bool hasUV = true;
            public bool hasColors = true;
        }

        private bool _isRebuilding;

        private void Awake()
        {
            if (!Application.isPlaying || _isRebuilding)
                return;

            EnsureOptimizedModelBuilt();
            SetOptimizationEnabled(optimizeOnEnable);
        }

        public void SetOptimizationEnabled(bool isOptimizationEnabled)
        {
            optimizeOnEnable = isOptimizationEnabled;

            if (!Application.isPlaying)
                return;

            if (debugLogs)
            {
                Debug.Log(
                    $"[ChildModelOptimizer] SetOptimizationEnabled enabled={isOptimizationEnabled}, root='{name}', " +
                    $"rootLocalScale={transform.localScale}, rootLossyScale={transform.lossyScale}, " +
                    $"optimizedExists={(optimizedModel != null)}, optimizedLocalScale={(optimizedModel != null ? optimizedModel.localScale.ToString() : "null")}, " +
                    $"optimizedLossyScale={(optimizedModel != null ? optimizedModel.lossyScale.ToString() : "null")}",
                    this
                );
            }

            if (optimizedModel != null)
                optimizedModel.gameObject.SetActive(isOptimizationEnabled);

            SetSourceObjectsVisible(!isOptimizationEnabled);
        }

        [ContextMenu("Enable Optimization")]
        public void EnableOptimization()
        {
            SetOptimizationEnabled(true);
        }

        [ContextMenu("Disable Optimization")]
        public void DisableOptimization()
        {
            SetOptimizationEnabled(false);
        }

        [ContextMenu("Rebuild Optimized Model")]
        public void RebuildOptimizedModel()
        {
            if (!Application.isPlaying || _isRebuilding)
                return;

            bool isOptimizationEnabled = optimizeOnEnable;
            DestroyOptimizedModel();
            EnsureOptimizedModelBuilt();
            SetOptimizationEnabled(isOptimizationEnabled);
        }

        private void EnsureOptimizedModelBuilt()
        {
            if (optimizedModel != null)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[ChildModelOptimizer] Skip build because optimizedModel already exists on '{name}', " +
                        $"optimizedLocalScale={optimizedModel.localScale}, optimizedLossyScale={optimizedModel.lossyScale}",
                        this
                    );
                }
                return;
            }

            optimizedModel = FindExistingOptimizedModel();
            if (optimizedModel != null)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[ChildModelOptimizer] Found existing optimizedModel for '{name}', " +
                        $"optimizedLocalScale={optimizedModel.localScale}, optimizedLossyScale={optimizedModel.lossyScale}",
                        this
                    );
                }
                return;
            }

            Transform source = sourceRoot != null ? sourceRoot : transform;
            List<MeshRenderer> renderers = CollectValidRenderers(source);
            if (renderers.Count == 0)
            {
                Debug.LogWarning($"ChildModelOptimizer found no valid MeshRenderer under '{source.name}'.", this);
                return;
            }

            _isRebuilding = true;

            GameObject optimizedObject = new GameObject(GetOptimizedModelName());
            Transform parent = transform.parent;
            optimizedObject.transform.SetParent(parent, false);
            optimizedObject.transform.position = transform.position;
            optimizedObject.transform.rotation = transform.rotation;
            optimizedObject.transform.localScale = transform.localScale;

            if (parent != null)
                optimizedObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);

            MeshFilter meshFilter = optimizedObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = optimizedObject.AddComponent<MeshRenderer>();

            Mesh mesh = BuildCombinedMesh(renderers, optimizedObject.transform, out Material[] materials);
            if (mesh == null)
            {
                Debug.LogWarning("ChildModelOptimizer failed to build combined mesh.", this);
                Destroy(optimizedObject);
                _isRebuilding = false;
                return;
            }

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterials = materials;
            meshRenderer.shadowCastingMode = shadowCastingMode;
            meshRenderer.receiveShadows = receiveShadows;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.lightProbeUsage = lightProbeUsage;
            meshRenderer.reflectionProbeUsage = reflectionProbeUsage;

            optimizedModel = optimizedObject.transform;

            if (debugLogs)
            {
                Debug.Log(
                    $"[ChildModelOptimizer] Built optimizedModel for '{name}', " +
                    $"source='{source.name}', rendererCount={renderers.Count}, " +
                    $"rootLocalScale={transform.localScale}, rootLossyScale={transform.lossyScale}, " +
                    $"optimizedLocalScale={optimizedModel.localScale}, optimizedLossyScale={optimizedModel.lossyScale}",
                    this
                );
            }

            _isRebuilding = false;
        }

        private Transform FindExistingOptimizedModel()
        {
            Transform parent = transform.parent;
            if (parent == null)
                return null;

            string modelName = GetOptimizedModelName();
            Transform existing = parent.Find(modelName);
            if (existing == null)
                return null;

            if (ReferenceEquals(existing, transform))
                return null;

            return existing;
        }

        private string GetOptimizedModelName()
        {
            string baseName = string.IsNullOrEmpty(optimizedObjectName) ? "OptimizedModel" : optimizedObjectName;
            return $"{transform.name}_{transform.GetInstanceID()}_{baseName}";
        }

        private void SetSourceObjectsVisible(bool visible)
        {
            Transform source = sourceRoot != null ? sourceRoot : transform;
            for (int i = 0; i < source.childCount; i++)
            {
                Transform child = source.GetChild(i);
                if (child == null)
                    continue;

                if (optimizedModel != null && child == optimizedModel)
                    continue;

                child.gameObject.SetActive(visible);
            }
        }

        private void DestroyOptimizedModel()
        {
            if (optimizedModel == null)
                return;

            MeshFilter meshFilter = optimizedModel.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }

            Destroy(optimizedModel.gameObject);
            optimizedModel = null;
        }

        private List<MeshRenderer> CollectValidRenderers(Transform source)
        {
            List<MeshRenderer> validRenderers = new List<MeshRenderer>();
            if (source == null)
                return validRenderers;

            MeshRenderer[] renderers = source.GetComponentsInChildren<MeshRenderer>(includeInactiveChildren);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer meshRenderer = renderers[i];
                if (meshRenderer == null)
                    continue;

                if (optimizedModel != null && meshRenderer.transform.IsChildOf(optimizedModel))
                    continue;

                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                validRenderers.Add(meshRenderer);
            }

            return validRenderers;
        }

        private static Mesh BuildCombinedMesh(List<MeshRenderer> renderers, Transform root, out Material[] finalMaterials)
        {
            finalMaterials = null;

            Dictionary<Material, MaterialMeshBuilder> builderMap = new Dictionary<Material, MaterialMeshBuilder>();
            List<Material> materialOrder = new List<Material>();

            for (int i = 0; i < renderers.Count; i++)
            {
                MeshRenderer meshRenderer = renderers[i];
                if (meshRenderer == null)
                    continue;

                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                Mesh sourceMesh = meshFilter.sharedMesh;
                Material[] materials = meshRenderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                    continue;

                Vector3[] sourceVertices = sourceMesh.vertices;
                Vector3[] sourceNormals = sourceMesh.normals;
                Vector4[] sourceTangents = sourceMesh.tangents;
                Vector2[] sourceUVs = sourceMesh.uv;
                Color[] sourceColors = sourceMesh.colors;

                bool meshHasNormals = sourceNormals != null && sourceNormals.Length == sourceVertices.Length;
                bool meshHasTangents = sourceTangents != null && sourceTangents.Length == sourceVertices.Length;
                bool meshHasUV = sourceUVs != null && sourceUVs.Length == sourceVertices.Length;
                bool meshHasColors = sourceColors != null && sourceColors.Length == sourceVertices.Length;

                Matrix4x4 localToRoot = root.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                Matrix4x4 normalMatrix = localToRoot.inverse.transpose;

                int subMeshCount = Mathf.Min(sourceMesh.subMeshCount, materials.Length);
                for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                {
                    Material material = materials[subMeshIndex];
                    if (material == null)
                        continue;

                    if (!builderMap.TryGetValue(material, out MaterialMeshBuilder builder))
                    {
                        builder = new MaterialMeshBuilder();
                        builderMap.Add(material, builder);
                        materialOrder.Add(material);
                    }

                    builder.hasNormals &= meshHasNormals;
                    builder.hasTangents &= meshHasTangents;
                    builder.hasUV &= meshHasUV;
                    builder.hasColors &= meshHasColors;

                    int[] sourceIndices = sourceMesh.GetIndices(subMeshIndex);
                    Dictionary<int, int> remap = new Dictionary<int, int>();

                    for (int index = 0; index < sourceIndices.Length; index++)
                    {
                        int sourceIndex = sourceIndices[index];
                        if (!remap.TryGetValue(sourceIndex, out int destinationIndex))
                        {
                            destinationIndex = builder.vertices.Count;
                            remap.Add(sourceIndex, destinationIndex);

                            builder.vertices.Add(localToRoot.MultiplyPoint3x4(sourceVertices[sourceIndex]));

                            if (meshHasNormals)
                                builder.normals.Add(normalMatrix.MultiplyVector(sourceNormals[sourceIndex]).normalized);

                            if (meshHasTangents)
                            {
                                Vector4 tangent = sourceTangents[sourceIndex];
                                Vector3 tangentVector = normalMatrix.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
                                builder.tangents.Add(new Vector4(tangentVector.x, tangentVector.y, tangentVector.z, tangent.w));
                            }

                            if (meshHasUV)
                                builder.uvs.Add(sourceUVs[sourceIndex]);

                            if (meshHasColors)
                                builder.colors.Add(sourceColors[sourceIndex]);
                        }

                        builder.indices.Add(destinationIndex);
                    }
                }
            }

            if (materialOrder.Count == 0)
                return null;

            List<Vector3> finalVertices = new List<Vector3>();
            List<Vector3> finalNormals = new List<Vector3>();
            List<Vector4> finalTangents = new List<Vector4>();
            List<Vector2> finalUVs = new List<Vector2>();
            List<Color> finalColors = new List<Color>();
            List<int[]> finalSubMeshes = new List<int[]>();
            List<Material> finalMaterialList = new List<Material>();

            bool useNormals = true;
            bool useTangents = true;
            bool useUV = true;
            bool useColors = true;

            for (int i = 0; i < materialOrder.Count; i++)
            {
                MaterialMeshBuilder builder = builderMap[materialOrder[i]];
                useNormals &= builder.hasNormals;
                useTangents &= builder.hasTangents;
                useUV &= builder.hasUV;
                useColors &= builder.hasColors;
            }

            int vertexOffset = 0;
            for (int i = 0; i < materialOrder.Count; i++)
            {
                Material material = materialOrder[i];
                MaterialMeshBuilder builder = builderMap[material];

                finalVertices.AddRange(builder.vertices);
                if (useNormals)
                    finalNormals.AddRange(builder.normals);
                if (useTangents)
                    finalTangents.AddRange(builder.tangents);
                if (useUV)
                    finalUVs.AddRange(builder.uvs);
                if (useColors)
                    finalColors.AddRange(builder.colors);

                int[] triangles = new int[builder.indices.Count];
                for (int j = 0; j < builder.indices.Count; j++)
                {
                    triangles[j] = builder.indices[j] + vertexOffset;
                }

                finalSubMeshes.Add(triangles);
                finalMaterialList.Add(material);
                vertexOffset += builder.vertices.Count;
            }

            Mesh finalMesh = new Mesh();
            finalMesh.name = "OptimizedCombinedMesh";

            if (finalVertices.Count > 65535)
                finalMesh.indexFormat = IndexFormat.UInt32;

            finalMesh.SetVertices(finalVertices);

            if (useNormals && finalNormals.Count == finalVertices.Count)
                finalMesh.SetNormals(finalNormals);
            if (useTangents && finalTangents.Count == finalVertices.Count)
                finalMesh.SetTangents(finalTangents);
            if (useUV && finalUVs.Count == finalVertices.Count)
                finalMesh.SetUVs(0, finalUVs);
            if (useColors && finalColors.Count == finalVertices.Count)
                finalMesh.SetColors(finalColors);

            finalMesh.subMeshCount = finalSubMeshes.Count;
            for (int i = 0; i < finalSubMeshes.Count; i++)
            {
                finalMesh.SetTriangles(finalSubMeshes[i], i, true);
            }

            if (!useNormals)
                finalMesh.RecalculateNormals();

            finalMesh.RecalculateBounds();
            finalMaterials = finalMaterialList.ToArray();
            return finalMesh;
        }
    }
}

