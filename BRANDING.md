# KeyRecorder - Branding Guide

## Logo Assets

### Location
All logo files are stored in: `KeyRecorder.UI/Assets/`

### Available Logos

1. **logo.png** - Icon only (square format)
   - Dimensions: Optimized for 48x48px - 512x512px
   - Use: Window icon, header logo, taskbar icon
   - Features: Keyboard symbol with recording indicator and waveform

2. **logo-name.png** - Logo with "Recorder" text (horizontal format)
   - Dimensions: Optimized for wide format (600px+ width)
   - Use: Splash screens, about dialogs, documentation headers
   - Features: Full branding with logo + brand name

## Brand Colors

From the logo design:

```
Primary Blue:   #0085d8  (Logo elements, primary actions)
Light Gray:     #eeeff1  (Backgrounds, secondary buttons)
Accent Red:     #e61f47  (Recording indicator, alerts)
Dark:           #0d0f10  (Header background, text)
```

### Color Usage

- **#0085d8 (Blue)**: Primary brand color
  - Main buttons (Pause/Resume)
  - Links and interactive elements
  - Time labels in timeline
  - Status indicators

- **#eeeff1 (Light Gray)**:
  - Statistics bar background
  - Secondary buttons (Refresh)
  - Hover states

- **#e61f47 (Red)**:
  - Recording status (when stopped/paused)
  - Error messages
  - Critical alerts

- **#0d0f10 (Dark)**:
  - Header background
  - Footer background
  - Primary text color

## Current Implementation

### 1. Main Window (MainWindow.xaml)

**Header Section:**
- Logo: 48x48px in top-left corner
- Background: #0d0f10 (Dark)
- Title text: "KeyRecorder" in #0085d8 (Blue)
- Status text: #eeeff1 (Light gray)

**Buttons:**
- About button: Transparent with #444 border, hover: #0085d8
- Pause/Resume: #0085d8 background, white text
- Refresh: #eeeff1 background, dark text

**Statistics Bar:**
- Background: #eeeff1
- Labels: #0d0f10 (dark text)
- Values: #0085d8 (blue) and #e61f47 (red for status)

**Timeline:**
- Background: White
- Time labels: #0085d8
- Keystroke text: #0d0f10
- Hover: #f8f9fa

**Footer:**
- Background: #0d0f10
- Top border: #0085d8 (2px)
- Text: #eeeff1

### 2. About Window (AboutWindow.xaml)

**Header:**
- Full logo-name.png (logo with "Recorder" text)
- Background: #0d0f10 (Dark)
- Height: 80px for logo

**Content:**
- Features list in #eeeff1 box
- Blue accents (#0085d8) for headers
- Professional layout with version info

**Close Button:**
- #0085d8 background
- Hover: #0096ed (lighter blue)

### 3. Window Icon

The logo.png is set as the window icon via:
```xml
Icon="/Assets/logo.png"
```

This appears in:
- Window title bar
- Taskbar
- Alt+Tab switcher
- Task Manager

### 4. Installation Script (install-service.ps1)

**Banner:**
```
╔═══════════════════════════════════════════════════════════╗
║              KeyRecorder Service Installer                ║
║         Keyboard Activity Monitor for Windows             ║
╚═══════════════════════════════════════════════════════════╝
```

**Color Scheme:**
- Cyan for borders and headers
- Green for success messages
- Yellow for warnings
- White for info text

### 5. Documentation (README.md)

**Header:**
- Displays logo-name.png centered at top
- Width: 600px
- Alt text: "KeyRecorder Logo"
- Badge icons for .NET, Windows, License

## Usage Guidelines

### DO ✓

- Use logo.png for square/icon contexts (48x48 to 256x256)
- Use logo-name.png for wide format contexts (headers, banners)
- Maintain aspect ratios when scaling
- Use brand colors consistently (#0085d8, #eeeff1, #e61f47, #0d0f10)
- Provide adequate padding/margin around logos
- Use on light or dark backgrounds (logo has both light/dark elements)

### DON'T ✗

- Don't distort or stretch logos
- Don't change logo colors
- Don't add effects (shadows, gradients) to logos
- Don't use logos smaller than 32x32px
- Don't place logos on busy backgrounds
- Don't modify the recording indicator (red dot)
- Don't separate the waveform from the keyboard icon

## File References

### In Code

**XAML (MainWindow.xaml):**
```xml
<!-- Window Icon -->
Icon="/Assets/logo.png"

<!-- Header Logo -->
<Image Source="/Assets/logo.png" Width="48" Height="48"/>
```

**XAML (AboutWindow.xaml):**
```xml
<!-- Full Brand Logo -->
<Image Source="/Assets/logo-name.png" Stretch="Uniform" Height="80"/>
```

**Project File (KeyRecorder.UI.csproj):**
```xml
<ItemGroup>
  <Resource Include="Assets\logo.png" />
  <Resource Include="Assets\logo-name.png" />
</ItemGroup>
```

### Build Action

Both logo files are set as **Resource**:
- Embedded in the assembly
- Accessible via Pack URI syntax
- No external file deployment needed

## Future Enhancements

Potential uses for the logos:

1. **Windows Installer (.msi)**
   - Use logo-name.png for installer banner
   - Use logo.png for Add/Remove Programs icon

2. **System Tray Icon**
   - Create 16x16 and 32x32 ICO versions from logo.png
   - Show in notification area when UI is minimized

3. **Splash Screen**
   - Use logo-name.png during application startup
   - Add animation or fade-in effect

4. **Notifications**
   - Use logo.png for Windows toast notifications
   - Status updates, sync completion messages

5. **Context Menus**
   - System tray right-click menu with logo

## Converting to ICO (Optional)

To create a Windows ICO file with multiple resolutions:

```powershell
# Using ImageMagick (if installed)
magick convert logo.png -define icon:auto-resize=256,128,64,48,32,16 icon.ico
```

Or use online tools:
- https://convertio.co/png-ico/
- https://www.icoconverter.com/

Include sizes: 16x16, 32x32, 48x48, 64x64, 128x128, 256x256

---

## Summary

The KeyRecorder branding is now fully integrated throughout the application:

✓ **Main Window** - Logo in header, branded colors throughout
✓ **About Window** - Full logo with name, feature showcase
✓ **Window Icon** - Logo appears in taskbar and title bar
✓ **Installation Script** - Branded ASCII banner
✓ **Documentation** - Logo in README header
✓ **Consistent Colors** - All UI elements use brand palette

The professional blue/red/dark theme creates a cohesive, modern appearance while the keyboard+waveform logo clearly communicates the application's purpose.
