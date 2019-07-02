using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    class SGameConsole : IClickableMenu
    {
        public static SGameConsole Instance;
        public bool isVisible;

        private readonly LinkedList<KeyValuePair<ConsoleLogLevel, string>> consoleMessageQueue = new LinkedList<KeyValuePair<ConsoleLogLevel, string>>();
        private readonly TextBox textBox;
        private readonly MobileScrollbox scrollbox;
        private Rectangle textBoxBounds;

        private SpriteFont smallFont;

        private  Vector2 size;

        private bool scrolling = false;

        internal SGameConsole()
        {
            Instance = this;
            this.isVisible = true;
            this.textBox = new TextBox(null, null, Game1.dialogueFont, Game1.textColor)
            {
                X = 0,
                Y = 0,
                Width = 1280,
                Height = 320
            };
            this.scrollbox = new MobileScrollbox(0, 0, 1280, 320, this.consoleMessageQueue.Count, new Rectangle(0, 0, 1280, 320));
            this.textBoxBounds = new Rectangle(this.textBox.X, this.textBox.Y, this.textBox.Width, this.textBox.Height);
            this.scrollbox.Bounds = this.textBoxBounds;

            
        }

        internal void InitializeContent(LocalizedContentManager content)
        {
            this.smallFont = content.Load<SpriteFont>(@"Fonts\SmallFont");
            this.size = this.smallFont.MeasureString("aA");
        }

        public void Show()
        {
            Game1.activeClickableMenu = this;
            this.isVisible = true;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.textBoxBounds.Contains(x, y))
            {
                this.scrollbox.receiveLeftClick(x, y);
                this.scrolling = this.scrollbox.panelScrolling;
                typeof(TextBox).GetMethod("ShowAndroidKeyboard", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(this.textBox, new object[] { });
                Game1.keyboardDispatcher.Subscriber = this.textBox;
                SGame.instance.CommandQueue.Enqueue(this.textBox.Text);
                this.textBox.Text = "";
            }
            else
            {
                Game1.activeClickableMenu = null;
                this.isVisible = false;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.scrolling)
            {
                this.scrollbox.leftClickHeld(x, y);
                this.scrollbox.setYOffsetForScroll(9999);
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            this.scrollbox.releaseLeftClick(x, y);
        }

        public void WriteLine(string consoleMessage, ConsoleLogLevel level)
        {
            lock (this.consoleMessageQueue)
            {
                this.consoleMessageQueue.AddFirst(new KeyValuePair<ConsoleLogLevel, string>(level, consoleMessage));
                if (this.consoleMessageQueue.Count > 2000)
                {
                    this.consoleMessageQueue.RemoveLast();
                }
            }
        }

        public override void update(GameTime time)
        {
            this.scrollbox.update(time);
        }

        public override void draw(SpriteBatch b)
        {
            float y = Game1.game1.screen.Height - this.size.Y;
            lock (this.consoleMessageQueue)
            {
                foreach (var log in this.consoleMessageQueue)
                {
                    string text = log.Value;
                    if (text.Length > 125)
                    {
                        text = text.Insert(125, "\n");
                    }
                    switch (log.Key)
                    {
                        case ConsoleLogLevel.Critical:
                        case ConsoleLogLevel.Error:
                            b.DrawString(this.smallFont, text, new Vector2(16, y), Color.Red);
                            break;
                        case ConsoleLogLevel.Alert:
                        case ConsoleLogLevel.Warn:
                            b.DrawString(this.smallFont, text, new Vector2(16, y), Color.Orange);
                            break;
                        case ConsoleLogLevel.Info:
                        case ConsoleLogLevel.Success:
                            b.DrawString(this.smallFont, text, new Vector2(16, y), Color.AntiqueWhite);
                            break;
                        case ConsoleLogLevel.Debug:
                        case ConsoleLogLevel.Trace:
                            b.DrawString(this.smallFont, text, new Vector2(16, y), Color.LightGray);
                            break;
                        default:
                            b.DrawString(this.smallFont, text, new Vector2(16, y), Color.LightGray);
                            break;
                    }
                    
                    this.size = this.smallFont.MeasureString(text);
                    if (y < 0)
                    {
                        break;
                    }
                    y -= this.size.Y;
                }
            }
        }
    }
}
