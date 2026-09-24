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
    public class ProcessingService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string ProcessingnpcName;
        public int ItemsProcessed = 0;
        public readonly CollectionServiceManager manager;
        public bool Shipping = false;
        public ProcessingService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }

        public void RunProcessingService(IMonitor monitor, string processingnpcName)
        {
            this.monitor = monitor;
            this.ProcessingnpcName = processingnpcName;
            var npc = Game1.getCharacterFromName(this.ProcessingnpcName);
            this.friendshipPointsEarned = 0;
            this.ItemsProcessed = 0;
            this.totalFeesPaid = 0;

            int npcLevel = UpdateNPCLevel(this.ProcessingnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePercent = Config.SeviceContractFees[ServiceId.Processing];
            int feePerItem = 0;

            string dialogText =
                T("ProcessingOffer", new { Name = this.ProcessingnpcName, Fee = feePercent }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseYesDeliver")),
                    new Response("Ship", T("ResponseShip")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    if (answer == "Ship")
                    {
                        Shipping = true;
                    }
                    else
                    {
                        Shipping = false;
                    }

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.ProcessingnpcName));
                    Game1.showGlobalMessage($"{this.ProcessingnpcName} " + T("FriendshipInitial"));

                    while (true)
                    {
                        SObject raw = ExtractNextItemFromAutograbbers();
                        bool fromChest = false;

                        if (raw == null)
                        {
                            raw = GetRawGoodsFromLocker("Processing");
                            fromChest = raw != null;
                        }

                        if (raw == null)
                            break;

                        int stackCount = raw.Stack;
                        int rawQuality = raw.Quality;

                        SObject rawStack = raw.getOne() as SObject;
                        rawStack.Stack = stackCount;
                        rawStack.Quality = rawQuality;

                        ProcessingRegistry.ProcessingMode = "";
                        SObject processed = ProcessingConverter.ConvertRawToProcessed(rawStack);

                        if (processed == null)
                        {
                            monitor.Log($"Converter returned null for raw item {rawStack?.ItemId}. Delivering raw item instead.", LogLevel.Warn);

                            DeliverProcessedStack(rawStack, Shipping, Config);

                            feePerItem = (int)(rawStack.Price * feePercent / 100);
                            if (feePerItem < 1)
                                feePerItem = 1;

                            if (!TryChargeFeeOrStopSimple(feePerItem * rawStack.Stack, this.ProcessingnpcName, monitor))
                                break;

                            this.totalFeesPaid += feePerItem * rawStack.Stack;
                            this.ItemsProcessed += rawStack.Stack;

                            if (fromChest)
                                Consume(raw, stackCount);

                            if (!Game1.newDay)
                                await Task.Delay(delay * rawStack.Stack);

                            continue;
                        }

                        if (IsOre(raw))
                        {
                            processed.Stack = (int)(stackCount / 5.0) + 1;
                        }
                        else
                        {
                            processed.Stack = stackCount;
                        }

                        processed.Quality = rawQuality;

                        DeliverProcessedStack(processed, Shipping, Config);

                        this.ItemsProcessed += stackCount;

                        feePerItem = (int)(feePercent * processed.Price / 100);
                        if (feePerItem < 1)
                            feePerItem = 1;
                        //monitor.Log($"Stack: {stackCount} Stack value: {processed.Price * stackCount} Stack Fee: {feePerItem * stackCount}", LogLevel.Warn);
                        if (!TryChargeFeeOrStopSimple(feePerItem * stackCount, this.ProcessingnpcName, monitor))
                            break;

                        this.totalFeesPaid += feePerItem * stackCount;

                        if (fromChest)
                            Consume(raw, stackCount);

                        if (!Game1.newDay)
                            await Task.Delay(delay * stackCount);
                    }
                    FinalizeProcessingService();
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

        private void FinalizeProcessingService()
        {
            this.friendshipPointsEarned = (int)(this.ItemsProcessed / 100);

            if (this.friendshipPointsEarned > 0)
            {
                var npc = Game1.getCharacterFromName(this.ProcessingnpcName);
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.ProcessingnpcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("ProcessingFinalMessage", new { Name = this.ProcessingnpcName, Count = this.ItemsProcessed}));
        }

        private SObject ExtractNextItemFromAutograbbers()
        {
            var farm = Game1.getFarm();

            foreach (var building in farm.buildings)
            {
                var indoors = building.indoors.Value;
                if (indoors == null)
                    continue;

                foreach (var pair in indoors.Objects.Pairs)
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        obj.ParentSheetIndex == 165)
                    {
                        if (obj.heldObject.Value is Chest chest)
                        {
                            var item = chest.Items
                                .OfType<SObject>()
                                .FirstOrDefault();

                            if (item != null)
                            {
                                chest.Items.Remove(item);
                                return item;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
