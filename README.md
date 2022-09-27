> Most of the time new wersion introduce new bugs so please if you are using working wersion keep using it untill some stable release come out :)

# HA_Desktop_Companion

[![Github All Releases](https://img.shields.io/github/downloads/GamerClassN7/HA_Desktop_Companion/total.svg)]()

Why did I make this app ? 

Cause I don't like existing implementations using MQTT and I took inspiration from awesome ESPhome and its native communication protocol to HA and implemented it my own way :)

Feel free to contribute any time :)

[HomeAssistant Comunity Forum Thread](https://community.home-assistant.io/t/ha-desktop-companion/)

## Installation
1) Download latest release [HERE](https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest)
2) Extract the zip file to some folder on your system, 
3) Run `HA_Desktop_Companion.exe`
4) Fill in "URL" & "API Token"
5) Click "Register"
6) Create shortcut to `HA_Desktop_Companion.exe` in `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup` (if you want app to start on computer boot)

## Sensors implemented currently:
- battery_level
- battery_state
- is_charging
- wifi_ssid
- current_active_window
- uptime
- camera_in_use
- cpu_temperature (only native api supported)
- free_ram
- wmic (You can integrate any wmic query you want :))
```yaml
- platform: wmic
  wmic_path: Win32_Battery
  wmic_selector: BatteryStatus
  wmic_namespace: \\root\CIMV2
  value_map: "Discharging|On AC|Fully Charged|Low|Critical|Charging|Charging and High|Charging and Low|Undefined|Partially Charged"
  name: Battery State
  unique_id: battery_state
  icon: "mdi:battery-minus"
  entity_category: "diagnostic"
  device_class: battery
``` 

## Screenshots

![image](https://user-images.githubusercontent.com/22167469/184820849-c2932b91-a4ee-4c0d-a220-58ab01444c29.png)

![image](https://user-images.githubusercontent.com/22167469/185061529-9868070a-cf1e-4531-877e-443c1b1be1e4.png)

## Automation Ideas:
Pause TTS when camera is in use (usefull when working from home) credits: [Hellis81](https://community.home-assistant.io/u/Hellis81)
```yaml
alias: Washing machine done
description: ""
trigger:
  - platform: numeric_state
    entity_id: sensor.washing_machine_program_progress
    above: "99"
  - platform: state
    entity_id: sensor.washing_machine_operation_state
    from: Run
    to: Finished
  - platform: state
    entity_id: sensor.washing_machine_operation_state
    from: Run
    to: Ready
condition: []
action:
  - if:
      - condition: state
        entity_id: binary_sensor.axlt2801_camera_in_use
        state: "on"
    then:
      - wait_for_trigger:
          - platform: state
            entity_id:
              - binary_sensor.axlt2801_camera_in_use
            to: "off"
        continue_on_timeout: false
    else: []
  - service: tts.cloud_say
    data:
      entity_id: media_player.hela_huset
      message: "{{ states('sensor.washing_machine_tts') }}"
      language: sv-SE
  - repeat:
      while:
        - condition: or
          conditions:
            - condition: state
              entity_id: binary_sensor.washing_machine_door
              state: "off"
            - condition: state
              entity_id: sensor.washing_machine_program_progress
              state: "100"
      sequence:
        - delay:
            hours: 0
            minutes: 5
            seconds: 0
            milliseconds: 0
        - choose:
            - conditions:
                - condition: and
                  conditions:
                    - condition: state
                      entity_id: binary_sensor.washing_machine_door
                      state: "off"
                    - condition: state
                      entity_id: sensor.washing_machine_program_progress
                      state: "100"
              sequence:
                - service: tts.cloud_say
                  data:
                    entity_id: media_player.hela_huset
                    message: >-
                      "{{ states('sensor.washing_machine_tts') }} och luckan är
                      fortfarande stängd."
                    language: sv-SE
                - service: homeassistant.update_entity
                  target:
                    entity_id: sensor.washing_machine_json
                  data: {}
          default: []
mode: single
```
## Future plans:
- Simple configuration of sensors in YAML
- Improved debug mode
- Encryption
