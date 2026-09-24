
using StardewModdingAPI;
using StardewValley;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.CollectionHelpers;

namespace CommunityContracts.Core.NPC
{
    public class EvelynProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public string QualityName { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public int NPCLevel { get; set; } = 0;
        public string CharacterName { get; set; } = "Evelyn";
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }
        public int ContractPercent { get; set; } = 0;

        private readonly IMonitor Monitor;

        public static class EvelynContract
        {
            private static EvelynProfile profile = new EvelynProfile();
            public static async void OfferDetailedContract()
            {
                profile.ContractPercent = GetContractPercent("Custom");

                float FriendshipAdd = profile.ContractPercent / 10;
                string processingLine = "";

                if (profile.ProcessorsOperated > 0)
                {
                    processingLine =
                        T("EvelynPreparedCloth", new { count = profile.ProcessorsOperated * (1 + profile.Quality), quality = profile.QualityName }) + "\n\n" +
                        T("EvelynPackShipment");
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

                            StardewValley.NPC Evelyn = Game1.getCharacterFromName(profile.CharacterName);

                            if (Evelyn != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, Evelyn);
                                Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = profile.CharacterName, points = (int)FriendshipAdd }));
                            }

                            var shipment = await GenerateProductShipmentWithDelay2
                            (
                                profile.CharacterName,
                                0,
                                profile.ProcessorsOperated * (1 + profile.Quality),
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

            public static void EvelynIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.ProcessorsOperated = CountProcessors(17);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.FarmerSkillLevel = Game1.player.farmingLevel.Value;

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 440 },
	                new int[] { 440 },
	                new int[] { 440 },
	                new int[] { 440 }
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
                    T("EvelynAskContractRabbits", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

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