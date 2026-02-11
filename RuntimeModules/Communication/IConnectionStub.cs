namespace Runtime.Modules.Communication;

/// <summary>Stub connection (Modbus, OPC UA) for config/status pipeline. No real I/O.</summary>
public interface IConnectionStub
{
    void Start();
    void Stop();
}
