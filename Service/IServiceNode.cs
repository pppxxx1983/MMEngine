public interface IGroupNode
{
}

public interface IFlowPort
{
    bool HasEnterPort { get; }
    bool HasNextPort { get; }
}

public interface IMirrorNode
{
    bool IsMirror { get; set; }
}
