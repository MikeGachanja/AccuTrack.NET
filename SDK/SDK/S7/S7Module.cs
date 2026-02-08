using System.Text.Json;
using AccuTrack.SDK.Communication;
using S7.Net;

namespace AccuTrack.SDK.S7;

/// <summary>
/// Siemens S7 communication module: ModuleType => "S7"; config from JSON (host, rack, slot, port).
/// Uses S7netplus to connect and read/write DB blocks; tag address format via S7AddressParser.
/// </summary>
public class S7Module : CommunicationModuleBase
{
    public override string ModuleType => "S7";

    private string _host = string.Empty;
    private int _rack = 0;
    private int _slot = 2;
    private int _port = 102;
    private Plc? _plc;
    private readonly object _plcLock = new();

    /// <summary>Plc instance for read/write. Null when not connected.</summary>
    public Plc? Plc
    {
        get { lock (_plcLock) return _plc; }
    }

    public override bool Configure(JsonElement config)
    {
        if (!ValidateConfiguration(config))
        {
            OnErrorOccurred("Invalid S7 configuration");
            return false;
        }

        Name = config.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
        _host = config.TryGetProperty("host", out var h) ? h.GetString() ?? string.Empty : string.Empty;
        _rack = config.TryGetProperty("rack", out var r) ? r.GetInt32() : 0;
        _slot = config.TryGetProperty("slot", out var s) ? s.GetInt32() : 2;
        _port = config.TryGetProperty("port", out var p) ? p.GetInt32() : 102;

        SetConfig(config);
        return true;
    }

    public override bool ValidateConfiguration(JsonElement config)
    {
        if (!config.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
            return false;
        if (!config.TryGetProperty("host", out var host) || host.ValueKind != JsonValueKind.String)
            return false;
        return true;
    }

    public override bool Start()
    {
        if (IsRunning) return true;
        if (string.IsNullOrEmpty(_host))
        {
            OnErrorOccurred("S7 host not configured");
            return false;
        }

        lock (_plcLock)
        {
            try
            {
                _plc = new Plc(CpuType.S71500, _host, (short)_rack, (short)_slot);
                _plc.Open();
                if (!_plc.IsConnected)
                {
                    OnErrorOccurred("S7 connection failed");
                    _plc = null;
                    return false;
                }
                SetRunning(true);
                OnStatusChanged("Connected");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex.Message);
                _plc = null;
                return false;
            }
        }
    }

    public override void Stop()
    {
        if (!IsRunning) return;
        lock (_plcLock)
        {
            try
            {
                _plc?.Close();
                _plc = null;
            }
            catch { }
        }
        SetRunning(false);
        OnStatusChanged("Disconnected");
    }

    /// <summary>Read value by address string (e.g. "DB1.DBD0", "DB2.DBW10"). Returns boxed value or null.</summary>
    public object? Read(string address)
    {
        lock (_plcLock)
        {
            if (_plc == null || !_plc.IsConnected)
                return null;
            try
            {
                var parsed = S7AddressParser.Parse(address);
                var plcAddr = parsed.IsValid ? parsed.ToPlcAddress() : address;
                return _plc.Read(plcAddr);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Write value by address string. Returns true on success.</summary>
    public bool Write(string address, object? value)
    {
        lock (_plcLock)
        {
            if (_plc == null || !_plc.IsConnected)
                return false;
            try
            {
                var parsed = S7AddressParser.Parse(address);
                var plcAddr = parsed.IsValid ? parsed.ToPlcAddress() : address;
                _plc.Write(plcAddr, value ?? 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
