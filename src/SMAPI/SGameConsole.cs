using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        private TextBoxEvent textBoxEvent;

        private Vector2 size;

        private bool scrolling = false;

        internal SGameConsole()
        {
            Instance = this;
            this.isVisible = true;
            this.textBox = new TextBox(null, null, Game1.dialogueFont, Game1.textColor)
            {
                X = 0,
                Y = 100,
                Width = IClickableMenu.viewport.Width,
                Height = IClickableMenu.viewport.Height
            };
            this.scrollbox = new MobileScrollbox(0, 0, 1280, 320, this.consoleMessageQueue.Count, new Rectangle(0, 0, 1280, 320));
            this.textBoxBounds = new Rectangle(this.textBox.X, this.textBox.Y, this.textBox.Width, this.textBox.Height);
            this.scrollbox.Bounds = this.textBoxBounds;
            this.textBoxEvent = new TextBoxEvent(this.textBoxEnter);
        }

        internal void InitializeContent(LocalizedContentManager content)
        {
            this.smallFont = content.Load<SpriteFont>(@"Fonts\SmallFont");
            this.size = this.smallFont.MeasureString("aA");
        }

        public void Show()
        {
            if (this.upperRightCloseButton == null)
                this.initializeUpperRightCloseButton();
            Game1.activeClickableMenu = this;
            this.isVisible = true;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.textBoxBounds.Contains(x, y))
            {
                this.scrollbox.receiveLeftClick(x, y);
                this.scrolling = this.scrollbox.panelScrolling;
                this.textBox.Selected = true;
                this.textBox.OnEnterPressed += this.textBoxEvent;

                this.textBox.Update();
                Game1.keyboardDispatcher.Subscriber = this.textBox;
                this.textBoxEnter(this.textBox);
            }

            if (this.upperRightCloseButton.bounds.Contains(x, y))
            {
                this.isVisible = false;
                Game1.activeClickableMenu = null;
                Game1.playSound("bigDeSelect");
            }
        }

        public void textBoxEnter(TextBox text)
        {
            this.textBox.OnEnterPressed -= this.textBoxEvent;
            string command = text.Text.Trim();

            SGame.instance.CommandQueue.Enqueue(command);
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
                    if (text.Contains("\n"))
                    {
                        text = text.Replace("\n", "");
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

            if (Context.IsWorldReady)
                this.upperRightCloseButton.draw(b);
        }
    }
}
