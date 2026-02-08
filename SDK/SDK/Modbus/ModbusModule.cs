using System.IO.Ports;
using System.Text.Json;
using AccuTrack.SDK.Communication;

namespace AccuTrack.SDK.Modbus;

/// <summary>
/// Modbus communication module: extends CommunicationModuleBase, config from JSON (host, port, slaveId,
/// protocol TCP/RTU, serial settings, timeout, retries, addressOffset). Holds ModbusConnection for Runtime TagIOHandler.
/// </summary>
public class ModbusModule : CommunicationModuleBase
{
    public override string ModuleType => "Modbus";

    private string _host = string.Empty;
    private int _port = 502;
    private int _slaveId = 1;
    private string _protocol = "TCP";
    private string _portName = string.Empty;
    private int _baudRate = 9600;
    private int _dataBits = 8;
    private int _parity; // 0=None, 2=Even, 3=Odd
    private int _stopBits = 1;
    private int _timeout = 1000;
    private int _retries = 3;
    private int _addressOffset;
    private ModbusConnection? _connection;
    private readonly object _connectionLock = new();

    /// <summary>ModbusConnection for Device creation and Runtime TagIOHandler.</summary>
    public ModbusConnection? Connection
    {
        get { lock (_connectionLock) return _connection; }
    }

    public override bool Configure(JsonElement config)
    {
        if (!ValidateConfiguration(config))
        {
            OnErrorOccurred("Invalid Modbus configuration");
            return false;
        }

        Name = config.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
        _protocol = config.TryGetProperty("protocol", out var p) ? p.GetString() ?? "TCP" : "TCP";
        _slaveId = config.TryGetProperty("slaveId", out var s) ? s.GetInt32() : 1;
        _timeout = config.TryGetProperty("timeout", out var t) ? t.GetInt32() : 1000;
        if (_timeout <= 0) _timeout = 1000;
        _retries = config.TryGetProperty("retries", out var r) ? r.GetInt32() : 3;
        if (_retries < 0) _retries = 3;
        _addressOffset = config.TryGetProperty("addressOffset", out var a) ? a.GetInt32() : 0;

        if (_protocol == "TCP")
        {
            _host = config.TryGetProperty("host", out var h) ? h.GetString() ?? string.Empty : string.Empty;
            _port = config.TryGetProperty("port", out var port) ? port.GetInt32() : 502;
        }
        else
        {
            _portName = config.TryGetProperty("portName", out var pn) ? pn.GetString() ?? string.Empty : string.Empty;
            _baudRate = config.TryGetProperty("baudRate", out var b) ? b.GetInt32() : 9600;
            _dataBits = config.TryGetProperty("dataBits", out var d) ? d.GetInt32() : 8;
            if (config.TryGetProperty("parity", out var par))
            {
                if (par.ValueKind == JsonValueKind.String)
                {
                    var ps = par.GetString()?.ToUpperInvariant();
                    _parity = ps == "EVEN" ? 2 : ps == "ODD" ? 3 : 0;
                }
                else
                    _parity = par.GetInt32();
            }
            if (config.TryGetProperty("stopBits", out var sb))
            {
                if (sb.ValueKind == JsonValueKind.String)
                {
                    var sbs = sb.GetString();
                    _stopBits = sbs == "1.5" ? 3 : sbs == "2" ? 2 : 1;
                }
                else
                    _stopBits = sb.GetInt32();
            }
        }

        SetConfig(config);
        CreateConnection();
        return true;
    }

    public override bool ValidateConfiguration(JsonElement config)
    {
        if (!config.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
            return false;
        var protocol = config.TryGetProperty("protocol", out var p) ? p.GetString() ?? "TCP" : "TCP";
        if (protocol == "TCP")
        {
            if (!config.TryGetProperty("host", out var h) || h.ValueKind != JsonValueKind.String)
                return false;
        }
        else
        {
            if (!config.TryGetProperty("portName", out var pn) || pn.ValueKind != JsonValueKind.String)
                return false;
        }
        return true;
    }

    public override bool Start()
    {
        if (IsRunning) return true;
        if (string.IsNullOrEmpty(Name) && (_protocol != "TCP" || string.IsNullOrEmpty(_host)) && (_protocol != "RTU" || string.IsNullOrEmpty(_portName)))
        {
            OnErrorOccurred("Module not properly configured");
            return false;
        }

        lock (_connectionLock)
        {
            if (_connection == null)
            {
                OnErrorOccurred("Connection not created");
                return false;
            }
            if (_connection.Connect())
            {
                SetRunning(true);
                OnStatusChanged("Connected");
                return true;
            }
            OnErrorOccurred(_connection.LastError);
            return false;
        }
    }

    public override void Stop()
    {
        if (!IsRunning) return;
        lock (_connectionLock)
        {
            _connection?.Disconnect();
        }
        SetRunning(false);
        OnStatusChanged("Disconnected");
    }

    private void CreateConnection()
    {
        lock (_connectionLock)
        {
            _connection?.Dispose();
            _connection = new ModbusConnection();
            _connection.StateChanged += (_, state) => OnStatusChanged(state.ToString());
            _connection.ErrorOccurred += (_, err) => OnErrorOccurred(err);
            _connection.SetTimeout(_timeout);
            _connection.SetRetries(_retries);
            _connection.SetAddressOffset(_addressOffset);

            if (_protocol == "TCP")
                _connection.ConfigureTcp(_host, _port, _slaveId);
            else
                _connection.ConfigureRtu(_portName, _baudRate, _dataBits, ParityFromInt(_parity), StopBitsFromInt(_stopBits), _slaveId);
        }
    }

    private static Parity ParityFromInt(int p) => p switch { 2 => Parity.Even, 3 => Parity.Odd, _ => Parity.None };
    private static StopBits StopBitsFromInt(int s) => s switch { 3 => StopBits.OnePointFive, 2 => StopBits.Two, _ => StopBits.One };
}
