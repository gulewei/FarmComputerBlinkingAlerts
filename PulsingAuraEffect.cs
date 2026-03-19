using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;

namespace FarmComputerBlinkingAlerts
{
    /// <summary>Manages the pulsing aura effect around the Farm Computer.</summary>
    public class PulsingAuraEffect
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private bool isPulsing;
        private float pulseTimer;
        private Texture2D? auraTexture;

        /// <summary>The color of the aura.</summary>
        public Color AuraColor { get; set; } = new Color(255, 80, 80, 120); // Red color matching antenna

        /// <summary>The base size of the aura.</summary>
        public float BaseSize { get; set; } = 32f; // Smaller size for antenna effect

        /// <summary>The pulse speed multiplier.</summary>
        public float PulseSpeed { get; set; } = 2f;

        /// <summary>The pulse size multiplier.</summary>
        public float PulseSizeMultiplier { get; set; } = 0.3f;

        /// <summary>Initializes a new instance of the <see cref="PulsingAuraEffect"/> class.</summary>
        /// <param name="helper">Mod helper for accessing APIs.</param>
        /// <param name="monitor">Monitor for logging.</param>
        public PulsingAuraEffect(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.isPulsing = false;
            this.pulseTimer = 0f;
        }

        /// <summary>Starts the pulsing effect.</summary>
        public void StartPulsing()
        {
            if (!this.isPulsing)
            {
                this.isPulsing = true;
                this.pulseTimer = 0f;
                this.monitor.Log("Started pulsing aura effect.", LogLevel.Debug);
            }
        }

        /// <summary>Stops the pulsing effect.</summary>
        public void StopPulsing()
        {
            if (this.isPulsing)
            {
                this.isPulsing = false;
                this.monitor.Log("Stopped pulsing aura effect.", LogLevel.Debug);
            }
        }

        /// <summary>Updates the pulsing animation.</summary>
        public void Update()
        {
            if (!this.isPulsing)
                return;

            // Update pulse timer
            this.pulseTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds * this.PulseSpeed;
            if (this.pulseTimer > Math.PI * 2)
                this.pulseTimer -= (float)(Math.PI * 2);
        }

        /// <summary>Draws the aura effect around the Farm Computer.</summary>
        /// <param name="spriteBatch">The sprite batch to draw with.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!this.isPulsing)
            {
                this.monitor.Log("Draw called but not pulsing.", LogLevel.Trace);
                return;
            }

            this.monitor.Log("Drawing pulsing aura.", LogLevel.Trace);

            // Find the Farm Computer object
            var farmComputer = this.FindFarmComputer();
            if (farmComputer == null)
            {
                this.monitor.Log("Farm Computer not found for aura drawing.", LogLevel.Debug);
                return;
            }

            // Get screen position
            Vector2 screenPos = this.GetScreenPosition(farmComputer);
            if (screenPos == Vector2.Zero)
                return;

            // Create texture if needed
            if (this.auraTexture == null || this.auraTexture.IsDisposed)
                this.CreateAuraTexture(spriteBatch.GraphicsDevice);

            // Calculate pulse values
            float pulseFactor = (float)Math.Sin(this.pulseTimer);
            float currentSize = this.BaseSize * (1f + pulseFactor * this.PulseSizeMultiplier);
            float alpha = 0.5f + pulseFactor * 0.3f;

            // Draw aura
            Rectangle destination = new Rectangle(
                (int)(screenPos.X - currentSize / 2),
                (int)(screenPos.Y - currentSize / 2),
                (int)currentSize,
                (int)currentSize
            );

            Color drawColor = this.AuraColor * alpha;
            spriteBatch.Draw(this.auraTexture, destination, drawColor);
        }

        /// <summary>Finds the Farm Computer object in the game world.</summary>
        /// <returns>The Farm Computer object, or null if not found.</returns>
        private StardewValley.Object? FindFarmComputer()
        {
            var farm = Game1.getFarm();
            if (farm == null)
            {
                this.monitor.Log("Farm is null, cannot find Farm Computer.", LogLevel.Debug);
                return null;
            }

            // Farm Computer has ParentSheetIndex = 787 (in vanilla Stardew Valley)
            const int farmComputerId = 787;
            int totalObjectsChecked = 0;
            var foundObjects = new List<string>();

            // Check objects on farm
            foreach (var kvp in farm.objects.Pairs)
            {
                totalObjectsChecked++;
                foundObjects.Add($"{kvp.Value.DisplayName} (ID: {kvp.Value.ParentSheetIndex}) at ({kvp.Key.X}, {kvp.Key.Y})");

                if (kvp.Value.ParentSheetIndex == farmComputerId)
                {
                    this.monitor.Log($"Found Farm Computer on farm at ({kvp.Key.X}, {kvp.Key.Y})", LogLevel.Debug);
                    return kvp.Value;
                }
            }

            // Also check in buildings (like sheds) and other locations
            foreach (var location in Game1.locations)
            {
                this.monitor.Log($"Checking location: {location.Name} (has {location.objects.Count()} objects)", LogLevel.Trace);

                foreach (var kvp in location.objects.Pairs)
                {
                    totalObjectsChecked++;
                    foundObjects.Add($"{kvp.Value.DisplayName} (ID: {kvp.Value.ParentSheetIndex}) in {location.Name} at ({kvp.Key.X}, {kvp.Key.Y})");

                    if (kvp.Value.ParentSheetIndex == farmComputerId)
                    {
                        this.monitor.Log($"Found Farm Computer in {location.Name} at ({kvp.Key.X}, {kvp.Key.Y})", LogLevel.Debug);
                        return kvp.Value;
                    }

                    // Also check by name in case ID is different
                    if (kvp.Value.DisplayName.Contains("Farm Computer", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Value.DisplayName.Contains("农场电脑", StringComparison.OrdinalIgnoreCase))
                    {
                        this.monitor.Log($"Found object with Farm Computer in name: {kvp.Value.DisplayName} (ID: {kvp.Value.ParentSheetIndex}) in {location.Name}", LogLevel.Debug);
                        return kvp.Value;
                    }
                }
            }

            this.monitor.Log($"Farm Computer not found. Checked {totalObjectsChecked} objects total.", LogLevel.Debug);
            if (foundObjects.Count > 0)
            {
                this.monitor.Log($"Sample of objects found (first 10): {string.Join(", ", foundObjects.Take(10))}", LogLevel.Debug);
            }
            else
            {
                this.monitor.Log("No objects found in any location.", LogLevel.Debug);
            }

            return null;
        }

        /// <summary>Gets the screen position of an object.</summary>
        /// <param name="obj">The object to get the position for.</param>
        /// <returns>The screen position, or Vector2.Zero if not on screen.</returns>
        private Vector2 GetScreenPosition(StardewValley.Object? obj)
        {
            if (obj == null)
                return Vector2.Zero;

            // Get tile position
            Vector2 tilePosition = obj.TileLocation * Game1.tileSize;

            // Convert to screen position
            Vector2 screenPosition = new Vector2(
                tilePosition.X - Game1.viewport.X,
                tilePosition.Y - Game1.viewport.Y
            );

            // Adjust for object height (Farm Computer is placed on ground)
            // Use the object's bounding box to get proper height
            var boundingBox = obj.boundingBox.Value;
            screenPosition.Y -= boundingBox.Height / 2;

            // Adjust position to be above and to the right of the object (antenna location)
            // The antenna is typically at the top-right of the Farm Computer
            screenPosition.X += Game1.tileSize * 0.65f; // Shift right
            screenPosition.Y -= Game1.tileSize * 0.35f; // Shift up further

            // Check if on screen
            if (screenPosition.X < -Game1.tileSize || screenPosition.X > Game1.viewport.Width + Game1.tileSize ||
                screenPosition.Y < -Game1.tileSize || screenPosition.Y > Game1.viewport.Height + Game1.tileSize)
            {
                return Vector2.Zero; // Not visible on screen
            }

            return screenPosition;
        }

        /// <summary>Creates a circular texture for the aura.</summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        private void CreateAuraTexture(GraphicsDevice graphicsDevice)
        {
            int size = 128;
            var texture = new Texture2D(graphicsDevice, size, size);
            var colorData = new Color[size * size];

            float radius = size / 2f;
            float radiusSquared = radius * radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        // Alpha fades out near edges
                        float alpha = 1f - (distanceSquared / radiusSquared);
                        colorData[y * size + x] = Color.White * alpha;
                    }
                    else
                    {
                        colorData[y * size + x] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            this.auraTexture = texture;
        }
    }
}