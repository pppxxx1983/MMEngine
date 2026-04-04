using UnityEngine;

/// <summary>
/// ResourceCenter / ObjectPool 资源分类。
/// 供 Inspector 下拉和资源查询共同使用。
/// </summary>
/// <summary>
/// 给 string 字段打上资源分类标记。
/// 编辑器会根据 category，从 Root 下的资源中心读取对应名称列表并绘制成下拉。
///
/// 用法：
/// [ResourceName(ResourceCategory.Effect)]
/// public string effectName;
/// </summary>
public sealed class ResourceNameAttribute : PropertyAttribute
{
    public readonly ResourceCategory category;

    public ResourceNameAttribute(ResourceCategory category)
    {
        this.category = category;
    }
}