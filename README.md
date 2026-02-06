Creates a ManagementEventWatcher to scan HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System for changes
Currently checks EnableLUA, and DontDisplayLastUsername, but that can easily be modified by changing Program.s_desiredValues

Must be <img src="https://img.icons8.com/?size=100&id=1YDhwgHDo9oS&format=png&color=000000" width=15/> Run as Administrator, otherwise registry cannot be modified.
