# HA_Desktop_Companion
Why did I make this app ? 

Cause I don't like existing implementations using MQTT and I took inspiration from awesome ESPhome and its native communication protocol to HA and implemented it my own way :)

Feel free to contribute any time :)

## Installation
1) Extract the zip file to some folder on your system, 
2) Run `HA_Desktop_Companion.exe`
3) Fill in "URL" & "API Token"
4) Click "Register"
2) Create shortcut to `HA_Desktop_Companion.exe` in `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup`

## Sensors implemented currently:
- battery_level
- battery_state
- is_charging
- wifi_ssid
- cpi_temp
- current_active_window

![image](https://user-images.githubusercontent.com/22167469/184626954-f727a699-79cc-4345-9c32-ff6dd912937d.png)

## Future plans:
- Simple configuration of sensors in YAML
- Improved debug mode
- Encryption
