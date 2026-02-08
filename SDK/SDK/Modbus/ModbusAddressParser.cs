using System.Text.RegularExpressions;

namespace AccuTrack.SDK.Modbus;

/// <summary>Data type for Modbus addresses.</summary>
public enum ModbusDataType
{
    Bool,
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float,
    Double,
    Unknown
}

/// <summary>Modbus function codes.</summary>
public enum ModbusFunctionCode
{
    ReadCoils = 0x01,
    ReadDiscreteInputs = 0x02,
    ReadHoldingRegisters = 0x03,
    ReadInputRegisters = 0x04,
    WriteSingleCoil = 0x05,
    WriteSingleRegister = 0x06,
    WriteMultipleCoils = 0x0F,
    WriteMultipleRegisters = 0x10,
    Unknown = 0x00
}

/// <summary>Parsed Modbus address used by Runtime TagIOHandler for read/write.</summary>
public readonly struct ModbusParsedAddress
{
    public bool IsValid { get; }
    public ModbusFunctionCode FunctionCode { get; }
    public ModbusDataType DataType { get; }
    public int StartAddress { get; }
    public int Quantity { get; }
    public int BitNumber { get; }
    public string RawAddress { get; }

    public ModbusParsedAddress(bool isValid, ModbusFunctionCode functionCode, ModbusDataType dataType,
        int startAddress, int quantity, int bitNumber, string rawAddress)
    {
        IsValid = isValid;
        FunctionCode = functionCode;
        DataType = dataType;
        StartAddress = startAddress;
        Quantity = quantity;
        BitNumber = bitNumber;
        RawAddress = rawAddress ?? string.Empty;
    }

    public static ModbusParsedAddress Invalid(string rawAddress)
        => new(false, ModbusFunctionCode.Unknown, ModbusDataType.Unknown, -1, 1, -1, rawAddress ?? string.Empty);
}

/// <summary>
/// Parses Modbus address strings (e.g. "40001", "0x0001", "holding:100", "MW100", "I0.0", "Q1.5")
/// to function type and offset for Runtime TagIOHandler.
/// </summary>
public static class ModbusAddressParser
{
    /// <summary>Parse a Modbus address string.</summary>
    public static ModbusParsedAddress Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return ModbusParsedAddress.Invalid(address ?? string.Empty);

        var raw = address.Trim();
        var addr = raw.ToUpperInvariant();

        ModbusParsedAddress result = addr.StartsWith("MW", StringComparison.Ordinal) ? ParseMw(addr) :
            addr.StartsWith("MD", StringComparison.Ordinal) ? ParseMd(addr) :
            addr.StartsWith("I", StringComparison.Ordinal) && (addr.Length == 1 || addr[1] == '.' || char.IsDigit(addr[1])) ? ParseI(addr) :
            addr.StartsWith("Q", StringComparison.Ordinal) && (addr.Length == 1 || addr[1] == '.' || char.IsDigit(addr[1])) ? ParseQ(addr) :
            addr.StartsWith("DB", StringComparison.Ordinal) ? ParseDb(addr) :
            char.IsDigit(addr[0]) ? ParseNumeric(addr) :
            ModbusParsedAddress.Invalid(raw);

        if (result.IsValid && !ValidateAddressRange(result))
            return ModbusParsedAddress.Invalid(raw);

        return result.RawAddress == raw ? result : new ModbusParsedAddress(
            result.IsValid, result.FunctionCode, result.DataType, result.StartAddress, result.Quantity, result.BitNumber, raw);
    }

    /// <summary>Validate address format without full range check.</summary>
    public static bool IsValidFormat(string address)
    {
        var p = Parse(address);
        return p.IsValid;
    }

    /// <summary>Get function code for read/write (e.g. ReadCoils -> WriteSingleCoil for write).</summary>
    public static ModbusFunctionCode GetFunctionCode(ModbusParsedAddress address, bool isWrite)
    {
        if (!address.IsValid) return ModbusFunctionCode.Unknown;
        if (isWrite)
        {
            return address.FunctionCode == ModbusFunctionCode.ReadCoils ? ModbusFunctionCode.WriteSingleCoil :
                address.FunctionCode == ModbusFunctionCode.ReadHoldingRegisters ? ModbusFunctionCode.WriteSingleRegister :
                address.FunctionCode;
        }
        return address.FunctionCode;
    }

    public static string FunctionCodeToString(ModbusFunctionCode code) => code switch
    {
        ModbusFunctionCode.ReadCoils => "Read Coils (0x01)",
        ModbusFunctionCode.ReadDiscreteInputs => "Read Discrete Inputs (0x02)",
        ModbusFunctionCode.ReadHoldingRegisters => "Read Holding Registers (0x03)",
        ModbusFunctionCode.ReadInputRegisters => "Read Input Registers (0x04)",
        ModbusFunctionCode.WriteSingleCoil => "Write Single Coil (0x05)",
        ModbusFunctionCode.WriteSingleRegister => "Write Single Register (0x06)",
        ModbusFunctionCode.WriteMultipleCoils => "Write Multiple Coils (0x0F)",
        ModbusFunctionCode.WriteMultipleRegisters => "Write Multiple Registers (0x10)",
        _ => "Unknown"
    };

    public static string DataTypeToString(ModbusDataType type) => type switch
    {
        ModbusDataType.Bool => "Bool",
        ModbusDataType.UInt16 => "UInt16",
        ModbusDataType.Int16 => "Int16",
        ModbusDataType.UInt32 => "UInt32",
        ModbusDataType.Int32 => "Int32",
        ModbusDataType.Float => "Float",
        ModbusDataType.Double => "Double",
        _ => "Unknown"
    };

    // MW<number>, MW<number>.<bit>, MW<number>:<qty>, MW<number>[<qty>]
    private static ModbusParsedAddress ParseMw(string address)
    {
        var m = Regex.Match(address, @"^MW(\d+)(?:\.(\d+))?(?::(\d+)|\[(\d+)\])?$");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var addr) || addr < 0 || addr > 65535)
            return ModbusParsedAddress.Invalid(address);

        int quantity = 1, bitNumber = -1;
        if (m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out var bit) && bit >= 0 && bit <= 15)
            bitNumber = bit;
        if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var q3) && q3 > 0 && q3 <= 125)
            quantity = q3;
        else if (m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var q4) && q4 > 0 && q4 <= 125)
            quantity = q4;

        var dataType = bitNumber >= 0 ? ModbusDataType.Bool : ModbusDataType.UInt16;
        return new ModbusParsedAddress(true, ModbusFunctionCode.ReadHoldingRegisters, dataType, addr, quantity, bitNumber, address);
    }

    private static ModbusParsedAddress ParseMd(string address)
    {
        var m = Regex.Match(address, @"^MD(\d+)$");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var addr) || addr < 0 || addr > 65535)
            return ModbusParsedAddress.Invalid(address);
        return new ModbusParsedAddress(true, ModbusFunctionCode.ReadHoldingRegisters, ModbusDataType.UInt32, addr, 2, -1, address);
    }

    // I<byte>.<bit>[:<qty>|\[<qty>\]]
    private static ModbusParsedAddress ParseI(string address)
    {
        var m = Regex.Match(address, @"^I(\d+)\.(\d+)(?::(\d+)|\[(\d+)\])?$");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var byteAddr) || byteAddr < 0 || byteAddr > 65535
            || !int.TryParse(m.Groups[2].Value, out var bit) || bit < 0 || bit > 7)
            return ModbusParsedAddress.Invalid(address);

        int quantity = 1;
        if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var q3) && q3 > 0 && q3 <= 2000) quantity = q3;
        else if (m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var q4) && q4 > 0 && q4 <= 2000) quantity = q4;

        int startAddress = byteAddr * 8 + bit;
        return new ModbusParsedAddress(true, ModbusFunctionCode.ReadDiscreteInputs, ModbusDataType.Bool, startAddress, quantity, bit, address);
    }

    private static ModbusParsedAddress ParseQ(string address)
    {
        var m = Regex.Match(address, @"^Q(\d+)\.(\d+)(?::(\d+)|\[(\d+)\])?$");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var byteAddr) || byteAddr < 0 || byteAddr > 65535
            || !int.TryParse(m.Groups[2].Value, out var bit) || bit < 0 || bit > 7)
            return ModbusParsedAddress.Invalid(address);

        int quantity = 1;
        if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var q3) && q3 > 0 && q3 <= 2000) quantity = q3;
        else if (m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var q4) && q4 > 0 && q4 <= 2000) quantity = q4;

        int startAddress = byteAddr * 8 + bit;
        return new ModbusParsedAddress(true, ModbusFunctionCode.ReadCoils, ModbusDataType.Bool, startAddress, quantity, bit, address);
    }

    // DB<number>.DB[DWB](<offset>)
    private static ModbusParsedAddress ParseDb(string address)
    {
        var m = Regex.Match(address, @"^DB(\d+)\.DB([DWB])(\d+)$");
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var dbNum) || dbNum < 0 || dbNum > 65535
            || !int.TryParse(m.Groups[3].Value, out var offset) || offset < 0 || offset > 65535)
            return ModbusParsedAddress.Invalid(address);

        var type = m.Groups[2].Value;
        ModbusDataType dataType; int quantity;
        if (type == "D") { dataType = ModbusDataType.UInt32; quantity = 2; }
        else if (type == "W") { dataType = ModbusDataType.UInt16; quantity = 1; }
        else if (type == "B") { dataType = ModbusDataType.UInt16; quantity = 1; }
        else return ModbusParsedAddress.Invalid(address);

        return new ModbusParsedAddress(true, ModbusFunctionCode.ReadHoldingRegisters, dataType, offset, quantity, -1, address);
    }

    // 4xxxx holding, 3xxxx input reg, 1xxxx coil, 0xxxx discrete
    private static ModbusParsedAddress ParseNumeric(string address)
    {
        var m = Regex.Match(address, @"^([0-4])(\d{4})(?::(\d+)|\[(\d+)\])?$");
        if (!m.Success || !int.TryParse(m.Groups[2].Value, out var addr) || addr < 1 || addr > 9999)
            return ModbusParsedAddress.Invalid(address);

        int startAddress = addr - 1;
        int typeDigit = int.Parse(m.Groups[1].Value);
        int maxQty;
        ModbusFunctionCode fc;
        ModbusDataType dt;
        switch (typeDigit)
        {
            case 0: fc = ModbusFunctionCode.ReadDiscreteInputs; dt = ModbusDataType.Bool; maxQty = 2000; break;
            case 1: fc = ModbusFunctionCode.ReadCoils; dt = ModbusDataType.Bool; maxQty = 2000; break;
            case 3: fc = ModbusFunctionCode.ReadInputRegisters; dt = ModbusDataType.UInt16; maxQty = 125; break;
            case 4: fc = ModbusFunctionCode.ReadHoldingRegisters; dt = ModbusDataType.UInt16; maxQty = 125; break;
            default: return ModbusParsedAddress.Invalid(address);
        }

        int quantity = 1;
        if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var q3) && q3 > 0 && q3 <= maxQty) quantity = q3;
        else if (m.Groups[4].Success && int.TryParse(m.Groups[4].Value, out var q4) && q4 > 0 && q4 <= maxQty) quantity = q4;

        return new ModbusParsedAddress(true, fc, dt, startAddress, quantity, -1, address);
    }

    private static bool ValidateAddressRange(ModbusParsedAddress address)
    {
        if (address.StartAddress < 0 || address.StartAddress > 65535) return false;
        int maxQty = address.FunctionCode is ModbusFunctionCode.ReadCoils or ModbusFunctionCode.ReadDiscreteInputs
            or ModbusFunctionCode.WriteMultipleCoils ? 2000 :
            address.FunctionCode is ModbusFunctionCode.ReadHoldingRegisters or ModbusFunctionCode.ReadInputRegisters
            or ModbusFunctionCode.WriteMultipleRegisters ? 125 : 1;
        if (address.Quantity < 1 || address.Quantity > maxQty) return false;
        if (address.StartAddress + address.Quantity - 1 > 65535) return false;
        return true;
    }
}
