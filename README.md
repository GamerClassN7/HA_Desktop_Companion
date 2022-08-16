# HA_Desktop_Companion
Why did I make this app ? 

Cause I don't like existing implementations using MQTT and I took inspiration from awesome ESPhome and its native communication protocol to HA and implemented it my own way :)

Feel free to contribute any time :)

## NOTICE
in order to run this applikaction you need to hawe [.NET Desktop Runtime (v6.0.7 or later)](https://download.visualstudio.microsoft.com/download/pr/b4a17a47-2fe8-498d-b817-30ad2e23f413/00020402af25ba40990c6cc3db5cb270/windowsdesktop-runtime-6.0.8-win-x64.exe) installed!

## Installation
1) Extract the zip file to some folder on your system, 
2) Run `HA_Desktop_Companion.exe`
3) Fill in "URL" & "API Token"
4) Click "Register"
2) Create shortcut to `HA_Desktop_Companion.exe` in `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup`
  - if you want app to start on computer boot

## Sensors implemented currently:
- battery_level
- battery_state
- is_charging
- wifi_ssid
- cpi_temp
- current_active_window
- uptime

## Screenshots

![image](https://user-images.githubusercontent.com/22167469/184820849-c2932b91-a4ee-4c0d-a220-58ab01444c29.png)

![image](https://user-images.githubusercontent.com/22167469/184820793-09eac437-ff73-4015-b2e6-9dcf952bcafe.png)


## Future plans:
- Simple configuration of sensors in YAML
- Improved debug mode
- Encryption
