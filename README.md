# Farm Computer Blinking Alerts

A Stardew Valley mod that makes the Farm Computer pulse with a glowing aura when there are things on your farm that need attention.

## Features

- **Automatic Daily Scanning**: Every morning when you wake up, the mod scans your farm for:
  - Crops ready to harvest
  - Machines finished processing (furnaces, kegs, preserve jars, etc.)
  - Empty hay in the silo (when you have animals)
- **Visual Alert**: When something needs attention, the Farm Computer item will pulse with a light blue aura (similar to the Junimo Hut effect).
- **Simple Interaction**: Click on the Farm Computer to make the aura stop (acknowledges the alert).
- **No Configuration Needed**: Works out of the box with sensible defaults.

## Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download this mod from the releases page.
3. Extract the `FarmComputerBlinkingAlerts` folder into your `Stardew Valley/Mods` directory.
4. Launch the game using SMAPI.

## How It Works

1. Each morning at 6:00 AM (game time), the mod scans your farm.
2. If any crops are ready, machines are finished, or hay is empty, the Farm Computer will start pulsing.
3. The pulsing aura is visible wherever the Farm Computer is placed (on a table, on the ground, etc.).
4. To acknowledge the alert, simply right-click on the Farm Computer.
5. The aura will stop pulsing until the next morning's scan.

## Compatibility

- Compatible with Stardew Valley 1.6+.
- Should work with most other mods.
- Uses the vanilla Farm Computer item (ID: 787).
- No known conflicts.

## Building from Source

If you want to build the mod yourself:

1. Install [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Clone this repository.
3. Run `dotnet build` in the project directory.
4. The built mod will be in the `bin` folder.

## Troubleshooting

- **Aura not appearing**: Make sure you have placed the Farm Computer item somewhere on your farm. The mod only works when the item is placed, not when it's in your inventory.
- **False alerts**: The scan happens once per day at 6:00 AM. If you harvest crops after the scan, the aura will remain until you interact with the Farm Computer.
- **Performance issues**: The scan is optimized and should not cause lag even on large farms.

