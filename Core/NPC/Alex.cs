
using StardewModdingAPI;
using StardewValley;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.CollectionHelpers;

namespace CommunityContracts.Core.NPC
{
    public class AlexProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public string QualityName { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public int PrimaryProcessors { get; set; } = 0;
        public int SecondaryProcessors { get; set; } = 0;
        public int NPCLevel { get; set; } = 0;
        public string CharacterName { get; set; } = "Alex";
        public int FarmerSkillLevel { get; set; } = 0;
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }
        public int ContractPercent { get; set; } = 0;

        private readonly IMonitor Monitor;

        public static class AlexContract
        {
            private static AlexProfile profile = new AlexProfile();
            public static async void OfferDetailedContract()
            {
                profile.ContractPercent = GetContractPercent("Custom");

                float FriendshipAdd = profile.ContractPercent / 10;
                string processingLine = "";

                string dialogText =
                    T("AlexOfferContractOre", new { npc = profile.CharacterName }) + "\n\n" +
                    processingLine + "\n\n" +
                    T("ContractPayPercent", new { percent = profile.ContractPercent }) + "\n\n" +
                    T("ContractAcceptPrompt");

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    new Response[]
                    {
                        new Response("Yes", T("ResponseYes")),
                        new Response("No", T("ResponseNo"))
                    },
                    async (farmer, answer) =>
                    {
                        if (answer == "Yes")
                        {
                            Game1.showGlobalMessage(T("NPCContractAccepted", new { npc = profile.CharacterName }));

                            StardewValley.NPC Alex = Game1.getCharacterFromName(profile.CharacterName);

                            if (Alex != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, Alex);
                                Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = profile.CharacterName, points = (int)FriendshipAdd }));
                            }

                            foreach (var pair in GetMinerGiftQuantities())
                            {
                                var raw = ItemRegistry.Create($"{pair.Key}") as SObject;

                                var shipment = await GenerateProductShipmentWithDelay2
                                (
                                    profile.CharacterName,
                                    0,
                                    0,
                                    raw,
                                    pair.Value,
                                    "",
                                    "Custom",
                                    false,
                                    profile.FarmerSkillLevel
                                );

                                DeliverContractsItems(new List<ContractsDelivery>
                                {
                                    new ContractsDelivery
                                    {
                                        Items = shipment.Values.Cast<Item>().ToList(),
                                        RecipientID = Game1.player.UniqueMultiplayerID
                                    }
                                }, Config);
                            }
                        }
                        else if (answer == "No")
                        {
                            Game1.showGlobalMessage(T("MaybeLater"));
                        }
                    });
            }

            public static Dictionary<int, int> GetMinerGiftQuantities()
            {
                int mineDepth = Game1.player.deepestMineLevel;
                int CopperQty = Math.Max(0, mineDepth);
                int IronQty = Math.Max(0, mineDepth - 40);
                int GoldQty = Math.Max(0, mineDepth - 80);
                int IridiumQty = Math.Max(0, mineDepth - 105);

                int CoalQty = (mineDepth / 2);

                return new Dictionary<int, int>
                {
                    { 378, CopperQty },
                    { 380, IronQty },
                    { 382, CoalQty },
                    { 384, GoldQty },
                    { 386, IridiumQty }
                };
            }
            public static void AlexIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.FarmerSkillLevel = Game1.player.miningLevel.Value;
                profile.PrimaryProcessors = CountProcessors(13);
                profile.SecondaryProcessors = CountNamedProcessors("HeavyFurnace");
                profile.ProcessorsOperated = profile.PrimaryProcessors + (profile.SecondaryProcessors * 5);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 }
 			    };

                var seasonalOptions = SeasonalCollect[profile.SeasonIndex]
                    .Concat(SeasonalCollect[profile.SeasonIndex])
                    .ToList();

                Random rng = new Random();
                profile.SelectedItemID = seasonalOptions[rng.Next(seasonalOptions.Count)];
                profile.BaseItem = ItemRegistry.Create(profile.SelectedItemID.ToString()) as SObject;
                profile.PreparedItemName = GetItemName(profile.SelectedItemID.ToString());
                profile.Quantity = SafeMultiplier(profile.FarmerSkillLevel) * SafeMultiplier(profile.NPCLevel);

                string dialogText =
                    T("AlexAskContractOre", new { npc = profile.CharacterName });

                var responses = new List<Response>
                {
                    new Response("Accept", T("ResponseAccept")),
                    new Response("Decline", T("ResponseDecline")),
                };

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    responses.ToArray(),
                    (farmer, answer) =>
                    {
                        if (answer == "Accept")
                        {
                            Game1.delayedActions.Add(new DelayedAction(100, () =>
                            {
                                OfferDetailedContract();
                            }));
                        }

                        else if (answer == "Decline")
                        {
                            Game1.showGlobalMessage(T("MaybeLater"));
                        }
                    }
                );
            }
        }
    }
}