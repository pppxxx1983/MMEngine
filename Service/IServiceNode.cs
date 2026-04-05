using System.Collections.Generic;

/// <summary>
/// 分组节点接口。
/// 创建节点时会根据这个名字，把对象挂到 Root 下对应名字的父节点下面。
/// </summary>
public interface IGroupNode
{
    /// <summary>
    /// Root 下的父节点名字。
    /// 为空时直接挂到 Graph 下。
    /// </summary>
    string GroupParentName { get; }
}

/// <summary>
/// 单连接引导节点接口。
/// 保存一个 Enter 和一个 Next 的节点 Id 以及 Service 引用。
/// 任何和 IGuideNode 相连的流程线，都只保存引用，不改变双方父子关系。
/// </summary>
public interface IGuideNode
{
    string EnterId { get; set; }
    string NextId { get; set; }
    SP.Service EnterService { get; set; }
    SP.Service NextService { get; set; }
}

/// <summary>
/// 单个下一步 Service 接口。
/// Service.Next() 时如果这里有值，会激活这个 Service。
/// </summary>
public interface INextServiceNode
{
    SP.Service NextService { get; set; }
}

/// <summary>
/// 多连接引导节点接口。
/// 可以保存多个 Enter 和多个 Next 的节点 Id 以及 Service 引用。
/// 任何和 IMultiGuideNode 相连的流程线，都只保存引用，不改变双方父子关系。
/// </summary>
public interface IMultiGuideNode
{
    List<string> EnterIds { get; }
    List<string> NextIds { get; }
    List<SP.Service> EnterServices { get; }
    List<SP.Service> NextServices { get; }
}

/// <summary>
/// 多个下一步 Service 接口。
/// Service.Next() 时会依次激活列表里的所有 Service。
/// </summary>
public interface IMultiNextServiceNode
{
    List<SP.Service> NextServices { get; }
}

/// <summary>
/// 控制节点是否显示 Enter / Next 流程口。
/// </summary>
public interface IFlowPort
{
    bool HasEnterPort { get; }
    bool HasNextPort { get; }
}

/// <summary>
/// 控制节点是否支持镜像显示。
/// </summary>
public interface IMirrorNode
{
    bool IsMirror { get; set; }
}
