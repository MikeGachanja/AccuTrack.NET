namespace AccuTrack.SDK.OPC;

/// <summary>
/// Tag address format for OPC: full NodeId string (e.g. "ns=2;s=MyVariable", "ns=2;i=1234")
/// or namespace index + identifier for Runtime TagIOHandler.
/// </summary>
public static class OPCAddressFormat
{
    /// <summary>Parse "ns=2;s=MyVar" or "2:MyVar" to namespace index and identifier. Returns (ns, id) or null if invalid.</summary>
    public static (ushort NamespaceIndex, string Identifier)? Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var s = address.Trim();

        // Full form: ns=2;s=MyVar or ns=2;i=1234
        if (s.StartsWith("ns=", StringComparison.OrdinalIgnoreCase))
        {
            var eq = s.IndexOf('=');
            var semi = s.IndexOf(';', eq + 1);
            if (eq < 0 || semi < 0)
                return null;
            if (!ushort.TryParse(s.Substring(eq + 1, semi - eq - 1), out var ns))
                return null;
            var rest = s.AsSpan(semi + 1);
            if (rest.StartsWith("s=", StringComparison.OrdinalIgnoreCase))
                return (ns, rest.Slice(2).ToString());
            if (rest.StartsWith("i=", StringComparison.OrdinalIgnoreCase))
                return (ns, "i=" + rest.Slice(2).ToString());
            return null;
        }

        // Short form: 2:MyVar or 2:1234
        var colon = s.IndexOf(':');
        if (colon > 0 && ushort.TryParse(s.AsSpan(0, colon), out var n))
            return (n, s.Substring(colon + 1));

        return null;
    }

    /// <summary>Build NodeId string from namespace and identifier (e.g. "s=MyVar" or "i=1234").</summary>
    public static string ToNodeIdString(ushort namespaceIndex, string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return $"ns={namespaceIndex};s=";
        if (identifier.StartsWith("i=", StringComparison.OrdinalIgnoreCase))
            return $"ns={namespaceIndex};{identifier}";
        return $"ns={namespaceIndex};s={identifier}";
    }
}
