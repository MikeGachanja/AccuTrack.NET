# Versioning System Analysis - AccuTrackQt Designer

## Overview
The AccuTrackQt Designer implemented a comprehensive versioning system with multiple levels of version tracking:

1. **Project-level versioning** - Tracks the overall project version
2. **SCADA project-level versioning** - Tracks individual SCADA project versions
3. **File format versioning** - Tracks serialization format versions for migration

## Implementation Details

### 1. Project Settings Version
**Location**: `ProjectSettings` struct in `projectmanager.h`

```cpp
struct ProjectSettings {
    QString version;  // Format: "major.minor.patch" (e.g., "1.0.0")
    // ... other fields
}
```

- Stored in the main project `.isc` file under `settings.version`
- Default value: `"1.0.0"`
- Used for project-level version tracking
- Accessible via `ProjectDirectory::getProjectVersion(projectPath)`

### 2. SCADA Project Version
**Location**: `ScadaProject` struct in `projectmanager.h`

```cpp
struct ScadaProject {
    QString version;  // Format: "major.minor.patch" (e.g., "1.0.0")
    // ... other fields
}
```

- Stored in the SCADA project's metadata file (`{scadaName}_metadata.json`)
- Default value: `"1.0.0"` when creating new SCADA projects
- Incremented automatically when building a project
- Persisted via `updateScadaMetadata()` method

### 3. Version Increment Logic
**Location**: `ProjectManager::incrementVersion()` in `projectmanager.cpp`

**Algorithm**:
- Parses version string as `"major.minor.patch"`
- Increments patch version: `1.0.0` → `1.0.1` → `1.0.2` → ... → `1.0.9`
- When patch reaches 9, resets to 0 and increments minor: `1.0.9` → `1.1.0`
- When minor reaches 9, resets to 0 and increments major: `1.9.9` → `2.0.0`

**Key Method**:
```cpp
QString ProjectManager::incrementVersion(const QString &version)
{
    // Parse "major.minor.patch"
    // Increment patch, handle rollover to minor/major
    return QString("%1.%2.%3").arg(major).arg(minor).arg(patch);
}
```

### 4. Version Increment Trigger
**Location**: `ProjectManager::incrementScadaProjectVersion()` and `mainwindow.cpp`

**When version is incremented**:
- Automatically called before building a SCADA project
- Triggered in `MainWindow::buildProject()` method
- Flow:
  1. Save project
  2. Increment SCADA project version
  3. Save project again (to persist version)
  4. Build project

**Code Flow**:
```cpp
// In mainwindow.cpp - Build Project
saveProject();
incrementScadaProjectVersion(selectedScada);
saveProject();  // Save updated version
// ... build process
```

### 5. Version Storage Locations

#### Project Level
- **File**: `{projectName}.isc` (main project file)
- **Path**: `settings.version`
- **Access**: `ProjectSettings.version`

#### SCADA Project Level
- **File**: `{scadaName}_metadata.json`
- **Path**: `metadata.version`
- **Access**: `ScadaProject.version`
- **Default**: `"1.0.0"` if not present

### 6. Version Retrieval

#### From Project File
```cpp
QString ProjectDirectory::getProjectVersion(const QString &projectPath)
{
    // Reads {projectName}.isc
    // Extracts settings.version
    return settingsObj["version"].toString();
}
```

#### From SCADA Metadata
```cpp
// In ProjectManager::openProject()
scada.version = metadata.contains("version") 
    ? metadata["version"].toString() 
    : "1.0.0";
```

### 7. Version Display
**Location**: `startuppage.cpp`

- Displayed in project table: `Project Name | Author | Version | Path`
- Retrieved via `ProjectDirectory::getProjectVersion()`
- Shows "Unknown" if version cannot be retrieved

## Key Differences: AccuTrackQt vs DarkStar

### AccuTrackQt (Previous Implementation)

#### Version Types
1. **Project Settings Version** (`ProjectSettings.version`)
   - Format: `"major.minor.patch"` (e.g., `"1.0.0"`)
   - Stored in: `.isc` file → `settings.version`
   - Purpose: Overall project version tracking
   - Default: `"1.0.0"`

2. **SCADA Project Version** (`ScadaProject.version`)
   - Format: `"major.minor.patch"` (e.g., `"1.0.0"`)
   - Stored in: `{scadaName}_metadata.json` → `version`
   - Purpose: Individual SCADA project version
   - Default: `"1.0.0"`
   - **Auto-incremented on build**

#### Version Increment Logic
```cpp
// AccuTrackQt - More sophisticated rollover
QString incrementVersion(const QString &version)
{
    // Parse "major.minor.patch"
    patch++;
    if (patch > 9) {
        patch = 0;
        minor++;
        if (minor > 9) {
            minor = 0;
            major++;
        }
    }
    return QString("%1.%2.%3").arg(major).arg(minor).arg(patch);
}
```
- **Rollover**: Patch (0-9) → Minor (0-9) → Major (unlimited)
- **Example**: `1.0.9` → `1.1.0` → `1.9.9` → `2.0.0`

#### Build Integration
- Version incremented **automatically** before build
- Flow: Save → Increment → Save → Build

#### Version Retrieval
- `ProjectDirectory::getProjectVersion(projectPath)` - Reads from `.isc`
- Displayed in startup page project table

---

### DarkStar (Current Implementation)

#### Version Types
1. **File Format Version** (`ProjectSerializer.CURRENT_PROJECT_VERSION`)
   - Format: Integer (e.g., `1`)
   - Stored in: `.isc` file → `version` (top-level)
   - Purpose: **Serialization format version** (for migration)
   - **NOT** the same as project version

2. **Component Format Version** (`ComponentSerializer.CURRENT_COMPONENT_VERSION`)
   - Format: Integer (e.g., `1`)
   - Stored in: Component JSON → `componentVersion`
   - Purpose: **Component serialization format version**

3. **SCADA Project Version** (`ScadaProject.Version`)
   - Format: `"major.minor.patch"` (e.g., `"1.0.0"`)
   - Stored in: Metadata file (likely `_metadata.json`)
   - Purpose: SCADA project version
   - Default: `"1.0.0"`

4. **Project Settings Version** (`ProjectSettings.Version`)
   - Format: `"major.minor.patch"` (e.g., `"1.0.0"`)
   - Stored in: `.isc` file → `settings.version`
   - Purpose: Project-level version
   - Default: `"1.0.0"`

#### Version Increment Logic
```csharp
// DarkStar - Simpler increment (only patch)
public static string IncrementVersion(string version)
{
    var parts = version.Split('.');
    if (parts.Length >= 3 && int.TryParse(parts[2], out int patch))
    {
        patch++;
        return $"{parts[0]}.{parts[1]}.{patch}";
    }
    return version;
}
```
- **No rollover**: Only increments patch (can go beyond 9)
- **Example**: `1.0.9` → `1.0.10` → `1.0.11` (no minor/major increment)

#### Migration Support
- `MigrateProject()` - Migrates project file format versions
- `MigrateComponent()` - Migrates component format versions
- **Separate from** semantic versioning

#### Build Integration
- Version increment method exists but **may not be auto-called**
- Check `MainForm.cs` or build handler for integration

## Recommendations for DarkStar

### 1. Enhance Version Increment Logic
**Current Issue**: DarkStar's `IncrementVersion()` only increments patch without rollover

**Recommendation**: Implement rollover logic similar to AccuTrackQt:
```csharp
public static string IncrementVersion(string version)
{
    var parts = version.Split('.');
    if (parts.Length >= 3 && 
        int.TryParse(parts[0], out int major) &&
        int.TryParse(parts[1], out int minor) &&
        int.TryParse(parts[2], out int patch))
    {
        patch++;
        if (patch > 9)
        {
            patch = 0;
            minor++;
            if (minor > 9)
            {
                minor = 0;
                major++;
            }
        }
        return $"{major}.{minor}.{patch}";
    }
    return version;
}
```

### 2. Automatic Version Increment on Build
**Current Status**: Method exists but may not be auto-called

**Recommendation**: Integrate into build process:
```csharp
// In build handler (e.g., CompilerModule or MainForm)
public bool BuildProject(string scadaName)
{
    // Save project
    SaveProject();
    
    // Increment version
    _projectManager.IncrementScadaProjectVersion(scadaName);
    _projectManager.SaveProject();
    
    // Build...
}
```

### 3. Version Display in UI
**Recommendation**: Add version column to project list (like AccuTrackQt startup page)

### 4. Clarify Version Types
**Current**: Multiple version types may cause confusion

**Recommendation**: Document clearly:
- **File Format Version** (`CURRENT_PROJECT_VERSION`): For migration, not user-facing
- **Project Version** (`ProjectSettings.Version`): User-facing project version
- **SCADA Version** (`ScadaProject.Version`): User-facing SCADA project version

### 5. Version Storage Consistency
**Recommendation**: Ensure SCADA version is stored in metadata file:
- Check `ScadaProject` serialization
- Verify metadata file includes version field
- Ensure version persists across saves

## Files to Review in DarkStar

### Core Versioning Files
- `DesignerModules/Project/ProjectSerializer.cs` - File format versioning & migration
- `DesignerModules/Project/ProjectManager.cs` - SCADA version increment logic
- `DesignerModules/Components/ComponentSerializer.cs` - Component versioning
- `DesignerModules/Project/ScadaProject.cs` - SCADA project structure & version storage
- `DesignerModules/Project/Project.cs` - Project structure & settings

### Integration Points
- `Designer/MainForm.cs` - Build process integration
- `DesignerModules/Compiler/CompilerModule.cs` - Build handler
- `DesignerModules/Project/ProjectView.cs` - Version display

## Action Items

1. ✅ **Review current implementation** - Understand existing versioning
2. ✅ **Enhance increment logic** - Add rollover support (implemented in `ProjectManager.IncrementVersion`)
3. ✅ **Verify build integration** - Auto-increment on build already in `MainForm.PerformBuild`
4. ✅ **Check version persistence** - Versions saved via `UpdateScadaMetadata` and in `.isc`; loaded in `LoadScadaProjectData`
5. ✅ **Add UI display** - SCADA version in project tree as "Name (vX.Y.Z)" + tooltip; startup page already has Version column
6. ⚠️ **Document version types** - Clarify purpose of each version field (see Recommendations above)
