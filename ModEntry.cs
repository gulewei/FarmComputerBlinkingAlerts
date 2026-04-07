using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Linq;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private FarmScanner? scanner;
        private PulsingAuraEffect? effect;
        private FarmComputerTracker? tracker;
        private InteractionHandler? interaction;
        private bool needsAttention;

        private const int FarmComputerId = 787;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            scanner = new FarmScanner();
            tracker = new FarmComputerTracker(this.Monitor);
            effect = new PulsingAuraEffect(this.Monitor);
            interaction = new InteractionHandler(effect, this.Monitor, () =>
            {
#if DEBUG
                this.Monitor.Log($"Interaction callback called. Current needsAttention={needsAttention}", LogLevel.Debug);
#endif
                needsAttention = false;
#if DEBUG
                this.Monitor.Log($"Interaction callback: needsAttention set to {needsAttention}.", LogLevel.Debug);
#endif
            });

            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.interaction.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.World.ObjectListChanged += this.OnObjectListChanged;

#if DEBUG
            this.Monitor.Log("Farm Computer Blinking Alerts mod loaded.", LogLevel.Debug);
#endif
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Clear texture cache in case Farm Computer was moved
            effect!.ClearTextureCache();

            // First check if there is a Farm Computer in the game world
            if (!tracker!.HasFarmComputer())
            {
#if DEBUG
                this.Monitor.Log("No Farm Computer found, skipping daily scan.", LogLevel.Debug);
#endif
                needsAttention = false;
                effect!.StopPulsing();
                return;
            }

            // Scan the farm for things that need attention
            var alerts = scanner!.ScanAll();
            bool newNeedsAttention = alerts.Any();

#if DEBUG
            this.Monitor.Log($"OnDayStarted: alerts count={alerts.Count}, newNeedsAttention={newNeedsAttention}, current needsAttention={needsAttention}", LogLevel.Debug);
#endif

            if (newNeedsAttention != needsAttention)
            {
                needsAttention = newNeedsAttention;
#if DEBUG
                this.Monitor.Log($"needsAttention changed to {needsAttention}", LogLevel.Debug);
#endif
            }

            if (needsAttention)
            {
#if DEBUG
                this.Monitor.Log($"Found {alerts.Count} things needing attention: {string.Join(", ", alerts)}", LogLevel.Debug);
#endif
                effect!.StartPulsing();
            }
            else
            {
#if DEBUG
                this.Monitor.Log("No things needing attention found.", LogLevel.Debug);
#endif
                effect!.StopPulsing();
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (needsAttention)
            {
#if DEBUG
                this.Monitor.Log($"OnUpdateTicked: needsAttention={needsAttention}, updating effect", LogLevel.Debug);
#endif
                effect!.Update();
            }
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (needsAttention)
            {
#if DEBUG
                this.Monitor.Log($"OnRendered: needsAttention={needsAttention}, drawing effect", LogLevel.Debug);
#endif
                var computer = tracker!.GetTrackedComputer();
#if DEBUG
                this.Monitor.Log($"OnRendered: tracked computer={(computer != null ? "found" : "null")}", LogLevel.Debug);
#endif
                effect!.Draw(e.SpriteBatch, computer);
            }
        }

        /// <summary>Raised after the player loads a save slot and the world is initialized.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Reset state when loading a save
            needsAttention = false;
            effect!.StopPulsing();
            effect!.ClearTextureCache();
            tracker!.Clear();

            // Search for existing Farm Computers in the save
            bool found = tracker!.FindAnyFarmComputer();
#if DEBUG
            this.Monitor.Log(found ? "Found existing Farm Computer in save." : "No existing Farm Computer found in save.", LogLevel.Debug);
#endif
        }

        /// <summary>Raised after objects are added or removed in a location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
        {
#if DEBUG
            this.Monitor.Log($"OnObjectListChanged: location={e.Location?.Name ?? "null"}, added={e.Added.Count()}, removed={e.Removed.Count()}, current needsAttention={needsAttention}", LogLevel.Debug);
#endif

            // Check if any farm computers were added or removed
            bool computerAdded = false;

            foreach (var added in e.Added)
            {
                if (IsFarmComputer(added.Value))
                {
                    computerAdded = true;
#if DEBUG
                    this.Monitor.Log($"Farm Computer added at {e.Location?.Name ?? "null"} ({added.Key.X}, {added.Key.Y})", LogLevel.Debug);
#endif
                    // Notify tracker about the newly placed computer (player's last placed)
                    tracker!.NotifyComputerPlaced(added.Value, e.Location?.Name ?? "null", added.Key);
                }
            }

            foreach (var removed in e.Removed)
            {
                if (IsFarmComputer(removed.Value))
                {
#if DEBUG
                    this.Monitor.Log($"Farm Computer removed from {e.Location?.Name ?? "null"} ({removed.Key.X}, {removed.Key.Y})", LogLevel.Debug);
#endif
                    // Check if this is the currently tracked computer
                    bool wasTracked = tracker!.CheckIfComputerRemoved(removed.Value);

                    // If the tracked computer was removed, stop pulsing and clear needsAttention
                    if (wasTracked && needsAttention)
                    {
                        needsAttention = false;
                        effect!.StopPulsing();
#if DEBUG
                        this.Monitor.Log("Tracked Farm Computer removed, stopping aura.", LogLevel.Debug);
#endif
                    }
                }
            }

            // If a computer was added, trigger a scan to update alerts
            if (computerAdded)
            {
                var alerts = scanner!.ScanAll();
                bool newNeedsAttention = alerts.Any();

                if (newNeedsAttention != needsAttention)
                {
                    needsAttention = newNeedsAttention;
                    if (needsAttention)
                    {
#if DEBUG
                        this.Monitor.Log($"Computer placed - Found {alerts.Count} things needing attention.", LogLevel.Debug);
#endif
                        effect!.StartPulsing();
                    }
                    else
                    {
#if DEBUG
                        this.Monitor.Log("Computer placed - No things needing attention found.", LogLevel.Debug);
#endif
                        effect!.StopPulsing();
                    }
                }
            }
        }

        /// <summary>Checks if an object is a Farm Computer.</summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object is a Farm Computer.</returns>
        private bool IsFarmComputer(StardewValley.Object obj)
        {
            return obj.ParentSheetIndex == FarmComputerId ||
                   obj.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("Computer", StringComparison.OrdinalIgnoreCase) ||
                   obj.DisplayName.Contains("电脑", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            // Clean up when returning to title
            needsAttention = false;
            effect!.StopPulsing();
            effect!.ClearTextureCache();
            tracker!.Clear();
        }

    }
}