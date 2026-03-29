using StardewModdingAPI;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Mod configuration model.</summary>
    public class ModConfig
    {
        /// <summary>Whether to scan for crops ready to harvest.</summary>
        public bool EnableCropScanning { get; set; } = true;

        /// <summary>Whether to scan for machines that have finished processing.</summary>
        public bool EnableMachineScanning { get; set; } = true;

        /// <summary>Whether to scan for empty hay in the silo when animals are present.</summary>
        public bool EnableHayScanning { get; set; } = true;

        /// <summary>The minimum quality level for casks to trigger alerts.</summary>
        /// <remarks>
        /// Quality values:
        /// 0 = normal (no star)
        /// 1 = silver
        /// 2 = gold
        /// 4 = iridium
        /// </remarks>
        public int CaskMinimumQuality { get; set; } = 4; // Default: iridium only

        /// <summary>Gets the default configuration.</summary>
        public static ModConfig GetDefault()
        {
            return new ModConfig();
        }

        /// <summary>Validates the configuration values.</summary>
        public void Validate()
        {
            // Ensure CaskMinimumQuality is a valid quality value
            if (CaskMinimumQuality != 0 && CaskMinimumQuality != 1 && CaskMinimumQuality != 2 && CaskMinimumQuality != 4)
            {
                CaskMinimumQuality = 4; // Default to iridium if invalid
            }
        }
    }
}