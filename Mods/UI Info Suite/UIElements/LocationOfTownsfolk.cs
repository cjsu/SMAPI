using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UIInfoSuite.UIElements
{
    class LocationOfTownsfolk : IDisposable
    {
        #region Members
        private List<NPC> _townsfolk = new List<NPC>();
        private List<OptionsCheckbox> _checkboxes = new List<OptionsCheckbox>();
        private const int SocialPanelWidth = 190;
        private const int SocialPanelXOffset = 160;
        private SocialPage _socialPage;
        private string[] _friendNames;
        private readonly IDictionary<string, string> _options;
        private readonly IModHelper _helper;

        private static readonly Dictionary<string, KeyValuePair<int, int>> _mapLocations = new Dictionary<string, KeyValuePair<int, int>>()
        {
            { "HarveyRoom", new KeyValuePair<int, int>(677, 304) },
            { "BathHouse_Pool", new KeyValuePair<int, int>(576, 60) },
            { "WizardHouseBasement", new KeyValuePair<int, int>(196, 352) },
            { "BugLand", new KeyValuePair<int, int>(0, 0) },
            { "Desert", new KeyValuePair<int, int>(60, 92) },
            { "Cellar", new KeyValuePair<int, int>(0, 0) },
            { "JojaMart", new KeyValuePair<int, int>(872, 280) },
            { "Tent", new KeyValuePair<int, int>(784, 128) },
            { "HaleyHouse", new KeyValuePair<int, int>(652, 408) },
            { "Hospital", new KeyValuePair<int, int>(677, 304) },
            { "FarmHouse", new KeyValuePair<int, int>(470, 260) },
            { "Farm", new KeyValuePair<int, int>(470, 260) },
            { "ScienceHouse", new KeyValuePair<int, int>(732, 148) },
            { "ManorHouse", new KeyValuePair<int, int>(768, 395) },
            { "AdventureGuild", new KeyValuePair<int, int>(0, 0) },
            { "SeedShop", new KeyValuePair<int, int>(696, 296) },
            { "Blacksmith", new KeyValuePair<int, int>(852, 388) },
            { "JoshHouse", new KeyValuePair<int, int>(740, 320) },
            { "SandyHouse", new KeyValuePair<int, int>(40, 40) },
            { "Tunnel", new KeyValuePair<int, int>(0, 0) },
            { "CommunityCenter", new KeyValuePair<int, int>(692, 204) },
            { "Backwoods", new KeyValuePair<int, int>(460, 156) },
            { "ElliottHouse", new KeyValuePair<int, int>(826, 550) },
            { "SebastianRoom", new KeyValuePair<int, int>(732, 148) },
            { "BathHouse_Entry", new KeyValuePair<int, int>(576, 60) },
            { "Greenhouse", new KeyValuePair<int, int>(0, 0) },
            { "Sewer", new KeyValuePair<int, int>(380, 596) },
            { "WizardHouse", new KeyValuePair<int, int>(196, 352) },
            { "Trailer", new KeyValuePair<int, int>(780, 360) },
            { "pamHouseUpgrade", new KeyValuePair<int, int>(780, 360) },
            { "Forest", new KeyValuePair<int, int>(80, 272) },
            { "Woods", new KeyValuePair<int, int>(100, 272) },
            { "WitchSwamp", new KeyValuePair<int, int>(0, 0) },
            { "ArchaeologyHouse", new KeyValuePair<int, int>(892, 416) },
            { "FishShop", new KeyValuePair<int, int>(844, 608) },
            { "Saloon", new KeyValuePair<int, int>(714, 354) },
            { "LeahHouse", new KeyValuePair<int, int>(452, 436) },
            { "Town", new KeyValuePair<int, int>(680, 360) },
            { "Mountain", new KeyValuePair<int, int>(762, 154) },
            { "BusStop", new KeyValuePair<int, int>(516, 224) },
            { "Railroad", new KeyValuePair<int, int>(644, 64) },
            { "SkullCave", new KeyValuePair<int, int>(0, 0) },
            { "BathHouse_WomensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Beach", new KeyValuePair<int, int>(790, 550) },
            { "BathHouse_MensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Mine", new KeyValuePair<int, int>(880, 100) },
            { "WitchHut", new KeyValuePair<int, int>(0, 0) },
            { "AnimalShop", new KeyValuePair<int, int>(420, 392) },
            { "SamHouse", new KeyValuePair<int, int>(612, 396) },
            { "WitchWarpCave", new KeyValuePair<int, int>(0, 0) },
            { "Club", new KeyValuePair<int, int>(60, 92) }
        };

#endregion

        public LocationOfTownsfolk(IModHelper helper, IDictionary<string, string> options)
        {
            this._helper = helper;
            this._options = options;
        }

        public void Dispose()
        {
            this.ToggleShowNPCLocationsOnMap(false);
        }

        public void ToggleShowNPCLocationsOnMap(bool showLocations)
        {
            this.ExtendMenuIfNeeded();
            this._helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu_DrawSocialPageOptions;
            this._helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu_DrawNPCLocationsOnMap;
            this._helper.Events.Input.ButtonPressed -= this.OnButtonPressed_ForSocialPage;
            this._helper.Events.Display.MenuChanged -= this.OnMenuChanged;

            if (showLocations)
            {
                this._helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu_DrawSocialPageOptions;
                this._helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu_DrawNPCLocationsOnMap;
                this._helper.Events.Input.ButtonPressed += this.OnButtonPressed_ForSocialPage;
                this._helper.Events.Display.MenuChanged += this.OnMenuChanged;
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            this.ExtendMenuIfNeeded();
        }

        private void ExtendMenuIfNeeded()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                List<IClickableMenu> clickableMenuList = gameMenu.pages;

                foreach (IClickableMenu menu in clickableMenuList)
                {
                    if (menu is SocialPage)
                    {
                        this._socialPage = menu as SocialPage;
                        this._friendNames = (typeof(SocialPage).GetField("names", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu) as List<object>)
                            .Select(name => name.ToString())
                            .ToArray();
                        break;
                    }
                }
                this._townsfolk.Clear();
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (NPC npc in location.characters)
                    {
                        if (Game1.player.friendshipData.ContainsKey(npc.Name))
                            this._townsfolk.Add(npc);
                    }
                }
                this._checkboxes.Clear();
                foreach (string friendName in this._friendNames)
                {
                    int hashCode = friendName.GetHashCode();
                    OptionsCheckbox checkbox = new OptionsCheckbox("", hashCode);
                    this._checkboxes.Add(checkbox);

                    //default to on
                    bool optionForThisFriend = true;
                    if (!Game1.player.friendshipData.ContainsKey(friendName))
                    {
                        checkbox.greyedOut = true;
                        optionForThisFriend = false;
                    }
                    else
                    {
                        string optionValue = this._options.SafeGet(hashCode.ToString());

                        if (string.IsNullOrEmpty(optionValue))
                        {
                            this._options[hashCode.ToString()] = optionForThisFriend.ToString();
                        }
                        else
                        {
                            optionForThisFriend = optionValue.SafeParseBool();
                        }
                    }
                    checkbox.isChecked = optionForThisFriend;
                }
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed_ForSocialPage(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu && (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA))
            {
                this.CheckSelectedBox();
            }
        }

        private void CheckSelectedBox()
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                int slotPosition = (int)typeof(SocialPage)
                    .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(this._socialPage);

                for (int i = slotPosition; i < slotPosition + 5; ++i)
                {
                    OptionsCheckbox checkbox = this._checkboxes[i];
                    if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()) &&
                        !checkbox.greyedOut)
                    {
                        checkbox.isChecked = !checkbox.isChecked;
                        this._options[checkbox.whichOption.ToString()] = checkbox.isChecked.ToString();
                        Game1.playSound("drumkit6");
                    }
                }
            }
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu_DrawNPCLocationsOnMap(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                if (gameMenu.currentTab == 4)
                {
                    List<string> namesToShow = new List<string>();
                    foreach (NPC character in this._townsfolk)
                    {
                        try
                        {
                            int hashCode = character.Name.GetHashCode();

                            bool drawCharacter = this._options.SafeGet(hashCode.ToString()).SafeParseBool();

                            if (drawCharacter)
                            {
                                KeyValuePair<int, int> location = new KeyValuePair<int, int>((int)character.Position.X, (int)character.position.Y);
                                string locationName = character.currentLocation?.Name ?? character.DefaultMap;

                                switch (locationName)
                                {
                                    case "Town":
                                    case "Forest":
                                        {
                                            int xStart = 0;
                                            int yStart = 0;
                                            int areaWidth = 0;
                                            int areaHeight = 0;

                                            switch (locationName)
                                            {
                                                case "Town":
                                                    {
                                                        xStart = 595;
                                                        yStart = 163;
                                                        areaWidth = 345;
                                                        areaHeight = 330;
                                                        break;
                                                    }

                                                case "Forest":
                                                    {
                                                        xStart = 183;
                                                        yStart = 378;
                                                        areaWidth = 319;
                                                        areaHeight = 261;
                                                        break;
                                                    }
                                            }
                                            xTile.Map map = character.currentLocation.Map;

                                            float xScale = (float)areaWidth / (float)map.DisplayWidth;
                                            float yScale = (float)areaHeight / (float)map.DisplayHeight;

                                            float scaledX = character.position.X * xScale;
                                            float scaledY = character.position.Y * yScale;
                                            int xPos = (int)scaledX + xStart;
                                            int yPos = (int)scaledY + yStart;
                                            location = new KeyValuePair<int, int>(xPos, yPos);

                                            break;
                                        }

                                    default:
                                        {
                                            _mapLocations.TryGetValue(locationName, out location);
                                            break;
                                        }
                                }

                                //if (character.currentLocation.Name == "Town")
                                //{
                                //    String locationName = character.currentLocation.Name;
                                //    xTile.Map map = character.currentLocation.Map;
                                //    int xStart = 595;
                                //    int yStart = 163;
                                //    int townWidth = 345;
                                //    int townHeight = 330;

                                //    float xScale = (float)townWidth / (float)map.DisplayWidth;
                                //    float yScale = (float)townHeight / (float)map.DisplayHeight;

                                //    float scaledX = character.position.X * xScale;
                                //    float scaledY = character.position.Y * yScale;
                                //    int xPos = (int)scaledX + xStart;
                                //    int yPos = (int)scaledY + yStart;
                                //    location = new KeyValuePair<int, int>(xPos, yPos);
                                //}
                                //else
                                //{
                                //    _mapLocations.TryGetValue(character.currentLocation.name, out location);
                                //}
                                Rectangle headShot = character.GetHeadShot();
                                int xBase = Game1.activeClickableMenu.xPositionOnScreen - 20;
                                int yBase = Game1.activeClickableMenu.yPositionOnScreen + 40;

                                int x = xBase + location.Key;
                                int y = yBase + location.Value;

                                Color color = character.CurrentDialogue.Count <= 0 ?
                                    Color.Gray : Color.White;
                                ClickableTextureComponent textureComponent =
                                    new ClickableTextureComponent(
                                        character.Name,
                                        new Rectangle(x, y, 0, 0),
                                        null,
                                        character.Name,
                                        character.Sprite.Texture,
                                        headShot,
                                        2.3f);

                                float headShotScale = 2f;
                                Game1.spriteBatch.Draw(
                                    character.Sprite.Texture,
                                    new Vector2(x, y),
                                    new Rectangle?(headShot),
                                    color,
                                    0.0f,
                                    Vector2.Zero,
                                    headShotScale,
                                    SpriteEffects.None,
                                    1f);

                                int mouseX = Game1.getMouseX();
                                int mouseY = Game1.getMouseY();

                                if (mouseX >= x && mouseX <= x + headShot.Width * headShotScale &&
                                    mouseY >= y && mouseY <= y + headShot.Height * headShotScale)
                                {
                                    namesToShow.Add(character.displayName);
                                }

                                foreach (Quest quest in Game1.player.questLog)
                                {
                                    if (quest.accepted.Value && quest.dailyQuest.Value && !quest.completed.Value)
                                    {
                                        bool isQuestTarget = false;
                                        switch (quest.questType.Value)
                                        {
                                            case 3: isQuestTarget = (quest as ItemDeliveryQuest).target.Value == character.Name; break;
                                            case 4: isQuestTarget = (quest as SlayMonsterQuest).target.Value == character.Name; break;
                                            case 7: isQuestTarget = (quest as FishingQuest).target.Value == character.Name; break;
                                            case 10: isQuestTarget = (quest as ResourceCollectionQuest).target.Value == character.Name; break;
                                        }

                                        if (isQuestTarget)
                                            Game1.spriteBatch.Draw(
                                                Game1.mouseCursors,
                                                new Vector2(x + 10, y - 12),
                                                new Rectangle(394, 495, 4, 10),
                                                Color.White,
                                                0.0f,
                                                Vector2.Zero,
                                                3f,
                                                SpriteEffects.None,
                                                1f);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
                        }
                    }

                    if (namesToShow.Count > 0)
                    {
                        StringBuilder text = new StringBuilder();
                        int longestLength = 0;
                        foreach (string name in namesToShow)
                        {
                            text.AppendLine(name);
                            longestLength = Math.Max(longestLength, (int)Math.Ceiling(Game1.smallFont.MeasureString(name).Length()));
                        }

                        int windowHeight = Game1.smallFont.LineSpacing * namesToShow.Count + 25;
                        Vector2 windowPos = new Vector2(Game1.getMouseX() + 40, Game1.getMouseY() - windowHeight);
                        IClickableMenu.drawTextureBox(
                            Game1.spriteBatch,
                            (int)windowPos.X,
                            (int)windowPos.Y,
                            longestLength + 30,
                            Game1.smallFont.LineSpacing * namesToShow.Count + 25,
                            Color.White);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            text,
                            new Vector2(windowPos.X + 17, windowPos.Y + 17),
                            Game1.textShadowColor);

                        Game1.spriteBatch.DrawString(
                            Game1.smallFont,
                            text,
                            new Vector2(windowPos.X + 15, windowPos.Y + 15),
                            Game1.textColor);
                    }

                    //The cursor needs to show up in front of the character faces
                    Tools.DrawMouseCursor();

                    string hoverText = (string)typeof(MapPage)
                        .GetField(
                            "hoverText",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(gameMenu.pages[gameMenu.currentTab]);

                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText,
                        Game1.smallFont);
                }
            }
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu_DrawSocialPageOptions(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 2)
            {
                Game1.drawDialogueBox(
                    Game1.activeClickableMenu.xPositionOnScreen - SocialPanelXOffset, 
                    Game1.activeClickableMenu.yPositionOnScreen, 
                    SocialPanelWidth, 
                    Game1.activeClickableMenu.height, 
                    false, 
                    true);

                int slotPosition = (int)typeof(SocialPage)
                    .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(this._socialPage);
                int yOffset = 0;

                for (int i = slotPosition; i < slotPosition + 5 && i < this._friendNames.Length; ++i)
                {
                    OptionsCheckbox checkbox = this._checkboxes[i];
                    checkbox.bounds.X = Game1.activeClickableMenu.xPositionOnScreen - 60;

                    checkbox.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;

                    checkbox.draw(Game1.spriteBatch, 0, 0);
                    yOffset += 112;
                    Color color = checkbox.isChecked ? Color.White : Color.Gray;

                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors, 
                        new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y), 
                        new Rectangle(80, 0, 16, 16), 
                        color,
                        0.0f, 
                        Vector2.Zero, 
                        3f, 
                        SpriteEffects.None, 
                        1f);

                    if (yOffset != 560)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.staminaRect, 
                            new Rectangle(
                                checkbox.bounds.X - 50, 
                                checkbox.bounds.Y + 72, 
                                SocialPanelWidth / 2 - 6, 
                                4), 
                            Color.SaddleBrown);

                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                checkbox.bounds.X - 50,
                                checkbox.bounds.Y + 76,
                                SocialPanelWidth / 2 - 6,
                                4),
                            Color.BurlyWood);
                    }
                    if (!Game1.options.hardwareCursor)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            new Vector2(
                                Game1.getMouseX(),
                                Game1.getMouseY()),
                            Game1.getSourceRectForStandardTileSheet(
                                Game1.mouseCursors,
                                Game1.mouseCursor,
                                16,
                                16),
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f),
                            SpriteEffects.None,
                            1f);
                    }

                    if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                        IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            "Track on map",
                            Game1.dialogueFont);
                }
            }
        }
    }
}
