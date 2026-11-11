VR Remote Desktop & Cyberpunk Virtual Workstation

![Unity](https://img.shields.io/badge/Unity-6000.6.0f1%20(Unity%206)-blue.svg?logo=unity) ![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-informational.svg) ![XR](https://img.shields.io/badge/XR-OpenXR%20%7C%20XRI%203.0-blueviolet.svg) ![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20%7C%20VR-lightgrey.svg?logo=windows) ![License](https://img.shields.io/badge/License-MIT-green.svg)

An immersive virtual reality workstation and low-latency remote desktop system built in Unity 6 (URP). Seamlessly operate your host Windows PC inside VR across a triple-monitor cyberpunk setup with physically accurate cursor transitions, and mirror your workspace in real time onto a massive 70mm VIP Theater IMAX projection screen.

Overview

!Workstation Screenshot (Replace with your project screenshot)

Unlike conventional VR remote desktops that render floating windows with disconnected pointers, VR Remote Desktop treats physical monitors as contiguous virtual surfaces:

Physical Surface Projection: The cursor is strictly projected onto the monitor screen meshes—never floating through empty air.
Edge Transition Continuity: Smoothly glide the cursor from Left (Telemetry) to Center (Host Remote Desktop) to Right (Operations NOC).
3D World-Space Height Preservation: Transitioning across monitors preserves the exact vertical elevation in world space, eliminating vertical jumping.
Bi-Directional Input Forwarding: Complete control of the host Windows OS, including clicking, dragging, context menus, and typing, directly from inside Unity.
Live Cinema Mirroring: Desktop output mirrors in real time onto a grand 70mm VIP cinema projection screen.
Key Features
1. Seamless Multi-Monitor Extended Desktop
Physical Surface Projection: The mouse cursor is geometrically mapped to screen UVs and clamped to display meshes, preventing cursor detachment.
Edge Transition Continuity: Smoothly glide the cursor from Left (Telemetry) ↔ Center (Host Remote Desktop) ↔ Right (Operations NOC).
3D World-Space Height Preservation: Transitioning across monitors preserves the exact vertical elevation in world space, eliminating vertical jumping.
Hysteresis & Anti-Bounce: Built-in transition thresholds and dynamic cooldowns prevent border flutter and accidental screen switching.
2. Bi-Directional Host Windows Input Forwarding
Real-Time Desktop Capture: High-speed, multi-threaded GDI desktop capture rendering at up to 60 FPS with zero main-thread stutter.
Full Mouse Control: Hardware-level cursor synchronization (SetCursorPos + SendInput), enabling smooth hover tooltips, click and drag, double-click launch, right-click context menus, and scroll wheel navigation.
True Unicode & Keyboard Forwarding: Type directly into host Windows applications (browsers, code editors, terminals) with Unicode text injection and system key support (Enter, Backspace, Arrows, Tab, Win, Ctrl, Alt).
Unbounded Fluid Movement: Hardware raw input delta polling (CursorLockMode.Locked) prevents the mouse from getting trapped against game window borders.
Background Persistence: Application.runInBackground ensures smooth, unthrottled streaming even when an external desktop app is focused.
3. Real-Time 70mm VIP Theater IMAX Mirroring
Instant Cinema Projection: Project the active workstation desktop onto a massive 70mm curved IMAX cinema screen with a single hotkey (F8).
Dynamic Lighting Array: High-intensity cinematic projector beam with atmospheric volumetric lighting and emissive cinema canvas shaders.
Dual-Canvas Surface Sync: Synchronous rendering supporting both high-resolution render textures and direct host stream output.
4. Ergonomic Cyberpunk Workstation
Seated First-Person Navigation: Approach the desk and press E to transition smoothly into the seated cockpit.
Desk Rail Slide & Zoom: Slide horizontally along the desk rail (A/D or 1/2/3) to focus on specific monitors; lean in (W/S) for fine inspection.
Independent Free-Look Swivel: Hold Right-Click (RMB) to swivel your head and inspect the cyberpunk environment without disturbing cursor focus.
Reactive Ambilight & Themes: Dynamic ambient backlighting reacting to screen content with selectable cyberpunk color palettes (F5 / F6).
Controls
Action	PC / Keyboard & Mouse	VR Controller
Sit at Desk / Stand Up	E / ESC	Primary Button / Grip
Move Cursor	Mouse Movement	Ray Pointer / Thumbstick
Left Click / Drag	LMB (Hold to Drag)	Trigger
Right Click (Context Menu)	RMB (Single Click)	B / Y Button
Inspect / Free-Look Head Swivel	RMB (Hold + Move Mouse)	Headset Tracking (6-DOF)
Slide Seat Along Desk	A / D or 1, 2, 3	Left Thumbstick (Horizontal)
Lean In / Zoom	W / S	Left Thumbstick (Vertical)
Reset View / Recenter	Middle Mouse Button	Thumbstick Click
Toggle Reactive Ambilight	F5	HUD Dock Button
Cycle Visual Themes	F6	HUD Dock Button
Toggle Host PC Stream	F7	HUD Dock Button
Toggle VIP IMAX Projection	F8	HUD Dock Button
Architecture Overview
text
VRRemoteDeskTop/
├── Assets/
│   ├── Scripts/
│   │   ├── Computer/
│   │   │   ├── MultiMonitorManager.cs         # Coordinates triple-monitor cursor & boundaries
│   │   │   ├── ComputerInteractionManager.cs   # Manages seated ergonomics, camera glide & modes
│   │   │   └── RemoteDesktopScreen.cs         # Physical monitor mesh display & brightness
│   │   ├── Remote/
│   │   │   ├── WindowsHostScreenCapture.cs     # Multi-threaded GDI desktop capture engine
│   │   │   ├── WindowsHostInputForwarder.cs    # Low-level Win32 input injection (Mouse & Keys)
│   │   │   └── HostDesktopStreamManager.cs     # Central stream manager & input router
│   │   ├── Environment/
│   │   │   ├── ScreenShareToImaxManager.cs    # Real-time 70mm cinema screen mirroring
│   │   │   └── EnvironmentThemeController.cs  # Cyberpunk lighting & reactive ambilight
│   │   ├── UI/
│   │   │   ├── DesktopModeHUD.cs              # Workstation HUD, notification strips & dock
│   │   │   ├── LeftDashboardUI.cs             # System telemetry & diagnostics dashboard
│   │   │   └── RightDashboardUI.cs            # NOC log streaming & network operations
│   │   └── XR/
│   │       ├── VRDesktopInteractable.cs       # Surface raycast & UI pointer dispatcher
│   │       └── AuxiliaryScreenInteractable.cs # Left/Right monitor surface interaction

Tech Stack & Requirements
Engine: Unity 6 (6000.6.0f1 or later recommended)
Render Pipeline: Universal Render Pipeline (URP)
Target OS: Windows 10 / 11 (64-bit)
APIs Used: Win32 (user32.dll, gdi32.dll), Raw Input (WM_INPUT), SendInput API
XR SDK: OpenXR Plugin + Unity XR Interaction Toolkit (XRI 3.0+)
Input System: Unity New Input System (com.unity.inputsystem)
Getting Started
Clone the Repository:
bash
git clone https://github.com/your-username/VRRemoteDesktop.git
cd VRRemoteDesktop

Open in Unity Hub:
Add the cloned repository folder to Unity Hub.
Open with Unity 6 (6000.6.0f1+).
Open the Main Scene:
Navigate to Assets/Scenes/ and open haha.unity or igfinal.unity.
Run Play Mode:
Press Play in the Unity Editor.
Walk up to the workstation desk and press E to sit down.
The middle screen will automatically start capturing your Windows desktop and project onto the IMAX screen (F8).
License

Distributed under the MIT License. See LICENSE for more information.
