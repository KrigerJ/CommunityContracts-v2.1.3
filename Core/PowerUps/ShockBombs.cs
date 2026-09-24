using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using static ModEntry;
using SObject = StardewValley.Object;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace CommunityContracts.Core.PowerUps
{
    internal class ShockBombs
    {
        public static ModConfig config;
        private static IMonitor monitor;
        private static IModHelper helper;
        internal static bool ShockBombTriggered = false;
        internal static SObject? CapturedBomb = null;
        internal static IModHelper Helper;
        public static void Initialize(IModHelper h, IMonitor m, ModConfig cfg)
        {
            helper = h;
            monitor = m;
            config = cfg;
        }
        public struct BombStats
        {
            public int Radius;
            public int Damage;

            public BombStats(int radius, int damage)
            {
                Radius = radius;
                Damage = damage;
            }
        }
        internal static void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Config.BombActive)
                return;

            if (Game1.activeClickableMenu != null)
                return;

            var bounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
            if (!bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                return;

            if (Game1.onScreenMenus.Any(m => m.isWithinBounds(Game1.getMouseX(), Game1.getMouseY())))
                return;

            if (Game1.activeClickableMenu != null || Game1.eventUp)
                return;

            XnaRectangle screenBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
            if (!screenBounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                return;

            if (Game1.onScreenMenus.Any(m => m.isWithinBounds(Game1.getMouseX(), Game1.getMouseY())))
                return;

            if (!e.Button.IsUseToolButton())
                return;

            Farmer farmer = Game1.player;

            if (farmer.CurrentTool is not MeleeWeapon)
                return;

            SObject fuel = GetFuelFromLocker();

            if (fuel == null)
                return;

            Vector2 tile = e.Cursor.Tile;
            TriggerShockBomb(farmer, fuel, tile);

            fuel.Stack--;
            if (fuel.Stack <= 0)
                farmer.removeItemFromInventory(fuel);
        }
        public static void TriggerShockBomb(Farmer who, SObject fuel, Vector2 clickTile)
        {
            int kentFriendship = who.getFriendshipLevelForNPC("Kent");
            int scale = kentFriendship / 250 + 1;

            int itemValue = fuel.sellToStorePrice();

            int damage = (itemValue / 2) * scale;
            int radius = 1 + (itemValue / 100) * scale;

            int mapWidth = Game1.currentLocation.Map.Layers[0].LayerWidth;
            int mapHeight = Game1.currentLocation.Map.Layers[0].LayerHeight;

            int maxSafeRadius = Math.Min(mapWidth, mapHeight) / 2;
            maxSafeRadius = Math.Min(maxSafeRadius, 20);

            radius = Math.Min(radius, maxSafeRadius);

            Game1.currentLocation.explode(
                clickTile,
                radius,
                who,
                false,
                damage
            );

            float volumeFactor = Math.Min(1f, damage / 100f);

            float normalized = Math.Min(1f, damage / 300f);

            float pitch = MathHelper.Lerp(-0.40f, 0.20f, normalized);

            var cue = Game1.soundBank.GetCue("explosion");

            cue.SetVariable("Pitch", pitch * 100f);
            cue.SetVariable("Volume", volumeFactor * 100f);

            cue.Play();
        }
        private static SObject GetFuelFromLocker()
        {
            if (!Config.FuelLockerAssigned)
                return null;

            Chest locker = GetLockerChest();
            if (locker == null)
                return null;

            return locker.Items
                .OfType<SObject>()
                .FirstOrDefault(o => o.Stack > 0);
        }
        private static Chest GetLockerChest()
        {
            if (!Config.FuelLockerAssigned)
            {
                Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
                return null;
            }

            GameLocation loc = Game1.getLocationFromName(Config.FuelLockerLocation);
            if (loc == null)
            {
                Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
                return null;
            }

            Vector2 tile = ParseTile(Config.FuelLockerTile);
            if (tile == Vector2.Zero)
            {
                Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
                return null;
            }

            if (loc.objects.TryGetValue(tile, out StardewValley.Object obj) && obj is Chest chest)
                return chest;

            Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
            return null;
        }
        private static Vector2 ParseTile(string tileString)
        {
            if (string.IsNullOrWhiteSpace(tileString))
                return Vector2.Zero;

            string[] parts = tileString.Split(',');
            if (parts.Length != 2)
                return Vector2.Zero;

            if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                return new Vector2(x, y);

            return Vector2.Zero;
        }
    }
}
