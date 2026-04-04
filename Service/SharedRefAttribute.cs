using System;

/// <summary>
/// 标记某个 Service 字段参与 Executor 级共享引用配置。
///
/// 支持两种字段：
/// 1. 单个 UnityEngine.Object 引用字段
/// 2. List<T>，其中 T : UnityEngine.Object
///
/// 用法示例：
/// [SharedRef("Target")]
/// [SerializeField] private Transform target;
///
/// [SharedRef("Targets")]
/// [SerializeField] private List<Transform> targets;
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class SharedRefAttribute : Attribute
{
    public string Key { get; }

    public SharedRefAttribute(string key)
    {
        Key = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }
}