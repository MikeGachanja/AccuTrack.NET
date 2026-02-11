namespace Runtime.Modules.Discovery;

/// <summary>Discovery module interface: transfer server and device responder.</summary>
public interface IDiscovery
{
    ProjectTransferServer TransferServer { get; }
    DeviceDiscoveryResponder DiscoveryResponder { get; }
}
