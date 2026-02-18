# Example Lua Script: Tag Data & Alarms

Use this script in the Designer (Run) or deploy to Runtime. It uses `read_tag(name)` and `write_tag(name, value)` to interact with tags. Writing to tags that are linked to alarm conditions will trigger alarm evaluation (e.g. high/low limits, quality bad).

**Reference tags** (from `data/json/tags.json`):

| Tag             | Address | Type    | Use |
|-----------------|---------|---------|-----|
| mFIORun         | M0.0    | Bit     | Run command |
| mFIOReset       | M0.1    | Bit     | Reset |
| mFIOPause       | M0.2    | Bit     | Pause |
| mFIOCycles      | MD4     | Float32 | Cycle count |
| mFIOTimeScale   | MD0     | Float32 | Time scale |
| iSensorA        | M0.3    | Bit     | Sensor A input |
| ISensorB        | M0.4    | Bit     | Sensor B input |
| oConveyor       | M0.5    | Bit     | Conveyor output |
| oEntryConveyor  | M0.6    | Bit     | Entry conveyor output |

---

```lua
-- ============================================================
-- Example: Read tags, write tags, and influence alarms
-- Alarms trigger when tag values/quality change (configure in alarms.json).
-- ============================================================

print("=== Lua script: tag read/write and alarm interaction ===\n")

-- Read and print current tag values
local function show_tag(name)
  local v = read_tag(name)
  if v == nil then
    print(name .. " = (nil / not found)")
  else
    print(name .. " = " .. tostring(v) .. " [" .. type(v) .. "]")
  end
  return v
end

print("-- Current tag values --")
show_tag("mFIORun")
show_tag("mFIOReset")
show_tag("mFIOPause")
show_tag("mFIOCycles")
show_tag("mFIOTimeScale")
show_tag("iSensorA")
show_tag("ISensorB")
show_tag("oConveyor")
show_tag("oEntryConveyor")

print("\n-- Writing to tags (can trigger alarms if alarms are bound to these tags) --")

-- Write numeric tags (Float32). Writing out-of-range or "bad" values can trigger alarm conditions.
write_tag("mFIOCycles", 100)
write_tag("mFIOTimeScale", 1.0)
print("Written: mFIOCycles = 100, mFIOTimeScale = 1.0")

-- Write bit/boolean tags (Run, Reset, Pause, outputs)
write_tag("mFIORun", true)
write_tag("mFIOReset", false)
write_tag("mFIOPause", false)
print("Written: mFIORun = true, mFIOReset = false, mFIOPause = false")

-- Optional: set outputs (e.g. for testing)
write_tag("oConveyor", false)
write_tag("oEntryConveyor", false)
print("Written: oConveyor = false, oEntryConveyor = false")

print("\n-- Re-read after write --")
show_tag("mFIOCycles")
show_tag("mFIORun")

-- Example: simulate a "high" value that might trigger a high alarm if configured
-- write_tag("mFIOCycles", 9999)

print("\n=== Done ===")
```

---

## Copy-paste (script only)

```lua
print("=== Tag read/write example ===\n")
local function show_tag(name)
  local v = read_tag(name)
  print(name .. " = " .. (v == nil and "(nil)" or tostring(v)))
  return v
end
show_tag("mFIORun")
show_tag("mFIOCycles")
show_tag("mFIOTimeScale")
write_tag("mFIOCycles", 100)
write_tag("mFIOTimeScale", 1.0)
write_tag("mFIORun", true)
print("\nDone.")
```
