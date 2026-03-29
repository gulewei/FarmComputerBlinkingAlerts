# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Stardew Valley mod that makes the Farm Computer pulse with a glowing aura when there are things on your farm that need attention (crops ready, machines finished, casks with iridium quality, empty hay). The mod is built with C# using the SMAPI (Stardew Modding API) framework.

## Common Development Tasks

- **Build**: `dotnet build` - Outputs to `bin/` directory
- **Test**: `dotnet test --no-build` - Runs any unit tests (note: no test projects currently exist)
- **Clean**: `dotnet clean` - Cleans build artifacts
- **Release Build**: The GitHub Actions workflow uses Pathoschild's SMAPI-ModBuildWorkflow to package release zips

## Architecture Overview

The mod follows a modular design with three main components:

1. **FarmScanner** (`FarmScanner.cs`): Scans the farm for conditions that need attention
2. **PulsingAuraEffect** (`PulsingAuraEffect.cs`): Manages the visual aura effect around the Farm Computer
3. **InteractionHandler** (`InteractionHandler.cs`): Handles player interaction to stop the aura

**ModEntry** (`ModEntry.cs`) is the main entry point that orchestrates these components and hooks into SMAPI events.

## Key Components and Responsibilities

### FarmScanner
- Scans daily at 6:00 AM game time (triggered by `DayStarted` event)
- Checks for:
  - Ready-to-harvest crops (`ScanForReadyCrops`)
  - Finished machines (`ScanForReadyMachines`) with special handling for casks
  - Empty hay in silo when animals are present (`ScanForEmptyHay`)
  - *(Commented out)* Animal products, fruit trees, crab pots
- Returns list of alert messages; any alert triggers the aura

### PulsingAuraEffect
- Creates and renders a circular pulsing aura texture
- Positions aura above and to the right of the Farm Computer (antenna location)
- Uses sine wave for pulsating size and alpha
- Searches all game locations for the Farm Computer object (ID 787)

### InteractionHandler
- Listens for action button (right-click) presses
- Detects interaction with Farm Computer via tile checking and name matching
- Debounces interactions to prevent rapid triggering
- Calls back to `ModEntry` to reset `needsAttention` flag

## Important IDs and Constants

- **Farm Computer ID**: 787 (vanilla ParentSheetIndex)
- **Cask ID**: 163 (special handling for iridium quality only)
- **Crab Pot ID**: 710 (currently unused)
- **Quality Values**: 0 = normal, 1 = silver, 2 = gold, 4 = iridium
- **Aura Color**: Red (255, 80, 80, 120) matching antenna color
- **Aura Position**: Offset +65% right, -35% up from tile position

## Logging Conventions

The mod uses SMAPI's `IMonitor` interface with these log levels:
- **Trace**: Detailed debugging information (object positions, button presses)
- **Debug**: General debugging (scan results, aura state changes)
- **Info**: Important events (mod loaded, Farm Computer found)
- **Error**: Exception reporting

## Event Hooks

Registered in `ModEntry.Entry()`:
- `GameLoop.DayStarted`: Triggers daily scan
- `GameLoop.UpdateTicked`: Updates aura animation (~60 FPS)
- `Display.Rendered`: Draws aura effect
- `Input.ButtonPressed`: Handles player interaction
- `GameLoop.SaveLoaded`: Resets state on save load
- `GameLoop.ReturnedToTitle`: Cleans up on returning to title screen

## Testing Notes

- No unit test projects currently exist
- Manual testing should verify:
  - Aura appears when conditions are met
  - Aura stops on interaction
  - Scan correctly identifies ready crops/machines
  - Cask alerts only trigger at iridium quality (quality >= 4)
- Use SMAPI's console for real-time log monitoring

## CI/CD

- GitHub Actions workflow (`.github/workflows/main.yml`) builds on push
- Uses Pathoschild's SMAPI-ModBuildWorkflow actions for mod packaging
- Creates release zips with attestations for stable branch pushes
- Build environment includes .NET 6.0 SDK

## Development Notes

- **Commented Features**: `FarmScanner.ScanAll()` has commented lines (42-44) for animal products, fruit trees, and crab pots detection
- **Cask Special Logic**: Casks are excluded from general machine checking; they only trigger alerts at iridium quality
- **Localization Support**: Interaction detection includes both English ("Farm Computer", "Computer") and Chinese ("农场电脑", "电脑") name matching
- **Performance**: Scanning is optimized; checks are performed once per day at most

## Configuration System

The mod supports configurable scanning options via `config.json` and an in-game configuration menu (requires Generic Mod Config Menu mod).

### Configuration Model (`ModConfig.cs`)
- **EnableCropScanning** (bool): Whether to scan for crops ready to harvest (default: true)
- **EnableMachineScanning** (bool): Whether to scan for machines that have finished processing (default: true)
- **EnableHayScanning** (bool): Whether to scan for empty hay in the silo when animals are present (default: true)
- **CaskMinimumQuality** (int): Minimum quality level for casks to trigger alerts (0=normal, 1=silver, 2=gold, 4=iridium, default: 4)

### Configuration Loading
- Configuration is loaded in `ModEntry.Entry()` via `helper.ReadConfig<ModConfig>()`
- The configuration is validated via `Validate()` method to ensure quality values are valid
- Configuration changes take effect on the next daily scan

### Generic Mod Config Menu Integration
- The mod registers with Generic Mod Config Menu in `OnGameLaunched` if the mod is available
- The `RegisterConfigMenu()` method sets up the configuration UI with sections for scanning options and cask settings
- Uses `IGenericModConfigMenuApi` interface defined in `IGenericModConfigMenuApi.cs`
- Configuration changes are saved via `helper.WriteConfig()` when the user saves in the menu

### FarmScanner Integration
- `FarmScanner` receives the `ModConfig` instance in its constructor
- `ScanAll()` method checks configuration before adding alerts for each scan type
- `IsCaskReadyForHarvest()` uses `config.CaskMinimumQuality` instead of hardcoded value