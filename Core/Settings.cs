using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static CommunityContracts.Core.PowerUps.PowerHelpers;
using static ModEntry;

namespace CommunityContracts.Core
{
    public class Settings : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;

        private readonly List<ClickableComponent> menuButtons = new();

        private TextBox delayBox;

        private ClickableComponent delayBoxClickable;

        private ClickableTextureComponent delayMinusButton;
        private ClickableTextureComponent delayPlusButton;
        private static string DelayTooltip => T("DelayTooltip");

        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private static string ReturnCharactersTooltip => T("ReturnCharactersTooltip");
        private ClickableComponent ReturnCharactersButton;

        private TextBox HotkeyBox;
        private ClickableComponent HotKeyBoxClickable;
        private static string HotkeyTooltip => T("HotkeyTooltip");

        private ClickableTextureComponent TimeReductionCheckbox;
        private static string ReductionTooltip => T("ReductionTooltip");

        private ClickableTextureComponent ReturnDamageCheckbox;

        private static string ReturnT1;
        private static string ReturnT2;
        public static string ReturnDamageTooltip = $"{ReturnT1}\n{ReturnT2}";

        private ClickableTextureComponent FriendShieldCheckbox;

        private static string ShieldT1;
        private static string ShieldT2;
        public static string FriendShieldTooltip = $"{ShieldT1}\n{ShieldT2}";

        private ClickableTextureComponent ChestColorButton;
        private List<string> ChestColorNames;
        private int ChestColorIndex;
        private static string ChestColorTooltip => T("ChestColorTooltip");

        private ClickableTextureComponent HighlightColorButton;
        private List<string> HighlightColorNames;
        private int HighlightColorIndex;
        private static string HighlightColorTooltip => T("HighlightColorTooltip");

        private ClickableTextureComponent FontColorButton;
        private List<string> FontColorNames;
        private int FontColorIndex;
        private static string FontColorTooltip => T("FontColorTooltip");
        private static string FuelLockerTooltip => T("FuelLockerTooltip");
        private ClickableComponent FuelLockerButton;
        private static string ProcessingLockerTooltip => T("ProcessingLockerTooltip");
        private ClickableComponent ProcessingLockerButton;

        static Settings()
        {
            var helper = Instance.Helper;

            ReturnT1 = helper.Translation.Get("ReturnDamageTooltip_Line1");
            ReturnT2 = helper.Translation.Get("ReturnDamageTooltip_Line2");
            ReturnDamageTooltip = $"{ReturnT1}\n{ReturnT2}";

            ShieldT1 = helper.Translation.Get("ReturnDamageTooltip_Line1");
            ShieldT2 = helper.Translation.Get("ReturnDamageTooltip_Line2");
            FriendShieldTooltip = $"{ShieldT1}\n{ShieldT2}";
        }

        public Settings(IModHelper helper, ModConfig config)
        {
            this.Helper = helper;
            Monitor = monitor;
            this.Config = config;

            int startX = 20;
            int startY = 20;

            int buttonX = startX + 100;
            int buttonY = startY + 20;

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 440, 60),
                "ReturnToolbar"
            );

            ReturnCharactersButton = new ClickableComponent(
                new Rectangle(buttonX + 300, buttonY, 440, 60),
                "ReturnCharacters"
            );

            int HotkeyBoxX = startX + 60;
            int HotkeyBoxY = startY + 110;

            HotkeyBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )

            {
                X = HotkeyBoxX,
                Y = HotkeyBoxY,
                Width = 50,
                Text = Config.CheatMenuHotkey.ToString()
            };
            
            HotkeyBox.OnEnterPressed += (tb) =>
            {
                ApplyHotkeyChange();
                HotkeyBox.Selected = false;
            };
            
            HotKeyBoxClickable = new ClickableComponent(
                new Rectangle(HotkeyBoxX, HotkeyBoxY, 60, 42),
                "HotkeyBox"
            );

            menuButtons.Add(HotKeyBoxClickable);

            int ReductionX = startX + 66;
            int ReductionY = startY + 175;

            TimeReductionCheckbox = new ClickableTextureComponent(
                new Rectangle(ReductionX, ReductionY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(227, 425, 9, 9), 
                4f
            )
            { name = "TimeReduction" };

            int ReturnDamageX = startX + 66;
            int ReturnDamageY = startY + 225;

            ReturnDamageCheckbox = new ClickableTextureComponent(
                new Rectangle(ReturnDamageX, ReturnDamageY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(227, 425, 9, 9),
                4f
            )
            { name = "ReturnDamage" };

            int FriendShieldX = startX + 66;
            int FriendShieldY = startY + 275;

            FriendShieldCheckbox = new ClickableTextureComponent(
                new Rectangle(FriendShieldX, FriendShieldY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(227, 425, 9, 9),
                4f
            )
            { name = "FriendShield" };

            int delayBoxX = startX + 90;
            int delayBoxY = startY + 325;

            delayBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = delayBoxX,
                Y = delayBoxY,
                Width = 80,
                Text = Config.CollectionDelay.ToString()
            };

            delayBox.OnEnterPressed += (tb) =>
            {
                if (int.TryParse(delayBox.Text, out int w))
                {
                    Config.CollectionDelay = w;
                    Game1.showGlobalMessage(T("DelaySet", new { Delay = w }));
                    Instance.Helper.WriteConfig(Config);
                }
                delayBox.Selected = false;
            };

            delayBoxClickable = new ClickableComponent(
                new Rectangle(delayBoxX, delayBoxY, 80, 40),
                "delayBox"
            );

            menuButtons.Add(delayBoxClickable);

            int buttonSize = 30;

            delayMinusButton = new ClickableTextureComponent(
                new Rectangle(delayBox.X - buttonSize + 1, delayBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            delayPlusButton = new ClickableTextureComponent(
                new Rectangle(delayBox.X + delayBox.Width + 4, delayBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            menuButtons.Add(delayMinusButton);
            menuButtons.Add(delayPlusButton);

            int ChestColorX = startX + 60;
            int ChestColorY = startY + 400;

            ChestColorNames = Config.ChestColors.Keys.ToList();

            ChestColorIndex = ChestColorNames.IndexOf(Config.DeliveryChestColor);
            if (ChestColorIndex < 0)
                ChestColorIndex = 0;

            ChestColorButton = new ClickableTextureComponent(
                new Rectangle(ChestColorX, ChestColorY, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );

            int HighlightColorX = startX + 60;
            int HighlightColorY = startY + 475;

            HighlightColorNames = Config.HighlightColors.Keys.ToList();

            HighlightColorIndex = HighlightColorNames.IndexOf(Config.HighlightColor);
            if (HighlightColorIndex < 0)
                HighlightColorIndex = 0;

            HighlightColorButton = new ClickableTextureComponent(
                new Rectangle(HighlightColorX, HighlightColorY, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );

            int FontColorX = startX + 60;
            int FontColorY = startY + 550;

            FontColorNames = Config.FontColors.Keys.ToList();

            FontColorIndex = FontColorNames.IndexOf(Config.FontColor);
            if (FontColorIndex < 0)
                FontColorIndex = 0;

            FontColorButton = new ClickableTextureComponent(
                new Rectangle(FontColorX, FontColorY, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );

            int FuelLockerX = startX + 60;
            int FuelLockerY = startY + 625;

            FuelLockerButton = new ClickableComponent(
                new Rectangle(FuelLockerX, FuelLockerY, 440, 60),
                "FuelLocker"
            );

            int ProcessingLockerX = startX + 60;
            int ProcessingLockerY = startY + 700;

            ProcessingLockerButton = new ClickableComponent(
                new Rectangle(ProcessingLockerX, ProcessingLockerY, 440, 60),
                "ProcessingLocker"
            );
        }

        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = 800;
            int frameHeight = 840;
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

            base.draw(b);

            Utility.drawTextWithShadow(
                b,
                T("HotkeyBoxLabel"),
                Game1.smallFont,
                new Vector2(HotkeyBox.X + 60, HotkeyBox.Y + 8),
                Game1.textColor
            );

            HotkeyBox.Draw(b, false);

            Utility.drawTextWithShadow(
                b,
                T("ReductionLabel", new { Fee = FriendPower.DailyTimeReductionFee }),
                Game1.smallFont,
                new Vector2(TimeReductionCheckbox.bounds.X + 60, TimeReductionCheckbox.bounds.Y + 6),
                Game1.textColor
            );

            Utility.drawTextWithShadow(
                b,
                T("ReturnDamageLabel", new { Fee = FriendPower.DailyReturnFee }),
                Game1.smallFont,
                new Vector2(ReturnDamageCheckbox.bounds.X + 60, ReturnDamageCheckbox.bounds.Y + 6),
                Game1.textColor
            );

            Utility.drawTextWithShadow(
                b,
                T("FriendShieldLabel", new { Fee = FriendPower.DailyShieldFee }),
                Game1.smallFont,
                new Vector2(FriendShieldCheckbox.bounds.X + 60, FriendShieldCheckbox.bounds.Y + 6),
                Game1.textColor
            );

            Utility.drawTextWithShadow(
                b,
                T("DelayLabel"),
                Game1.smallFont,
                new Vector2(delayBox.X + 140, delayBox.Y + 8),
                Game1.textColor
            );

            delayBox.Draw(b, false);

            delayMinusButton.draw(b);
            delayPlusButton.draw(b);

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

            int ReturnToolbaradjusteddelay = (int)ReturnToolbarTextSize.X + 40;

            ReturnToolbarButton.bounds = new Rectangle(
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbaradjusteddelay,
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

            int ReturnCharactersadjusteddelay = (int)ReturnCharactersTextSize.X + 40;

            ReturnCharactersButton.bounds = new Rectangle(
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersadjusteddelay,
                ReturnCharactersButton.bounds.Height
            );

            TimeReductionCheckbox.draw(b);

            if (Config.EnableProcessTimeReduction)
            {
                b.Draw(
                    Game1.mouseCursors,
                    new Vector2(TimeReductionCheckbox.bounds.X + 0, TimeReductionCheckbox.bounds.Y + 0),
                    new Rectangle(236, 425, 9, 9),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }

            ReturnDamageCheckbox.draw(b);

            if (Config.ReturnDamage)
            {
                b.Draw(
                    Game1.mouseCursors,
                    new Vector2(ReturnDamageCheckbox.bounds.X + 0, ReturnDamageCheckbox.bounds.Y + 0),
                    new Rectangle(236, 425, 9, 9),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }

            FriendShieldCheckbox.draw(b);

            if (Config.FriendShield)
            {
                b.Draw(
                    Game1.mouseCursors,
                    new Vector2(FriendShieldCheckbox.bounds.X + 0, FriendShieldCheckbox.bounds.Y + 0),
                    new Rectangle(236, 425, 9, 9),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }

            Utility.drawTextWithShadow(
                b,
                T("ChestColorLabel"),
                Game1.smallFont,
                new Vector2(ChestColorButton.bounds.X + 220, ChestColorButton.bounds.Y + 12),
                Game1.textColor
            );

            drawTextureBox(
                b,
                ChestColorButton.bounds.X,
                ChestColorButton.bounds.Y,
                ChestColorButton.bounds.Width,
                ChestColorButton.bounds.Height,
                Color.White
            );

            string name = ChestColorNames[ChestColorIndex];
            b.DrawString(
                Game1.smallFont,
                name,
                new Vector2(ChestColorButton.bounds.X + 16, ChestColorButton.bounds.Y + 16),
                Color.Black
            );

            Color preview = Config.ChestColors[name];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    ChestColorButton.bounds.Right - 60,
                    ChestColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                preview
            );

            Utility.drawTextWithShadow(
                b,
                T("HighlightColorLabel"),
                Game1.smallFont,
                new Vector2(HighlightColorButton.bounds.X + 220, HighlightColorButton.bounds.Y + 12),
                Game1.textColor
            );

            drawTextureBox(
                b,
                HighlightColorButton.bounds.X,
                HighlightColorButton.bounds.Y,
                HighlightColorButton.bounds.Width,
                HighlightColorButton.bounds.Height,
                Color.White
            );

            string HCname = HighlightColorNames[HighlightColorIndex];
            b.DrawString(
                Game1.smallFont,
                HCname,
                new Vector2(HighlightColorButton.bounds.X + 16, HighlightColorButton.bounds.Y + 16),
                Color.Black
            );

            Color HCpreview = Config.HighlightColors[HCname];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    HighlightColorButton.bounds.Right - 60,
                    HighlightColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                HCpreview
            );

            Utility.drawTextWithShadow(
                b,
                T("FontColorLabel"),
                Game1.smallFont,
                new Vector2(FontColorButton.bounds.X + 220, FontColorButton.bounds.Y + 12),
                Game1.textColor
            );

            drawTextureBox(
                b,
                FontColorButton.bounds.X,
                FontColorButton.bounds.Y,
                FontColorButton.bounds.Width,
                FontColorButton.bounds.Height,
                Color.White
            );

            string FCname = FontColorNames[FontColorIndex];
            b.DrawString(
                Game1.smallFont,
                FCname,
                new Vector2(FontColorButton.bounds.X + 16, FontColorButton.bounds.Y + 16),
                Color.Black
            );

            Color FCpreview = Config.FontColors[FCname];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    FontColorButton.bounds.Right - 60,
                    FontColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                FCpreview
            );

            Color FuelLockerColor = FuelLockerButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                    ? Color.LimeGreen
                    : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                FuelLockerButton.bounds.X,
                FuelLockerButton.bounds.Y,
                FuelLockerButton.bounds.Width,
                FuelLockerButton.bounds.Height,
                FuelLockerColor,
                1f,
                false
            );

            string FuelLockertext = T("FuelLocker");
            Vector2 FuelLockertextSize = Game1.smallFont.MeasureString(FuelLockertext);

            float FuelLockertextX = FuelLockerButton.bounds.X + (FuelLockerButton.bounds.Width / 2f) - (FuelLockertextSize.X / 2f);
            float FuelLockertextY = FuelLockerButton.bounds.Y + (FuelLockerButton.bounds.Height / 2f) - (FuelLockertextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                FuelLockertext,
                Game1.smallFont,
                new Vector2(FuelLockertextX, FuelLockertextY),
                Game1.textColor
            );

            string FuelLockerText = T("FuelLocker");
            Vector2 FuelLockerTextSize = Game1.smallFont.MeasureString(FuelLockerText);

            int FuelLockeradjusteddelay = (int)FuelLockerTextSize.X + 40;

            FuelLockerButton.bounds = new Rectangle(
                FuelLockerButton.bounds.X,
                FuelLockerButton.bounds.Y,
                FuelLockeradjusteddelay,
                FuelLockerButton.bounds.Height
            );

            Color ProcessingLockerColor = ProcessingLockerButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                ? Color.LimeGreen
                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                ProcessingLockerButton.bounds.X,
                ProcessingLockerButton.bounds.Y,
                ProcessingLockerButton.bounds.Width,
                ProcessingLockerButton.bounds.Height,
                ProcessingLockerColor,
                1f,
                false
            );

            string ProcessingLockertext = T("ProcessingLocker");
            Vector2 ProcessingLockertextSize = Game1.smallFont.MeasureString(ProcessingLockertext);

            float ProcessingLockertextX = ProcessingLockerButton.bounds.X + (ProcessingLockerButton.bounds.Width / 2f) - (ProcessingLockertextSize.X / 2f);
            float ProcessingLockertextY = ProcessingLockerButton.bounds.Y + (ProcessingLockerButton.bounds.Height / 2f) - (ProcessingLockertextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                ProcessingLockertext,
                Game1.smallFont,
                new Vector2(ProcessingLockertextX, ProcessingLockertextY),
                Game1.textColor
            );

            string ProcessingLockerText = T("ProcessingLocker");
            Vector2 ProcessingLockerTextSize = Game1.smallFont.MeasureString(ProcessingLockerText);

            int ProcessingLockeradjusteddelay = (int)ProcessingLockerTextSize.X + 40;

            ProcessingLockerButton.bounds = new Rectangle(
                ProcessingLockerButton.bounds.X,
                ProcessingLockerButton.bounds.Y,
                ProcessingLockeradjusteddelay,
                ProcessingLockerButton.bounds.Height
            );

            drawMouse(b);

            if (ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnToolbarTooltip,
                    Game1.smallFont,
                    xOffset: -100
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

            if (HotKeyBoxClickable.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    HotkeyTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (delayBoxClickable.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    DelayTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (TimeReductionCheckbox.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReductionTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (ReturnDamageCheckbox.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnDamageTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (FriendShieldCheckbox.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    FriendShieldTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (ChestColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ChestColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (HighlightColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    HighlightColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (FontColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    FontColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (FuelLockerButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    FuelLockerTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );

                ForceShowFuelChestHighlight = true;
            }

            else
            {
                ForceShowFuelChestHighlight = false;
            }

            if (ProcessingLockerButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ProcessingLockerTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );

                ForceShowProcessChestHighlight = true;
            }

            else
            {
                ForceShowProcessChestHighlight = false;
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (HotKeyBoxClickable.containsPoint(x, y))
            {
                HotkeyBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = HotkeyBox;
            }

            else
            {
                if (HotkeyBox.Selected)
                   ApplyHotkeyChange();

                HotkeyBox.Selected = false;
            }

            if (delayBoxClickable.containsPoint(x, y))
            {
                delayBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = delayBox;
            }

            else
            {
                delayBox.Selected = false;
            }

            if (delayMinusButton.containsPoint(x, y))
            {
                Config.CollectionDelay = Math.Max(100, Config.CollectionDelay - 100);
                delayBox.Text = Config.CollectionDelay.ToString();
                Game1.playSound("smallSelect");

                Instance.Helper.WriteConfig(Config);
            }

            if (delayPlusButton.containsPoint(x, y))
            {
                Config.CollectionDelay = Math.Min(3000, Config.CollectionDelay + 100);
                delayBox.Text = Config.CollectionDelay.ToString();
                Game1.playSound("smallSelect");

                Instance.Helper.WriteConfig(Config);
            }

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

            if (TimeReductionCheckbox.containsPoint(x, y))
            {
                Config.EnableProcessTimeReduction = !Config.EnableProcessTimeReduction;
                Game1.playSound("drumkit6");

                if (Config.EnableProcessTimeReduction)
                {
                    ApplyProductionTimeReduction(Config, Instance.Monitor);
                    Game1.player.Money -= FriendPower.DailyTimeReductionFee;
                }

                Instance.Helper.WriteConfig(Config);
            }

            if (ReturnDamageCheckbox.containsPoint(x, y))
            {
                Config.ReturnDamage = !Config.ReturnDamage;
                Game1.playSound("drumkit6");

                if (Config.ReturnDamage)
                {
                    Game1.player.Money -= FriendPower.DailyReturnFee;
                }

                Instance.Helper.WriteConfig(Config);
            }

            if (FriendShieldCheckbox.containsPoint(x, y))
            {
                Config.FriendShield = !Config.FriendShield;
                Game1.playSound("drumkit6");

                if (Config.FriendShield)
                {
                    Game1.player.Money -= FriendPower.DailyShieldFee;
                }

                Instance.Helper.WriteConfig(Config);
            }

            if (ChestColorButton.containsPoint(x, y))
            {
                ChestColorIndex++;

                if (ChestColorIndex >= ChestColorNames.Count)
                    ChestColorIndex = 0;

                string selected = ChestColorNames[ChestColorIndex];
                Config.DeliveryChestColor = selected;

                Game1.playSound("smallSelect");

                Instance.Helper.WriteConfig(Config);
            }

            if (HighlightColorButton.containsPoint(x, y))
            {
                HighlightColorIndex++;

                if (HighlightColorIndex >= HighlightColorNames.Count)
                    HighlightColorIndex = 0;

                string selected = HighlightColorNames[HighlightColorIndex];
                Config.HighlightColor = selected;

                Game1.playSound("smallSelect");

                Instance.Helper.WriteConfig(Config);
            }

            if (FontColorButton.containsPoint(x, y))
            {
                FontColorIndex++;

                if (FontColorIndex >= FontColorNames.Count)
                    FontColorIndex = 0;

                string selected = FontColorNames[FontColorIndex];
                Config.FontColor = selected;

                Game1.playSound("smallSelect");

                Instance.Helper.WriteConfig(Config);
            }

            if (FuelLockerButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                WaitingForFuelLockerSelection = true;
                AssignFuelLocker();
                Game1.addHUDMessage(new HUDMessage(T("FuelLockerMessage"), HUDMessage.newQuest_type));
                return;
            }

            if (ProcessingLockerButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                WaitingForProcessingLockerSelection = true;
                AssignProcessLocker();
                Game1.addHUDMessage(new HUDMessage(T("ProcessingLockerMessage"), HUDMessage.newQuest_type));
                return;
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (int.TryParse(delayBox.Text, out int w))
            {
                Config.CollectionDelay = Math.Clamp(w, 100, 3000);
            }
        }

        private void ApplyHotkeyChange()
        {
            HotkeyBox.Text = HotkeyBox.Text.ToUpperInvariant();

            if (Enum.TryParse(HotkeyBox.Text, true, out SButton newKey))
            {
                Config.CheatMenuHotkey = newKey;
                Instance.Helper.WriteConfig(Config);
                Game1.showGlobalMessage(T("HotKeySet", new { Key = newKey }));
            }

            else
            {
                Game1.showRedMessage(T("InvalidKey"));
            }
        }
    }
}
