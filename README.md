> Most of the time new version introduce new bugs so please if you are using working version keep using it until some stable release come out :)

# HA Desktop Companion - Reborn
[![Github All Releases](https://img.shields.io/github/downloads/GamerClassN7/HA_Desktop_Companion/total.svg)]()

Why did I make this app ? 

Cause I don't like existing implementations using MQTT and I took inspiration from awesome ESPhome and its native communication protocol to HA and implemented it my own way :)

Feel free to contribute any time :)

[HomeAssistant Comunity Forum](https://community.home-assistant.io/t/ha-desktop-companion/)

[![Join our Discord server!](https://invidget.switchblade.xyz/Kth2GyZMU7)](http://discord.gg/Kth2GyZMU7)
[Discord](https://discord.com/invite/Kth2GyZMU7)

## Installation
1) Download latest release [HERE](https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest)
2) Extract the zip file to some folder on your system, 
3) Run `HA.exe`
4) Fill in "URL" & "API Token"
5) Click "Save"

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
- wmic (You can integrate any wmix query syou want :))
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
App which is using native HA Api to comunicate and report data to HA

## Screenshots
![image](https://user-images.githubusercontent.com/22167469/184820849-c2932b91-a4ee-4c0d-a220-58ab01444c29.png)
![image](https://user-images.githubusercontent.com/22167469/185061529-9868070a-cf1e-4531-877e-443c1b1be1e4.png)

## Future plans [TODO](./HADC_REBORN/TODO.md):
- Improved debug mode
- Encryption

## Notifications
Example Basic Notification:
![image](https://user-images.githubusercontent.com/22167469/231707378-59b4cd34-9218-4219-87d7-5a12671d353e.png)
![image](https://user-images.githubusercontent.com/22167469/231707458-5d9ba8db-6c73-4095-9e9f-2113f3ae9236.png)

Example Inline Image Notification:
![image](https://user-images.githubusercontent.com/22167469/231706977-89879e9c-8ac9-43ce-8e66-0fb073925238.png)
![image](https://user-images.githubusercontent.com/22167469/231706759-1cc1aaa2-2f08-41ce-8799-adf5a01d22c5.png)
```json
{
  "image":"https://upload.wikimedia.org/wikipedia/commons/9/9f/Old_wikipedia_logo.png"
}
```

Example Audio Notification:
![image](https://user-images.githubusercontent.com/22167469/231707164-9b0cda16-5257-4edc-b275-8fbcac7dfcbf.png)

Example Emulate Send Key Notification:
* Require `keys:` in your `configuration.yaml`
![image](https://github.com/GamerClassN7/HA_Desktop_Companion/assets/22167469/730fdbf4-4744-48a5-9b19-379978e81ef5)
* Keys Codes can be found [Here](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes) in Colum: __Value__

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

