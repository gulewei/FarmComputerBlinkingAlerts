using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Tracks the Farm Computer objects in the game world.</summary>
    public class FarmComputerTracker
    {
        private readonly IMonitor monitor;
        private StardewValley.Object? lastPlacedComputer;
        private string? lastPlacedLocationName;
        private Microsoft.Xna.Framework.Vector2 lastPlacedTile;

        /// <summary>Initializes a new instance of the <see cref="FarmComputerTracker"/> class.</summary>
        /// <param name="monitor">Monitor for logging.</param>
        public FarmComputerTracker(IMonitor monitor)
        {
            this.monitor = monitor;
        }

        /// <summary>Checks if an object is a Farm Computer.</summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is a Farm Computer.</returns>
        public bool IsFarmComputer(StardewValley.Object obj)
        {
            return obj.ParentSheetIndex == 787 ||
                   obj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Gets the currently tracked Farm Computer.</summary>
        /// <returns>The Farm Computer object, or null if none is tracked.</returns>
        public StardewValley.Object? GetTrackedComputer()
        {
            if (this.lastPlacedComputer != null)
            {
                try
                {
                    // Simple validation: check if object appears to be a Farm Computer
                    if (this.IsFarmComputer(this.lastPlacedComputer))
                    {
#if DEBUG
                        this.monitor.Log($"Using tracked Farm Computer.", LogLevel.Debug);
#endif
                        return this.lastPlacedComputer;
                    }
                    else
                    {
                        // Object is not a Farm Computer anymore
#if DEBUG
                        this.monitor.Log("Tracked computer is no longer a Farm Computer, clearing.", LogLevel.Debug);
#endif
                        this.Clear();
                    }
                }
                catch
                {
                    // Object may have been disposed
#if DEBUG
                    this.monitor.Log("Tracked computer appears disposed, clearing.", LogLevel.Debug);
#endif
                    this.Clear();
                }
            }

            return null;
        }

        /// <summary>Checks if there is at least one Farm Computer being tracked.</summary>
        /// <returns>True if a Farm Computer is being tracked.</returns>
        public bool HasFarmComputer()
        {
            return this.GetTrackedComputer() != null;
        }

        /// <summary>Notifies that a Farm Computer was placed by the player.</summary>
        /// <param name="computer">The Farm Computer object that was placed.</param>
        /// <param name="locationName">The name of the location where it was placed.</param>
        /// <param name="tile">The tile coordinates where it was placed.</param>
        public void NotifyComputerPlaced(StardewValley.Object computer, string locationName, Microsoft.Xna.Framework.Vector2 tile)
        {
            this.lastPlacedComputer = computer;
            this.lastPlacedLocationName = locationName;
            this.lastPlacedTile = tile;

#if DEBUG
            this.monitor.Log($"Tracked Farm Computer placed at {locationName} ({tile.X}, {tile.Y})", LogLevel.Debug);
#endif
        }

        /// <summary>Checks if a removed Farm Computer is the one we're tracking.</summary>
        /// <param name="removedComputer">The Farm Computer object that was removed.</param>
        /// <returns>True if the removed computer was the currently tracked computer.</returns>
        public bool CheckIfComputerRemoved(StardewValley.Object removedComputer)
        {
            if (this.lastPlacedComputer == removedComputer)
            {
#if DEBUG
                this.monitor.Log("Tracked Farm Computer was removed, clearing tracking.", LogLevel.Debug);
#endif
                this.Clear();
                return true;
            }

            return false;
        }

        /// <summary>Clears the tracked Farm Computer reference.</summary>
        public void Clear()
        {
            this.lastPlacedComputer = null;
            this.lastPlacedLocationName = null;
            this.lastPlacedTile = Microsoft.Xna.Framework.Vector2.Zero;
#if DEBUG
            this.monitor.Log("Cleared tracked Farm Computer.", LogLevel.Debug);
#endif
        }

        /// <summary>Searches all game locations for any existing Farm Computer.</summary>
        /// <returns>True if a Farm Computer was found and tracked.</returns>
        public bool FindAnyFarmComputer()
        {
#if DEBUG
            this.monitor.Log("Searching for existing Farm Computers in all locations...", LogLevel.Debug);
#endif

            foreach (var location in Game1.locations)
            {
                foreach (var kvp in location.objects.Pairs)
                {
                    if (this.IsFarmComputer(kvp.Value))
                    {
                        this.lastPlacedComputer = kvp.Value;
                        this.lastPlacedLocationName = location.Name;
                        this.lastPlacedTile = kvp.Key;

#if DEBUG
                        this.monitor.Log($"Found existing Farm Computer at {location.Name} ({kvp.Key.X}, {kvp.Key.Y})", LogLevel.Debug);
#endif
                        return true;
                    }
                }
            }

#if DEBUG
            this.monitor.Log("No existing Farm Computer found in any location.", LogLevel.Debug);
#endif
            return false;
        }
    }
}