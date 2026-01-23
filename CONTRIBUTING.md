# Contributing to KeyRecorder

Thank you for your interest in contributing to KeyRecorder! 🎉

We welcome contributions from everyone, whether you're fixing a typo, improving documentation, or adding new features.

## 🚀 Quick Start

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a branch** for your changes
4. **Make your changes** and test them
5. **Submit a pull request**

## 📋 Ways to Contribute

### For Non-Developers

- 🐛 **Report Bugs** - Found an issue? [Open a bug report](../../issues/new)
- 💡 **Suggest Features** - Have an idea? [Start a discussion](../../discussions)
- 📝 **Improve Documentation** - Help make our docs better
- 🌐 **Translate** - Help localize KeyRecorder to other languages
- 💬 **Help Others** - Answer questions in Discussions

### For Developers

- 🎨 **UI/UX** - Improve the interface, add visualizations
- 🚀 **Performance** - Optimize code and database queries
- 🔍 **Features** - Implement new functionality
- 🧪 **Testing** - Write unit tests and integration tests
- 🐛 **Bug Fixes** - Fix reported issues

## 🛠️ Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or [VS Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)
- Windows 10 or 11

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/keyrecorder.git
cd keyrecorder

# Add upstream remote
git remote add upstream https://github.com/ORIGINAL-OWNER/keyrecorder.git

# Build the solution
dotnet build KeyRecorder.slnx

# Run tests (when available)
dotnet test
```

### Running Locally

**Service (Console Mode):**
```powershell
dotnet run --project KeyRecorder.Service
```

**UI Application:**
```powershell
dotnet run --project KeyRecorder.UI
```

## 📝 Coding Guidelines

### C# Code Style

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful names for variables, methods, and classes
- Add XML documentation comments for public APIs
- Keep methods small and focused (ideally < 50 lines)
- Use `async/await` for I/O operations
- Handle exceptions appropriately

**Example:**

```csharp
/// <summary>
/// Captures a keystroke event and stores it in the database.
/// </summary>
/// <param name="keystroke">The keystroke event to record.</param>
/// <returns>The ID of the recorded keystroke.</returns>
public async Task<long> RecordKeystrokeAsync(KeystrokeEvent keystroke)
{
    ArgumentNullException.ThrowIfNull(keystroke);

    keystroke.SequenceId = Interlocked.Increment(ref _sequenceCounter);
    return await _hotDb.InsertKeystrokeAsync(keystroke);
}
```

### XAML Style

- Use meaningful names for controls (e.g., `RefreshButton`, not `button1`)
- Group related elements with comments
- Use data binding where appropriate
- Follow WPF best practices

### Commit Messages

Use clear, descriptive commit messages:

```
Add keyboard shortcut for pause/resume

- Added Ctrl+P hotkey to toggle recording
- Updated UI to show shortcut in tooltip
- Added keyboard event handler to MainWindow
```

**Format:**
- First line: Summary in imperative mood (< 72 chars)
- Blank line
- Body: Explain what and why (optional)

**Good:**
- ✅ `Add CSV export functionality`
- ✅ `Fix memory leak in keyboard hook`
- ✅ `Update README with installation instructions`

**Bad:**
- ❌ `Updated stuff`
- ❌ `Fixed bug`
- ❌ `asdfasdf`

## 🔄 Pull Request Process

### Before Submitting

1. ✅ **Build succeeds** - `dotnet build KeyRecorder.slnx`
2. ✅ **Tests pass** - `dotnet test` (when available)
3. ✅ **Code is formatted** - Follow C# conventions
4. ✅ **Documentation updated** - If you added features
5. ✅ **No merge conflicts** - Rebase on latest main

### Submitting

1. **Push to your fork:**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Open a Pull Request** on GitHub

3. **Fill out the PR template:**
   - Describe what you changed and why
   - Link related issues (e.g., "Fixes #123")
   - Add screenshots for UI changes
   - List any breaking changes

4. **Wait for review** - A maintainer will review your PR

### During Review

- Be responsive to feedback
- Make requested changes in new commits
- Don't force-push unless asked
- Be patient and respectful

## 🧪 Testing

### Manual Testing

For UI changes:
1. Build in Release mode
2. Install and run the service
3. Test the UI thoroughly
4. Verify database operations work correctly

For service changes:
1. Test as console application first
2. Install as Windows Service
3. Verify service starts/stops correctly
4. Check Event Viewer for errors

### Writing Tests

We welcome unit tests! Use xUnit for testing:

```csharp
public class DatabaseManagerTests
{
    [Fact]
    public async Task RecordKeystroke_ShouldIncrementSequenceId()
    {
        // Arrange
        var dbManager = new DatabaseManager(testPath);
        var keystroke = new KeystrokeEvent { KeyName = "A" };

        // Act
        await dbManager.RecordKeystrokeAsync(keystroke);

        // Assert
        Assert.True(keystroke.SequenceId > 0);
    }
}
```

## 🎨 UI/UX Contributions

### Design Principles

- **Simplicity** - Keep the UI clean and uncluttered
- **Performance** - UI should be responsive (<16ms/frame)
- **Consistency** - Follow the existing design language
- **Accessibility** - Consider keyboard navigation and screen readers

### Brand Colors

Use the official color palette (see [BRANDING.md](BRANDING.md)):

- Primary Blue: `#0085d8`
- Light Gray: `#eeeff1`
- Accent Red: `#e61f47`
- Dark: `#0d0f10`

## 📚 Documentation

### Where to Document

- **README.md** - User-facing features and installation
- **Code comments** - Explain complex logic
- **XML docs** - Document public APIs
- **Wiki** - Detailed guides and tutorials

### Documentation Style

- Write for clarity, not cleverness
- Use examples to illustrate concepts
- Keep it up-to-date with code changes
- Use screenshots for UI features

## 🐛 Bug Reports

### Before Reporting

1. Check if the bug is already reported
2. Try to reproduce on latest version
3. Collect diagnostic information

### What to Include

- **Title** - Clear, descriptive summary
- **Description** - What happened vs. what you expected
- **Steps to Reproduce** - Numbered steps
- **Environment** - Windows version, .NET version
- **Logs** - Event Viewer logs if service-related
- **Screenshots** - For UI issues

**Example:**

```markdown
### Bug: UI freezes when loading large datasets

**Description:**
The UI becomes unresponsive when loading more than 10,000 keystrokes.

**Steps to Reproduce:**
1. Let service run for several days
2. Open KeyRecorder UI
3. Click Refresh button
4. UI freezes for 30+ seconds

**Environment:**
- Windows 11 22H2
- .NET 10.0.2
- KeyRecorder 1.0.0

**Expected:**
UI should remain responsive with smooth scrolling.

**Actual:**
UI freezes completely until data loads.

**Logs:**
No errors in Event Viewer.
```

## 💡 Feature Requests

### Before Requesting

1. Check existing feature requests
2. Consider if it fits the project's scope
3. Think about implementation complexity

### What to Include

- **Use Case** - Why is this feature needed?
- **Proposed Solution** - How should it work?
- **Alternatives** - Other ways to solve the problem
- **Mockups** - Screenshots or sketches (optional)

## 🏗️ Project Structure

```
KeyRecorder/
├── KeyRecorder.Core/           # Shared library
│   ├── Capture/                # Keyboard hook
│   │   ├── KeyboardHook.cs     # Main hook implementation
│   │   └── NativeMethods.cs    # Win32 API calls
│   ├── Data/                   # Database layer
│   │   ├── DatabaseManager.cs  # Orchestrates databases
│   │   ├── HotDatabase.cs      # Live buffer
│   │   ├── MainDatabase.cs     # Historical storage
│   │   └── SnapshotDatabase.cs # Backup snapshots
│   ├── IPC/                    # Inter-process communication
│   │   ├── IpcServer.cs        # Named Pipes server
│   │   └── IpcClient.cs        # Named Pipes client
│   └── Models/                 # Data models
│       ├── KeystrokeEvent.cs
│       └── AppConfiguration.cs
├── KeyRecorder.Service/        # Windows Service
│   ├── KeyRecorderWorker.cs    # Main service logic
│   └── Program.cs              # Service host
└── KeyRecorder.UI/             # WPF Application
    ├── MainWindow.xaml         # Main UI
    ├── AboutWindow.xaml        # About dialog
    └── Assets/                 # Images, resources
```

## 📜 License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).

## ❓ Questions?

- 💬 Ask in [GitHub Discussions](../../discussions)
- 📧 Contact maintainers (see README)
- 📖 Check the [documentation](docs/)

---

**Thank you for contributing to KeyRecorder!** 🚀

Every contribution, no matter how small, helps make KeyRecorder better for everyone.
