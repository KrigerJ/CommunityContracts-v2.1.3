using Microsoft.Xna.Framework;
using StardewModdingAPI;
using static CommunityContracts.Core.NPCServiceMenu;
using static CommunityContracts.Core.Services.WarpMenu;

public class ModConfig
{
    public SButton CheatMenuHotkey { get; set; } = (SButton)86;
    public string DeliveryChestColor { get; set; } = "White";
    public string HighlightColor { get; set; } = "Yellow";
    public string FontColor { get; set; } = "Black";

    public Dictionary<string, int> NPCContractPercents { get; set; } = new()
    {
        { "Basic", 80 },
        { "Custom", 70 },
        { "Processing", 3 },
    };

    public Dictionary<string, int> CraftablFee { get; set; } = new()
    {
        { "GardenPot", 400 },
        { "BeeHouse", 2000 },
        { "Tapper", 900 },
        { "Bait", 5 },
    };

    public Dictionary<ServiceId, int> SeviceContractFees { get; set; } = new()
    {
        { ServiceId.SetCrabPots, 100 },
        { ServiceId.BaitCrabPots, 2 },
        { ServiceId.CrabPots, 3 },
        { ServiceId.Crops, 4 },
        { ServiceId.Forageables, 3 },
        { ServiceId.Hardwood, 5 },
        { ServiceId.Honey, 5 },
        { ServiceId.Stone, 4 },
        { ServiceId.Weeds, 2 },
        { ServiceId.Wood, 4 },
        { ServiceId.Tappers, 3 },
        { ServiceId.Producers, 3 },
        { ServiceId.Till, 5 },
        { ServiceId.Water, 1 },
        { ServiceId.Fertilize, 1 },
        { ServiceId.Seeds, 3 },
        { ServiceId.PlaceInvisiblePots, 100 },
        { ServiceId.PlaceBeeHouse, 100 },
        { ServiceId.SeedMaker, 10 },
        { ServiceId.Animals, 5 },
        { ServiceId.Warp, 5 },
        { ServiceId.RockCandy, 12 },
        { ServiceId.Processing, 2 }
    };

    public Dictionary<string, WarpLocationEntry> WarpLocations { get; set; } = new()
    {
        ["Home"] = new WarpLocationEntry { Map = "Farm", X = 64, Y = 15 },
    };

    public float ConeAngleDegrees { get; set; } = 90f;
    public int ConeRange { get; set; } = 90;
    public int RectangleWidth { get; set; } = 7;
    public int RectangleLength { get; set; } = 5;
    public bool PlaceNearToFar = false;
    public int MenuColumns { get; set; } = 5;
    public int CollectionDelay { get; set; } = 2000;
    public string DropLocationName { get; set; } = "Farm";
    public int DropTileX { get; set; } = 68;
    public int DropTileY { get; set; } = 15;
    public bool BombActive { get; set; } = false;
    public bool EnableProcessTimeReduction = false;
    public bool ReturnDamage { get; set; } = false;
    public bool FriendShield{ get; set; } = false;
    public bool NPCOrderDescending { get; set; } = true;
    public int RetDamgFee { get; set; } = 1;

    public Dictionary<string, Color> ChestColors { get; set; } = new()
    {
        { "White", Color.White },
        { "Sky Blue", Color.LightSkyBlue },
        { "Gold", Color.Gold },
        { "Green", Color.LightGreen },
        { "Purple", Color.MediumPurple },
        { "Orange", Color.Orange },
        { "Pink", Color.Pink }
    };
    public Dictionary<string, Color> HighlightColors { get; set; } = new()
    {
        { "Yellow", Color.Yellow * 0.75f },
        { "Red", Color.Red * 0.6f },
        { "Green", Color.LimeGreen * 0.6f },
        { "Blue", Color.DeepSkyBlue * 0.6f },
        { "White", Color.White * 0.5f },
        { "Purple", Color.MediumPurple * 0.6f }
    };
    public Dictionary<string, Color> FontColors { get; set; } = new()
    {
        { "Black", Color.Black },
        { "White", Color.White },
        { "Yellow", Color.Yellow },
        { "Red", Color.Red },
        { "Green", Color.LimeGreen },
        { "Blue", Color.DeepSkyBlue },
        { "Purple", Color.MediumPurple }
    };

    public bool FuelLockerAssigned { get; set; } = false;
    public string FuelLockerLocation { get; set; } = "";
    public string FuelLockerTile { get; set; } = "";
    public bool ProcessingLockerAssigned { get; set; } = false;
    public string ProcessingLockerLocation { get; set; } = "";
    public string ProcessingLockerTile { get; set; } = "";
    public bool AllowEmptyTilePotPlacement { get; set; } = false;
    public List<string> ExcludedItemIds { get; set; } = new()
    {
        "(O)92", // Sap by default
        "(O)388", // Wood by default
        "(O)390", // Stone by default
        "(O)0", // Weeds by default
        "0", // Weeds by default
    };
}
