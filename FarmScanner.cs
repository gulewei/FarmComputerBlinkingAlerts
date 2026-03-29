using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Scans the farm for things that need the player's attention.</summary>
    public class FarmScanner
    {
        // Helper methods for type identification
        private bool IsCask(StardewValley.Object obj)
        {
            // Cask has ParentSheetIndex 163 in vanilla Stardew Valley
            return obj.ParentSheetIndex == 163 || obj.Name.Contains("Cask");
        }

        private bool IsCrabPot(StardewValley.Object obj)
        {
            // Crab pot has ParentSheetIndex 710
            return obj.ParentSheetIndex == 710 || obj.Name.Contains("Crab Pot");
        }

        private bool IsCaskReadyForHarvest(StardewValley.Object cask)
        {
            if (cask.heldObject.Value == null)
                return false;

            // Quality values: 0 = normal, 1 = silver, 2 = gold, 4 = iridium
            return cask.heldObject.Value.Quality >= 4;
        }
        /// <summary>Scans for all things that need attention.</summary>
        /// <returns>A list of alert messages describing what needs attention.</returns>
        public List<string> ScanAll()
        {
            var alerts = new List<string>();

            alerts.AddRange(this.ScanForReadyCrops());
            alerts.AddRange(this.ScanForReadyMachines());
            // alerts.AddRange(this.ScanForAnimalProducts());
            // alerts.AddRange(this.ScanForReadyFruitTrees());
            // alerts.AddRange(this.ScanForReadyCrabPots());
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

                // Special handling for casks
                if (IsCask(obj))
                {
                    if (IsCaskReadyForHarvest(obj))
                    {
                        alerts.Add($"Ready cask at ({kvp.Key.X}, {kvp.Key.Y})");
                    }
                    continue; // Skip general machine check for casks
                }

                // Original machine checking logic for non-casks
                if (obj.readyForHarvest.Value)
                {
                    alerts.Add($"Ready machine {obj.DisplayName} at ({kvp.Key.X}, {kvp.Key.Y})");
                }
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

        /// <summary>Scans for animal products ready for collection.</summary>
        /// <returns>Alert messages for ready animal products.</returns>
        public List<string> ScanForAnimalProducts()
        {
            var alerts = new List<string>();
            var farm = Game1.getFarm();

            if (farm == null)
                return alerts;

            foreach (var building in farm.buildings)
            {
                if (building.indoors.Value is AnimalHouse animalHouse)
                {
                    foreach (var animal in animalHouse.animals.Values)
                    {
                        // Check if animal has produce ready
                        if (int.TryParse(animal.currentProduce.Value, out int produceId) && produceId > 0)
                        {
                            alerts.Add($"{animal.displayName} has produce ready in {building.buildingType.Value}");
                        }
                    }
                }
            }

            return alerts;
        }

        /// <summary>Scans for fruit trees ready to harvest.</summary>
        /// <returns>Alert messages for ready fruit trees.</returns>
        public List<string> ScanForReadyFruitTrees()
        {
            var alerts = new List<string>();
            var farm = Game1.getFarm();

            if (farm == null)
                return alerts;

            foreach (var kvp in farm.terrainFeatures.Pairs)
            {
                if (kvp.Value is FruitTree fruitTree)
                {
                    // Check if tree is mature (growth stage >= 4) and has fruit
                    if (fruitTree.growthStage.Value >= 4 && fruitTree.fruit.Count > 0)
                    {
                        alerts.Add($"Fruit tree ready at ({kvp.Key.X}, {kvp.Key.Y})");
                    }
                }
            }

            return alerts;
        }

        /// <summary>Scans for crab pots ready to collect.</summary>
        /// <returns>Alert messages for ready crab pots.</returns>
        public List<string> ScanForReadyCrabPots()
        {
            var alerts = new List<string>();
            var farm = Game1.getFarm();

            if (farm == null)
                return alerts;

            foreach (var kvp in farm.objects.Pairs)
            {
                var obj = kvp.Value;

                if (IsCrabPot(obj) && obj.heldObject.Value != null && obj.readyForHarvest.Value)
                {
                    alerts.Add($"Crab pot ready at ({kvp.Key.X}, {kvp.Key.Y})");
                }
            }

            return alerts;
        }
    }
}