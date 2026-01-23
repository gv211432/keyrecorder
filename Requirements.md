# 🔐 Keyboard Activity Recorder – Requirements Specification

**Target Platform:** Windows 10 / 11
**Implementation Stack:** .NET 8 (C#), Windows Service + WPF UI
**Audience:** Solo developer / internal debugging tool
**Security Level:** High (non-exfiltrating, local-only)

---

## 1. System Lifecycle & Reliability Requirements

### 1.1 Autostart & Persistence

* The application **must start automatically on system boot**
* Runs as a **Windows Service**
* Service must:

  * Auto-restart on crash
  * Resume recording without data loss
  * Survive user logoff / fast user switching
* No manual intervention required once installed

### 1.2 Uptime Guarantees

* Designed for **24/7 continuous operation**
* Must tolerate:

  * Sleep / wake cycles
  * Temporary system load spikes
  * UI crashes (service continues running)

---

## 2. Keystroke Capture Requirements

### 2.1 Scope of Capture

* Capture **all keyboard events**:

  * Single keys
  * Modifier combinations (Ctrl, Alt, Shift, Win)
  * Repeated keys
  * Non-printable keys (Enter, Backspace, Esc, F-keys)
* Capture must be **global**, independent of active application

### 2.2 Timestamping

* Each keystroke must be recorded with:

  * High-resolution timestamp (minimum: milliseconds)
  * Optional monotonic sequence ID (for ordering safety)

---

## 3. Data Storage Architecture

### 3.1 Storage Engine

* **SQLite** (local, embedded, no network access)
* WAL mode enabled for crash safety

### 3.2 Database Files

The system must maintain **three distinct SQLite files**:

#### 3.2.1 Hot File (Current Session)

* Stores **current live keystrokes**
* Optimized for frequent writes
* Acts as a write-ahead buffer
* No long-term retention guarantees

#### 3.2.2 Main File (Historical Store)

* Stores consolidated historical data
* Read-optimized
* Enforced retention rules apply here

#### 3.2.3 Snapshot File (Recovery / Audit)

* Periodic immutable snapshots of Main File
* Used for:

  * Recovery
  * Integrity verification
  * Rollback if corruption detected

---

## 4. Synchronization & Maintenance Jobs

### 4.1 Hot → Main Sync

* Hot file **syncs to main file every 5 minutes**
* Sync must be:

  * Transactional
  * Idempotent
  * Crash-safe
* On successful sync:

  * Hot entries are marked committed or purged

### 4.2 Integrity & Error Correction

* Every **1 hour**, system performs:

  * Integrity checks on main file
  * Detection of orphaned / duplicated entries
  * Timestamp ordering validation
* If corruption detected:

  * Attempt automatic repair
  * Fallback to last known valid snapshot

### 4.3 Snapshotting

* After successful hourly integrity check:

  * Create snapshot of main file
  * Snapshots are versioned and timestamped
  * Old snapshots are pruned automatically

---

## 5. UI / UX Requirements

### 5.1 UI Characteristics

* Single lightweight window
* Native Windows look & feel
* Smooth scrolling and rendering
* No noticeable CPU / RAM spike

### 5.2 Live View

* UI displays **real-time keystrokes**
* Updates streamed from service via IPC
* UI can be closed without stopping recording

### 5.3 Visualization Model

* Keystrokes displayed in **time-ordered rows**
* **One row per minute** (configurable)
* Rows must be collapsible / expandable

---

## 6. Time-Based Grouping Logic

### 6.1 Grouping Mode

* Optional feature (ON/OFF toggle)
* User-configurable grouping threshold (e.g. 10ms, 50ms, 300ms)

### 6.2 Grouping Rules

* If time gap between two keystrokes ≤ threshold:

  * Group into same word/token
* If gap > threshold:

  * Start new group
* Backspace handling:

  * Must mutate the active group appropriately

---

## 7. Interaction & Inspection Features

### 7.1 Hover Metadata

* On **letter hover**:

  * Show exact capture timestamp
* On **word/group hover**:

  * Show:

    * Start time
    * End time
    * Total duration

### 7.2 Filtering

* Filter by:

  * Time range
  * Group size
  * Application focus (optional future enhancement)

---

## 8. Retention & History Policies

### 8.1 Default Retention

* Maintain **last 7 days** of history by default

### 8.2 Extended Retention Options

User-selectable limits:

* Up to **365 days**, OR
* Maximum **N keystrokes** (e.g. 10,000 letters)
* Maximum **storage size** (optional)

### 8.3 Pruning Rules

* Retention enforced on Main File only
* Pruning runs during maintenance window
* Snapshot consistency preserved

---

## 9. Security & Privacy Requirements (Critical)

### 9.1 Local-Only Guarantee

* No network access
* No telemetry
* No cloud sync

### 9.2 Process Isolation

* Service runs under restricted privileges
* UI runs under user context
* IPC secured (Named Pipes with ACLs)

### 9.3 User Control

* Global pause/resume shortcut
* Optional exclusion mode (secure desktop / password fields)

---

## 10. Performance Constraints

* Max CPU usage (idle): **<1%**
* Max memory usage (service): **<100 MB**
* Keystroke capture latency: **<5ms**
* UI render latency: **<16ms/frame**

---

## 11. Non-Goals (Explicitly Out of Scope)

* Remote monitoring
* Network transmission
* Employee surveillance features
* Stealth / hidden operation

---

## 12. Future-Ready Extensions (Optional)

* Per-application timelines
* Heatmap per key
* Export to JSON / CSV
* IDE-specific visualization

---

### ✅ Evaluation of Original Points

All the points are **valid and solid**, but:

* They **needed transactional guarantees**
* Snapshot logic needed **clear ordering**
* Retention needed **explicit pruning rules**
* Service/UI separation needed to be explicit

This version removes ambiguity and prevents you from painting yourself into a corner later.
