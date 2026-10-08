<p align="center">
  <img src="docs/logo.png" width="128" alt="RightLayout logo">
</p>

# RightLayout

Automatically switch Windows to the right input method for whichever keyboard you type on.

Use your laptop keyboard in one language and an external keyboard in another: RightLayout notices which keyboard each key press comes from and switches the input method for you. It remembers every keyboard, including Bluetooth keyboards that disconnect and reconnect when the laptop sleeps.

RightLayout is a continuation of [RightKeyboard](https://github.com/gmcouto/RightKeyboard) by Gabriel Couto, which is no longer maintained. It is free and open source under the [MIT License](LICENSE); see [License and credits](#license-and-credits).

## Features

- **Remembers your keyboards.** Choose an input method once per keyboard. It is kept through sleep, reconnects and restarts until you forget the keyboard.
- **One entry per physical keyboard.** Windows reports a single keyboard as several devices (main keys, media keys, hotkeys). RightLayout groups them, so your keyboard appears once and every part of it uses the same input method.
- **Keyboards window.** See all your keyboards by name (for example *AULA-F75 5.0 KB* or *Built-in keyboard*), whether each is connected, and its input method. Change, rename or forget any of them. The keyboard you are typing on is highlighted.
- **Clear new-keyboard prompt.** Names the keyboard it detected, lets you search your installed input methods, and offers *Don't switch for this keyboard* for keyboards you want left alone. Keys you were still typing for another window are ignored, never shown.
- **Windows 11 look.** Follows your light or dark app setting and stays sharp on high-DPI screens.

## Requirements

- Windows 10 or 11.
- .NET Framework 4, which is already part of Windows 10 and 11.

## Before you start

RightLayout switches between the *languages* installed in Windows (Settings → Time & language → Language & region). Give each keyboard layout you want to use its own language. For example:

- English (United States) with only the US layout
- French (France) with only the French AZERTY layout

If two layouts belong to the same language, RightLayout can't tell them apart.

## Installing

1. Download `RightLayout-v4.7.exe` from the [Releases](../../releases) page (see [Building](#building) to build it yourself), and [check that it wasn't altered](#verifying-the-download).
2. Put it in a folder of its own, for example `%LOCALAPPDATA%\Programs\RightLayout`. Don't run it from a shared folder such as Downloads: a malicious DLL saved there could be loaded by .NET as the app starts.
3. Press **Windows + R**, type `shell:startup` and press Enter. Right-click in that folder and choose **New → Shortcut**, pointing to the exe, so RightLayout starts when you sign in.
4. Double-click the exe to start it now. RightLayout runs in the notification area of the taskbar.

If you used RightKeyboard or an earlier RightLayout before, close it first so the two don't both react to your typing, and remove its exe or shortcut from the startup folder. Your saved keyboards are kept.

Windows SmartScreen may warn that the app is from an unknown publisher, because the exe is not code-signed. Choose **More info → Run anyway** after verifying the download.

### Verifying the download

Each release lists the SHA-256 hash of the exe and includes it as a `.sha256` file. In PowerShell, in the folder holding the exe:

```powershell
Get-FileHash .\RightLayout-v4.7.exe -Algorithm SHA256
```

The hash shown must match the published one exactly. If it doesn't, delete the file.

### Privacy

RightLayout only reads *which* keyboard each key press comes from, never which key was pressed. It doesn't record or store your typing, has no network access, and runs without administrator rights.

## Using RightLayout

**The first time you type on a keyboard**, RightLayout asks which input method it should use:

- **Use this input method** saves your choice for that keyboard.
- **Don't switch for this keyboard** saves that the keyboard should never change the input method.
- **Closing the window** skips that keyboard until RightLayout restarts.

**The tray icon:**

- **Left-click** or choose **Keyboards...** to open the Keyboards window.
- **Open app location** opens File Explorer with `RightLayout-v4.7.exe` selected, handy if you forgot where you put it.
- **Forget all keyboards...** clears every saved keyboard. RightLayout asks again the next time you type on each one.
- **Exit** closes RightLayout.

**In the Keyboards window:**

- **Your keyboards** lists every saved keyboard, connected or not. Pick a different input method from its drop-down; the change is saved immediately.
- **Other connected keyboards** lists keyboards you haven't set up yet. You can choose their input method before typing on them.
- The **⋯** button on a keyboard lets you **rename** it or **forget** it.
- **Mixed, choose one** means different parts of that keyboard were saved with different input methods, usually by an older version. Pick one to fix it.
- The **☰** menu (top right) has **Open app location** and **Open settings file location**, which opens File Explorer with `config.txt` selected, for example to back it up.

### How keyboards are recognised

Each keyboard is identified by the device name Windows gives it, which stays the same when it reconnects. Devices that Windows reports as belonging to the same physical device (the same *container*) are shown as one keyboard. Everything built into the laptop, such as the keyboard and its Fn/hotkey controller, appears as **Built-in keyboard**.

A keyboard that connects in two ways, for example over Bluetooth and through a 2.4 GHz USB receiver, appears as two keyboards, because Windows sees two different devices. Set both to the same input method if you want them to behave the same.

Re-pairing a Bluetooth keyboard can give it a new device name, in which case RightLayout asks once more.

### Where settings are stored

`%LOCALAPPDATA%\RightLayout\config.txt` (open it from **☰ → Open settings file location** in the Keyboards window).

On first run, RightLayout copies `%LOCALAPPDATA%\RightKeyboard\config.txt` if it exists. The old file is left in place.

## Building

The project targets .NET Framework 4 and builds with the MSBuild that ships with Windows; Visual Studio is not required.

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" RightLayout\RightLayout.csproj `
    /t:Rebuild /p:Configuration=Release /p:AssemblyName=RightLayout-v4.7 /p:OutDir=..\dist\
```

This produces `dist\RightLayout-v4.7.exe`, a single file of about 96 KB.

Without the .NET Framework 4 targeting pack, MSBuild also copies framework files such as `mscorlib.dll` into `dist`. The app doesn't need them and they can be deleted.

Release builds contain no debug symbols, so the exe doesn't include the folder path (and username) of the machine that built it.

## Version history

### 4.7
Security and reliability fixes:
- Listing keyboards could write past the end of a buffer if a device connected at the wrong moment, which could crash the app. Windows is now told the real size of the buffer.
- The current input method is read from the right window. A truncated thread ID could make RightLayout read the wrong one, so it switched when it didn't need to, and the new-keyboard prompt could preselect the wrong input method.
- The exe no longer contains the folder path of the machine it was built on.
- The project, namespace and assembly are renamed from RightKeyboard to RightLayout.

Now open source under the MIT License:
- All code that came from RightKeyboard has been rewritten (see [License and credits](#license-and-credits)).
- Input method names now come from Windows instead of a list bundled with the app.

### 4.6
- **Open app location** in the tray menu shows where RightLayout is running from.
- New **☰** menu in the Keyboards window with **Open app location** and **Open settings file location**.

### 4.5
Security and reliability fixes:
- Windows DLLs are loaded only from System32. Previously a malicious DLL with the right name placed next to the exe (for example in Downloads) could be loaded.
- The new-keyboard prompt no longer shows what you were typing. It used to put the cursor in its search box, so text meant for another window, possibly a password, appeared there. Keys pressed in the first 1.5 seconds after it opens are now ignored.
- An input event Windows can't describe no longer makes the app quit silently.

### 4.0
- Renamed to RightLayout, with a new logo.
- Settings moved to `%LOCALAPPDATA%\RightLayout`, copied from RightKeyboard on first run.
- The exe is smaller: the icon is stored once and compressed.

### 3.5
- Keyboards are grouped by physical device, so one keyboard no longer appears as several entries.
- New parts of an already configured keyboard use its input method instead of prompting.
- Bluetooth LE keyboards show their product name.

### 3.0
- New Keyboards window and redesigned new-keyboard prompt.
- Windows 11 styling with light and dark themes, and high-DPI support.
- Keyboards can be renamed, forgotten, or set to *Don't switch*.

### Reconnect fix
- Keyboards are remembered by device name instead of a handle that changes on every reconnect, so a Bluetooth keyboard is no longer asked about after each sleep.
- Saved keyboards are kept even when they are not connected at startup.
- Choices are saved immediately rather than only on exit.

### Earlier changes (RightKeyboard, by Gabriel Couto)
- Removed the "current layout cache" and read the current layout from Windows instead, as the cache failed after sleep.
- Added a *Clear* menu item to clear the configuration.

## License and credits

RightLayout is released under the [MIT License](LICENSE). Copyright (c) 2026 Koushik.

### Where RightLayout comes from

- [RightKeyboard](https://github.com/gmcouto/RightKeyboard) by Gabriel Couto, which RightLayout continues. Thank you, Gabriel.
- RightKeyboard was based on the CodeProject article [Using multiple keyboards with different layouts on the same machine](http://www.codeproject.com/Articles/20994/Using-multiple-keyboards-with-different-layouts-on), which showed how to tell keyboards apart with Windows Raw Input and switch the input method for each one.

### Why the code was rewritten

Neither RightKeyboard nor the CodeProject article published a license for its code, and a [request for one](https://github.com/gmcouto/RightKeyboard/issues) has gone unanswered. So that RightLayout can be shared freely, version 4.7 replaces every remaining part of that code with new code:

| Original code | Replaced in 4.7 by |
| --- | --- |
| `MainForm` (hidden window, tray icon and menu, layout switching), with its designer and resource files | `TrayApp.cs`: the tray icon and menu built in code, and a separate invisible window that receives keyboard input |
| Win32 declarations in `API.cs`, `RAWINPUTDEVICE.cs` and `RAWINPUTHEADER.cs` | A new `API.cs` that wraps each Windows call in a small function (listen to keyboards, get a device name, switch language) |
| `Layout.cs` and the bundled list of language names `Layouts.txt` | A new `Layout.cs` that takes language names from Windows, so no list is bundled |
| `Program.cs`, `AssemblyInfo.cs`, the `Properties` resource and settings files, the project and solution files | New, minimal files; the unused resource and settings files are gone |

The rest of the app was written for RightLayout from version 3.0 onwards: the Keyboards window, the new-keyboard prompt, keyboard grouping and naming, settings storage, theming and the security fixes.

What remains in common with the original is limited to what any program doing the same job must contain, such as the names and values of the Windows functions and messages involved. The Git history still includes the earlier RightKeyboard code as it was published; the MIT License covers RightLayout's code from version 4.7 on.
