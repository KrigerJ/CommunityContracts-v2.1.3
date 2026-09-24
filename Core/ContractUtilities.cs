using CommunityContracts.Core.NPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using xTile.Dimensions;
using static CommunityContracts.Core.NPCServiceMenu;
using static CommunityContracts.Core.CollectionHelpers;
using static ModEntry;
using SObject = StardewValley.Object;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace CommunityContracts.Core
{
    public static class ContractUtilities
    {
        public class ContractsDelivery
        {
            public List<Item> Items { get; set; } = new();
            public long RecipientID { get; set; }
        }
        public static int SafeMultiplier(int value)
        {
            return Math.Max(1, value);
        }
        public static int GetContractPercent(string ProductType)
        {
            return Config.NPCContractPercents.TryGetValue(ProductType, out var value)
                ? value
                : Config.NPCContractPercents["Basic"];
        }
        public static string GetItemTypeLabel(string ItemType)
        {
            return ItemTypeLabels.TryGetValue(ItemType, out var value)
                ? value
                : ItemTypeLabels["Weeds"];
        }
        public static int GetQuality(int NPCLevel)
        {
            return NPCLevel switch
            {
                >= 10 => 4,
                >= 7 => 2,
                >= 3 => 1,
                _ => 0
            };
        }
        public static float GetQualityMultiplier(int ProductQuality)
        {
            return ProductQuality switch
            {
                0 => 1.0f,
                1 => 1.25f,
                2 => 1.5f,
                4 => 2.0f,
                _ => 1.0f
            };
        }

        public static string GetQualityName(int Quality)
        {
            return Quality switch
            {
                >= 4 => T("QualityIridium"),
                >= 2 => T("QualityGold"),
                >= 1 => T("QualitySilver"),
                _ => T("QualityNormal")
            };
        }
        public static int GetSeasonIndex(string Season)
        {
            return Season switch
            {
                "spring" => 0,
                "summer" => 1,
                "fall" => 2,
                "winter" => 3,
                _ => 0
            };
        }

        public static int CountNamedProcessors(string processorName)
        {
            var allLocations = GetAllLocationsRecursive();

            return allLocations
                .SelectMany(loc => loc.objects.Values)
                .Count(obj => obj != null && obj.Name == processorName);
        }
        
        public static int CountProcessors(int processorIndex)
        {
            var allLocations = GetAllLocationsRecursive();

            return allLocations
                .SelectMany(loc => loc.objects.Values)
                .Count(obj => obj != null
                    && obj.bigCraftable.Value
                    && obj.ParentSheetIndex == processorIndex);
        }

        public static List<GameLocation> GetAllLocationsRecursive()
        {
            var results = new List<GameLocation>();
            var visited = new HashSet<string>();

            void Scan(GameLocation loc)
            {
                if (loc == null || visited.Contains(loc.Name))
                    return;

                visited.Add(loc.Name);
                results.Add(loc);

                if (loc is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building.indoors?.Value != null)
                            Scan(building.indoors.Value);
                    }
                }

                foreach (var warp in loc.warps)
                {
                    var target = Game1.getLocationFromName(warp.TargetName);
                    if (target != null)
                        Scan(target);
                }

                if (loc is FarmHouse house)
                {
                    var cellar = house.GetCellar();
                    if (cellar != null)
                        Scan(cellar);
                }
            }

            foreach (var loc in Game1.locations)
                Scan(loc);

            return results;
        }


        public static string GetItemName(string id)
        {
            return ItemRegistry.GetData(id)?.DisplayName
                ?? T("UnknownItem", new { id });
        }
        public static void DeliverContractsItems(List<ContractsDelivery> deliveries, ModConfig config)
        {
            if (deliveries.Count == 0)
                return;

            foreach (var delivery in deliveries)
            {
                var farmer = Game1.getAllFarmers().FirstOrDefault(f => f.UniqueMultiplayerID == delivery.RecipientID);
                if (farmer == null)
                    continue;

                GameLocation location = Game1.getLocationFromName(config.DropLocationName);

                if (location == null)
                {
                    location = Game1.getLocationFromName("Farm");
                }

                Vector2 dropTile = new Vector2(config.DropTileX, config.DropTileY);

                if (location is Farm farmLocation &&
                    !farmLocation.isTileLocationOpen(new Location((int)dropTile.X, (int)dropTile.Y)))
                {
                    Instance.Monitor.Log(T("DropTileBlocked", new { tile = dropTile }), LogLevel.Trace);
                    dropTile = new Vector2(59, 15);
                }

                if (!location.objects.TryGetValue(dropTile, out var obj) || obj is not Chest chest)
                {
                    chest = new Chest(true)
                    {
                        TileLocation = dropTile,
                        Location = location
                    };

                    chest.modData["CommunityContracts/DeliveryChestId"] = $"{location.Name}_{dropTile.X}_{dropTile.Y}";

                    location.objects[dropTile] = chest;

                    chest.name = T("ContractDeliveryChest");

                    if (config.ChestColors.TryGetValue(config.DeliveryChestColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                        chest.modData["CommunityContracts/DeliveryColor"] = config.DeliveryChestColor;
                    }
                }
                else if (obj is Chest existingChest)
                {
                    chest = existingChest;

                    if (chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }

                var chestItems = chest.GetItemsForPlayer(farmer.UniqueMultiplayerID);
                foreach (var item in delivery.Items)
                    chestItems.Add(item);

                if (farmer.IsLocalPlayer)
                {
                    string summary = string.Join(", ", delivery.Items.Select(i => $"{i.Stack} {i.DisplayName}"));
                    Game1.showGlobalMessage( T("ShipmentDelivered", new { summary }));
                }
            }
            deliveries.Clear();
        }
        public static void ShipContractItems(List<ContractsDelivery> deliveries, ModConfig config)
        {
            if (deliveries.Count == 0)
                return;

            foreach (var delivery in deliveries)
            {
                var farmer = Game1.getAllFarmers()
                    .FirstOrDefault(f => f.UniqueMultiplayerID == delivery.RecipientID);

                if (farmer == null)
                    continue;

                var shippingBin = Game1.getFarm().getShippingBin(farmer);

                foreach (var item in delivery.Items)
                {
                    shippingBin.Add(item);
                }

                if (farmer.IsLocalPlayer)
                {
                    string summary = string.Join(", ", delivery.Items.Select(i => $"{i.Stack} {i.DisplayName}"));
                    Game1.showGlobalMessage(T("ShipmentShipped", new { summary }));
                }
            }

            deliveries.Clear();
        }

        public static int UpdateNPCLevel(string NPCName)
        {
            return (int)((Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0) / 224);
        }

        public static void DrawDeliveryLocationHighlight(
            SpriteBatch spriteBatch,
            string dropLocationName,
            ModConfig config,
            Func<string, string> T
            )
        {
            if (Game1.currentLocation?.Name != dropLocationName)
                return;

            Vector2 DropTile = new Vector2(config.DropTileX, config.DropTileY);
            Vector2 ProcessTile = ParseTile(Config.ProcessingLockerTile);
            Vector2 FuelTile = ParseTile(Config.FuelLockerTile);

            XnaRectangle DropRect = new XnaRectangle(
                (int)(DropTile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(DropTile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );


            XnaRectangle ProcessRect = new XnaRectangle(
                (int)(ProcessTile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(ProcessTile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            XnaRectangle FuelRect = new XnaRectangle(
                (int)(FuelTile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(FuelTile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            Color highlightColor = config.HighlightColors.TryGetValue(
                config.HighlightColor,
                out var c
            ) ? c : Color.Yellow * 0.75f;

            spriteBatch.Draw(Game1.staminaRect, DropRect, highlightColor);
            spriteBatch.Draw(Game1.staminaRect, ProcessRect, highlightColor);
            spriteBatch.Draw(Game1.staminaRect, FuelRect, highlightColor);

            SpriteFont font = Game1.smallFont;
            string[] DropLines = { T("DeliveryLabel"), T("LocationLabel") };
            string[] ProcessLines = { T("ProcessLabel"), T("ChestLabel") };
            string[] FuelLines = { T("FuelLabel"), T("ChestLabel") };

            Color fontColor = config.FontColors.TryGetValue(
                config.FontColor,
                out var colr
            ) ? colr : Color.Black;

            for (int i = 0; i < DropLines.Length; i++)
            {
                Vector2 lineSize = font.MeasureString(DropLines[i]);
                Vector2 linePos = new Vector2(
                    DropRect.X,
                    DropRect.Y - lineSize.Y * (DropLines.Length - i) - 8
                );

                spriteBatch.DrawString(font, DropLines[i], linePos, fontColor);
            }

            for (int i = 0; i < ProcessLines.Length; i++)
            {
                Vector2 lineSize = font.MeasureString(ProcessLines[i]);
                Vector2 linePos = new Vector2(
                    ProcessRect.X,
                    ProcessRect.Y - lineSize.Y * (ProcessLines.Length - i) - 8
                );

                spriteBatch.DrawString(font, ProcessLines[i], linePos, fontColor);
            }

            for (int i = 0; i < FuelLines.Length; i++)
            {
                Vector2 lineSize = font.MeasureString(FuelLines[i]);
                Vector2 linePos = new Vector2(
                    FuelRect.X,
                    FuelRect.Y - lineSize.Y * (FuelLines.Length - i) - 8
                );

                spriteBatch.DrawString(font, FuelLines[i], linePos, fontColor);
            }
        }
        public static bool IsSafeForPlacement(GameLocation loc)
        {
            if (Game1.activeClickableMenu != null)
                return false;

            return !IsResettingLocation(loc);
        }

        public static bool IsResettingLocation(GameLocation loc)
        {
            if (loc == null)
                return false;

            if (loc is MineShaft)
                return true;

            if (loc is StardewValley.Locations.VolcanoDungeon)
                return true;

            if (loc.IsTemporary)
                return true;

            if (loc.IsOutdoors && loc.NameOrUniqueName.Contains("Mine"))
                return true;

            return false;
        }

        public static Dictionary<string, string> OreToBarMap = new()
        {
            { "378", "334" },
            { "380", "335" },
            { "384", "336" },
            { "386", "337" },
            { "909", "910" },
        };
        public static SObject RecycleTrash(SObject trash)
        {
            return trash.ParentSheetIndex switch
            {
                168 => new SObject("380", 1), // Iron Ore
                169 => new SObject("388", 3), // Wood 3
                170 => new SObject("338", 1), // Refined Quartz
                171 => new SObject("338", 1), // Refined Quartz
                172 => new SObject("382", 1), // Coal
                _ => trash
            };
        }
        public static Dictionary<string, string> RawToProductMap = new()
        {
            { "436", "426" }, //Goat Milk
            { "438", "426" }, //L Goat Milk
            { "184", "424" }, // Milk
            { "186", "424" }, //L Milk
            { "176", "306" }, //Egg
	        { "180", "306" }, //Egg
	        { "174", "306" }, //L Egg
	        { "182", "306" }, //L Egg
	        { "289", "306" }, //Ostrich Egg
	        { "928", "306" }, //Golden Egg
	        { "107", "807" }, //Dinosaur Egg
	        { "305", "308" }, //Void Egg
	        { "442", "307" }, //Duck Egg
	        { "440", "428" }, //Wool
	        { "430", "432" }, //Truffle
        };

        public static Dictionary<string, string> ItemTypeLabels = new()
        {
            { "Crab Pots", T("ItemTypeCrabPots") },
            { "Crops", T("ItemTypeCrops") },
            { "Forageables", T("ItemTypeForageables") },
            { "Hardwood", T("ItemTypeHardwood") },
            { "Honey", T("ItemTypeHoney") },
            { "Stone", T("ItemTypeStone") },
            { "Weeds", T("ItemTypeWeeds") },
            { "Wood", T("ItemTypeWood") },
            { "Tappers", T("ItemTypeTappers") },
            { "Till", T("ItemTypeTill") },
            { "Water", T("ItemTypeWater") },
            { "PlaceTappers", T("ItemTypePlaceTappers") },
            { "Fertilize", T("ItemTypeFertilize") },
            { "Seeds", T("ItemTypeSeeds") },
            { "InvisiblePots", T("ItemTypeInvisiblePots") },
            { "PlaceBeeHouse", T("ServicePlaceBeeHouse") },
            { "RockCandy", T("ServiceRockCandy") }
        };

        public static Dictionary<ServiceId, string> ServiceTypeLabels = new()
        {
            { ServiceId.CrabPots, T("ItemTypeCrabPots") },
            { ServiceId.Crops, T("ItemTypeCrops") },
            { ServiceId.Forageables, T("ItemTypeForageables") },
            { ServiceId.Hardwood, T("ItemTypeHardwood") },
            { ServiceId.Honey, T("ItemTypeHoney") },
            { ServiceId.Stone, T("ItemTypeStone") },
            { ServiceId.Weeds, T("ItemTypeWeeds") },
            { ServiceId.Wood, T("ItemTypeWood") },
            { ServiceId.Tappers, T("ItemTypeTappers") },
            { ServiceId.Till, T("ItemTypeTill") },
            { ServiceId.Water, T("ItemTypeWater") },
            { ServiceId.Fertilize, T("ItemTypeFertilize") },
            { ServiceId.Seeds, T("ItemTypeSeeds") },
            { ServiceId.PlaceInvisiblePots, T("ItemTypeInvisiblePots") },
            { ServiceId.PlaceBeeHouse, T("ServicePlaceBeeHouse") },
            { ServiceId.RockCandy, T("ServiceRockCandy") },
            { ServiceId.Processing, T("ServiceProcessing") }
        };

        public static Dictionary<string, object> NPCProfiles = new()
        {
            { "Abigail", new AbigailProfile() },
            { "Alex", new AlexProfile() },
            { "Caroline", new CarolineProfile() },
            { "Demetrius", new DemetriusProfile() },
            { "Elliott", new ElliottProfile() },
            { "Emily", new EmilyProfile() },
            { "Evelyn", new EvelynProfile() },
            { "George", new GeorgeProfile() },
            { "Haley", new HaleyProfile() },
            { "Jas", new JasProfile() },
            { "Jodi", new JodiProfile() },
            { "Leah", new LeahProfile() },
            { "Leo", new LeoProfile() },
            { "Linus", new LinusProfile() },
            { "Maru", new MaruProfile() },
            { "Pam", new PamProfile() },
            { "Penny", new PennyProfile() },
            { "Sam", new SamProfile() },
            { "Sandy", new SandyProfile() },
            { "Sebastian", new SebastianProfile() },
            { "Shane", new ShaneProfile() },
            { "Vincent", new VincentProfile() },
            { "Wizard", new WizardProfile() },
        };
    }
}
