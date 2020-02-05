using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class VirtualToggle
    {
        private readonly IModHelper helper;
        private readonly IMonitor Monitor;

        private int enabledStage = 0;
        private bool isDefault = true;
        private ClickableTextureComponent virtualToggleButton;

        private List<KeyButton> keyboard = new List<KeyButton>();
        private List<KeyButton> keyboardExtend = new List<KeyButton>();
        private ModConfig modConfig;
        private Texture2D texture;

        public VirtualToggle(IModHelper helper, IMonitor monitor)
        {
            this.Monitor = monitor;
            this.helper = helper;
            this.texture = this.helper.Content.Load<Texture2D>("assets/togglebutton.png", ContentSource.ModFolder);

            this.modConfig = helper.ReadConfig<ModConfig>();
            for (int i = 0; i < this.modConfig.buttons.Length; i++)
                this.keyboard.Add(new KeyButton(helper, this.modConfig.buttons[i], this.Monitor));
            for (int i = 0; i < this.modConfig.buttonsExtend.Length; i++)
                this.keyboardExtend.Add(new KeyButton(helper, this.modConfig.buttonsExtend[i], this.Monitor));

            if (this.modConfig.vToggle.rectangle.X != 36 || this.modConfig.vToggle.rectangle.Y != 12)
                this.isDefault = false;

            this.virtualToggleButton = new ClickableTextureComponent(new Rectangle(Game1.toolbarPaddingX + 64, 12, 128, 128), this.texture, new Rectangle(0, 0, 16, 16), 5.75f, false);
            helper.WriteConfig(this.modConfig);

            this.helper.Events.Display.RenderingHud += this.OnRenderingHUD;
            this.helper.Events.Input.ButtonPressed += this.VirtualToggleButtonPressed;
        }

        private void VirtualToggleButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            Vector2 screenPixels = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(screenPixels))
            {
                switch (this.enabledStage)
                {
                    case 0:
                        foreach (var keys in this.keyboard)
                        {
                            keys.hidden = false;
                        }
                        foreach (var keys in this.keyboardExtend)
                        {
                            keys.hidden = true;
                        }
                        this.enabledStage = 1;
                        break;
                    case 1 when this.keyboardExtend.Count > 0:
                        foreach (var keys in this.keyboardExtend)
                        {
                            keys.hidden = false;
                        }
                        this.enabledStage = 2;
                        break;
                    default:
                        foreach (var keys in this.keyboard)
                        {
                            keys.hidden = true;
                        }
                        foreach (var keys in this.keyboardExtend)
                        {
                            keys.hidden = true;
                        }
                        this.enabledStage = 0;
                        if (Game1.activeClickableMenu is IClickableMenu menu)
                        {
                            menu.exitThisMenu();
                            Toolbar.toolbarPressed = true;
                        }
                        break;
                }
            }
        }

        private bool shouldTrigger(Vector2 screenPixels)
        {
            if (this.virtualToggleButton.containsPoint((int)(screenPixels.X * Game1.options.zoomLevel), (int)(screenPixels.Y * Game1.options.zoomLevel)))
            {
                Toolbar.toolbarPressed = true;
                return true;
            }
            return false;
        }

        private void OnRenderingHUD(object sender, EventArgs e)
        {
            if (this.isDefault)
            {
                if (Game1.options.verticalToolbar)
                    this.virtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 200;
                else
                    this.virtualToggleButton.bounds.X = Game1.toolbarPaddingX + Game1.toolbar.itemSlotSize + 50;

                if (Game1.toolbar.alignTop == true && !Game1.options.verticalToolbar)
                {
                    object toolbarHeight = this.helper.Reflection.GetField<int>(Game1.toolbar, "toolbarHeight").GetValue();
                    this.virtualToggleButton.bounds.Y = (int)toolbarHeight + 50;
                }
                else
                {
                    this.virtualToggleButton.bounds.Y = 12;
                }
            }
            else
            {
                this.virtualToggleButton.bounds.X = this.modConfig.vToggle.rectangle.X;
                this.virtualToggleButton.bounds.Y = this.modConfig.vToggle.rectangle.Y;
            }

            float scale = 1f;
            if (this.enabledStage == 0)
            {
                scale = 0.5f;
            }
            if (!Game1.eventUp && Game1.activeClickableMenu is GameMenu == false && Game1.activeClickableMenu is ShopMenu == false)
                scale = 0.25f;
            this.virtualToggleButton.draw(Game1.spriteBatch, Color.White * scale, 0.000001f);
        }
    }
}
