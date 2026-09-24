using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class CrabPotCatchService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        public int baitSet = 0;
        private StardewValley.NPC npc;
        private string crabCollectnpcName;
        public int crabPotsHarvested = 0;
        public bool Shipping = false;
        public bool Sashimi = false;

        public readonly CollectionServiceManager manager;
        public CrabPotCatchService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }

        public void OfferCrabPotService(IMonitor monitor, string CrabCollectnpcName)
        {
            this.monitor = monitor;
            this.crabCollectnpcName = CrabCollectnpcName;
            var npc = Game1.getCharacterFromName(this.crabCollectnpcName);
            int npcLevel = UpdateNPCLevel(this.crabCollectnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.fishingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            this.friendshipPointsEarned = 0;
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.CrabPots)
               ? SpecialtyNames[ServiceId.CrabPots]
               : T("ServiceWeeds");
            int friendshipCounter = 0;
            this.baitSet = 0;
            this.totalFeesPaid = 0;
            this.crabPotsHarvested = 0;
            int feePercent = Config.SeviceContractFees[ServiceId.Processing];


            var pots = ScanCrabPots();

            if (pots.Count == 0)
            {
                Game1.showGlobalMessage(T("NoCrabPotsReady"));
                return;
            }

            int feePerPot = Config.SeviceContractFees[ServiceId.CrabPots];
            int baitPurchaseFee = Config.CraftablFee["Bait"];

            string dialogText =
                T("ServiceOffer", new { npc = this.crabCollectnpcName, quantity = pots.Count, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeCrabPotCollect", new { Fee = feePercent }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseRawCrabYes")),
                    new Response("Deliver", T("ResponseDeliverSashimi")),
                    new Response("Ship", T("ResponseShipSashimi")),
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
                        Sashimi = true;
                    }

                    if (answer == "Ship")
                    {
                        Sashimi = true;
                        Shipping = true;
                    }
                    else
                    {
                        Shipping = false;
                    }

                    this.friendshipPointsEarned = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.crabCollectnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> stackMap = new();

                    for (int i = 0; i < pots.Count; i++)
                    {
                        var (loc, tile, pot) = pots[i];

                        var held = pot.heldObject.Value;
                        if (held is not SObject catchObj)
                            continue;

                        int quality = GetQuality(npcLevel);
                        int quantity = 1;

                        var key = (catchObj.ItemId, quality);

                        if (catchObj.Category == -20)
                        {
                            quantity = quality + 1;
                            quality = 0;
                        }

                        if (!stackMap.TryGetValue(key, out var stack))
                        {
                            stack = catchObj.getOne() as SObject;
                            stack.Stack = 0;
                            stack.Quality = quality;
                            stackMap[key] = stack;
                        }

                        stack.Stack += quantity;

                        if (stack.Stack >= 999)
                        {
                            SObject batch = stack.getOne() as SObject;
                            batch.Stack = 999;

                            if (Sashimi)
                            {
                                SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                if (processed == null)
                                {
                                    monitor.Log(
                                        $"Converter returned null for crab pot item {batch?.ItemId}. Delivering raw batch instead.",
                                        LogLevel.Warn
                                    );

                                    DeliverProcessedStack(batch, Shipping, Config);

                                    feePerPot = (batch.Price * feePercent) / 100;
                                    if (feePerPot < 1)
                                        feePerPot = 1;

                                    if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerPot * batch.Stack;
                                    this.crabPotsHarvested += batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                    continue;
                                }

                                processed.Stack = batch.Stack;
                                processed.Quality = batch.Quality;
                                
                                DeliverProcessedStack(processed, Shipping, Config);

                                feePerPot = feePercent * (int)(processed.Price / 100);
                                if (feePerPot < 1)
                                    feePerPot = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {feePerPot * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                    break;
                                
                            }
                            else
                            {
                                DeliverProcessedStack(batch, Shipping, Config);

                                feePerPot = feePercent * (int)(batch.Price / 100);
                                if (feePerPot < 1)
                                    feePerPot = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {batch.Price * batch.Stack} Stack Fee: {feePerPot * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                    break;
                            }

                            this.totalFeesPaid += feePerPot * batch.Stack;
                            this.crabPotsHarvested += stack.Stack;

                            if (!Game1.newDay)
                                await Task.Delay(delay * stack.Stack);

                            stack.Stack -= 999;
                        }

                        pot.heldObject.Value = null;
                        pot.readyForHarvest.Value = false;

                        if (!Game1.player.professions.Contains(11))
                        {
                            SObject? baitItem = TakeBaitFromLocker();

                            int feeThisPot = Config.SeviceContractFees[ServiceId.BaitCrabPots];

                            if (baitItem == null)
                            {
                                feeThisPot = baitPurchaseFee + Config.SeviceContractFees[ServiceId.BaitCrabPots];

                                if (!TryChargeFeeOrStopSimple(feeThisPot, this.crabCollectnpcName, monitor))
                                {
                                    break;
                                }

                                baitItem = new SObject("685", 1);
                                this.totalFeesPaid += feeThisPot;
                                this.baitSet++;
                            }

                            bool accepted = pot.performObjectDropInAction(baitItem, false, Game1.player);

                            if (!accepted)
                                pot.bait.Value = baitItem;

                            this.baitSet++;
                        }
                    }

                    foreach (var stack in stackMap.Values)
                    {
                        if (stack.Stack > 0)
                        {
                            SObject batch = stack.getOne() as SObject;
                            batch.Stack = stack.Stack;


                            if (Sashimi)
                            {
                                SObject processed = ProcessingConverter.ConvertRawToProcessed(batch);

                                if (processed == null)
                                {
                                    monitor.Log($"Converter returned null for crab pot item {batch?.ItemId}. Delivering raw batch instead.", LogLevel.Warn);

                                    DeliverProcessedStack(batch, Shipping, Config);

                                    feePerPot = (batch.Price * feePercent) / 100;
                                    if (feePerPot < 1)
                                        feePerPot = 1;

                                    if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                        break;

                                    this.totalFeesPaid += feePerPot * batch.Stack;
                                    this.crabPotsHarvested += batch.Stack;

                                    if (!Game1.newDay)
                                        await Task.Delay(delay * batch.Stack);

                                    stack.Stack -= 999;
                                    continue;
                                }

                                processed.Stack = batch.Stack;
                                processed.Quality = batch.Quality;
                                
                                DeliverProcessedStack(processed, Shipping, Config);

                                feePerPot = feePercent * (int)(processed.Price / 100);
                                if (feePerPot < 1)
                                    feePerPot = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {processed.Price * batch.Stack} Stack Fee: {feePerPot * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                    break;
                            }
                            else
                            {
                                DeliverProcessedStack(batch, Shipping, Config);

                                int feePerPot = feePercent * (int)(batch.Price / 100);
                                if (feePerPot < 1)
                                    feePerPot = 1;
                                //monitor.Log($"Stack: {batch.Stack} Stack value: {batch.Price * batch.Stack} Stack Fee: {feePerPot * batch.Stack}", LogLevel.Warn);
                                if (!TryChargeFeeOrStopSimple(feePerPot * batch.Stack, this.crabCollectnpcName, monitor))
                                    break;
                            }

                            this.totalFeesPaid += feePerPot * batch.Stack;
                            this.crabPotsHarvested += stack.Stack;

                            if (!Game1.newDay)
                                await Task.Delay(delay * stack.Stack);
                        }
                    }

                    this.friendshipPointsEarned = (int)(this.crabPotsHarvested / 100);

                    if (this.friendshipPointsEarned > 0)
                    {
                        var npc = Game1.getCharacterFromName(this.crabCollectnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.crabCollectnpcName, points = this.friendshipPointsEarned }));
                    }

                    Game1.showGlobalMessage(T("CrabPotFinalMessage", new { count = this.crabPotsHarvested, npc = this.crabCollectnpcName, Fee = this.totalFeesPaid }));
                });

            SObject? TakeBaitFromLocker()
            {
                SObject bait = GetRawGoodsFromLocker("Bait");

                if (bait != null)
                {
                    bait.Stack--;

                    if (bait.Stack <= 0)
                    {
                        Chest locker = GetProcessingLocker();
                        locker.Items.Remove(bait);
                    }

                    return new SObject("685", 1);
                }

                return null;
            }
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
