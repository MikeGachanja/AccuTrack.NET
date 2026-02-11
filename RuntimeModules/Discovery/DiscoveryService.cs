namespace Runtime.Modules.Discovery;

/// <summary>Discovery service: holds transfer server and discovery responder.</summary>
public sealed class DiscoveryService : IDiscovery
{
    public ProjectTransferServer TransferServer { get; } = new();
    public DeviceDiscoveryResponder DiscoveryResponder { get; } = new();
}
