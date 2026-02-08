namespace AccuTrack.SDK.S7;

/// <summary>
/// Tag address format for S7: "DB1.DBD0", "DB2.DBW10", "DB1.DBX0.0" or "DB1,0,Real", "DB2,10,Int".
/// Parsed result used by Runtime TagIOHandler for read/write.
/// </summary>
public readonly struct S7ParsedAddress
{
    public bool IsValid { get; }
    public int DbNumber { get; }
    public int ByteOffset { get; }
    public int BitOffset { get; } // -1 for non-bit
    public string DataType { get; } // "Bool", "Byte", "Word", "DWord", "Int", "Real", etc.
    public string RawAddress { get; }

    public S7ParsedAddress(bool isValid, int dbNumber, int byteOffset, int bitOffset, string dataType, string rawAddress)
    {
        IsValid = isValid;
        DbNumber = dbNumber;
        ByteOffset = byteOffset;
        BitOffset = bitOffset;
        DataType = dataType ?? string.Empty;
        RawAddress = rawAddress ?? string.Empty;
    }

    public static S7ParsedAddress Invalid(string raw) => new(false, 0, 0, -1, string.Empty, raw ?? string.Empty);

    /// <summary>Convert to S7netplus address string (e.g. "DB1.DBD0", "DB2.DBX0.0").</summary>
    public string ToPlcAddress()
    {
        if (!IsValid) return RawAddress;
        if (BitOffset >= 0)
            return $"DB{DbNumber}.DBX{ByteOffset}.{BitOffset}";
        return DataType.ToUpperInvariant() switch
        {
            "BOOL" or "BIT" => $"DB{DbNumber}.DBX{ByteOffset}.0",
            "BYTE" or "BYTE" => $"DB{DbNumber}.DBB{ByteOffset}",
            "WORD" or "INT" or "UINT" => $"DB{DbNumber}.DBW{ByteOffset}",
            "DWORD" or "DINT" or "UDINT" or "REAL" => $"DB{DbNumber}.DBD{ByteOffset}",
            _ => $"DB{DbNumber}.DBD{ByteOffset}"
        };
    }
}

/// <summary>Parses S7 tag address strings to DB number, offset, and type.</summary>
public static class S7AddressParser
{
    /// <summary>Parse "DB1.DBD0", "DB2.DBW10", "DB1.DBX0.0", or "DB1,0,Real", "DB2,10,Int".</summary>
    public static S7ParsedAddress Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return S7ParsedAddress.Invalid(address ?? string.Empty);

        var s = address.Trim();
        var upper = s.ToUpperInvariant();

        // DB1.DBD0, DB2.DBW10, DB1.DBX0.0
        if (upper.StartsWith("DB") && upper.Contains('.'))
        {
            var dot = upper.IndexOf('.');
            if (dot < 0 || !int.TryParse(upper.AsSpan(2, dot - 2), out var dbNum) || dbNum < 0)
                return S7ParsedAddress.Invalid(s);
            var part = upper.AsSpan(dot + 1);
            if (part.StartsWith("DBX"))
            {
                var rest = part.Slice(3).ToString();
                var bits = rest.Split('.');
                if (bits.Length >= 1 && int.TryParse(bits[0], out var byteOff))
                {
                    var bitOff = bits.Length >= 2 && int.TryParse(bits[1], out var b) ? b : -1;
                    return new S7ParsedAddress(true, dbNum, byteOff, bitOff, "Bool", s);
                }
            }
            else if (part.StartsWith("DBB") && int.TryParse(part.Slice(3).ToString(), out var bOff))
                return new S7ParsedAddress(true, dbNum, bOff, -1, "Byte", s);
            else if (part.StartsWith("DBW") && int.TryParse(part.Slice(3).ToString(), out var wOff))
                return new S7ParsedAddress(true, dbNum, wOff, -1, "Word", s);
            else if (part.StartsWith("DBD") && int.TryParse(part.Slice(3).ToString(), out var dOff))
                return new S7ParsedAddress(true, dbNum, dOff, -1, "DWord", s);
        }

        // DB1,0,Real or DB2,10,Int
        if (upper.StartsWith("DB"))
        {
            var parts = s.Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[0].AsSpan(2), out var dbNum) && int.TryParse(parts[1].Trim(), out var offset))
            {
                var dataType = parts.Length >= 3 ? parts[2].Trim() : "DWord";
                return new S7ParsedAddress(true, dbNum, offset, -1, dataType, s);
            }
        }

        return S7ParsedAddress.Invalid(s);
    }

    public static bool IsValidFormat(string address)
    {
        var p = Parse(address);
        return p.IsValid;
    }
}
