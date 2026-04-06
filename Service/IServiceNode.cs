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

/// <summary>
/// 引用端口接口。
/// 当 RefEnter 连接到 Next 时，目标 Service 保存此接口实现者的 id。
/// 目标调用 Next() 时会触发 OnRefActivated。
/// </summary>
public interface IRefPort
{
    string EnterId { get; set; }
    string NextId { get; set; }
    
    /// <summary>
    /// 当目标 Service 调用 Next() 时触发此方法。
    /// </summary>
    void OnRefActivated();
}
