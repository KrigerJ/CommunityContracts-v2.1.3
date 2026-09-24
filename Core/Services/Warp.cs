using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static CommunityContracts.Core.NPCServiceMenu;
using static CommunityContracts.Core.Services.WarpMenu;
using static ModEntry;

namespace CommunityContracts.Core.Services
{
    public class WarpMenu : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        public ModConfig Config;

        private List<WarpMenuOption> options = new();

        private Dictionary<string, Texture2D> npcPortraits = new();
        private const int ButtonWidth = 220;
        private const int ButtonHeight = 60;
        private const int Columns = 6;
        private const int WSpacing = 20;

        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private static string ReturnCharactersTooltip => T("ReturnCharactersTooltip");
        private ClickableComponent ReturnCharactersButton;
        private static string WarpSetTooltip => T("WarpSetTooltip");
        private ClickableComponent WarpSetButton;
        private ClickableComponent npcPortraitButton;
        private ClickableComponent npcNameButton;
        //private static string WarpDescription => T("WarpDescription");
        private static string WarpDescriptionTooltip => T("WarpDescriptionTooltip");
        private int WarpFee;
        public static string NPCName = "Maru";

        public class WarpLocationEntry
        {
            public string Map { get; set; } = "";
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class WarpMenuOption
        {
            public string Label { get; set; }
            public string Map { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public ClickableComponent Button { get; set; }
        }

        public WarpMenu(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            this.Helper = helper;
            this.Monitor = monitor;
            this.Config = config;

            this.WarpFee = config.SeviceContractFees[ServiceId.Warp];

            int startX = 20;
            int startY = 20;

            CurrentFriendship = Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0;

            try
            {
                npcPortraits[NPCName] = Game1.content.Load<Texture2D>($"Portraits/{NPCName}");
            }

            catch (Exception ex)
            {

            }

            int PortraitX = startX + 20;
            int PortraitY = startY + 30;

            npcPortraitButton = new ClickableComponent(new Rectangle(PortraitX, PortraitY, 200, 200), "NPCPortraits");

            int buttonX = startX + 260;
            int buttonY = startY + 20;

            int NPCNameButtonX = PortraitX;
            int NPCNameButtonY = PortraitY + 220;

            npcNameButton = new ClickableComponent(new Rectangle(NPCNameButtonX, NPCNameButtonY, 200, 60), NPCName);

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX + 320, buttonY, 440, 60),
                "ReturnToolbar"
            );

            ReturnCharactersButton = new ClickableComponent(
                new Rectangle(buttonX + 610, buttonY, 440, 60),
                "ReturnCharacters"
            );

            WarpSetButton = new ClickableComponent(
                new Rectangle(buttonX + 900, buttonY, 440, 60),
                "WarpSet"
            );
        }

        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = Game1.viewport.Width - 50;
            int frameHeight = Game1.viewport.Height - 50;
            int frameX = 20;
            int frameY = 20;

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

            if (npcPortraits.TryGetValue(NPCName, out var portrait))
            {
                Rectangle sourceRect = new Rectangle(0, 0, 64, 64);
                Rectangle destRect = new Rectangle(npcPortraitButton.bounds.X, npcPortraitButton.bounds.Y, npcPortraitButton.bounds.Width, npcPortraitButton.bounds.Height);

                b.Draw(
                    portrait,
                    destRect,
                    sourceRect,
                    Color.White
                );
            }

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                npcNameButton.bounds.X,
                npcNameButton.bounds.Y,
                npcNameButton.bounds.Width,
                npcNameButton.bounds.Height,
                Color.White,
                1f,
                false
            );

            Vector2 NameButtonTextSize = Game1.smallFont.MeasureString(NPCName);

            float NameButtonTextX = npcNameButton.bounds.X + (npcNameButton.bounds.Width / 2f) - (NameButtonTextSize.X / 2f);
            float NameButtonTextY = npcNameButton.bounds.Y + (npcNameButton.bounds.Height / 2f) - (NameButtonTextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                NPCName,
                Game1.smallFont,
                new Vector2(NameButtonTextX, NameButtonTextY),
                Game1.textColor
            );

            base.draw(b);

            int spacing = 20;
            int rowSpacing = 20;
            int maxWidth = (ButtonWidth + WSpacing) * Columns - 20;
            int startY = 20;

            List<List<(WarpMenuOption option, int width)>> rows = new();
            List<(WarpMenuOption option, int width)> currentRow = new();

            int currentRowWidth = 0;

            foreach (var entry in Config.WarpLocations)
            {
                string label = entry.Key;
                string map = entry.Value.Map;
                int x = entry.Value.X;
                int y = entry.Value.Y;

                WarpMenuOption option = new WarpMenuOption
                {
                    Label = label,
                    Map = map,
                    X = x,
                    Y = y
                };

                Vector2 WarpNametextSize = Game1.smallFont.MeasureString(label);
                int adjustedWidth = (int)WarpNametextSize.X + 40;

                int extraSpacing = currentRow.Count > 0 ? spacing : 0;

                if (currentRowWidth + adjustedWidth + extraSpacing > maxWidth)
                {
                    rows.Add(currentRow);
                    currentRow = new List<(WarpMenuOption option, int width)>();
                    currentRowWidth = 0;
                    extraSpacing = 0;
                }

                currentRow.Add((option, adjustedWidth));
                currentRowWidth += adjustedWidth + extraSpacing;
            }

            if (currentRow.Count > 0)
                rows.Add(currentRow);

            int currentY = startY + 120;

            foreach (var row in rows)
            {
                int rowWidth = row.Sum(r => r.width) + spacing * (row.Count - 1);
                int rowStartX = frameX + 260;
                int currentX = rowStartX;

                foreach (var (option, width) in row)
                {
                    option.Button = new ClickableComponent(
                        new Rectangle(currentX, currentY, width, ButtonHeight),
                        option.Label
                    );

                    bool hover = option.Button.containsPoint(Game1.getMouseX(), Game1.getMouseY());
                    Color boxColor = hover ? Color.LimeGreen : Color.White;

                    drawTextureBox(
                        b,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        option.Button.bounds.X,
                        option.Button.bounds.Y,
                        option.Button.bounds.Width,
                        option.Button.bounds.Height,
                        boxColor,
                        1f,
                        false
                    );

                    Vector2 ButtontextSize = Game1.smallFont.MeasureString(option.Label);
                    float ButtontextX = option.Button.bounds.X + (option.Button.bounds.Width / 2f) - (ButtontextSize.X / 2f);
                    float ButtontextY = option.Button.bounds.Y + (option.Button.bounds.Height / 2f) - (ButtontextSize.Y / 2f);

                    Utility.drawTextWithShadow(
                        b,
                        option.Label,
                        Game1.smallFont,
                        new Vector2(ButtontextX, ButtontextY),
                        Game1.textColor
                    );

                    currentX += width + spacing;

                    options.Add(option);
                }

                currentY += ButtonHeight + rowSpacing;
                frameHeight = currentY + 100;
                frameHeight = Math.Min(frameHeight, Game1.viewport.Height - 50);
            }

            Utility.drawTextWithShadow(
                b,
                T("WarpDescription"),
                Game1.smallFont,
                new Vector2(frameX + 260, startY + 25), // Position Warp Description above the buttons
                Game1.textColor
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

            string textReturn = T("ReturnToolbar");
            Vector2 textSize = Game1.smallFont.MeasureString(textReturn);

            float textX = ReturnToolbarButton.bounds.X + (ReturnToolbarButton.bounds.Width / 2f) - (textSize.X / 2f);
            float textY = ReturnToolbarButton.bounds.Y + (ReturnToolbarButton.bounds.Height / 2f) - (textSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                textReturn,
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

            Color ReturnCharactersColor = ReturnCharactersButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersButton.bounds.Width,
                ReturnCharactersButton.bounds.Height,
                ReturnCharactersColor,
                1f,
                false
            );

            string Characterstext = T("MainMenuButton");
            Vector2 CharacterstextSize = Game1.smallFont.MeasureString(Characterstext);

            float CharacterstextX = ReturnCharactersButton.bounds.X + (ReturnCharactersButton.bounds.Width / 2f) - (CharacterstextSize.X / 2f);
            float CharacterstextY = ReturnCharactersButton.bounds.Y + (ReturnCharactersButton.bounds.Height / 2f) - (CharacterstextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                Characterstext,
                Game1.smallFont,
                new Vector2(CharacterstextX, CharacterstextY),
                Game1.textColor
            );

            string ReturnCharactersText = T("MainMenuButton");
            Vector2 ReturnCharactersTextSize = Game1.smallFont.MeasureString(ReturnCharactersText);

            int ReturnCharactersadjustedWidth = (int)ReturnCharactersTextSize.X + 40;

            ReturnCharactersButton.bounds = new Rectangle(
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersadjustedWidth,
                ReturnCharactersButton.bounds.Height
            );

            Color WarpSetColor = WarpSetButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                    ? Color.LimeGreen
                    : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                WarpSetButton.bounds.X,
                WarpSetButton.bounds.Y,
                WarpSetButton.bounds.Width,
                WarpSetButton.bounds.Height,
                WarpSetColor,
                1f,
                false
            );

            string WarpSettext = T("WarpSetButton");
            Vector2 WarpSettextSize = Game1.smallFont.MeasureString(WarpSettext);

            float WarpSettextX = WarpSetButton.bounds.X + (WarpSetButton.bounds.Width / 2f) - (WarpSettextSize.X / 2f);
            float WarpSettextY = WarpSetButton.bounds.Y + (WarpSetButton.bounds.Height / 2f) - (WarpSettextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                WarpSettext,
                Game1.smallFont,
                new Vector2(WarpSettextX, WarpSettextY),
                Game1.textColor
            );

            string WarpSetText = T("WarpSetButton");
            Vector2 WarpSetTextSize = Game1.smallFont.MeasureString(WarpSetText);

            int WarpSetadjustedWidth = (int)WarpSetTextSize.X + 40;

            WarpSetButton.bounds = new Rectangle(
                WarpSetButton.bounds.X,
                WarpSetButton.bounds.Y,
                WarpSetadjustedWidth,
                WarpSetButton.bounds.Height
            );

            drawMouse(b);

            if (npcPortraitButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) || npcNameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    WarpDescriptionTooltip,
                    Game1.smallFont
                );
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

            if (ReturnCharactersButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnCharactersTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }

            if (WarpSetButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    WarpSetTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (ReturnToolbarButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
                return;
            }

            if (ReturnCharactersButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new NPCDropdownMenu(Helper, Monitor, Config);
                return;
            }

            foreach (var option in options)
            {
                if (option.Button.containsPoint(x, y))
                {
                    OnMenuButtonClicked(option.Button);
                    return;
                }
            }

            if (WarpSetButton.containsPoint(x, y))
            {
                SaveCustomWarp();
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new WarpMenu(Helper, Monitor, Config);
                return;
            }

            if (this.isWithinBounds(x, y))
                return;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            var option = this.options.FirstOrDefault(o => o.Button.containsPoint(x, y));

            if (option != null)
            {
                Game1.activeClickableMenu = new RenameWarpMenu(option, this);
            }
        }

        private void OnMenuButtonClicked(ClickableComponent button)
        {
            var option = this.options.FirstOrDefault(o => o.Button == button);

            if (option != null && Game1.player.Money >= WarpFee)
            {
                Game1.playSound("wand");
                Game1.exitActiveMenu();
                Game1.warpFarmer(option.Map, option.X, option.Y, false);
                Game1.player.Money -= WarpFee;
            }
        }

        public void SaveCustomWarp()
        {
            string map;
            if (Game1.player.currentLocation.Name == "FarmHouse")
            {
                map = "FarmHouse";
            }
            else
            {
                map = Game1.player.currentLocation.NameOrUniqueName;
            }

            int tileX = Game1.player.TilePoint.X;
            int tileY = Game1.player.TilePoint.Y;

            string label = $"{map} {tileX}, {tileY}";

            Config.WarpLocations[label] = new WarpLocationEntry
            {
                Map = map,
                X = tileX,
                Y = tileY
            };

            Instance.Helper.WriteConfig(Config);

            Game1.addHUDMessage(new HUDMessage($"Saved warp: {label}", HUDMessage.newQuest_type));
        }

        public void RenameWarp(WarpMenuOption option, string newLabel)
        {
            var entry = Config.WarpLocations[option.Label];
            Config.WarpLocations.Remove(option.Label);

            Config.WarpLocations[newLabel] = entry;

            Instance.Helper.WriteConfig(Config);

            Game1.activeClickableMenu = new WarpMenu(Helper, Monitor, Config);
        }

        public void DeleteWarp(WarpMenuOption option)
        {
            if (option == null)
                return;

            if (Config.WarpLocations.ContainsKey(option.Label))
                Config.WarpLocations.Remove(option.Label);

            Instance.Helper.WriteConfig(Config);

            Game1.activeClickableMenu = new WarpMenu(Helper, Monitor, Config);
        }
    }

    public class RenameWarpMenu : IClickableMenu
    {
        private WarpMenuOption option;
        private WarpMenu parent;
        private TextBox textBox;
        private ClickableTextureComponent okButton;
        private ClickableTextureComponent cancelButton;
        private ClickableTextureComponent deleteButton;
        private Rectangle boxBounds;

        public RenameWarpMenu(WarpMenuOption option, WarpMenu parent)
        {
            this.option = option;
            this.parent = parent;

            float textWidth = Game1.dialogueFont.MeasureString(option.Label).X;
            int boxWidth = (int)textWidth + 120;
            boxWidth = Math.Max(300, boxWidth);
            boxWidth = Math.Min(Game1.viewport.Width - 50, boxWidth);
            boxWidth = (int)(Math.Round(boxWidth / 64f) * 64);
            int boxHeight = 280;
            int x = Game1.viewport.Width / 2 - boxWidth / 2;
            int y = Game1.viewport.Height / 2 - boxHeight / 2;

            boxBounds = new Rectangle(x, y, boxWidth, boxHeight);

            textBox = new TextBox(null, null, Game1.dialogueFont, Game1.textColor)
            {
                X = x + 12,
                Y = y,
                Width = boxWidth - 24,
                Height = boxHeight,
                Text = option.Label
            };

            textBox.SelectMe();
            Game1.keyboardDispatcher.Subscriber = textBox;

            okButton = new ClickableTextureComponent(
                new Rectangle(x + 40, y + 64, 64, 64),
                Game1.mouseCursors,
                new Rectangle(128, 256, 64, 64),
                1f
            );

            cancelButton = new ClickableTextureComponent(
                new Rectangle(x + 138, y + 64, 64, 64),
                Game1.mouseCursors,
                new Rectangle(192, 256, 64, 64),
                1f
            );

            deleteButton = new ClickableTextureComponent(
                new Rectangle(x + 236, y + 56, 64, 64),
                Game1.mouseCursors,
                new Rectangle(562, 100, 20, 28),
                3f
            );
        }

        public override void draw(SpriteBatch b)
        {
            textBox.Draw(b);
            okButton.draw(b);
            cancelButton.draw(b);
            deleteButton.draw(b);
            drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (okButton.containsPoint(x, y))
            {
                SaveNewName();
            }
            else if (cancelButton.containsPoint(x, y))
            {
                Game1.activeClickableMenu = parent;
            }
            else if (deleteButton.containsPoint(x, y))
            {
                parent.DeleteWarp(option);
            }
        }

        private void SaveNewName()
        {
            string newLabel = textBox.Text.Trim();

            if (!string.IsNullOrEmpty(newLabel))
            {
                parent.RenameWarp(option, newLabel);
            }

            Game1.activeClickableMenu = parent;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Enter)
            {
                SaveNewName();
                return;
            }

            if (key == Keys.Escape)
            {
                Game1.activeClickableMenu = parent;
                return;
            }

            if (textBox.Selected)
            {
                textBox.RecieveSpecialInput(key);
                return;
            }

            base.receiveKeyPress(key);
        }
    }
}
