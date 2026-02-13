# AccuTrack SCADA Requirements Specification (SRS)

**Project:** AccuTrack
**Section:** 1.1 – Requirements Definition
**Status:** Draft v1.0
**Applies to:** SCADA Designer (Qt C++), SCADA Runtime (Qt/QML)
**Platforms:** Cross-platform (Linux, Windows)

---

## 1. Introduction

### 1.1 Purpose

This document defines the functional and non-functional requirements for the AccuTrack SCADA system. It establishes a single, authoritative specification governing the scope, behavior, performance, and constraints of both the **SCADA Designer** and the **SCADA Runtime**.

The intent is to provide a stable foundation for architecture design, implementation, verification, and long-term maintenance.

### 1.2 Intended Audience

* System architect
* SCADA/controls engineer
* Software developers (Designer and Runtime)
* Test and validation engineers
* Academic or industrial reviewers

### 1.3 System Overview

AccuTrack is a modular, cross-platform SCADA system composed of:

* **Designer:** A Qt/C++ desktop application used to configure projects (tags, alarms, trends, scripts, users, layouts).
* **Runtime:** A Qt/QML execution environment responsible for deterministic data acquisition, control logic execution, visualization, and alarm handling.

The Designer produces portable project artifacts consumed by the Runtime without modification.

### 1.4 Definitions and Abbreviations

* **SCADA:** Supervisory Control and Data Acquisition
* **Tag:** A named data point representing a process value
* **Runtime:** The execution engine running deployed SCADA projects
* **Designer:** The configuration and project authoring tool
* **HMI:** Human–Machine Interface

---

## 2. Functional Requirements – SCADA Scope

### 2.1 Tags

#### 2.1.1 General

The system shall support a centralized tag database shared consistently between Designer and Runtime.

#### 2.1.2 Supported Data Types

* Boolean
* Signed and unsigned integers (16/32/64-bit)
* Floating point (single and double precision)
* String
* Enumerations
* Structured/composite types

#### 2.1.3 Tag Sources

* External devices (PLC, fieldbus, network protocol)
* Internal/virtual tags
* Computed tags (expressions derived from other tags)

#### 2.1.4 Update Mechanisms

* Periodic polling
* Event-driven updates
* On-demand read/write

#### 2.1.5 Tag Metadata

Each tag shall support:

* Unique identifier
* Human-readable name
* Engineering units
* Scaling and offset
* Valid range and limits
* Quality/status flag
* Timestamp

#### 2.1.6 Lifecycle Management

* Create, edit, delete tags in Designer
* Runtime shall load tags as read-only configuration
* Runtime shall not permit structural modification of tags

---

### 2.2 Alarms

#### 2.2.1 Alarm Types

* Digital (on/off)
* Analog threshold (high, low, high-high, low-low)
* Deviation
* Rate-of-change

#### 2.2.2 Priority and Severity

* Configurable priority levels
* Visual and logical differentiation by severity

#### 2.2.3 Alarm Behavior

* Latching and non-latching modes
* Manual and automatic acknowledgement
* Alarm shelving and suppression

#### 2.2.4 Alarm Persistence

* Alarm events shall be timestamped
* Alarm history shall be retained according to configuration
* Alarm state shall survive Runtime restart where applicable

---

### 2.3 Trends

#### 2.3.1 Trend Types

* Real-time trends
* Historical trends

#### 2.3.2 Sampling

* Configurable sampling rate per tag
* Independent of screen refresh rate

#### 2.3.3 Storage

* Buffered in-memory storage
* Persistent historical storage
* Configurable retention policy

#### 2.3.4 Visualization

* Multiple tags per trend
* Zoom, pan, and cursor inspection
* Export to standard file formats

---

### 2.4 Scripts

#### 2.4.1 Purpose

Scripts shall enable custom logic beyond static configuration.

#### 2.4.2 Execution Model

* Event-triggered
* Periodic execution
* Startup/shutdown execution

#### 2.4.3 Scope and Access

* Read/write access to tags
* Alarm interaction
* No unrestricted OS-level access

#### 2.4.4 Safety

* Script execution shall be sandboxed
* Execution time shall be bounded
* Script failures shall not crash the Runtime

---

### 2.5 Users and Security

#### 2.5.1 Authentication

* Username/password authentication
* Extensible authentication backend

#### 2.5.2 Authorization

* Role-based access control (RBAC)
* Permissions for:

  * Viewing
  * Control actions
  * Configuration
  * Administration

#### 2.5.3 Auditing

* User actions shall be logged
* Configuration changes shall be traceable

---

## 3. Non-Functional Requirements

### 3.1 Performance

* Support at least 10,000 active tags per Runtime instance
* Deterministic update cycle configurable down to 50 ms
* Alarm propagation latency < 100 ms under nominal load
* UI rendering shall not block core data processing

### 3.2 Determinism

* Deterministic execution order for:

  * Tag updates
  * Script execution
  * Alarm evaluation
* Worst-case execution time shall be bounded and measurable

### 3.3 Boot and Startup

* Cold start to operational state ≤ configurable target
* Deterministic initialization sequence
* Graceful handling of partial configuration failures

### 3.4 Reliability and Availability

* Runtime shall recover gracefully from non-fatal errors
* No single script or UI failure shall crash the system
* Persistent data shall not be corrupted on abnormal shutdown

### 3.5 Portability

* Designer and Runtime shall compile and run on:

  * Linux
  * Windows
* Platform-specific code shall be isolated behind abstraction layers

---

## 4. Constraints and Assumptions

* Implementation based on Qt framework
* No hard real-time operating system assumed
* Hardware interfaces abstracted via drivers/modules
* Runtime executes projects generated by compatible Designer versions

---

## 5. Out of Scope

The following are explicitly excluded from this phase:

* Machine learning or predictive analytics
* Cloud-based deployment or synchronization
* Web-based Designer or Runtime
* Safety-certified SIL compliance

---

## 6. Outputs

* This Requirements Specification document
* Input to Architecture and System Design (Section 1.2)
* Baseline for validation and acceptance testing

---

