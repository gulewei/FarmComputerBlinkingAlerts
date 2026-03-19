using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Handles player interaction with the Farm Computer.</summary>
    public class InteractionHandler
    {
        private readonly PulsingAuraEffect effect;
        private readonly IMonitor monitor;
        private readonly Action onInteractionCallback;
        private DateTime lastInteractionTime;

        /// <summary>Initializes a new instance of the <see cref="InteractionHandler"/> class.</summary>
        /// <param name="effect">The pulsing aura effect to control.</param>
        /// <param name="monitor">Monitor for logging.</param>
        /// <param name="onInteractionCallback">Callback to invoke when player interacts with Farm Computer.</param>
        public InteractionHandler(PulsingAuraEffect effect, IMonitor monitor, Action onInteractionCallback)
        {
            this.effect = effect;
            this.monitor = monitor;
            this.onInteractionCallback = onInteractionCallback;
            this.lastInteractionTime = DateTime.MinValue;
        }

        /// <summary>Called when a button is pressed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            try
            {
                this.monitor.Log($"Button pressed: {e.Button} (IsActionButton: {e.Button.IsActionButton()})", LogLevel.Trace);

                // Only process action button (usually right click)
                if (!e.Button.IsActionButton())
                {
                    this.monitor.Log($"Not an action button, skipping.", LogLevel.Trace);
                    return;
                }

                // Avoid processing too quickly (debounce)
                if ((DateTime.Now - this.lastInteractionTime).TotalSeconds < 0.5)
                {
                    this.monitor.Log($"Debouncing, last interaction was {(DateTime.Now - this.lastInteractionTime).TotalSeconds:F2} seconds ago.", LogLevel.Trace);
                    return;
                }

                this.monitor.Log($"Processing action button press.", LogLevel.Debug);

                // Check if player is interacting with a Farm Computer
                if (this.IsInteractingWithFarmComputer(e))
                {
                    this.monitor.Log("Player interacted with Farm Computer, stopping aura.", LogLevel.Debug);
                    this.effect.StopPulsing();
                    this.onInteractionCallback?.Invoke();
                    this.lastInteractionTime = DateTime.Now;
                }
                else
                {
                    this.monitor.Log("Action button press did not interact with Farm Computer.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                this.monitor.Log($"Error in interaction handler: {ex}", LogLevel.Error);
            }
        }

        /// <summary>Checks if the player is interacting with a Farm Computer.</summary>
        /// <param name="e">The button pressed event data.</param>
        /// <returns>True if interacting with a Farm Computer.</returns>
        private bool IsInteractingWithFarmComputer(ButtonPressedEventArgs e)
        {
            // Get the tile the player is trying to interact with using multiple methods
            var cursorTile = e.Cursor.Tile;
            var grabTile = e.Cursor.GrabTile;
            var absolutePos = e.Cursor.AbsolutePixels;

            this.monitor.Log($"Checking interaction - Tile: {cursorTile}, GrabTile: {grabTile}, AbsolutePixels: {absolutePos}", LogLevel.Debug);
            var playerPos = Game1.player.Position;
            var playerTilePos = new Microsoft.Xna.Framework.Vector2((int)(playerPos.X / Game1.tileSize), (int)(playerPos.Y / Game1.tileSize));
            this.monitor.Log($"Player position: {playerTilePos}, facing: {Game1.player.FacingDirection}", LogLevel.Debug);

            // Farm Computer has ParentSheetIndex = 787 (in vanilla Stardew Valley)
            const int farmComputerId = 787;

            // Check all game locations
            int totalObjectsChecked = 0;
            foreach (var location in Game1.locations)
            {
                int locationObjects = location.objects.Count();
                this.monitor.Log($"Checking location: {location.Name} (objects: {locationObjects}, furniture: {location.furniture.Count})", LogLevel.Debug);

                // Try cursor tile first
                if (location.objects.TryGetValue(cursorTile, out var obj))
                {
                    totalObjectsChecked++;
                    this.monitor.Log($"Found object at cursor tile: {obj.DisplayName} (ID: {obj.ParentSheetIndex}, Type: {obj.GetType().Name}, ReadyForHarvest: {obj.readyForHarvest.Value})", LogLevel.Debug);
                    if (obj.ParentSheetIndex == farmComputerId ||
                        obj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                        obj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                        obj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                        obj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase))
                    {
                        this.monitor.Log($"FARM COMPUTER FOUND at cursor tile! {obj.DisplayName} (ID: {obj.ParentSheetIndex})", LogLevel.Info);
                        return true;
                    }
                }

                // Also try grab tile
                if (cursorTile != grabTile && location.objects.TryGetValue(grabTile, out var grabObj))
                {
                    totalObjectsChecked++;
                    this.monitor.Log($"Found object at grab tile: {grabObj.DisplayName} (ID: {grabObj.ParentSheetIndex}, Type: {grabObj.GetType().Name})", LogLevel.Debug);
                    if (grabObj.ParentSheetIndex == farmComputerId ||
                        grabObj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                        grabObj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                        grabObj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                        grabObj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase))
                    {
                        this.monitor.Log($"FARM COMPUTER FOUND at grab tile! {grabObj.DisplayName} (ID: {grabObj.ParentSheetIndex})", LogLevel.Info);
                        return true;
                    }
                }

                // Also check furniture at the cursor tile (though Farm Computer is an object, not furniture)
                if (location.furniture.Count > 0)
                {
                    foreach (var furniture in location.furniture)
                    {
                        totalObjectsChecked++;
                        if (furniture.TileLocation == cursorTile &&
                            (furniture.ParentSheetIndex == farmComputerId ||
                             furniture.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                             furniture.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                             furniture.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                             furniture.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase)))
                        {
                            this.monitor.Log($"FARM COMPUTER FOUND as furniture at cursor: {furniture.DisplayName} (ID: {furniture.ParentSheetIndex})", LogLevel.Info);
                            return true;
                        }
                    }
                }

                // Check adjacent tiles (3x3 area around cursor) in case click is slightly off
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var checkTile = new Microsoft.Xna.Framework.Vector2(cursorTile.X + dx, cursorTile.Y + dy);

                        if (location.objects.TryGetValue(checkTile, out var adjacentObj))
                        {
                            totalObjectsChecked++;
                            this.monitor.Log($"Found object at adjacent tile ({dx},{dy}): {adjacentObj.DisplayName} (ID: {adjacentObj.ParentSheetIndex}, Type: {adjacentObj.GetType().Name})", LogLevel.Trace);
                            if (adjacentObj.ParentSheetIndex == farmComputerId ||
                                adjacentObj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                                adjacentObj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                                adjacentObj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                                adjacentObj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase))
                            {
                                this.monitor.Log($"FARM COMPUTER FOUND at adjacent tile ({dx},{dy})! {adjacentObj.DisplayName} (ID: {adjacentObj.ParentSheetIndex})", LogLevel.Info);
                                return true;
                            }
                        }
                    }
                }
            }

            this.monitor.Log($"No Farm Computer found at cursor position or adjacent tiles. Checked {totalObjectsChecked} total objects across all locations.", LogLevel.Debug);

            // Backup check: look for Farm Computer near player position (5x5 area)
            this.monitor.Log("Starting backup check around player position...", LogLevel.Debug);
            var playerPos2 = Game1.player.Position;
            var playerTile = new Microsoft.Xna.Framework.Vector2((int)(playerPos2.X / Game1.tileSize), (int)(playerPos2.Y / Game1.tileSize));
            int backupChecks = 0;
            const int searchRadius = 2; // 5x5 area (radius 2)

            foreach (var location in Game1.locations)
            {
                foreach (var kvp in location.objects.Pairs)
                {
                    float distanceX = Math.Abs(kvp.Key.X - playerTile.X);
                    float distanceY = Math.Abs(kvp.Key.Y - playerTile.Y);

                    if (distanceX <= searchRadius && distanceY <= searchRadius)
                    {
                        backupChecks++;
                        var obj = kvp.Value;
                        if (obj.ParentSheetIndex == farmComputerId ||
                            obj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                            obj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                            obj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                            obj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase))
                        {
                            this.monitor.Log($"FARM COMPUTER FOUND near player in backup check! {obj.DisplayName} (ID: {obj.ParentSheetIndex}) at ({kvp.Key.X}, {kvp.Key.Y}) in {location.Name}", LogLevel.Info);
                            return true;
                        }
                    }
                }
            }

            this.monitor.Log($"Backup check completed. Checked {backupChecks} objects near player. No Farm Computer found.", LogLevel.Debug);
            return false;
        }
    }
}