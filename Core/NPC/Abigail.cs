
using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.CollectionHelpers;
using static ModEntry;

using SObject = StardewValley.Object;

namespace CommunityContracts.Core.NPC
{
    public class AbigailProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public string QualityName { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public int NPCLevel { get; set; } = 0;
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }
        public string CharacterName { get; set; } = "Abigail";
        public int ContractPercent { get; set; } = 0;

        private readonly IMonitor Monitor;

        public static class AbigailContract
        {
            private static AbigailProfile profile = new AbigailProfile();
            public static async void OfferDetailedContract()
            {
                profile.ContractPercent = GetContractPercent("Custom");

                float FriendshipAdd = profile.ContractPercent / 10;
                string processingLine = "";

                if (profile.ProcessorsOperated > 0)
                {
                    processingLine =
                        T("AbigailExtractedTruffleOil", new { count = profile.ProcessorsOperated, quality = profile.QualityName }) + "\n\n" +
                        T("AbigailPackShipment");
                }

                string dialogText =
                    T("ContractOffer", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
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

                            StardewValley.NPC Abigail = Game1.getCharacterFromName(profile.CharacterName);

                            if (Abigail != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, Abigail);
                                Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = profile.CharacterName, points = (int)FriendshipAdd }));
                            }

                            var shipment = await GenerateProductShipmentWithDelay2
                            (
                                profile.CharacterName,
                                profile.Quality,
                                profile.ProcessorsOperated,
                                profile.BaseItem,
                                profile.Quantity,
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
                        else if (answer == "No")
                        {
                            Game1.showGlobalMessage(T("MaybeLater"));
                        }
                    });
            }

            public static void AbigailIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.ProcessorsOperated = CountProcessors(19);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.FarmerSkillLevel = Game1.player.farmingLevel.Value;

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 430 },
	                new int[] { 430 },
	                new int[] { 430 },
	                new int[] { 430 }
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
                    T("AbigailContractOfferPigs", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

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