# sbox-time-system
☀️ A simple library for implementing a 24 hour time system for day and night. Includes a time controller entity for mappers to use to set specific colors and settings for each time period.

# Entities
Maps can add the Time Controller entity to configure how the lighting should look at each point of the day. This includes stuff like ambient light color, sky color, as well as fog settings for each too. It also has outputs for when the time of day changes between Dawn, Dusk, Day and Night.

# API
Everything can be accessed from `TimeSystem`. There are public properties that allow you to get the current time of day, and current temperature.

## Events
You can use `TimeSystem.OnSectionChanged` to add a callback for when the time of day changes between Dawn, Dusk, Day and Night.

# ConVars
`time.speed` is a ConVar that controls the speed at which the time of day progresses. This can also be changed by code using `TimeSystem.Speed`.
