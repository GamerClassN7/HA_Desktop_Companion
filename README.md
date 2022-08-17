# HA_Desktop_Companion
Why did I make this app ? 

Cause I don't like existing implementations using MQTT and I took inspiration from awesome ESPhome and its native communication protocol to HA and implemented it my own way :)

Feel free to contribute any time :)

## Installation
1) Extract the zip file to some folder on your system, 
2) Run `HA_Desktop_Companion.exe`
3) Fill in "URL" & "API Token"
4) Click "Register"
2) Create shortcut to `HA_Desktop_Companion.exe` in `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup` (if you want app to start on computer boot)

## Sensors implemented currently:
- battery_level
- battery_state
- is_charging
- wifi_ssid
- cpu_temp
- current_active_window
- uptime
- camera_in_use
- cpu_temperature (only native api supported)
- free_ram

## Screenshots

![image](https://user-images.githubusercontent.com/22167469/184820849-c2932b91-a4ee-4c0d-a220-58ab01444c29.png)

![image](https://user-images.githubusercontent.com/22167469/185061529-9868070a-cf1e-4531-877e-443c1b1be1e4.png)

## Automation Ideas:
Pause TTS when camera is in use (usefull when working from home) credits: [Hellis81](https://community.home-assistant.io/u/Hellis81)

## Future plans:
- Simple configuration of sensors in YAML
- Improved debug mode
- Encryption
