namespace Designer.Modules.Events;

/// <summary>
/// Event categories for organizing event actions.
/// </summary>
public enum EventCategory
{
    ScreenNavigation = 0,
    TagOperations = 1,
    ScriptActions = 2,
    ComponentControl = 3,
    SystemActions = 4,
    DataOperations = 5,
    Security = 6
}

/// <summary>
/// Event action types.
/// </summary>
public enum ActionType
{
    // Screen Navigation
    NavigateScreen = 0,
    CloseScreen = 1,
    SwitchToScreen = 2,
    PreviousScreen = 3,
    NextScreen = 4,
    ShowDialog = 5,
    CloseDialog = 6,
    
    // Tag Operations
    SetBit = 10,
    ResetBit = 11,
    ToggleBit = 12,
    WriteTag = 13,
    IncrementTag = 14,
    DecrementTag = 15,
    CopyTagValue = 16,
    SwapTagValues = 17,
    
    // Script Actions
    RunScript = 20,
    StopScript = 21,
    PauseScript = 22,
    ResumeScript = 23,
    ExecuteFunction = 24,
    
    // Component Control
    ShowComponent = 30,
    HideComponent = 31,
    EnableComponent = 32,
    DisableComponent = 33,
    MoveComponent = 34,
    ResizeComponent = 35,
    ChangeStyle = 36,
    StartAnimation = 37,
    StopAnimation = 38,
    
    // System Actions
    StartProcess = 40,
    StopProcess = 41,
    RestartApplication = 42,
    LogEvent = 43,
    ClearLogs = 44,
    SystemBackup = 45,
    SystemRestore = 46,
    PrintScreen = 47,
    
    // Data Operations
    ImportData = 50,
    ExportData = 51,
    ClearData = 52,
    SaveSettings = 53,
    LoadSettings = 54,
    ResetToDefault = 55,
    BackupData = 56,
    RestoreData = 57,
    
    // Security
    Login = 60,
    Logout = 61,
    ChangeUser = 62,
    ChangePassword = 63,
    LockScreen = 64,
    UnlockScreen = 65,
    EnableSecurity = 66,
    DisableSecurity = 67,
    
    // Other
    ShowMessage = 100
}

/// <summary>
/// Event trigger types.
/// </summary>
public enum TriggerType
{
    OnClick = 0,
    OnDoubleClick = 1,
    OnRightClick = 2,
    OnMouseDown = 3,
    OnMouseUp = 4,
    OnMouseEnter = 5,
    OnMouseLeave = 6,
    OnKeyPress = 7,
    OnValueChange = 8,
    OnStateChange = 9,
    OnFocusIn = 10,
    OnFocusOut = 11,
    OnTimer = 12,
    OnTagChange = 13,
    OnCondition = 14
}
