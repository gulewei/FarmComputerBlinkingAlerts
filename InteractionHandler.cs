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
                // Only process action button (usually right click)
                if (!e.Button.IsActionButton())
                    return;

                // Avoid processing too quickly (debounce)
                if ((DateTime.Now - this.lastInteractionTime).TotalSeconds < 0.5)
                    return;

                // Check if player is interacting with a Farm Computer
                if (this.IsInteractingWithFarmComputer(e))
                {
#if DEBUG
                    this.monitor.Log("Player interacted with Farm Computer, stopping aura.", LogLevel.Debug);
#endif
                    this.effect.StopPulsing();
                    this.onInteractionCallback?.Invoke();
                    this.lastInteractionTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                this.monitor.Log($"Error in interaction handler: {ex}", LogLevel.Debug);
#endif
            }
        }

        /// <summary>Checks if an object is a Farm Computer.</summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is a Farm Computer.</returns>
        private bool IsFarmComputer(StardewValley.Object obj)
        {
            return obj.ParentSheetIndex == 787 ||
                   obj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Checks if the player is interacting with a Farm Computer.</summary>
        /// <param name="e">The button pressed event data.</param>
        /// <returns>True if interacting with a Farm Computer.</returns>
        private bool IsInteractingWithFarmComputer(ButtonPressedEventArgs e)
        {
            var cursorTile = e.Cursor.Tile;
#if DEBUG
            this.monitor.Log($"IsInteractingWithFarmComputer: cursorTile={cursorTile}, location={Game1.currentLocation?.Name ?? "null"}", LogLevel.Debug);
#endif

            // Check current location for object at cursor tile
            var location = Game1.currentLocation;
            if (location == null)
                return false;

            if (location.objects.TryGetValue(cursorTile, out var obj))
            {
                bool isFarmComputer = this.IsFarmComputer(obj);
#if DEBUG
                this.monitor.Log($"IsInteractingWithFarmComputer: object found, name={obj.DisplayName}, ParentSheetIndex={obj.ParentSheetIndex}, isFarmComputer={isFarmComputer}", LogLevel.Debug);
#endif
                return isFarmComputer;
            }
            else
            {
#if DEBUG
                this.monitor.Log($"IsInteractingWithFarmComputer: no object at cursor tile", LogLevel.Debug);
#endif
            }

            return false;
        }
    }
}