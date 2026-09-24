using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core
{
    public class CollectionHelpers
    {
        public static bool IsTillable(GameLocation loc, int x, int y)
        {
            Vector2 tile = new Vector2(x, y);

            if (loc.doesTileHaveProperty(x, y, "Diggable", "Back") == null)
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            if (loc.terrainFeatures.ContainsKey(tile))
                return false;

            if (!loc.isTileLocationOpen(tile))
                return false;

            if (!loc.isTilePassable(new Location(x * 64, y * 64), Game1.viewport))
                return false;

            return true;
        }

        public static bool IsHarvestableTile(GameLocation loc, Vector2 tile)
        {
            if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                feature is HoeDirt dirt &&
                dirt.crop is Crop crop &&
                !crop.dead.Value)
            {
                string seedId = crop.netSeedIndex.Value;
                if (!string.IsNullOrEmpty(seedId) &&
                    Game1.cropData.TryGetValue(seedId, out var data))
                {
                    int phase = crop.currentPhase.Value;
                    int finalPhase = crop.phaseDays.Count - 1;

                    if (phase >= finalPhase && data.HarvestItemId != null)
                        return true;
                }
            }

            if (loc.objects.TryGetValue(tile, out var obj) &&
                obj is IndoorPot pot &&
                pot.hoeDirt?.Value?.crop is Crop potCrop &&
                !potCrop.dead.Value)
            {
                string seedId = potCrop.netSeedIndex.Value;
                if (!string.IsNullOrEmpty(seedId) &&
                    Game1.cropData.TryGetValue(seedId, out var data))
                {
                    int phase = potCrop.currentPhase.Value;
                    int finalPhase = potCrop.phaseDays.Count - 1;

                    if (phase >= finalPhase && data.HarvestItemId != null)
                        return true;
                }
            }

            return false;
        }

        public static int GetVirtualCropYield(Crop crop, Farmer farmer)
        {
            if (!Game1.cropData.TryGetValue(crop.rowInSpriteSheet.Value.ToString(), out var data))
                return 1;

            int amount = data.HarvestMinStack;

            if (data.HarvestMaxStack > data.HarvestMinStack)
            {
                amount = Game1.random.Next(
                    data.HarvestMinStack,
                    data.HarvestMaxStack + 1
                );

                if (data.HarvestMaxIncreasePerFarmingLevel > 0)
                {
                    int bonus = (int)(farmer.FarmingLevel / data.HarvestMaxIncreasePerFarmingLevel);
                    amount += bonus;
                }
            }

            if (data.ExtraHarvestChance > 0f)
            {
                while (Game1.random.NextDouble() < data.ExtraHarvestChance)
                    amount++;
            }

            return amount < 1 ? 1 : amount;
        }

        public static bool TryChargeFeeOrStop(
            int feePerTile,
            string npcName,
            Dictionary<(string id, int quality), SObject> itemMap,
            IMonitor monitor,
            ModConfig Config)
        {
            if (Game1.player.Money < feePerTile)
            {
                var partialItems = itemMap.Values
                    .Where(i => i.Stack > 0)
                    .Cast<Item>()
                    .ToList();

                if (partialItems.Count > 0)
                {
                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = partialItems,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);
                }

                Game1.showGlobalMessage(T("StoppedEarlyGold", new { npc = npcName }));
                return false;
            }

            Game1.player.Money -= feePerTile;
            return true;
        }

        public static bool TryChargeFeeOrStopSimple(
            int feePerTile,
            string npcName,
            IMonitor monitor)
        {
            if (Game1.player.Money < feePerTile)
            {
                Game1.showGlobalMessage(T("StoppedEarlyGold", new { npc = npcName }));
                return false;
            }

            Game1.player.Money -= feePerTile;
            return true;
        }

        public static List<(GameLocation loc, Vector2 tile, CrabPot pot)> ScanCrabPots()
        {
            var results = new List<(GameLocation, Vector2, CrabPot)>();

            foreach (var loc in Game1.locations)
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value is CrabPot pot &&
                        pot.readyForHarvest.Value &&
                        pot.heldObject.Value is SObject)
                    {
                        results.Add((loc, pair.Key, pot));
                    }
                }
            }

            return results;
        }

        public static void AddCrabPotItemToMap(
            Dictionary<(string id, int quality), SObject> itemMap,
            SObject catchObj,
            int Quantity,
            int quality)
        {
            string id = catchObj.ItemId;
            var key = (id, quality);

            if (!itemMap.TryGetValue(key, out var stacked))
            {
                stacked = new SObject(id, Quantity) { Quality = quality };
                itemMap[key] = stacked;
            }
            else
            {
                stacked.Stack += Quantity;
            }
        }

        public static void FriendshipProgressTick(
        ref int counter,
        ref int totalPoints,
        int threshold)
        {
            counter++;

            if (counter >= threshold)
            {
                totalPoints++;
                counter = 0;
            }
        }

        public static void FriendshipInitialAward(ref int totalPoints)
        {
            totalPoints++;
        }

        public static List<(GameLocation loc, Vector2 tile, SObject tapper)> ScanTappers()
        {
            var results = new List<(GameLocation, Vector2, SObject)>();

            foreach (var loc in Game1.locations)
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value is SObject tapper &&
                        tapper.bigCraftable.Value &&
                        (tapper.ParentSheetIndex == 105 || tapper.ParentSheetIndex == 264) &&
                        tapper.readyForHarvest.Value &&
                        tapper.heldObject.Value is SObject)
                    {
                        results.Add((loc, pair.Key, tapper));
                    }
                }
            }

            return results;
        }

        public static void AddTapperItemToMap(
            Dictionary<(string id, int quality), SObject> itemMap,
            SObject tappedProduct,
            int quality)
        {
            string id = tappedProduct.ItemId;
            int amount = tappedProduct.Stack + (1 + quality);

            var key = (id, quality);

            if (!itemMap.TryGetValue(key, out var stacked))
            {
                stacked = new SObject(id, amount);
                itemMap[key] = stacked;
            }
            else
            {
                stacked.Stack += amount;
            }
        }

        public static bool IsSafeCrabPotTile(GameLocation loc, Vector2 tile)
        {
            if (!IsStableLocation(loc))
                return false;

            if (loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") == null)
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            var buildingsTile = loc.Map.GetLayer("Buildings").Tiles[(int)tile.X, (int)tile.Y];
            if (buildingsTile != null)
                return false;

            return true;
        }

        public static List<(GameLocation loc, Vector2 tile, SObject honey)> ScanReadyBeeHouses()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        obj.ParentSheetIndex == 10 &&
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject honey)
                    {
                        list.Add((location, pair.Key, honey));
                    }
                }
            }

            return list;
        }

        public static List<(GameLocation loc, ResourceClump clump)> ScanHardwoodClumps()
        {
            var list = new List<(GameLocation, ResourceClump)>();

            foreach (var location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                        clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                    {
                        list.Add((location, clump));
                    }
                }
            }
            return list;
        }

        public static List<(GameLocation loc, Vector2 tile, SObject weed)> ScanWeeds()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            int[] weedIndices =
            {
                0, 313, 314, 315, 316, 317, 318,
                452,
                674, 675, 676, 677, 678, 679,
                747, 748, 750, 784, 785, 786, 792, 793, 794,
                882, 883, 884
            };

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        weedIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        list.Add((location, pair.Key, obj));
                    }
                }
            }

            return list;
        }

        public static List<(GameLocation loc, Vector2 tile, SObject debris)> ScanWoodDebris()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            int[] woodIndices =
            {
                30, 294, 295, 388
            };

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        woodIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        list.Add((location, pair.Key, obj));
                    }
                }
            }

            return list;
        }

        public static List<(GameLocation loc, Vector2 tile, int baseYield, bool isClump)> ScanStoneDebris()
        {
            var list = new List<(GameLocation, Vector2, int, bool)>();

            int[] smallStoneIndices = { 343, 450, 668, 670 };

            foreach (var location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.boulderIndex)
                    {
                        list.Add((location, clump.Tile, 15, true));
                    }
                }

                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        smallStoneIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value &&
                        obj.CanBeGrabbed)
                    {
                        list.Add((location, pair.Key, 1, false));
                    }
                }
            }
            return list;
        }

        public static List<(GameLocation loc, Vector2 tile, SObject item, bool fromProducer)> ScanForageableSources()
        {
            var list = new List<(GameLocation, Vector2, SObject, bool)>();

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is not SObject obj)
                        continue;

                    if (!obj.bigCraftable.Value && obj.CanBeGrabbed && obj.IsSpawnedObject)
                    {
                        list.Add((location, pair.Key, obj, false));
                    }

                    else if (obj.bigCraftable.Value &&
                             (obj.Name == "Mushroom Box" || obj.Name == "Mushroom Log") &&
                             obj.heldObject.Value is SObject held)
                    {
                        list.Add((location, pair.Key, held, true));
                    }
                }
            }
            foreach (var location in GetAllLocations())
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is Tree tree && tree.hasMoss.Value)
                    {
                        var moss = new SObject("815", 1);
                        list.Add((location, pair.Key, moss, false));
                    }
                }
            }
            return list;
        }

        public static IEnumerable<GameLocation> GetAllLocations()
        {
            foreach (var loc in Game1.locations)
            {
                yield return loc;

                if (loc is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building.indoors.Value != null)
                            yield return building.indoors.Value;
                    }
                }
            }
        }

        public static bool IsStableLocation(GameLocation loc)
        {
            return loc is not MineShaft
                && loc is not VolcanoDungeon
                && !loc.IsTemporary;
        }

        public static void DrawSquarePlacementOverlay(SpriteBatch spriteBatch)
        {
            Farmer farmer = Game1.player;
            GameLocation loc = farmer.currentLocation;

            int length = Config.RectangleWidth;
            int width = Config.RectangleLength;

            var tiles = SortTileSquare();

            foreach (var tile in tiles)
            {
                var previewObj = new IndoorPot(tile);

                bool placeable = CanPlaceObjectHere(loc, tile, previewObj);

                Color color = placeable
                    ? new Color(60, 255, 60, 140)
                    : new Color(255, 60, 60, 140);

                DrawTileHighlight(spriteBatch, tile, color);
            }
        }

        private static void DrawTileHighlight(SpriteBatch spriteBatch, Vector2 tile, Color color)
        {
            Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(
                (int)(tile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(tile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            spriteBatch.Draw(Game1.staminaRect, rect, color);
        }

        public static IEnumerable<GameLocation> GetAllLocations_ForDirt()
        {
            HashSet<GameLocation> result = new();

            foreach (var loc in Game1.locations)
                result.Add(loc);

            var farmhouse = Game1.getLocationFromName("FarmHouse");
            if (farmhouse != null)
                result.Add(farmhouse);

            for (int i = 0; i < 10; i++)
            {
                var cabin = Game1.getLocationFromName($"Cabin{i}");
                if (cabin != null)
                    result.Add(cabin);
            }

            if (Game1.getLocationFromName("Farm") is Farm farm)
            {
                foreach (var building in farm.buildings)
                {
                    if (building.indoors.Value != null)
                        result.Add(building.indoors.Value);
                }
            }

            return result;
        }

        public static bool CanPlaceObjectHere(GameLocation loc, Vector2 tile, SObject obj)
        {
            //Allow pots on empty indoor tiles
            if (Config.AllowEmptyTilePotPlacement)
            {
                // Only require that the tile is not occupied
                if (loc.isTileOnMap(tile) &&
                    !loc.objects.ContainsKey(tile) &&
                    !loc.IsOutdoors)
                {
                    return true;
                }
            }

            if (!loc.isTileOnMap(tile))
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            return obj.canBePlacedHere(loc, tile);
        }

        public static bool IsSeedAllowedHere(string seedId, GameLocation loc, Vector2 tile)
        {
            if (loc.objects.TryGetValue(tile, out var obj) &&
                obj is IndoorPot pot &&
                pot.hoeDirt.Value is HoeDirt potDirt)
            {
                if (!loc.IsOutdoors)
                    return seedId != "RiceShoot";
            }

            if (loc.Name.StartsWith("Island"))
                return true;

            if (seedId == "MixedSeeds" || seedId == "770" || seedId.EndsWith("770"))
            {
                if (loc.IsGreenhouse)
                    return true;

                if (!loc.IsFarm || !loc.IsOutdoors)
                    return false;

                return Game1.currentSeason is "spring" or "summer" or "fall";
            }

            if (!Game1.cropData.TryGetValue(seedId, out var data))
                return false;

            if (loc.IsGreenhouse)
                return true;

            if (data.Seasons == null || data.Seasons.Count == 0)
                return false;

            if (!Enum.TryParse<Season>(Game1.currentSeason, true, out var currentSeasonEnum))
                return false;

            return data.Seasons.Contains(currentSeasonEnum);
        }

        public static SObject GetNextValidSeed(GameLocation loc, Vector2 tile)
        {
            Chest locker = GetProcessingLocker();
            if (locker == null)
                return null;

            foreach (var item in locker.Items.OfType<SObject>())
            {
                if (item.Category != SObject.SeedsCategory)
                    continue;

                if (IsSeedAllowedHere(item.ItemId, loc, tile))
                    return item;
            }

            return null;
        }

        public static int CountSeedsAllowedHere(GameLocation loc, Vector2 tile)
        {
            return Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.Category == SObject.SeedsCategory &&
                            IsSeedAllowedHere(i.ItemId, loc, tile))
                .Sum(i => i.Stack);
        }

        public static IEnumerable<Vector2> GetEmptyTroughTiles(AnimalHouse house)
        {
            var layer = house.map.GetLayer("Buildings");
            if (layer == null)
                yield break;

            int width = layer.LayerWidth;
            int height = layer.LayerHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = layer.Tiles[x, y];
                    if (tile != null && tile.Properties.ContainsKey("Trough"))
                    {
                        Vector2 pos = new Vector2(x, y);

                        if (!house.objects.ContainsKey(pos))
                            yield return pos;
                    }
                }
            }
        }

        public static Chest GetProcessingLocker()
        {
            if (!Config.ProcessingLockerAssigned)
            {
                Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
                return null;
            }

            GameLocation loc = Game1.getLocationFromName(Config.ProcessingLockerLocation);
            if (loc == null)
            {
                Game1.addHUDMessage(new HUDMessage(T("NoChestFound"), HUDMessage.newQuest_type));
                return null;
            }

            Vector2 tile = ParseTile(Config.ProcessingLockerTile);
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

        public static Vector2 ParseTile(string tileString)
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

        public static SObject GetRawGoodsFromLocker(string Product)
        {
            Chest locker = GetProcessingLocker();
            if (locker == null)
                return null;

            else if (Product == "Seeds")
            {
                return locker.Items
                    .OfType<SObject>()
                    .Where(o =>
                        (
                            o.Category == -75 ||
                            o.Category == -79
                        ) && o.Stack >= 1 )
                    .FirstOrDefault();
            }

            else if (Product == "Processing")
            {
                return locker.Items
                    .OfType<SObject>()
                    .Where(o =>
                        (
                            o.Category == -4 ||
                            o.Category == -5 ||
                            o.Category == -6 ||
                            o.Category == -20 ||
                            o.Category == -75 ||
                            o.Category == -79 ||
                            o.Category == -80 ||
                            o.Category == -81 ||
                            IsOre(o) && o.Stack >= 5 ||
                            RawToProductMap.ContainsKey(o.ItemId) ||
                            o.ParentSheetIndex == 340 ||
                            o.ParentSheetIndex == 812 ||
                            o.ParentSheetIndex == 433
                        ) && o.Stack >= 1 )
                    .FirstOrDefault();
            }

            else if (Product == "Bait")
            {
                return locker.Items
                    .OfType<SObject>()
                    .Where(o => o.ParentSheetIndex == 685 && o.Stack >= 1)
                    .FirstOrDefault();
            }

            else
                return null;
        }

        public static void Consume(SObject item, int amount)
        {
            item.Stack -= amount;

            if (item.Stack <= 0)
            {
                Chest locker = GetProcessingLocker();

                if (locker != null && locker.Items.Contains(item))
                {
                    Game1.delayedActions.Add(new DelayedAction(1, () =>
                    {
                        locker.Items.Remove(item);
                    }));
                }
                else
                {
                    Game1.player.removeItemFromInventory(item);
                }
            }
        }

        public static bool IsOre(SObject obj)
        {
            return OreToBarMap.ContainsKey(obj.ItemId);
        }

        public static bool IsTrash(SObject obj)
        {
            return obj.ParentSheetIndex is 168 or 169 or 170 or 171 or 172;
        }

        public static async Task<Dictionary<(string id, int quality), SObject>> GenerateProductShipmentWithDelay2
        (
        string NpcName,
        int Quality,
        int ProcessorsOperated,
        SObject BaseItem,
        int BaseQuantity,
        string ProcessType,
        string ContractPerc,
        bool Shipping,
        int FarmerSkillLevel)
        {
            int NPCLevel = UpdateNPCLevel(NpcName);
            int qual = Quality;

            ProcessingRegistry.ProcessingMode = ProcessType;

            var itemMap = new Dictionary<(string itemId, int quality), SObject>();

            if (BaseItem == null)
            {
                monitor.Log("BaseItem was null in GenerateProductShipmentWithDelay2", LogLevel.Warn);
                return new Dictionary<(string id, int quality), SObject>();
            }

            if (IsOre(BaseItem))
            {
                ProcessorsOperated /= 5 + 1;
            }

            if (ProcessorsOperated > 0)
            {
                var processedMap = new Dictionary<(string itemId, int quality), SObject>();

                for (int i = 0; i < ProcessorsOperated; i++)
                {
                    var raw = BaseItem.getOne() as SObject;
                    if (raw == null)
                        continue;

                    var processed = ProcessingConverter.ConvertRawToProcessed(raw, monitor);

                    if (processed == null)
                    {
                        processed = raw;
                    }
                    else
                    {

                    }

                    processed.Quality = qual;

                    var key = (processed.ItemId, processed.Quality);

                    if (!processedMap.TryGetValue(key, out var existing))
                    {
                        processedMap[key] = processed;

                        int feePerItem = GetContractPercent(ContractPerc) * processed.Price / 100;

                        if (!TryChargeFeeOrStopSimple(feePerItem, NpcName, monitor))
                            break;
                    }
                    else
                    {
                        existing.Stack += processed.Stack;

                        int feePerItem = GetContractPercent(ContractPerc) * processed.Price / 100;

                        if (!TryChargeFeeOrStopSimple(feePerItem, NpcName, monitor))
                            break;
                    }

                    if (processed.Stack >= 999)
                    {
                        var deliveryChunk = new List<Item> { processed.getOne() };
                        deliveryChunk[0].Stack = 999;

                        processed.Stack -= 999;

                        if (Shipping)
                        {
                            ShipContractItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = deliveryChunk,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }
                        else
                            DeliverContractsItems(new List<ContractsDelivery>
                          {
                              new ContractsDelivery
                              {
                                  Items = deliveryChunk,
                                  RecipientID = Game1.player.UniqueMultiplayerID
                              }
                          }, Config);

                        Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                    }

                    if (!Game1.newDay)
                        await Task.Delay(Config.CollectionDelay / SafeMultiplier(NPCLevel + FarmerSkillLevel));
                }

                foreach (var kvp in processedMap)
                {
                    var so = kvp.Value;

                    if (Shipping)
                    {
                        ShipContractItems(new List<ContractsDelivery>
                        {
                            new ContractsDelivery
                            {
                                Items = new List<Item> { so },
                                RecipientID = Game1.player.UniqueMultiplayerID
                            }
                        }, Config);
                    }
                    else
                        DeliverContractsItems(new List<ContractsDelivery>
                        {
                            new ContractsDelivery
                            {
                                Items = new List<Item> { so },
                                RecipientID = Game1.player.UniqueMultiplayerID
                            }
                        }, Config);
                }
            }

            for (int i = 0; i < BaseQuantity; i++)
            {
                var UnRefined = BaseItem.getOne() as SObject;
                if (UnRefined != null)
                {
                    var key = (UnRefined.ItemId, UnRefined.Quality);

                    if (!itemMap.TryGetValue(key, out var stacked))
                    {
                        stacked = UnRefined;
                        stacked.Quality = qual;

                        itemMap[key] = stacked;

                        int feePerItem = GetContractPercent(ContractPerc) * stacked.Price / 100;

                        if (!TryChargeFeeOrStopSimple(feePerItem, NpcName, monitor))
                            break;
                    }
                    else
                    {
                        stacked.Stack += 1;

                        int feePerItem = GetContractPercent(ContractPerc) * stacked.Price / 100;

                        if (!TryChargeFeeOrStopSimple(feePerItem, NpcName, monitor))
                            break;
                    }

                    if (stacked.Stack >= 999)
                    {
                        var deliveryChunk = new List<Item> { stacked.getOne() };
                        deliveryChunk[0].Stack = 999;

                        stacked.Stack -= 999;

                        if (Shipping)
                        {
                            ShipContractItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = deliveryChunk,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }
                        else
                            DeliverContractsItems(new List<ContractsDelivery>
                          {
                              new ContractsDelivery
                              {
                                  Items = deliveryChunk,
                                  RecipientID = Game1.player.UniqueMultiplayerID
                              }
                          }, Config);

                        Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                    }

                    if (!Game1.newDay)
                    {
                        await Task.Delay(Config.CollectionDelay / SafeMultiplier(NPCLevel + FarmerSkillLevel));
                    }
                    else
                    {

                    }
                }
            }

            return itemMap;
        }

        public class ProcessRecipe
        {
            public string OutputId { get; set; }

            public Func<SObject, string>? NameBuilder { get; set; }

            public Func<SObject, int>? PriceBuilder { get; set; }

            public SObject.PreserveType? PreserveType { get; set; }

            public Func<SObject, string>? PreservedParentIndex { get; set; }
            
        }

        public static class ProcessingRegistry
        {
            public static string ProcessingMode { get; set; }

            public static readonly List<(Func<SObject, bool> predicate, ProcessRecipe recipe)> Recipes = new();

            static ProcessingRegistry()
            {
                // Soggy Newspaper → Cloth
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "172",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "428",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Broken CD → Refined Quartz
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "171",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "338",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Broken Glasses → Refined Quartz
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "170",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "338",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Driftwood → Wood
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "169",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "388",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Trash → Iron Ore
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "168",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "380",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Joja Cola → Vinegar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "167",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "419",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Copper Ore → Copper Bar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "378",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "334",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Iron Ore → Iron Bar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "380",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "335",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Gold Ore → Gold Bar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "384",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "336",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Iridium Ore → Iridium Bar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "386",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "337",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Radioactive Ore → Radioactive Bar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "909",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "910",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Tea Leaves → Green Tea
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "815",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "614",
                        NameBuilder = input => "Green Tea",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Coffee Beans → Coffee
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "433",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "395",
                        NameBuilder = input => "Coffee",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Wool → Cloth
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "440",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "428",
                        NameBuilder = input => "Cloth",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Grapes → Raisins
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "398" && ProcessingMode == "DriedFruit",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "Raisins",
                        PriceBuilder = input => input.Price * 2,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fruit → Dried Fruit
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -79 && ProcessingMode == "DriedFruit",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "DriedFruit",
                        NameBuilder = input => "Dried " + input.Name,
                        PriceBuilder = input => input.Price * 2,
                        PreserveType = SObject.PreserveType.DriedFruit,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Sturgeon → Caviar
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "698" && ProcessingMode == "AgedRoe",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "445",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fish → Aged Roe
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -4 && ProcessingMode == "AgedRoe",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "447",
                        NameBuilder = input => "Aged " + input.Name + " Roe",
                        PriceBuilder = input => input.Price * 2,
                        PreserveType = SObject.PreserveType.AgedRoe,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Flowers → Honey
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -80 && ProcessingMode == "FlowerHoney",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "340",
                        NameBuilder = input => input.Name + " Honey",
                        PriceBuilder = input => 100 + input.Price * 2,
                        PreserveType = SObject.PreserveType.Honey,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Flowers → Essential Oil
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -80,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "247",
                        NameBuilder = input => input.Name + " Essential Oil",
                        PriceBuilder = input => 100 + input.Price * 3,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Duck Eggs → Duck Mayo
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "442",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "307",
                        NameBuilder = input => "Duck Mayonnaise",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Void Eggs → Void Mayo
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "305",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "308",
                        NameBuilder = input => "Void Mayonnaise",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Dino Eggs → Dino Mayo
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "107",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "807",
                        NameBuilder = input => "Dinosaur Mayonnaise",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fruit → Wine
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -79 && ProcessingMode == "Wine",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "348",
                        NameBuilder = input => input.Name + " Wine",
                        PriceBuilder = input => input.Price * 3,
                        PreserveType = SObject.PreserveType.Wine,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fish → Smoked Fish
                Recipes.Add((
                    predicate: input => input.Category == -4 && ProcessingMode == "SmokedFish",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "SmokedFish",
                        NameBuilder = input => "Smoked " + input.Name,
                        PriceBuilder = input => (int)(input.Price * 2.5f),
                        PreserveType = SObject.PreserveType.SmokedFish,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Eggs → Mayo
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -5,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "306",
                        NameBuilder = input => input.Name + " Mayonnaise",
                        PriceBuilder = input => input.Price * 4,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fruit → Jelly
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -79 && ProcessingMode == "Jelly",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "344",
                        NameBuilder = input => input.Name + " Jelly",
                        PriceBuilder = input => (int)(input.Price * 2.5f),
                        PreserveType = SObject.PreserveType.Jelly,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Forageables → Pickles
                Recipes.Add((
                    predicate: input => input.Category == -81,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "342",
                        NameBuilder = input => input.Name + " Pickles",
                        PriceBuilder = input => input.Price * 2,
                        PreserveType = SObject.PreserveType.Pickle,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Milk → Cheese
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "184",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "424",
                        NameBuilder = input => "Cheese",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Milk → Cheese
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "186",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "424",
                        NameBuilder = input => "Big Cheese",
                        PriceBuilder = input => (int)(input.Price * 1.6f),
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Goat Milk → Goat Cheese
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "436",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "426",
                        NameBuilder = input => "Goat Cheese",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Goat Milk → Goat Cheese
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "438",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "426",
                        NameBuilder = input => "Large Goat Cheese",
                        PriceBuilder = input => (int)(input.Price * 1.6f),
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Honey → Mead
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "340" || input.ItemId == "724",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "459",
                        NameBuilder = input => input.Name + " Mead",
                        PriceBuilder = input => input.Price * 3,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Sturgeon Roe → Caviar
                Recipes.Add((
                    predicate: (SObject input) => input.Name == "Sturgeon Roe",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "445",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Roe → Aged Roe
                Recipes.Add((
                    predicate: input => input.preserve.Value == SObject.PreserveType.Roe,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "447", // Aged Roe item
                        NameBuilder = input =>
                        {
                            var fishId = input.preservedParentSheetIndex.Value.ToString();
                            var fish = ItemRegistry.Create(fishId) as SObject;
                            var fishName = fish?.Name ?? "";
                            return "Aged " + fishName + " Roe";
                        },

                        PriceBuilder = input => input.Price * 2,
                        PreservedParentIndex = input => input.preservedParentSheetIndex.Value.ToString()
                    }
                ));

                // Truffle → Truffle Oil
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "430",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "432",
                        NameBuilder = input => "Truffle Oil",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fish → Sashimi
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -4,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "227",
                        NameBuilder = input => input.Name + " Sashimi",
                        PriceBuilder = input => input.Price * 2,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Fruit → Juice
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -79,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "350",
                        NameBuilder = input => input.Name + " Juice",
                        PriceBuilder = input => (int)(input.Price * 2.25f),
                        PreserveType = SObject.PreserveType.Juice,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Unmilled Rice → Rice
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "271",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "423",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Hops → Pale Ale
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "304",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "303",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Wheat → Beer
                Recipes.Add((
                    predicate: (SObject input) => input.ItemId == "262",
                    recipe: new ProcessRecipe
                    {
                        OutputId = "346",
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));

                // Vegetables → Juice
                Recipes.Add((
                    predicate: (SObject input) => input.Category == -75,
                    recipe: new ProcessRecipe
                    {
                        OutputId = "350",
                        NameBuilder = input => input.Name + " Juice",
                        PriceBuilder = input => (int)(input.Price * 2.25f),
                        PreserveType = SObject.PreserveType.Juice,
                        PreservedParentIndex = input => input.ParentSheetIndex.ToString()
                    }
                ));
            }
        }

        public static class ProcessingConverter
        {
            public static SObject? ConvertRawToProcessed(SObject input, IMonitor monitor = null)
            {
                var entry = ProcessingRegistry.Recipes.FirstOrDefault(r => r.predicate(input));
                if (entry.recipe == null)
                {
                    monitor?.Log($"No processing recipe found for item {input.ItemId} ({input.Name})", LogLevel.Trace);
                    return null;
                }

                var recipe = entry.recipe;

                var output = ItemRegistry.Create(recipe.OutputId) as SObject;
                if (output == null)
                {
                    monitor?.Log($"Failed to create output item {recipe.OutputId} for {input.ItemId}", LogLevel.Warn);
                    return null;
                }

                if (recipe.NameBuilder != null)
                    output.Name = recipe.NameBuilder(input);

                if (recipe.PriceBuilder != null)
                    output.Price = recipe.PriceBuilder(input);

                if (recipe.PreserveType.HasValue)
                {
                    output.preserve.Value = recipe.PreserveType.Value;

                    if (recipe.PreservedParentIndex != null)
                        output.preservedParentSheetIndex.Value = recipe.PreservedParentIndex(input);
                }

                output.modData["cc:price"] = output.Price.ToString();

                return output;
            }
        }

        public static List<SObject> GetAllObjectsInCategory(int category)
        {
            var results = new List<SObject>();

            var data = Game1.content.Load<Dictionary<string, ObjectData>>("Data/Objects");

            foreach (var kvp in data)
            {
                string id = kvp.Key;

                string qualifiedId = ItemRegistry.QualifyItemId(id);

                if (Config.ExcludedItemIds.Contains(qualifiedId))
                    continue;

                var item = ItemRegistry.Create(qualifiedId) as SObject;
                if (item == null)
                    continue;

                if (item.Category == category)
                    results.Add(item);
            }

            return results;
        }

        public static List<SObject> FilterBySeason(List<SObject> items, int seasonIndex)
        {
            return items.Where(i =>
                i.Category == -79 || // fruit
                i.Category == -75 || // vegetables
                i.Category == -80 || // flowers
                i.Category == -81 || // forage/mushrooms
                i.Category == -4     // fish
            ).ToList();
        }

        public static int GetRandomItemFromCategory(int category, int seasonIndex)
        {
            var allItems = GetAllObjectsInCategory(category);
            var seasonal = FilterBySeason(allItems, seasonIndex);

            if (seasonal.Count == 0)
                return -1;

            Random rng = new Random();
            return seasonal[rng.Next(seasonal.Count)].ParentSheetIndex;
        }
    }
}
