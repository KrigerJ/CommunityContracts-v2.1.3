using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SDV_NPC = StardewValley.NPC;

namespace CommunityContracts.Core
{
    public class NPCDropdownMenu : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        public static ModConfig config;
        private int scrollOffset = 0;
        private readonly List<ClickableComponent> menuButtons = new();
        private List<string> npcNames;

        // Scrolling fields
        private int contentHeight;
        private int maxScroll;
        private int viewportHeight = 600;
        private Rectangle scrollBarThumb;
        private Rectangle scrollBarTrack;
        private bool draggingThumb = false;
        private int dragOffsetY = 0;
        private int scrollCooldown = 1;
        int scrollStep = ButtonHeight + HSpacing;
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        private void ClampScroll()
        {
            if (scrollOffset < 0)
                scrollOffset = 0;

            if (scrollOffset > maxScroll)
                scrollOffset = maxScroll;
        }
        public void BuildNpcList()
        {
            var all = Utility.getAllCharacters() ?? new List<SDV_NPC>();

            npcNames = all
                .Where(npc =>
                    npc is SDV_NPC &&
                    npc.CanSocialize &&
                    !npc.IsMonster &&
                    Game1.player.friendshipData.ContainsKey(npc.Name))
                .Select(npc => npc.Name)
                .Distinct()
                .ToList();

            npcNames ??= new List<string>();
        }

        private List<NPCMenuOption> options = new List<NPCMenuOption>();
        private Dictionary<string, Texture2D> npcPortraits = new();
        private int selectedIndex = -1;
        private const int ButtonWidth = 160;
        private const int ButtonHeight = 74;
        private const int HSpacing = 10;
        private const int WSpacing = 120;
        public static int CurrentFriendship { get; set; } = 0;
        public static int NPCLevel { get; set; } = 0;
        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private ClickableTextureComponent ColumnMinusButton;
        private ClickableTextureComponent ColumnPlusButton;
        private static string ColumnTooltip => T("ColumnTooltip");
        private static string MenuOrderTooltip => T("MenuOrderTooltip");
        private ClickableComponent MenuOrderButton;

        int WarpFee = Config.SeviceContractFees[ServiceId.Warp];

        bool showScrollBar = false;
        public class NPCMenuOption
        {
            public string name;
            public ClickableComponent nameButton;
            public Rectangle portraitRect;
            public int Friendship;
            public int Level;
        }
        public NPCDropdownMenu(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;

            int startX = 120;
            int startY = 120;

            BuildNpcList();

            foreach (var name in npcNames)
            {
                try
                {
                    npcPortraits[name] = Game1.content.Load<Texture2D>($"Portraits/{name}");
                }
                catch
                {

                }
            }

            if (Config.NPCOrderDescending)
            {
                npcNames = npcNames
                    .OrderByDescending(n =>
                        Game1.player.friendshipData.TryGetValue(n, out var data) ? data.Points : 0
                    )
                    .ToList();
            }
            else
            {
                npcNames = npcNames
                    .OrderBy(n =>
                        Game1.player.friendshipData.TryGetValue(n, out var data) ? data.Points : 0
                    )
                    .ToList();
            }

            for (int i = 0; i < npcNames.Count; i++)
            {
                string name = npcNames[i];
                SDV_NPC npc = Game1.getCharacterFromName(name, mustBeVillager: false);

                if (npc == null || npc.currentLocation == null || npc.Position == Vector2.Zero)
                {
                    Instance.Monitor.Log(T("SkippingNPC", new { npc = name }), LogLevel.Trace);
                    continue;
                }

                int col = options.Count % Config.MenuColumns;
                int row = options.Count / Config.MenuColumns;

                int x = startX + col * (ButtonWidth + WSpacing);
                int y = startY + row * (ButtonHeight + HSpacing);

                int friendship = Game1.player.friendshipData.TryGetValue(name, out var data) ? data.Points : 0;
                int level = UpdateNPCLevel(name);

                var nameButton = new ClickableComponent(new Rectangle(x + 4, y, ButtonWidth, ButtonHeight), name);
                var portraitRect = new Rectangle(x - 70, y, 72, 72);

                options.Add(new NPCMenuOption
                {
                    name = name,
                    nameButton = nameButton,
                    portraitRect = portraitRect,

                    Friendship = friendship,
                    Level = level
                });
            }

            int buttonX = startX + (((ButtonWidth + WSpacing) * Config.MenuColumns) / 2) - 220;
            int ReturnToolbarX = startX - 40;
            int ReturnToolbarY = startY - 76;

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(ReturnToolbarX, ReturnToolbarY, 440, 60),
                "ReturnToolbar"
            );

            int ColumnMinusX = startX + 220;
            int ColumnMinusY = startY - 66;
            int buttonSize = 30;

            ColumnMinusButton = new ClickableTextureComponent(
                new Rectangle(ColumnMinusX, ColumnMinusY, 30, 30),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            ColumnPlusButton = new ClickableTextureComponent(
                new Rectangle(ColumnMinusX + 40, ColumnMinusY, 30, 30),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            menuButtons.Add(ColumnMinusButton);
            menuButtons.Add(ColumnPlusButton);

            int MenuOrderX = startX + 500;
            int MenuOrderY = startY - 76;

            MenuOrderButton = new ClickableComponent(
                new Rectangle(MenuOrderX, MenuOrderY, 440, 60),
                "MenuOrder"
            );

            ComputeMaxScroll();
        }
        private void ComputeMaxScroll()
        {
            if (options == null || options.Count == 0)
            {
                maxScroll = 0;
                return;
            }

            int contentTop = options.Min(o => o.nameButton.bounds.Y);
            int contentBottom = options.Max(o => o.nameButton.bounds.Bottom);
            int totalHeight = contentBottom - contentTop;
            int framePadding = 20;
            int frameHeight =
                ((npcNames.Count + Config.MenuColumns) / Config.MenuColumns)
                * (ButtonHeight + HSpacing * 2)
                + framePadding * 2 + 120;

            int frameY = 20;

            int viewportTop = contentTop;
            int viewportBottom = frameY + frameHeight;
            int viewportHeight = viewportBottom - viewportTop;

            maxScroll = Math.Max(0, totalHeight - viewportHeight);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            int scrollStep = ButtonHeight + HSpacing * 2;
            scrollOffset -= Math.Sign(direction) * scrollStep;

            float scrollPercent = scrollOffset / (float)maxScroll;

            ClampScroll();
            base.receiveScrollWheelAction(direction);
        }

        public override void draw(SpriteBatch b)
        {
            int yOffset = scrollOffset;
            int framePadding = 20;
            int frameWidth = (ButtonWidth + WSpacing) * Config.MenuColumns + framePadding * 6 - 80;
            int frameX = 20;
            int frameY = 20;
            int frameHeight = ((npcNames.Count + Config.MenuColumns) / Config.MenuColumns) * (ButtonHeight + HSpacing * 2) + framePadding * 2 + 100;

            frameHeight = Math.Min(frameHeight, Game1.viewport.Height - 100);

            int contentTop = options.Min(o => o.nameButton.bounds.Y);
            int contentBottom = options.Max(o => o.nameButton.bounds.Bottom);
            int totalHeight = contentBottom - contentTop;

            showScrollBar = maxScroll > 0;

            viewportHeight = scrollBarTrack.Height - 10;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                frameX,
                frameY,
                frameWidth,
                frameHeight,
                Color.White,
                drawShadow: false
            );

            Color ReturnToolbarColor = ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbarButton.bounds.Width,
                ReturnToolbarButton.bounds.Height,
                ReturnToolbarColor,
                1f,
                false
            );

            string text = T("ReturnToolbar");
            Vector2 textSize = Game1.smallFont.MeasureString(text);

            float textX = ReturnToolbarButton.bounds.X + (ReturnToolbarButton.bounds.Width / 2f) - (textSize.X / 2f);
            float textY = ReturnToolbarButton.bounds.Y + (ReturnToolbarButton.bounds.Height / 2f) - (textSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(textX, textY),
                Game1.textColor
            );

            string ReturnToolbarText = T("ReturnToolbar");
            Vector2 ReturnToolbarTextSize = Game1.smallFont.MeasureString(ReturnToolbarText);

            int ReturnToolbaradjustedWidth = (int)ReturnToolbarTextSize.X + 40;

            ReturnToolbarButton.bounds = new Rectangle(
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbaradjustedWidth,
                ReturnToolbarButton.bounds.Height
            );
            
            Utility.drawTextWithShadow(
                b,
                T("ColumnLabel"),
                Game1.smallFont,
                new Vector2(ColumnPlusButton.bounds.X + 40, ColumnPlusButton.bounds.Y + 4),
                Game1.textColor
            );

            ColumnMinusButton.draw(b);
            ColumnPlusButton.draw(b);

            Color MenuOrderColor = MenuOrderButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                MenuOrderButton.bounds.X,
                MenuOrderButton.bounds.Y,
                MenuOrderButton.bounds.Width,
                MenuOrderButton.bounds.Height,
                MenuOrderColor,
                1f,
                false
            );


            string Ordertext = T("MenuOrder");
            Vector2 OrdertextSize = Game1.smallFont.MeasureString(Ordertext);

            float OrdertextX = MenuOrderButton.bounds.X + (MenuOrderButton.bounds.Width / 2f) - (OrdertextSize.X / 2f);
            float OrdertextY = MenuOrderButton.bounds.Y + (MenuOrderButton.bounds.Height / 2f) - (OrdertextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                Ordertext,
                Game1.smallFont,
                new Vector2(OrdertextX, OrdertextY),
                Game1.textColor
            );

            string MenuOrderText = T("MenuOrder");
            Vector2 MenuOrderTextSize = Game1.smallFont.MeasureString(MenuOrderText);

            int MenuOrderadjustedWidth = (int)MenuOrderTextSize.X + 40;

            MenuOrderButton.bounds = new Rectangle(
                MenuOrderButton.bounds.X,
                MenuOrderButton.bounds.Y,
                MenuOrderadjustedWidth,
                MenuOrderButton.bounds.Height
            );

            base.draw(b);

            contentTop = options.Min(o => o.nameButton.bounds.Y);

            Rectangle clip = new Rectangle(
                frameX,
                contentTop - 8,
                frameWidth,
                frameHeight - (contentTop - frameY) - 36
            );

            RasterizerState oldRasterizer = b.GraphicsDevice.RasterizerState;
            Rectangle oldScissor = b.GraphicsDevice.ScissorRectangle;

            b.End();

            b.GraphicsDevice.ScissorRectangle = clip;

            RasterizerState scissorState = new RasterizerState() { ScissorTestEnable = true };
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, scissorState);

            b.GraphicsDevice.ScissorRectangle = clip;

            foreach (var option in options)
            {
                int drawY = option.nameButton.bounds.Y - yOffset;

                Color boxColor = option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? Color.Gold : Color.White;

                drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    option.nameButton.bounds.X,
                    drawY,
                    option.nameButton.bounds.Width,
                    option.nameButton.bounds.Height,
                    boxColor,
                    1f,
                    false
                );

                if (npcPortraits.TryGetValue(option.name, out var portrait))
                {
                    Rectangle sourceRect = new Rectangle(0, 0, 64, 64);

                    Rectangle portraitDrawRect = new Rectangle(
                        option.portraitRect.X,
                        option.portraitRect.Y - yOffset,
                        option.portraitRect.Width,
                        option.portraitRect.Height
                    );

                    b.Draw(portrait, portraitDrawRect, sourceRect, Color.White);
                }

                Vector2 nameSize = Game1.smallFont.MeasureString(option.name);
                float nameX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (nameSize.X / 2f);
                float nameY = drawY + 8;

                Utility.drawTextWithShadow(
                    b,
                    option.name,
                    Game1.smallFont,
                    new Vector2(nameX, nameY),
                    Game1.textColor
                );

                string infoLine = $"{option.Level} /  {option.Friendship}";
                Vector2 infoSize = Game1.smallFont.MeasureString(infoLine);

                float infoX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (infoSize.X / 2f);
                float infoY = drawY + 32;

                Utility.drawTextWithShadow(
                    b,
                    infoLine,
                    Game1.smallFont,
                    new Vector2(infoX, infoY),
                    Game1.textColor * 0.85f
                );
            }

            contentBottom = options.Max(o => o.nameButton.bounds.Bottom);

            int viewportTop = frameY + 100;

            viewportHeight = frameHeight - 150;

            maxScroll = Math.Max(0, (contentBottom - viewportTop) - viewportHeight);

            contentTop = options.Min(o => o.nameButton.bounds.Y);
            contentBottom = options.Max(o => o.nameButton.bounds.Bottom);
            totalHeight = contentBottom - contentTop;

            b.End();

            b.GraphicsDevice.ScissorRectangle = oldScissor;

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, oldRasterizer);

            drawMouse(b);

            foreach (var option in options)
            {
                Rectangle portraitHit = new Rectangle(
                    option.portraitRect.X,
                    option.portraitRect.Y - yOffset,
                    option.portraitRect.Width,
                    option.portraitRect.Height
                );

                if (portraitHit.Contains(Game1.getMouseX(), Game1.getMouseY()))

                {
                    CurrentFriendship = Game1.player.friendshipData.TryGetValue(option.name, out var data) ? data.Points : 0;
                    string tooltip = T("Warp", new { npc = option.name, Fee = WarpFee });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont
                    );
                    break;
                }
            }

            foreach (var option in options)
            {
                Rectangle portraitHit = new Rectangle(
                    option.portraitRect.X,
                    option.portraitRect.Y - yOffset,
                    option.portraitRect.Width,
                    option.portraitRect.Height
                );

                if (portraitHit.Contains(Game1.getMouseX(), Game1.getMouseY()))

                {
                    CurrentFriendship = Game1.player.friendshipData.TryGetValue(option.name, out var data) ? data.Points : 0;
                    string tooltip = T("GoToMenu", new { npc = option.name, points = CurrentFriendship });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont,
                        xOffset: -30
                    );
                    break;
                }
            }

            if (ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnToolbarTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }
            
            if (ColumnMinusButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) || ColumnPlusButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ColumnTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (MenuOrderButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    MenuOrderTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            int yOffset = scrollOffset;

            if (ColumnMinusButton.containsPoint(x, y))
            {
                Config.MenuColumns = Math.Max(2, Config.MenuColumns - 1);
                Helper.WriteConfig(Config);
                Game1.playSound("smallSelect");

                RecalculateLayout();
                return;
            }

            if (ColumnPlusButton.containsPoint(x, y))
            {
                Config.MenuColumns = Math.Min(7, Config.MenuColumns + 1);
                Helper.WriteConfig(Config);
                Game1.playSound("smallSelect");

                RecalculateLayout();
                return;
            }

            if (MenuOrderButton.containsPoint(x, y))
            {
                Config.NPCOrderDescending = !Config.NPCOrderDescending;
                Helper.WriteConfig(Config);
                Game1.playSound("smallSelect");

                RecalculateLayout();
                return;
            }

            if (ReturnToolbarButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
                return;
            }

            foreach (var option in options)
            {
                string npcName = option.name;
                SDV_NPC npc = Game1.getCharacterFromName(npcName, mustBeVillager: false);

                if (option.portraitRect.Contains(x, y))
                {
                    if (Game1.player.Money >= WarpFee)
                    {
                        if (npc == null || npc.currentLocation == null)
                            return;

                        GameLocation loc = npc.currentLocation;

                        Vector2 npcTile = npc.Position / Game1.tileSize;

                        Vector2[] candidates = new[]
                        {
                            npcTile,
                            npcTile + new Vector2(1, 0),
                            npcTile + new Vector2(-1, 0),
                            npcTile + new Vector2(0, 1),
                            npcTile + new Vector2(0, -1)
                        };

                        Vector2 safeTile = npcTile;
                        bool foundSafe = false;

                        foreach (var t in candidates)
                        {
                            if (!loc.isTileOnMap(t))
                                continue;

                            if (loc.isTilePassable(t))
                            {
                                safeTile = t;
                                foundSafe = true;
                                break;
                            }
                        }

                        Game1.warpFarmer(loc.Name, (int)safeTile.X, (int)safeTile.Y, false);
                        Game1.player.Money -= WarpFee;
                        Game1.exitActiveMenu();
                        Game1.playSound("wand");
                        return;
                    }
                    return;
                }

                Rectangle nameHit = new Rectangle(
                    option.nameButton.bounds.X,
                    option.nameButton.bounds.Y - yOffset,
                    option.nameButton.bounds.Width,
                    option.nameButton.bounds.Height
                );

                if (nameHit.Contains(x, y))
                {
                    Game1.exitActiveMenu();
                    Game1.activeClickableMenu = new NPCServiceMenu(npcName);
                    return;
                }
            }
            
            if (scrollBarThumb.Contains(x, y))
            {
                draggingThumb = true;
                dragOffsetY = y - scrollBarThumb.Y;
                return;
            }

            base.receiveLeftClick(x, y, playSound);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (showScrollBar)
            {
                if (key == Keys.Down)
                {
                    scrollOffset += scrollStep / 3;
                    ClampScroll();
                }
                else if (key == Keys.Up)
                {
                    scrollOffset -= scrollStep / 3;
                    ClampScroll();
                }
            }

            base.receiveKeyPress(key);
        }
        public override void update(GameTime time)
        {
            base.update(time);

            if (!showScrollBar)
                return;

            KeyboardState kb = Game1.input.GetKeyboardState();


            if (scrollCooldown > 0)
            {
                scrollCooldown--;
                return;
            }

            if (kb.IsKeyDown(Keys.Down))
            {
                scrollOffset += scrollStep;
                ClampScroll();
                scrollCooldown = 6;
            }
            else if (kb.IsKeyDown(Keys.Up))
            {
                scrollOffset -= scrollStep;
                ClampScroll();
                scrollCooldown = 6;
            }
        }
        private void RecalculateLayout()
        {
            options.Clear();

            int startX = 120;
            int startY = 120;

            BuildNpcList();

            foreach (var name in npcNames)
            {
                try
                {
                    npcPortraits[name] = Game1.content.Load<Texture2D>($"Portraits/{name}");
                }
                catch { }
            }

            if (Config.NPCOrderDescending)
            {
                npcNames = npcNames
                    .OrderByDescending(n =>
                        Game1.player.friendshipData.TryGetValue(n, out var data) ? data.Points : 0
                    )
                    .ToList();
            }
            else
            {
                npcNames = npcNames
                    .OrderBy(n =>
                        Game1.player.friendshipData.TryGetValue(n, out var data) ? data.Points : 0
                    )
                    .ToList();
            }

            for (int i = 0; i < npcNames.Count; i++)
            {
                string name = npcNames[i];
                SDV_NPC npc = Game1.getCharacterFromName(name, mustBeVillager: false);

                if (npc == null || npc.currentLocation == null || npc.Position == Vector2.Zero)
                    continue;

                int col = options.Count % Config.MenuColumns;
                int row = options.Count / Config.MenuColumns;

                int x = startX + col * (ButtonWidth + WSpacing);
                int y = startY + row * (ButtonHeight + HSpacing);

                int friendship = Game1.player.friendshipData.TryGetValue(name, out var data) ? data.Points : 0;
                int level = UpdateNPCLevel(name);

                var nameButton = new ClickableComponent(new Rectangle(x + 4, y, ButtonWidth, ButtonHeight), name);
                var portraitRect = new Rectangle(x - 70, y, 72, 72);

                options.Add(new NPCMenuOption
                {
                    name = name,
                    nameButton = nameButton,
                    portraitRect = portraitRect,
                    Friendship = friendship,
                    Level = level
                });
            }

            int totalRows = (int)Math.Ceiling(options.Count / (float)Config.MenuColumns);
            contentHeight = totalRows * (ButtonHeight + HSpacing);
            maxScroll = Math.Max(0, contentHeight - viewportHeight);

            UpdateScrollBarThumb();
        }
        private void UpdateScrollBarThumb()
        {
            if (contentHeight <= viewportHeight)
            {
                scrollBarThumb = new Rectangle(scrollBarTrack.X, scrollBarTrack.Y, scrollBarTrack.Width, scrollBarTrack.Height);
                return;
            }

            float visibleRatio = viewportHeight / (float)contentHeight;
            int thumbHeight = (int)(scrollBarTrack.Height * visibleRatio);

            thumbHeight = Math.Max(40, thumbHeight);

            float scrollRatio = scrollOffset / (float)maxScroll;
            int thumbY = scrollBarTrack.Y + (int)((scrollBarTrack.Height - thumbHeight) * scrollRatio);

            scrollBarThumb = new Rectangle(scrollBarTrack.X, thumbY, scrollBarTrack.Width, thumbHeight);
        }
    }
}
