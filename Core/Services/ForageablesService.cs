using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class ForageablesService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string forageablesName;
        public int forageablesCollected = 0;
        public readonly CollectionServiceManager manager;
        public bool Shipping = false;
        public bool Juice = false;
        public ForageablesService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferForageablesService(IMonitor monitor, string ForageablesName)
        {
            this.monitor = monitor;
            this.forageablesName = ForageablesName;
            var npc = Game1.getCharacterFromName(this.forageablesName);
            this.friendshipPointsEarned = 0;
            this.forageablesCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(this.forageablesName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePercent = Config.SeviceContractFees[ServiceId.Processing];

            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Forageables)
               ? SpecialtyNames[ServiceId.Forageables]
               : T("ServiceWeeds");

            var sources = ScanForageableSources();

            if (sources.Count == 0)
            {
                Game1.showGlobalMessage(T("NoForageablesReady"));
                return;
            }

            int itemsToCollect = sources.Count;

            string dialogText =
                T("ServiceOffer", new { npc = this.forageablesName, quantity = itemsToCollect, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeForageablesCollect", new { Fee = feePercent }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseRawGoodsYes")),
                    new Response("Deliver", T("ResponseDeliverPickles")),
                    new Response("Ship", T("ResponseShipPickles")),
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

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.forageablesName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> stackMap = new();

                    for (int i = 0; i < itemsToCollect; i++)
                    {
                        var (loc, tile, item, fromProducer) = sources[i];

                        string id = item.ItemId;
                        int quantity = 1;
                        var key = (id, quality);

                        if (!stackMap.TryGetValue(key, out var stack))
                        {
                            stack = new SObject(id, 0) { Quality = quality };
                            stackMap[key] = stack;
                        }

                        stack.Stack += quantity;
                        this.forageablesCollected += stack.Stack;

                        if (stack.Stack >= 999)
                        {
                            SObject batch = stack.getOne() as SObject;
                            batch.Stack = 999;
                            batch.Quality = quality;

                            if (Juice)
                            {
                                SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                if (processed == null)
                                {
                                    monitor.Log($"Converter returned null for forageable {batch?.ItemId}. Delivering raw batch.", LogLevel.Warn);

                                    DeliverProcessedStack(batch, Shipping, Config);

                                    int fee = (int)(batch.Price * feePercent / 100);
                                    if (fee < 1) fee = 1;

                                    if (!TryChargeFeeOrStopSimple(fee * batch.Stack, this.forageablesName, monitor))
                                        break;

                                    this.totalFeesPaid += fee * batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                    continue;
                                }

                                processed.Stack = batch.Stack;
                                processed.Quality = quality;

                                DeliverProcessedStack(processed, Shipping, Config);

                                int fee2 = (int)(processed.Price * feePercent / 100);
                                if (fee2 < 1) fee2 = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {fee2 * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(fee2 * batch.Stack, this.forageablesName, monitor))
                                    break;

                                this.totalFeesPaid += fee2 * batch.Stack;
                            }
                            else
                            {
                                DeliverProcessedStack(batch, Shipping, Config);

                                int fee = (int)(batch.Price * feePercent / 100);
                                if (fee < 1) fee = 1;

                                if (!TryChargeFeeOrStopSimple(fee * batch.Stack, this.forageablesName, monitor))
                                    break;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {batch.Price * batch.Stack} Stack Fee: {fee * batch.Stack}", LogLevel.Warn);
                                this.totalFeesPaid += fee * batch.Stack;
                            }

                            if (!Game1.newDay)
                                await Task.Delay(delay * batch.Stack);

                            stack.Stack -= 999;
                        }

                        if (fromProducer)
                        {
                            if (loc.Objects.TryGetValue(tile, out var prod) &&
                                prod is SObject box &&
                                box.heldObject.Value is not null)
                            {
                                box.heldObject.Value = null;
                            }
                        }
                        else if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                                 feature is Tree tree &&
                                 tree.hasMoss.Value)
                        {
                            tree.performUseAction(tile);
                        }
                        else
                        {
                            loc.Objects.Remove(tile);
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
                            batch.Quality = quality;

                            if (Juice)
                            {
                                SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                if (processed == null)
                                {
                                    monitor.Log($"Converter returned null for forageable {batch?.ItemId}. Delivering raw batch.", LogLevel.Warn);

                                    DeliverProcessedStack(batch, Shipping, Config);

                                    int fee = (int)(batch.Price * feePercent / 100);
                                    if (fee < 1) fee = 1;

                                    if (!TryChargeFeeOrStopSimple(fee * batch.Stack, this.forageablesName, monitor))
                                        break;

                                    this.totalFeesPaid += fee * batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    continue;
                                }

                                processed.Stack = batch.Stack;
                                processed.Quality = batch.Quality;

                                DeliverProcessedStack(processed, Shipping, Config);

                                int fee2 = (int)(processed.Price * feePercent / 100);
                                if (fee2 < 1) fee2 = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {fee2 * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(fee2 * batch.Stack, this.forageablesName, monitor))
                                    break;

                                this.totalFeesPaid += fee2 * batch.Stack;
                            }
                            else
                            {
                                DeliverProcessedStack(batch, Shipping, Config);

                                int fee = (int)(batch.Price * feePercent / 100);
                                if (fee < 1) fee = 1;

                                if (!TryChargeFeeOrStopSimple(fee * batch.Stack, this.forageablesName, monitor))
                                    break;

                                this.totalFeesPaid += fee * batch.Stack;
                            }

                            if (!Game1.newDay)
                                await Task.Delay(delay * batch.Stack);
                        }
                    }

                    this.friendshipPointsEarned = this.forageablesCollected / 100;
                    if (this.friendshipPointsEarned > 0)
                    {
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.forageablesName, points = this.friendshipPointsEarned }));
                    }
                    Game1.showGlobalMessage(T("ForageablesFinalMessage", new { Name = this.forageablesName, Count = this.forageablesCollected, Fee = this.totalFeesPaid }));
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
