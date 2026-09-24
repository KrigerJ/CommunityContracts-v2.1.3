using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class HarvestService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string harvestingnpcName;
        public int cropsHarvested = 0;
        public bool Shipping = false;
        public bool Juice = false;

        public readonly CollectionServiceManager manager;
        public HarvestService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }

        public void OfferHarvestingService(IMonitor monitor, string HarvestingnpcName)
        {
            this.monitor = monitor;
            this.harvestingnpcName = HarvestingnpcName;
            var candidateTiles = new List<(GameLocation loc, Vector2 tile)>();
            int npcLevel = UpdateNPCLevel(this.harvestingnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / (SafeMultiplier(npcLevel + farmerSkill));
            var npc = Game1.getCharacterFromName(this.harvestingnpcName);
            this.friendshipPointsEarned = 0;
            this.cropsHarvested = 0;
            this.totalFeesPaid = 0;
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Crops)
               ? SpecialtyNames[ServiceId.Crops]
               : T("ServiceWeeds");
            int friendshipCounter = 0;
            int feePercent = Config.SeviceContractFees[ServiceId.Processing];

            var visited = new HashSet<(GameLocation loc, Vector2 tile)>();

            List<GameLocation> allLocations = new();

            foreach (var loc in Game1.locations)
            {
                if (loc != null)
                    allLocations.Add(loc);
            }

            foreach (var building in Game1.getFarm().buildings)
            {
                var indoors = building.indoors.Value;
                if (indoors != null)
                    allLocations.Add(indoors);
            }

            foreach (var loc in allLocations)
            {
                int width = loc.Map.Layers[0].LayerWidth;
                int height = loc.Map.Layers[0].LayerHeight;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        if (IsHarvestableTile(loc, tile))
                        {
                            var key = (loc, tile);

                            if (!visited.Contains(key))
                            {
                                visited.Add(key);
                                candidateTiles.Add(key);
                            }
                        }
                    }
                }
            }

            if (candidateTiles.Count == 0)
            {
                Game1.showGlobalMessage(T("NoCropsReady"));
                return;
            }

            int feePerTile = Config.SeviceContractFees[ServiceId.Crops];
            int maxAffordable = Game1.player.Money / feePerTile;

            int tilesToHarvest = Math.Min(candidateTiles.Count, maxAffordable);
            int totalFee = tilesToHarvest * feePerTile;

            string dialogText =
                T("ServiceOffer", new { npc = this.harvestingnpcName, quantity = tilesToHarvest, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerTile }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseRawGoodsYes")),
                    new Response("Deliver", T("ResponseDeliverJuice")),
                    new Response("Ship", T("ResponseShipJuice")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    if (answer == "Deliver")
                    {
                        Juice = true;
                    }

                    if (answer == "Ship")
                    {
                        Juice = true;
                        Shipping = true;
                    }
                    else
                    {
                        Shipping = false;
                    }

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.harvestingnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> stackMap = new();

                    for (int i = 0; i < tilesToHarvest; i++)
                    {
                        var (loc, tile) = candidateTiles[i];

                        if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                            feature is HoeDirt dirt && dirt.crop != null)
                        {
                            var crop = dirt.crop;

                            string seedId = crop.netSeedIndex.Value;
                            if (string.IsNullOrEmpty(seedId) || !Game1.cropData.TryGetValue(seedId, out var data))
                                continue;

                            string id = crop.indexOfHarvest.Value;
                            int amount = GetVirtualCropYield(crop, Game1.player);

                            var key = (id, quality);

                            if (!stackMap.TryGetValue(key, out var stack))
                            {
                                stack = new SObject(id, 0) { Quality = quality };
                                stackMap[key] = stack;
                            }

                            stack.Stack += amount;

                            if (stack.Stack >= 999)
                            {
                                SObject batch = stack.getOne() as SObject;
                                batch.Stack = 999;
                                batch.Quality = stack.Quality;

                                if (Juice)
                                {
                                    SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                    if (processed == null)
                                    {
                                        monitor.Log($"Converter returned null for harvested crop {batch?.ItemId}. Delivering raw batch instead.", LogLevel.Warn);

                                        DeliverProcessedStack(batch, Shipping, Config);

                                        feePerTile = (int)(batch.Price * feePercent / 100);
                                        if (feePerTile < 1)
                                            feePerTile = 1;

                                        if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                            break;

                                        this.totalFeesPaid += feePerTile * batch.Stack;
                                        this.cropsHarvested += batch.Stack;

                                        if (!Game1.newDay)
                                            await Task.Delay(delay * batch.Stack);

                                        stack.Stack -= 999;
                                        continue;
                                    }

                                    processed.Stack = batch.Stack;
                                    processed.Quality = batch.Quality;

                                    DeliverProcessedStack(processed, Shipping, Config);

                                    feePerTile = (int)(processed.Price * feePercent / 100);
                                    if (feePerTile < 1)
                                        feePerTile = 1;
                                    monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {feePerTile * batch.Stack}", LogLevel.Warn);
                                    if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerTile * batch.Stack;
                                    this.cropsHarvested += batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                }

                                else
                                {
                                    DeliverProcessedStack(batch, Shipping, Config);

                                    feePerTile = (int)(batch.Price * feePercent / 100);
                                    if (feePerTile < 1)
                                        feePerTile = 1;
                                    //monitor.Log($"Stack: {batch.Stack} Stack value: {feePerTile * batch.Stack} Stack Fee: {feePerTile * batch.Stack}", LogLevel.Warn);
                                    if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerTile * batch.Stack;
                                }

                                this.cropsHarvested += batch.Stack;

                                if (!Game1.newDay)
                                    await Task.Delay(delay * batch.Stack);

                                stack.Stack -= 999;
                                
                            }

                            if (data.RegrowDays >= 0)
                            {
                                int regrowPhase = crop.phaseDays.Count - 2;
                                crop.currentPhase.Value = regrowPhase;
                                crop.dayOfCurrentPhase.Value = 0;
                                crop.fullyGrown.Value = false;
                                crop.updateDrawMath(tile);
                                dirt.crop = crop;
                            }
                            else
                            {
                                dirt.crop = null;
                            }
                        }

                        if (loc.objects.TryGetValue(tile, out var obj) && obj is IndoorPot pot)
                        {
                            var potdirt = pot.hoeDirt?.Value;
                            if (potdirt?.crop == null)
                                continue;

                            var crop = potdirt.crop;
 
                            string seedId = crop.netSeedIndex.Value;
                            if (string.IsNullOrEmpty(seedId) || !Game1.cropData.TryGetValue(seedId, out var data))
                                continue;

                            string id = crop.indexOfHarvest.Value;
                            int amount = GetVirtualCropYield(crop, Game1.player);

                            var key = (id, quality);

                            if (!stackMap.TryGetValue(key, out var stack))
                            {
                                stack = new SObject(id, 0) { Quality = quality };
                                stackMap[key] = stack;
                            }

                            stack.Stack += amount;

                            if (stack.Stack >= 999)
                            {
                                SObject batch = stack.getOne() as SObject;
                                batch.Stack = 999;
                                batch.Quality = stack.Quality;

                                if (Juice)
                                {
                                    SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                    if (processed == null)
                                    {
                                        monitor.Log($"Converter returned null for harvested crop {batch?.ItemId}. Delivering raw batch instead.", LogLevel.Warn);

                                        DeliverProcessedStack(batch, Shipping, Config);

                                        feePerTile = (int)(batch.Price * feePercent / 100);
                                        if (feePerTile < 1)
                                            feePerTile = 1;

                                        if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                            break;

                                        this.totalFeesPaid += feePerTile * batch.Stack;
                                        this.cropsHarvested += batch.Stack;

                                        if (!Game1.newDay)
                                            await Task.Delay(delay * batch.Stack);

                                        stack.Stack -= 999;
                                        continue;
                                    }

                                    processed.Stack = batch.Stack;
                                    processed.Quality = batch.Quality;

                                    DeliverProcessedStack(processed, Shipping, Config);

                                    feePerTile = (int)(processed.Price * feePercent / 100);
                                    if (feePerTile < 1)
                                        feePerTile = 1;
                                    //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {feePerTile * batch.Stack}", LogLevel.Warn);
                                    if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerTile * batch.Stack;
                                    this.cropsHarvested += batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                }

                                else
                                {
                                    DeliverProcessedStack(batch, Shipping, Config);

                                    feePerTile = (int)(batch.Price * feePercent / 100);
                                    if (feePerTile < 1)
                                        feePerTile = 1;
                                    //monitor.Log($"Stack: {batch.Stack} Stack value: {feePerTile * batch.Stack} Stack Fee: {feePerTile * batch.Stack}", LogLevel.Warn);
                                    if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerTile * batch.Stack;
                                }

                                this.cropsHarvested += batch.Stack;

                                if (!Game1.newDay)
                                    await Task.Delay(delay * batch.Stack);

                                stack.Stack -= 999;
                            }

                            if (data.RegrowDays >= 0)
                            {
                                int regrowPhase = crop.phaseDays.Count - 2;
                                crop.currentPhase.Value = regrowPhase;
                                crop.dayOfCurrentPhase.Value = 0;
                                crop.fullyGrown.Value = false;
                                crop.updateDrawMath(tile);
                                potdirt.crop = crop;
                            }
                            else
                            {
                                potdirt.crop = null;
                            }
                        }

                        if (!Game1.newDay)
                            await Task.Delay(delay);
                    }

                    foreach (var stack in stackMap.Values)
                    {
                        if (stack.Stack > 0)
                        {
                            SObject batch = stack.getOne() as SObject;
                            batch.Stack = stack.Stack;
                            batch.Quality = stack.Quality;

                            if (Juice)
                            {
                                SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                if (processed == null)
                                {
                                    monitor.Log($"Converter returned null for harvested crop {batch?.ItemId}. Delivering raw batch instead.", LogLevel.Warn);

                                    DeliverProcessedStack(batch, Shipping, Config);

                                    feePerTile = (int)(batch.Price * feePercent / 100);
                                    if (feePerTile < 1)
                                        feePerTile = 1;

                                    if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerTile * batch.Stack;
                                    this.cropsHarvested += batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                    continue;
                                }

                                processed.Stack = batch.Stack;
                                processed.Quality = batch.Quality;

                                DeliverProcessedStack(processed, Shipping, Config);

                                feePerTile = (int)(processed.Price * feePercent / 100);
                                if (feePerTile < 1)
                                    feePerTile = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {feePerTile * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                    break;

                                this.totalFeesPaid += feePerTile * batch.Stack;
                                this.cropsHarvested += batch.Stack;

                                if (!Game1.newDay)
                                    await Task.Delay(delay * batch.Stack);

                                stack.Stack -= 999;
                            }

                            else
                            {
                                DeliverProcessedStack(batch, Shipping, Config);

                                feePerTile = (int)(batch.Price * feePercent / 100);
                                if (feePerTile < 1)
                                    feePerTile = 1;

                                //monitor.Log($"Stack: {batch.Stack} Stack value: {batch.Price * batch.Stack} Stack Fee: {feePerTile * batch.Stack}",LogLevel.Warn);

                                if (!TryChargeFeeOrStopSimple(feePerTile * batch.Stack, this.harvestingnpcName, monitor))
                                    break;

                                this.totalFeesPaid += feePerTile * batch.Stack;
                            }

                            this.cropsHarvested += batch.Stack;

                            if (!Game1.newDay)
                                await Task.Delay(delay * batch.Stack);
                        }
                    }

                    this.friendshipPointsEarned = (int)(this.cropsHarvested / 100);

                    if (this.friendshipPointsEarned > 0)
                    {
                        var npc = Game1.getCharacterFromName(this.harvestingnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.harvestingnpcName, points = this.friendshipPointsEarned }));
                    }

                    Game1.showGlobalMessage(T("HarvestFinalMessage", new { Name = this.harvestingnpcName, Count = this.cropsHarvested, Fee = this.totalFeesPaid }));
                });
        }

        private void DeliverProcessedStack(SObject processed, bool shipping, ModConfig config)
        {
            var delivery = new ContractsDelivery
            {
                Items = new List<Item> { processed },
                RecipientID = Game1.player.UniqueMultiplayerID
            };

            if (shipping)
                ShipContractItems(new List<ContractsDelivery> { delivery }, config);
            else
                DeliverContractsItems(new List<ContractsDelivery> { delivery }, config);
        }
    }
}
