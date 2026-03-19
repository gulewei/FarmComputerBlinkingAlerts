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
        private InteractionHandler? interaction;
        private bool needsAttention;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            scanner = new FarmScanner();
            effect = new PulsingAuraEffect(helper, this.Monitor);
            interaction = new InteractionHandler(effect, this.Monitor, () =>
            {
                needsAttention = false;
                this.Monitor.Log("Interaction callback: needsAttention set to false.", LogLevel.Debug);
            });

            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.interaction.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

            this.Monitor.Log("Farm Computer Blinking Alerts mod loaded.", LogLevel.Info);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Scan the farm for things that need attention
            var alerts = scanner!.ScanAll();
            needsAttention = alerts.Any();

            if (needsAttention)
            {
                this.Monitor.Log($"Found {alerts.Count} things needing attention: {string.Join(", ", alerts)}", LogLevel.Debug);
                effect!.StartPulsing();
            }
            else
            {
                this.Monitor.Log("No things needing attention found.", LogLevel.Debug);
                effect!.StopPulsing();
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (needsAttention)
                effect!.Update();
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (needsAttention)
                effect!.Draw(e.SpriteBatch);
        }

        /// <summary>Raised after the player loads a save slot and the world is initialized.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Reset state when loading a save
            needsAttention = false;
            effect!.StopPulsing();
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            // Clean up when returning to title
            needsAttention = false;
            effect!.StopPulsing();
        }
    }
}