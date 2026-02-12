## 🌟 Highlights

- Creates a `ManagementEventWatcher` to scan `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` for changes
- Currently checks `EnableLUA`, and `DontDisplayLastUsername`, but that can easily be modified by changing `Program.s_desiredValues`

## ℹ️ Overview

Occasionally need this because some companies try to enforce some policies that make my daily work harder to perform.
Since I have an admin account on my PC.
- I can go around `EnableLUA` by simply running apps by holding Shift+right click and choosing "Run as different user" instead, 
but that is a little bit more complicated than simply choosing regular option "Run as Administrator".
- `DontDisplayLastUsername` is a QOL thing I've gotten used to as well. Most of the time I don't need my admin account anymore.

### ✍️ Authors

I'm [Per Jogbäck](https://github.com/PerJogback) and I created this small application so that at least me, myself and I can be a little more efficient.

## 🚀 Usage

Must be Run as Administrator <img src="https://img.icons8.com/?size=100&id=1YDhwgHDo9oS&format=png&color=000000" width=15/> , otherwise registry cannot be modified.

<img width="431" height="118" alt="image" src="https://github.com/user-attachments/assets/bf554b0f-5e75-4b94-ba1f-5c874064c1e7" />

## ⬇️ Installation

### 🖥️ Option 1: As console app:
- Just download, build and run `GpoRegistryWatcher.ConsoleApp.exe` as Administrator

### ⚙️ Option 2: As Windows Service
Install service with the following command(s):
```
sc.exe create GpoRegistryWatcher.WinService start=auto binPath= "C:\WindowsServices\GpoRegistryWatcher.WinService\GpoRegistryWatcher.WinService.exe" DisplayName= "GPO Registry Watcher"
sc.exe description GpoRegistryWatcher.WinService "Monitors and restores specific registry values to prevent unwanted GPO overrides."
```

