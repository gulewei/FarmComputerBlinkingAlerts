using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Scans the farm for things that need the player's attention.</summary>
    public class FarmScanner
    {
        /// <summary>Scans for all things that need attention.</summary>
        /// <returns>A list of alert messages describing what needs attention.</returns>
        public List<string> ScanAll()
        {
            var alerts = new List<string>();

            alerts.AddRange(this.ScanForReadyCrops());
            alerts.AddRange(this.ScanForReadyMachines());
            if (this.ScanForEmptyHay())
                alerts.Add("Hay is empty in the barn/silo");

            return alerts;
        }

        /// <summary>Scans for crops that are ready to harvest.</summary>
        /// <returns>Alert messages for ready crops.</returns>
        public List<string> ScanForReadyCrops()
        {
            var alerts = new List<string>();
            var farm = Game1.getFarm();

            if (farm == null)
                return alerts;

            foreach (var kvp in farm.terrainFeatures.Pairs)
            {
                if (kvp.Value is StardewValley.TerrainFeatures.HoeDirt hoeDirt &&
                    hoeDirt.crop != null &&
                    hoeDirt.crop.currentPhase.Value >= hoeDirt.crop.phaseDays.Count - 1 &&
                    hoeDirt.crop.fullyGrown.Value &&
                    !hoeDirt.crop.dead.Value)
                {
                    // Crop is fully grown and ready to harvest
                    alerts.Add($"Ready crop at ({kvp.Key.X}, {kvp.Key.Y})");
                }
            }

            return alerts;
        }

        /// <summary>Scans for machines that have finished processing.</summary>
        /// <returns>Alert messages for ready machines.</returns>
        public List<string> ScanForReadyMachines()
        {
            var alerts = new List<string>();
            var farm = Game1.getFarm();

            if (farm == null)
                return alerts;

            foreach (var kvp in farm.objects.Pairs)
            {
                var obj = kvp.Value;

                // Check if object is a machine that's ready for harvest
                if (obj.readyForHarvest.Value)
                {
                    alerts.Add($"Ready machine {obj.DisplayName} at ({kvp.Key.X}, {kvp.Key.Y})");
                }
                // Special case for objects with held object that's ready
                else if (obj.heldObject.Value != null && obj.heldObject.Value.readyForHarvest.Value)
                {
                    alerts.Add($"Ready machine {obj.DisplayName} at ({kvp.Key.X}, {kvp.Key.Y})");
                }
            }

            return alerts;
        }

        /// <summary>Scans if hay is empty in the silo.</summary>
        /// <returns>True if hay is empty and animals need feeding.</returns>
        public bool ScanForEmptyHay()
        {
            var farm = Game1.getFarm();
            if (farm == null)
                return false;

            // Check if there are any animals that need feeding
            bool hasAnimals = false;
            foreach (var building in farm.buildings)
            {
                if (building.indoors.Value is AnimalHouse animalHouse && animalHouse.animals.Any())
                {
                    hasAnimals = true;
                    break;
                }
            }

            if (!hasAnimals)
                return false; // No animals, so hay status doesn't matter

            // Check hay count in silo
            int hayCount = farm.piecesOfHay.Value;
            return hayCount <= 0;
        }
    }
}