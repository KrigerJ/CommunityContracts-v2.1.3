using CommunityContracts.Core;
using CommunityContracts.Core.PowerUps;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.PowerUps.PowerHelpers;
using SObject = StardewValley.Object;

public class ModEntry : Mod
{
    public static ModEntry Instance { get; private set; }

    public static bool IsSelectingTile = false;

    internal static ModConfig Config;

    public static CollectionServiceManager Services;

    public static IMonitor ModMonitor { get; private set; }

    public static Vector2? HighlightedDropTile;

    public static Dictionary<string, int> npcCooldowns = new();

    public static bool ForceShowDropLocationHighlight = false;
    public static bool ForceShowProcessChestHighlight = false;
    public static bool ForceShowFuelChestHighlight = false;

    public static IMonitor monitor;

    public static PowerHelpers FriendPower;

    public static int LastHP;
    public static bool InitializedHP = false;

    public static bool WaitingForFuelLockerSelection = false;
    public static bool WaitingForProcessingLockerSelection = false;

    public static bool FastFishingActive = false;

    public static string T(string key, object tokens = null)
    {
        var translation = Instance.Helper.Translation.Get(key);
        if (tokens != null)
            translation = translation.Tokens(tokens);
        return translation.ToString();
    }

    public static bool ShowPlacementOverlay = false;
    public bool SetPlacementMenuOpen = false;

    public class ContractDelivery
    {
        public List<Item> Items { get; set; }
        public string Source { get; set; }
        public long RecipientID { get; set; }
    }

    private readonly List<ContractDelivery> ContractsDeliveries = new();

    public void QueueContractsDelivery(ContractDelivery delivery)
    {
        if (delivery.Items.Count > 0)
        {
            ContractsDeliveries.Add(delivery);
            //Monitor.Log(T("QueuedDelivery", new { source = delivery.Source }), LogLevel.Info);
        }
    }

    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {

    }

    private void OnRenderingHud(object sender, RenderingHudEventArgs e)
    {
        if (Config.EnableProcessTimeReduction)
        {
            Vector2 cursorTile = Game1.currentCursorTile;

            if (Game1.currentLocation != null &&
                Game1.currentLocation.Objects.TryGetValue(cursorTile, out SObject obj) &&
                obj is not null &&
                obj.bigCraftable.Value &&
                obj.minutesUntilReady.Value > 0)
            {
                int minutesLeft = obj.minutesUntilReady.Value;

                int hours = minutesLeft / 60;
                int minutes = minutesLeft % 60;

                string tooltip = T("DisplayTime", new { DisName = obj.DisplayName, Hrs = hours, Min = minutes });

                IClickableMenu.drawHoverText(
                    e.SpriteBatch,
                    tooltip,
                    Game1.smallFont
                );
            }
        }
    }

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        ModMonitor = this.Monitor;

        monitor = this.Monitor;

        Config = helper.ReadConfig<ModConfig>();

        Services = new CollectionServiceManager();

        Helper.Events.Display.RenderingHud += OnRenderingHud;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.GameLoop.DayStarted += OnDayStarted;

        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.Events.Display.MenuChanged += OnMenuChanged;

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.Events.GameLoop.TimeChanged += OnTimeChanged;

        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

        Helper.Events.Display.RenderedWorld += OnRenderedWorld;

        ShockBombs.Helper = helper;

        helper.Events.Input.ButtonPressed += ShockBombs.OnButtonPressed;

        FriendPower = new PowerHelpers(Config);

        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.objects.Pairs)
                {
                    if (pair.Value is Chest chest &&
                        chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        Config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }
            }
        };

        foreach (var m in typeof(Crop).GetMethods())
        {
            if (m.Name == "harvest")
                Monitor.Log($"HARVEST METHOD: {m}", LogLevel.Warn);
        }

        var QualityHarmony = new Harmony(this.ModManifest.UniqueID);

        Helper.Events.GameLoop.Saving += OnSaving;

        var harmony = new Harmony(this.ModManifest.UniqueID);
        harmony.PatchAll();

        Helper.Events.GameLoop.DayEnding += (s, e) =>
        {
            var convertedDeliveries = ContractsDeliveries
                .Select(w => new ContractsDelivery
                {
                    Items = w.Items,
                    RecipientID = w.RecipientID
                })
                .ToList();

            DeliverContractsItems(convertedDeliveries, Config);
            ContractsDeliveries.Clear();
        };
    }

    private void OnGameLaunched(object sender, EventArgs e)
    {

    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        FriendPower.Recalculate();

        if (Config.EnableProcessTimeReduction)
        {
            ApplyProductionTimeReduction(Config, Monitor);
            Game1.player.Money -= FriendPower.DailyTimeReductionFee;
        }

        if (Config.ReturnDamage)
        {
            Game1.player.Money -= FriendPower.DailyReturnFee;
        }

        if (Config.FriendShield)
        {
            Game1.player.Money -= FriendPower.DailyShieldFee;
        }

        foreach (var loc in Game1.locations)
        {
            foreach (var obj in loc.objects.Values)
            {
                if (obj is Chest chest && chest.modData.ContainsKey("cc.processingChest"))
                {
                }
            }
        }
    }

    private void OnSaving(object sender, SavingEventArgs e)
    {

    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {

    }

    private void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {
        if (WaitingForFuelLockerSelection)
        {
            if (e.NewMenu is ItemGrabMenu igm && igm.context is Chest chest)
            {
                WaitingForFuelLockerSelection = false;

                Config.FuelLockerAssigned = true;
                Config.FuelLockerLocation = chest.Location.Name;
                Config.FuelLockerTile = $"{(int)chest.TileLocation.X},{(int)chest.TileLocation.Y}";

                Helper.WriteConfig(Config);

                Game1.addHUDMessage(new HUDMessage(T("FuelLockerAssigned"), HUDMessage.newQuest_type));
            }
        }

        else

            if (WaitingForProcessingLockerSelection)
            {
                if (e.NewMenu is ItemGrabMenu igmenu && igmenu.context is Chest ProcessingLocker)
                {
                    WaitingForProcessingLockerSelection = false;

                    Config.ProcessingLockerAssigned = true;
                    Config.ProcessingLockerLocation = ProcessingLocker.Location.Name;
                    Config.ProcessingLockerTile = $"{(int)ProcessingLocker.TileLocation.X},{(int)ProcessingLocker.TileLocation.Y}";

                    ProcessingLocker.modData["cc.processingChest"] = "true";

                    Helper.WriteConfig(Config);

                    Game1.addHUDMessage(new HUDMessage(T("ProcessLockerAssigned"), HUDMessage.newQuest_type));
                }
            }

            else
                return;
    }

    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        foreach (var key in npcCooldowns.Keys.ToList())
        {
            if (npcCooldowns[key] > 0)
                npcCooldowns[key] -= 10;

            if (npcCooldowns[key] <= 0)
                npcCooldowns.Remove(key);
        }
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Farmer farmer = Game1.player;

        if (Instance.SetPlacementMenuOpen)
        {
            if (Game1.activeClickableMenu is not SetPlacement)
            {
                ShowPlacementOverlay = false;
                Instance.SetPlacementMenuOpen = false;
            }
        }

        if (!InitializedHP)
        {
            LastHP = farmer.health;
            InitializedHP = true;
            return;
        }

        if (farmer.health < LastHP)
        {
            int rawDamage = LastHP - farmer.health;
            int finalDamage = rawDamage;

            if (Config.FriendShield)
            {
                int reduced = (int)(rawDamage * (1f - FriendPower.ShieldPercent));
                finalDamage = Math.Max(1, reduced);

                int healBack = rawDamage - finalDamage;
                if (healBack > 0)
                    farmer.health += healBack;
            }

            if (Config.ReturnDamage && FriendPower.ReturnPercent > 0f)
            {
                TriggerReturnDamageExplosions(finalDamage);
            }
        }

        LastHP = farmer.health;
    }

    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (ForceShowDropLocationHighlight || ForceShowProcessChestHighlight || ForceShowFuelChestHighlight)
        {
            DrawDeliveryLocationHighlight(
                e.SpriteBatch,
                Config.DropLocationName,
                Config,
                s => T(s)
            );
        }

        if (ShowPlacementOverlay)
                DrawSquarePlacementOverlay(e.SpriteBatch);
    }

    public static void AssignFuelLocker()
    {
        WaitingForFuelLockerSelection = true;
        Game1.addHUDMessage(new HUDMessage(T("AssignFuelLocker"), HUDMessage.newQuest_type));
    }

    public static void AssignProcessLocker()
    {
        WaitingForProcessingLockerSelection = true;
        Game1.addHUDMessage(new HUDMessage(T("AssignProcessLocker"), HUDMessage.newQuest_type));
    }

    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.N)
        {
        }

        if (!Context.IsWorldReady)
            return;

        if (!Context.IsPlayerFree)
            return;

        if (Config.CheatMenuHotkey != SButton.None && e.Button == Config.CheatMenuHotkey)
        {
            Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
        }

        if (e.Button == SButton.MouseLeft)
        { 
            Vector2 cursorPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
        } 

        if (!Context.IsWorldReady || !IsSelectingTile || e.Button != SButton.MouseLeft)
            return;

        var cursorTile = e.Cursor.Tile;
        var locationName = Game1.currentLocation.Name;

        Config.DropLocationName = locationName;
        Config.DropTileX = (int)cursorTile.X;
        Config.DropTileY = (int)cursorTile.Y;
        Helper.WriteConfig(Config);

        Game1.addHUDMessage(new HUDMessage(T("DeliveryLocationSet", new { location = locationName, x = cursorTile.X, y = cursorTile.Y }), HUDMessage.newQuest_type));
        IsSelectingTile = false;
    }
}