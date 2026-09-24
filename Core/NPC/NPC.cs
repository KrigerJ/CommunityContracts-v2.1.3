
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.NPC
{
    public class NPCProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public string QualityName { get; set; }
        public string NPCName { get; private set; }
        public int NPCLevel { get; set; } = 0;
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public string RecycledItemName { get; set; }
        public int SeasonIndex { get; set; }
        public int ContractPercent { get; set; } = 0;

        private readonly IMonitor Monitor;
 
        public static class NPCContract
        {
            private static NPCProfile profile = new NPCProfile();
            public static async void OfferDetailedContract()
            {
                profile.ContractPercent = GetContractPercent("Basic");

                float FriendshipAdd = profile.ContractPercent / 10;
                string processingLine = "";

                    processingLine =
                        T("NPCRecycleItems", new { count = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
                        T("NPCPackShipment");

                string dialogText =
                    T("NPCOfferContract", new { npc = profile.NPCName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
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
                            Game1.showGlobalMessage(T("NPCContractAccepted", new { npc = profile.NPCName }));

                            StardewValley.NPC NPC = Game1.getCharacterFromName(profile.NPCName);

                            if (NPC != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, NPC);
                                Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = profile.NPCName, points = (int)FriendshipAdd }));
                            }

                            var shipment = await GenerateProductShipmentWithDelay2
                            (
                                profile.NPCName,
                                profile.Quality,
                                profile.Quantity,
                                profile.BaseItem,
                                profile.Quantity,
                                "",
                                "Basic",
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

            public static void NPCIntroduction(string NewNPCName)
            {
                profile.NPCName = NewNPCName;
                profile.NPCLevel = UpdateNPCLevel(NewNPCName);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.FarmerSkillLevel = Game1.player.farmingLevel.Value + Game1.player.fishingLevel.Value + Game1.player.miningLevel.Value + Game1.player.foragingLevel.Value + Game1.player.combatLevel.Value; // Player Skill Level

                int[] selectableCategories = new[]
                {
                    -4,  // Fish
                    -5,  // Eggs
                    -6,  // Milk
                    -27, // Syrup
                    -75, // Vegetables
                    -79, // Fruit
                    -80, // Flowers
                    -81, // Forage/Mushrooms
                };

                Random rng = Game1.random;

                int chosenCategory = selectableCategories[rng.Next(selectableCategories.Length)];

                var allItems = GetAllObjectsInCategory(chosenCategory)
                    .Where(item =>
                        item != null &&
                        !item.bigCraftable.Value &&
                        item.Category == chosenCategory &&
                        !(item is Tool) &&
                        !(item is Furniture) &&
                        !(item is StardewValley.Tools.MeleeWeapon))
                    .ToList();

                var seasonalItems = FilterBySeason(allItems, profile.SeasonIndex);

                if (seasonalItems.Count > 0)
                {
                    var selected = seasonalItems[rng.Next(seasonalItems.Count)];
                    profile.SelectedItemID = selected.ParentSheetIndex;
                }

                profile.BaseItem = ItemRegistry.Create(profile.SelectedItemID.ToString()) as SObject;
                profile.Quantity = SafeMultiplier(profile.FarmerSkillLevel) + SafeMultiplier(profile.NPCLevel);
                profile.PreparedItemName = GetItemName(profile.SelectedItemID.ToString());

                string dialogText =
                    T("NPCAskContract", new { npc = profile.NPCName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

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