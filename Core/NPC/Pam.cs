
using StardewModdingAPI;
using StardewValley;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.CollectionHelpers;

namespace CommunityContracts.Core.NPC
{
    public class PamProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public string QualityName { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public int NPCLevel { get; set; } = 0;
        public string CharacterName { get; set; } = "Pam";
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }
        public int ContractPercent { get; set; } = 0;

        private readonly IMonitor Monitor;

        public static class PamContract
        {
            private static PamProfile profile = new PamProfile();
            public static async void OfferDetailedContract()
            {
                profile.ContractPercent = GetContractPercent("Custom");

                float FriendshipAdd = profile.ContractPercent / 10;
                string processingLine = "";

                if (profile.ProcessorsOperated > 0)
                {
                    processingLine =
                        T("PamFermentedMead", new { count = profile.ProcessorsOperated, quality = profile.QualityName }) + "\n\n" +
                        T("PamPackShipment");
                }

                string dialogText =
                    T("PamOfferContractJars", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
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

                            StardewValley.NPC Pam = Game1.getCharacterFromName(profile.CharacterName);

                            if (Pam != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, Pam);
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
                                    Items = shipment.Values.Cast < Item >().ToList(),
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

            public static void PamIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.ProcessorsOperated = CountProcessors(12);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.FarmerSkillLevel = Game1.player.farmingLevel.Value;

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 340 },
                    new int[] { 340 },
                    new int[] { 340 },
                    new int[] { 340 }
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
                    T("PamAskContractCollected", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

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