using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Linq;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig? config;
        private FarmScanner? scanner;
        private PulsingAuraEffect? effect;
        private InteractionHandler? interaction;
        private bool needsAttention;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Load configuration
            config = helper.ReadConfig<ModConfig>();
            config.Validate();

            scanner = new FarmScanner(config);
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
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            this.Monitor.Log("Farm Computer Blinking Alerts mod loaded.", LogLevel.Info);
            this.Monitor.Log($"Configuration loaded: CropScanning={config.EnableCropScanning}, MachineScanning={config.EnableMachineScanning}, HayScanning={config.EnableHayScanning}, CaskMinimumQuality={config.CaskMinimumQuality}", LogLevel.Debug);
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

        /// <summary>Raised after the game is launched.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            this.RegisterConfigMenu();
        }

        /// <summary>Registers the configuration menu with Generic Mod Config Menu if available.</summary>
        private void RegisterConfigMenu()
        {
            try
            {
                var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
                if (api == null)
                {
                    this.Monitor.Log("Generic Mod Config Menu not available, skipping config menu registration.", LogLevel.Debug);
                    return;
                }

                // Register mod configuration
                api.Register(
                    mod: this.ModManifest,
                    reset: () => this.config = ModConfig.GetDefault(),
                    save: () => this.Helper.WriteConfig(this.config!)
                );

                // Add configuration options
                api.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Scanning Options",
                    tooltip: () => "Configure which farm elements should be scanned for alerts."
                );

                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Scan for crops",
                    tooltip: () => "Enable scanning for crops ready to harvest.",
                    getValue: () => this.config!.EnableCropScanning,
                    setValue: value => this.config!.EnableCropScanning = value
                );

                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Scan for machines",
                    tooltip: () => "Enable scanning for machines that have finished processing.",
                    getValue: () => this.config!.EnableMachineScanning,
                    setValue: value => this.config!.EnableMachineScanning = value
                );

                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Scan for hay",
                    tooltip: () => "Enable scanning for empty hay in the silo when animals are present.",
                    getValue: () => this.config!.EnableHayScanning,
                    setValue: value => this.config!.EnableHayScanning = value
                );

                api.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Cask Settings",
                    tooltip: () => "Configure cask-specific alert settings."
                );

                api.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "Minimum quality",
                    tooltip: () => "Minimum quality level for casks to trigger alerts.\n0 = normal, 1 = silver, 2 = gold, 4 = iridium.",
                    getValue: () => this.config!.CaskMinimumQuality.ToString(),
                    setValue: value =>
                    {
                        if (int.TryParse(value, out int quality) && (quality == 0 || quality == 1 || quality == 2 || quality == 4))
                        {
                            this.config!.CaskMinimumQuality = quality;
                        }
                        else
                        {
                            this.Monitor.Log($"Invalid quality value: {value}. Must be 0, 1, 2, or 4.", LogLevel.Warn);
                        }
                    },
                    allowedValues: new[] { "0", "1", "2", "4" }
                );

                this.Monitor.Log("Registered configuration menu with Generic Mod Config Menu.", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error registering config menu: {ex}", LogLevel.Error);
            }
        }
    }
}