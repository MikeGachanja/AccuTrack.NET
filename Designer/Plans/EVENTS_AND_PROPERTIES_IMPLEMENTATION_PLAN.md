# Events and Properties Implementation Plan

## Overview
This plan addresses the implementation of:
1. Proper button component saving
2. Events system (Bit actions, Navigation actions, Animations, etc.)
3. Properties editor redesign for event configuration
4. Tag attachment to components
5. Runtime event handling

## Phase 1: Fix Button Component Saving

### 1.1 Component Serialization
- [ ] Review `ComponentSerializer.cs` to ensure ButtonComponent serialization includes all properties
- [ ] Verify `ButtonComponent.ToJson()` includes `Action` property
- [ ] Ensure `ScreenTemplate.cs` properly deserializes ButtonComponent with Action property
- [ ] Test that buttons created via drag-drop are saved correctly to screen JSON files

### 1.2 Component Deserialization
- [ ] Verify `ScreenTemplate.DeserializeComponent()` correctly creates ButtonComponent instances
- [ ] Ensure `ButtonComponent.FromJson()` properly restores Action property
- [ ] Test loading screens with existing buttons

**Files to Review/Update:**
- `DesignerModules/Components/ComponentSerializer.cs`
- `DesignerModules/Components/ButtonComponent.cs`
- `DesignerModules/ScreenEditor/ScreenTemplate.cs`

---

## Phase 2: Events System Design

### 2.1 Event Data Structures
- [ ] Create `EventTrigger` class (componentId, triggerType: "OnClick", "OnDoubleClick", etc.)
- [ ] Create `EventAction` class (type, parameters: screenId, tagName, value, script, etc.)
- [ ] Create `ScadaEvent` class (id, name, description, trigger, actions[], metadata)
- [ ] Create `EventCategory` enum (ScreenNavigation, TagOperations, ScriptActions, ComponentControl, SystemActions, DataOperations, Security)
- [ ] Create `ActionType` enum (NavigateScreen, SetBit, ResetBit, ToggleBit, WriteTag, RunScript, etc.)

**Files to Create:**
- `DesignerModules/Events/EventTrigger.cs`
- `DesignerModules/Events/EventAction.cs`
- `DesignerModules/Events/ScadaEvent.cs`
- `DesignerModules/Events/EventEnums.cs`

### 2.2 Events Module
- [ ] Create `EventsModule.cs` class to manage events
- [ ] Implement `LoadEventsFromJson(string path)` - loads events.json
- [ ] Implement `SaveEventsToJson(List<ScadaEvent> events)` - saves to events.json
- [ ] Implement `GetEventsForComponent(string componentId)` - filter events by component
- [ ] Implement `AddEvent(ScadaEvent event)` - add new event
- [ ] Implement `UpdateEvent(string eventId, ScadaEvent event)` - update existing event
- [ ] Implement `RemoveEvent(string eventId)` - remove event
- [ ] Implement `GetEventById(string eventId)` - retrieve event

**Files to Create:**
- `DesignerModules/Events/EventsModule.cs`
- `DesignerModules/Events/EventsModule.cs` (JSON serialization helpers)

### 2.3 Component Event Association
- [ ] Add `EventIds` property to `BaseComponent` (List<string>)
- [ ] Update `ButtonComponent` to support event IDs
- [ ] Update component serialization to include EventIds
- [ ] Update component deserialization to restore EventIds

**Files to Update:**
- `DesignerModules/Components/BaseComponent.cs`
- `DesignerModules/Components/ButtonComponent.cs`
- `DesignerModules/Components/ComponentSerializer.cs`

---

## Phase 3: Properties Editor Redesign

### 3.1 Properties Editor Structure
- [ ] Redesign `PropertyEditor.cs` to use tabbed interface:
  - General Tab: Position, Size, Colors, Font, Visibility, Enabled
  - Events Tab: Event list, Add/Remove events, Event configuration
  - Animation Tab: Animation properties (visibility, color, translation, etc.)
  - Tags Tab: Tag binding configuration
- [ ] Create `PropertyEditor.Designer.cs` with tabbed layout
- [ ] Implement `UpdateEditor(object item)` to populate tabs based on component type

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`
- `DesignerModules/Components/PropertyEditor.Designer.cs`

### 3.2 Tag Selector Widget
- [ ] Create `TagSelectorWidget.cs` UserControl
- [ ] Implement tag dropdown with available tags from tag tables
- [ ] Support local tags and remote tags (device::tagName format)
- [ ] Implement tag name/address resolution
- [ ] Add tag filtering/search functionality
- [ ] Display tag information (type, address, device)

**Files to Create:**
- `DesignerModules/Components/TagSelectorWidget.cs`
- `DesignerModules/Components/TagSelectorWidget.Designer.cs`

### 3.3 General Properties Tab
- [ ] Position properties (X, Y) with spinboxes
- [ ] Size properties (Width, Height) with spinboxes
- [ ] Color properties (BackColor, ForeColor, BorderColor) with color pickers
- [ ] Font properties (FontFamily, FontSize, FontStyle) with font dialog
- [ ] Visibility checkbox
- [ ] Enabled checkbox
- [ ] Z-Order spinbox
- [ ] Component-specific properties (Text for Button, Value for Numeric, etc.)

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`

### 3.4 Events Tab Implementation
- [ ] Create events list widget (ListBox) showing event names
- [ ] Add "Add Event" button
- [ ] Add "Remove Event" button
- [ ] Add "Edit Event" functionality
- [ ] Event configuration form:
  - Category ComboBox (Screen Navigation, Tag Operations, Script Actions, etc.)
  - Action ComboBox (populated based on category)
  - Trigger ComboBox (OnClick, OnDoubleClick, OnRightClick, OnMouseDown, OnMouseUp, OnMouseEnter, OnMouseLeave, OnKeyPress, OnValueChange, OnStateChange, OnFocusIn, OnFocusOut, OnTimer, OnTagChange, OnCondition)
  - Parameters section (stacked widget):
    - Screen Navigation: Screen selector dropdown, Modal checkbox
    - Tag Operations: Tag selector, Value input
    - Script Actions: Script path input, Browse button, Arguments input
    - Component Control: Component selector, Action parameters
    - System Actions: Action-specific parameters
    - Data Operations: Data source/target selectors
    - Security: User/role selectors
- [ ] Implement `LoadEventsForComponent(string componentId)` - populate events list
- [ ] Implement `SaveEventToComponent(string componentId, ScadaEvent event)` - save event
- [ ] Implement `RemoveEventFromComponent(string componentId, string eventId)` - remove event
- [ ] Implement `CreateEventFromUI()` - build event from UI controls

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`
- `DesignerModules/Components/PropertyEditor.Designer.cs`

### 3.5 Animation Tab Implementation
- [ ] Create animations list widget
- [ ] Add "Add Animation" button
- [ ] Add "Remove Animation" button
- [ ] Animation configuration:
  - Animation name (unique per component)
  - Animation type (Visibility, Color, Translation, Rotation, Scale, Opacity)
  - Trigger (Tag value change, Timer, Condition)
  - Parameters (tag name, condition, duration, easing curve)
  - Target values (color, position, size, etc.)
- [ ] Link to AnimationManager for animation management

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`
- `DesignerModules/Components/PropertyEditor.Designer.cs`
- `DesignerModules/Components/Animations.cs` (if exists)

### 3.6 Tags Tab Implementation
- [ ] Tag binding selector (TagSelectorWidget)
- [ ] Display current tag binding
- [ ] Support unbinding tags
- [ ] Show tag information (type, address, device)
- [ ] Support multiple tag bindings for different properties (if needed)

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`
- `DesignerModules/Components/PropertyEditor.Designer.cs`

### 3.7 Property Editor Integration
- [ ] Wire PropertyEditor to ScreenEditor selection changes
- [ ] Wire PropertyEditor to ProjectManager for tag tables and screens
- [ ] Wire PropertyEditor to EventsModule for event management
- [ ] Emit `RequestAutoSave` event when properties change
- [ ] Update component when properties change

**Files to Update:**
- `DesignerModules/ScreenEditor/ScreenEditor.cs`
- `DesignerModules/Project/ProjectView.cs`
- `Designer/MainForm.cs`

---

## Phase 4: Tag System Integration

### 4.1 Tag Availability
- [ ] Ensure TagEngine provides tag list to PropertyEditor
- [ ] Implement tag name to address resolution
- [ ] Implement address to tag name resolution
- [ ] Support cross-device tags (device::tagName format)
- [ ] Update tag tables when tags change

**Files to Update:**
- `DesignerModules/TagEngine/TagTable.cs`
- `DesignerModules/TagEngine/TagTableEditor.cs`
- `DesignerModules/Components/PropertyEditor.cs`

### 4.2 Component Tag Binding
- [ ] Update `BaseComponent.TagName` property usage
- [ ] Support tag binding in component serialization
- [ ] Support tag binding in component deserialization
- [ ] Update component rendering to show tag binding status
- [ ] Support tag binding for different component properties (value, text, color, etc.)

**Files to Update:**
- `DesignerModules/Components/BaseComponent.cs`
- `DesignerModules/Components/ButtonComponent.cs`
- `DesignerModules/Components/TextLabelComponent.cs`
- `DesignerModules/Components/NumericComponent.cs`
- `DesignerModules/Components/ComponentSerializer.cs`

### 4.3 Tag Selection in Events
- [ ] Use TagSelectorWidget in event configuration
- [ ] Resolve tag names to addresses for runtime
- [ ] Support tag selection for tag operations (SetBit, ResetBit, WriteTag, etc.)
- [ ] Validate tag types match operation requirements

**Files to Update:**
- `DesignerModules/Components/PropertyEditor.cs`
- `DesignerModules/Events/EventAction.cs`

---

## Phase 5: Runtime Event Handling

### 5.1 Event Loading
- [ ] Ensure `EventManager.LoadEventsFromJson()` loads events.json correctly
- [ ] Verify event structure matches designer format
- [ ] Test event loading on project startup
- [ ] Handle missing or invalid events gracefully

**Files to Review/Update:**
- `RuntimeModules/Screens/EventManager.cs`

### 5.2 Event Trigger Handling
- [ ] Update `EventManager.FireTrigger(string componentId, string triggerType)` to handle all trigger types
- [ ] Implement component event subscription in ScreenViewBuilder
- [ ] Wire button clicks to EventManager
- [ ] Wire other component interactions (double-click, right-click, etc.)
- [ ] Support timer-based triggers
- [ ] Support tag-change triggers

**Files to Update:**
- `RuntimeModules/Screens/EventManager.cs`
- `Runtime/ScreenViewBuilder.cs`
- `Runtime/Views/Controls/RuntimeButton.axaml.cs`
- `Runtime/Views/Controls/RuntimeCheckbox.axaml.cs`
- `Runtime/Views/Controls/RuntimeSlider.axaml.cs`
- `Runtime/Views/Controls/RuntimeTextInput.axaml.cs`

### 5.3 Action Execution
- [ ] Implement `NavigateScreen` action (already exists, verify)
- [ ] Implement `SetBit` action (write tag value 1)
- [ ] Implement `ResetBit` action (write tag value 0)
- [ ] Implement `ToggleBit` action (toggle tag value)
- [ ] Implement `WriteTag` action (write tag with value)
- [ ] Implement `IncrementTag` action (increment tag value)
- [ ] Implement `DecrementTag` action (decrement tag value)
- [ ] Implement `RunScript` action (execute script)
- [ ] Implement `ShowMessage` action (display message dialog)
- [ ] Implement `ShowComponent` action
- [ ] Implement `HideComponent` action
- [ ] Implement `EnableComponent` action
- [ ] Implement `DisableComponent` action
- [ ] Implement component control actions
- [ ] Implement system actions
- [ ] Implement data operations
- [ ] Implement security actions

**Files to Update:**
- `RuntimeModules/Screens/EventManager.cs`

### 5.4 Tag Operations
- [ ] Ensure TagIOHandler supports all tag write operations
- [ ] Implement tag value reading for toggle/increment/decrement
- [ ] Support tag address resolution (tag name -> address)
- [ ] Handle tag type validation
- [ ] Support cross-device tag operations

**Files to Review/Update:**
- `RuntimeModules/TagsEngine/TagIOHandler.cs`
- `RuntimeModules/TagsEngine/TagManager.cs`

### 5.5 Screen Navigation
- [ ] Verify `NavigateToScreen()` implementation
- [ ] Support screen ID resolution
- [ ] Support modal screen display
- [ ] Handle screen not found errors
- [ ] Support screen parameters passing

**Files to Review/Update:**
- `RuntimeModules/Screens/EventManager.cs`
- `RuntimeModules/Screens/ScreenManager.cs`

### 5.6 Script Execution
- [ ] Ensure ScriptEngine supports script loading and execution
- [ ] Implement script path resolution
- [ ] Support script arguments
- [ ] Handle script execution errors
- [ ] Support script return values

**Files to Review/Update:**
- `RuntimeModules/ScriptingEngine/ScriptingEngineModule.cs`
- `RuntimeModules/Screens/EventManager.cs`

---

## Phase 6: Animation System

### 6.1 Animation Configuration
- [ ] Review AnimationManager implementation
- [ ] Ensure animation configuration matches designer format
- [ ] Support visibility animations
- [ ] Support color animations
- [ ] Support translation animations
- [ ] Support rotation animations
- [ ] Support scale animations
- [ ] Support opacity animations

**Files to Review/Update:**
- `RuntimeModules/Screens/AnimationManager.cs`
- `DesignerModules/Components/Animations.cs` (if exists)

### 6.2 Animation Triggers
- [ ] Support tag value change triggers
- [ ] Support timer triggers
- [ ] Support condition triggers
- [ ] Wire animations to tag changes
- [ ] Wire animations to component events

**Files to Update:**
- `RuntimeModules/Screens/AnimationManager.cs`
- `RuntimeModules/Screens/EventManager.cs`

---

## Phase 7: Testing and Validation

### 7.1 Component Saving
- [ ] Test button creation via drag-drop
- [ ] Verify button is saved to screen JSON
- [ ] Test loading screen with buttons
- [ ] Verify button properties are restored correctly

### 7.2 Event Configuration
- [ ] Test adding navigation event to button
- [ ] Test adding tag operation event to button
- [ ] Test adding script event to button
- [ ] Test editing existing events
- [ ] Test removing events
- [ ] Verify events are saved to events.json

### 7.3 Runtime Event Execution
- [ ] Test navigation events in runtime
- [ ] Test tag operation events in runtime
- [ ] Test script execution events in runtime
- [ ] Test multiple actions per event
- [ ] Test event trigger types (click, double-click, etc.)

### 7.4 Tag Binding
- [ ] Test tag selection in property editor
- [ ] Test tag binding to components
- [ ] Test tag operations in events
- [ ] Verify tag name/address resolution

### 7.5 Animation
- [ ] Test animation configuration in property editor
- [ ] Test animation execution in runtime
- [ ] Test animation triggers
- [ ] Verify animation performance

---

## Phase 8: Documentation

### 8.1 User Documentation
- [ ] Document event configuration process
- [ ] Document tag binding process
- [ ] Document animation configuration
- [ ] Create examples for common scenarios

### 8.2 Developer Documentation
- [ ] Document event system architecture
- [ ] Document event data structures
- [ ] Document runtime event handling
- [ ] Document tag system integration

---

## Implementation Order

1. **Phase 1**: Fix button saving (critical blocker)
2. **Phase 2**: Design and implement events data structures
3. **Phase 3.1-3.3**: Redesign properties editor structure and general tab
4. **Phase 3.4**: Implement events tab
5. **Phase 4**: Integrate tag system
6. **Phase 5**: Implement runtime event handling
7. **Phase 6**: Implement animation system
8. **Phase 7**: Testing
9. **Phase 8**: Documentation

---

## Key Files Reference

### Designer Files
- `DesignerModules/Components/PropertyEditor.cs` - Main property editor
- `DesignerModules/Components/BaseComponent.cs` - Base component class
- `DesignerModules/Components/ButtonComponent.cs` - Button component
- `DesignerModules/Events/EventsModule.cs` - Events management
- `DesignerModules/Events/ScadaEvent.cs` - Event data structures
- `DesignerModules/ScreenEditor/ScreenEditor.cs` - Screen editor
- `DesignerModules/TagEngine/TagTable.cs` - Tag tables

### Runtime Files
- `RuntimeModules/Screens/EventManager.cs` - Runtime event handling
- `RuntimeModules/Screens/ScreenManager.cs` - Screen management
- `RuntimeModules/TagsEngine/TagIOHandler.cs` - Tag I/O operations
- `RuntimeModules/ScriptingEngine/ScriptingEngineModule.cs` - Script execution
- `RuntimeModules/Screens/AnimationManager.cs` - Animation management
- `Runtime/ScreenViewBuilder.cs` - Screen rendering

---

## Notes

- Reference AccuTrackQt implementation in:
  - `d:\Dev\AccuTrackQt\Designer\components_module\propertyeditor.cpp` - Properties editor implementation
  - `d:\Dev\AccuTrackQt\Runtime\event_dispatcher\` - Event dispatcher architecture
  - `d:\Dev\AccuTrackQt\Designer\components_module\components\` - Component implementations

- Events are stored in `json/events.json` in the project directory
- Events are associated with components via component ID
- Tag operations use tag addresses for runtime, but tag names for designer
- Animation system should integrate with existing AnimationManager
