using System.IO.Ports;
using System.Net.Sockets;
using NModbus;
using NModbus.Serial;

namespace AccuTrack.SDK.Modbus;

/// <summary>
/// Modbus TCP/RTU connection: Connect, Disconnect, Read/Write coils and registers.
/// Thread-safe; exposes state and events for Runtime/UI.
/// </summary>
public class ModbusConnection : IDisposable
{
    private readonly object _lock = new();
    private ModbusConnectionState _state = ModbusConnectionState.Disconnected;
    private string _lastError = string.Empty;
    private string _connectionType = "TCP"; // "TCP" or "RTU"
    private byte _slaveId = 1;
    private int _timeoutMs = 5000;
    private int _retries = 3;
    private int _addressOffset;
    private TcpClient? _tcpClient;
    private IModbusMaster? _master;
    private SerialPort? _serialPort;
    private SerialPortAdapter? _serialAdapter;
    private bool _disposed;

    // TCP
    private string _host = string.Empty;
    private int _port = 502;

    // RTU
    private string _portName = string.Empty;
    private int _baudRate = 9600;
    private int _dataBits = 8;
    private Parity _parity = Parity.None;
    private StopBits _stopBits = StopBits.One;

    public ModbusConnectionState State => _state;
    public string LastError => _lastError;
    public string ConnectionType => _connectionType;
    public byte SlaveId => _slaveId;
    public int AddressOffset => _addressOffset;

    public event EventHandler<ModbusConnectionState>? StateChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool ConfigureTcp(string host, int port = 502, int slaveId = 1)
    {
        lock (_lock)
        {
            if (_state == ModbusConnectionState.Connected)
                return false;
            _connectionType = "TCP";
            _host = host ?? string.Empty;
            _port = port;
            _slaveId = (byte)Math.Clamp(slaveId, 0, 255);
            return true;
        }
    }

    public bool ConfigureRtu(string portName, int baudRate = 9600, int dataBits = 8,
        Parity parity = Parity.None, StopBits stopBits = StopBits.One, int slaveId = 1)
    {
        lock (_lock)
        {
            if (_state == ModbusConnectionState.Connected)
                return false;
            _connectionType = "RTU";
            _portName = portName ?? string.Empty;
            _baudRate = baudRate;
            _dataBits = dataBits;
            _parity = parity;
            _stopBits = stopBits;
            _slaveId = (byte)Math.Clamp(slaveId, 0, 255);
            return true;
        }
    }

    public void SetTimeout(int timeoutMs) => _timeoutMs = Math.Max(100, timeoutMs);
    public void SetRetries(int retries) => _retries = Math.Max(0, retries);
    public void SetAddressOffset(int offset) => _addressOffset = Math.Max(0, offset);

    public bool Connect()
    {
        lock (_lock)
        {
            if (_state == ModbusConnectionState.Connected)
                return true;
            SetState(ModbusConnectionState.Connecting);
            try
            {
                if (_connectionType == "TCP")
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.ConnectAsync(_host, _port).GetAwaiter().GetResult();
                    if (!_tcpClient.Connected)
                    {
                        SetError("TCP connection failed");
                        SetState(ModbusConnectionState.Error);
                        return false;
                    }
                    var factory = new ModbusFactory();
                    _master = factory.CreateMaster(_tcpClient);
                }
                else
                {
                    _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits);
                    _serialPort.Open();
                    _serialAdapter = new SerialPortAdapter(_serialPort);
                    var factory = new ModbusFactory();
                    _master = factory.CreateRtuMaster(_serialAdapter);
                }
                SetState(ModbusConnectionState.Connected);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                SetState(ModbusConnectionState.Error);
                Cleanup();
                return false;
            }
        }
    }

    public bool Disconnect()
    {
        lock (_lock)
        {
            if (_state != ModbusConnectionState.Connected && _state != ModbusConnectionState.Connecting)
                return true;
            Cleanup();
            SetState(ModbusConnectionState.Disconnected);
            return true;
        }
    }

    public bool IsConnected => _state == ModbusConnectionState.Connected;

    private void Cleanup()
    {
        _master = null;
        try { _tcpClient?.Close(); _tcpClient?.Dispose(); } catch { }
        _tcpClient = null;
        try { _serialAdapter?.Dispose(); } catch { }
        _serialAdapter = null;
        try { _serialPort?.Close(); _serialPort?.Dispose(); } catch { }
        _serialPort = null;
    }

    private void SetState(ModbusConnectionState state)
    {
        if (_state == state) return;
        _state = state;
        StateChanged?.Invoke(this, state);
    }

    private void SetError(string error)
    {
        _lastError = error;
        ErrorOccurred?.Invoke(this, error);
    }

    private int ApplyOffset(int address) => address + _addressOffset;

    public bool ReadCoils(int startAddress, int quantity, out bool[] values)
    {
        values = Array.Empty<bool>();
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                values = _master.ReadCoils(_slaveId, (ushort)ApplyOffset(startAddress), (ushort)quantity);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool ReadDiscreteInputs(int startAddress, int quantity, out bool[] values)
    {
        values = Array.Empty<bool>();
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                values = _master.ReadInputs(_slaveId, (ushort)ApplyOffset(startAddress), (ushort)quantity);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool ReadHoldingRegisters(int startAddress, int quantity, out ushort[] values)
    {
        values = Array.Empty<ushort>();
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                values = _master.ReadHoldingRegisters(_slaveId, (ushort)ApplyOffset(startAddress), (ushort)quantity);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool ReadInputRegisters(int startAddress, int quantity, out ushort[] values)
    {
        values = Array.Empty<ushort>();
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                values = _master.ReadInputRegisters(_slaveId, (ushort)ApplyOffset(startAddress), (ushort)quantity);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool WriteSingleCoil(int address, bool value)
    {
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                _master.WriteSingleCoil(_slaveId, (ushort)ApplyOffset(address), value);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool WriteSingleRegister(int address, ushort value)
    {
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                _master.WriteSingleRegister(_slaveId, (ushort)ApplyOffset(address), value);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool WriteMultipleCoils(int startAddress, bool[] values)
    {
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                _master.WriteMultipleCoils(_slaveId, (ushort)ApplyOffset(startAddress), values);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public bool WriteMultipleRegisters(int startAddress, ushort[] values)
    {
        lock (_lock)
        {
            if (_master == null || _state != ModbusConnectionState.Connected)
                return false;
            try
            {
                _master.WriteMultipleRegisters(_slaveId, (ushort)ApplyOffset(startAddress), values);
                return true;
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
