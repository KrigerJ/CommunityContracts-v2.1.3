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
    public class RockCandyService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string rockCandynpcName;
        public int RockCandyMade = 0;
        public readonly CollectionServiceManager manager;
        public bool Shipping = false;
        public int remainder = 0;
        public RockCandyService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void RunRockCandyService(IMonitor monitor, string RockCandynpcName)
        {
            this.monitor = monitor;
            this.rockCandynpcName = RockCandynpcName;
            var npc = Game1.getCharacterFromName(this.rockCandynpcName);
            this.friendshipPointsEarned = 0;
            this.RockCandyMade = 0;
            this.totalFeesPaid = 0;
            this.remainder = 0;

            int npcLevel = UpdateNPCLevel(this.rockCandynpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.miningLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePerItem = Config.SeviceContractFees[ServiceId.RockCandy];

            string dialogText =
                T("RockCandyOffer", new { Name = this.rockCandynpcName, Fee = feePerItem }) + "\n\n" +
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
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.rockCandynpcName));
                    Game1.showGlobalMessage($"{this.rockCandynpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> stackMap = new();

                    Chest locker = GetProcessingLocker();
                    if (locker == null)
                        return;

                    var items = locker.Items.OfType<SObject>().Where(o => o.Stack > 0).ToList();
                    if (items.Count == 0)
                        return;

                    int totalValue = items.Sum(i =>
                    {
                        if (i.ItemId == "279") // Prevent duplication exploit
                            return 5000 * i.Stack;

                        float mult = GetQualityMultiplier(i.Quality);
                        return (int)(i.Price * mult) * i.Stack;
                    });

                    int candyCount = totalValue / 5000;
                    if (candyCount <= 0)
                        return;

                    this.remainder = totalValue % 5000;
                    this.friendshipPointsEarned += this.remainder / 150;

                    int quality = GetQuality(npcLevel);
                    int remaining = candyCount;

                    while (remaining > 0)
                    {
                        int batchSize = Math.Min(999, remaining);

                        SObject batch = ItemRegistry.Create("279") as SObject;
                        batch.Stack = batchSize;
                        batch.Quality = quality;

                        if (!Game1.newDay)
                            await Task.Delay(delay * batchSize);

                        DeliverProcessedStack(batch, Shipping, Config);

                        int fee = feePerItem * batchSize;
                        if (!TryChargeFeeOrStopSimple(fee, this.rockCandynpcName, monitor))
                            break;

                        this.totalFeesPaid += fee;
                        this.RockCandyMade += batchSize;

                        remaining -= batchSize;
                    }

                    locker.Items.Clear();

                    FinalizeRockCandyService();
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

        private void FinalizeRockCandyService()
        {
            if (this.friendshipPointsEarned > 0)
            {
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.rockCandynpcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("RockCandyFinalMessage", new { Name = this.rockCandynpcName, Count = this.RockCandyMade, Tip = this.remainder }));
        }
    }
}
