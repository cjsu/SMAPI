using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    public class SGameConsole : IClickableMenu
    {
        public static SGameConsole Instance;
        public bool isVisible;

        private readonly LinkedList<KeyValuePair<ConsoleLogLevel, string>> consoleMessageQueue = new LinkedList<KeyValuePair<ConsoleLogLevel, string>>();
        private MobileScrollbox scrollbox;

        private ClickableTextureComponent commandButton;

        private SpriteFont smallFont;

        private bool scrolling = false;

        private int scrollLastFakeY = 0;

        private int scrollLastY = 0;

        private int MaxScrollBoxHeight => (int)(Game1.graphics.PreferredBackBufferHeight * 20 / Game1.NativeZoomLevel);

        private int MaxTextAreaWidth => (int)((Game1.graphics.PreferredBackBufferWidth - 32) / Game1.NativeZoomLevel);

        internal SGameConsole()
        {
            Instance = this;
            this.isVisible = true;
        }

        internal void InitializeContent(LocalizedContentManager content)
        {
            this.scrollbox = new MobileScrollbox(0, 0, this.MaxTextAreaWidth, (int)(Game1.graphics.PreferredBackBufferHeight / Game1.NativeZoomLevel), this.MaxScrollBoxHeight,
                new Rectangle(0, 0, (int)(Game1.graphics.PreferredBackBufferWidth / Game1.NativeZoomLevel), (int)(Game1.graphics.PreferredBackBufferHeight / Game1.NativeZoomLevel)));
            this.smallFont = content.Load<SpriteFont>(@"Fonts\SmallFont");
        }

        public void Show()
        {
            if (this.upperRightCloseButton == null)
                this.initializeUpperRightCloseButton();
            if (this.commandButton == null)
                this.commandButton = new ClickableTextureComponent(new Rectangle(16, 0, 64, 64), Game1.mobileSpriteSheet, new Rectangle(0, 44, 16, 16), 4f, false);
            Game1.activeClickableMenu = this;
            this.isVisible = true;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {

            if (this.upperRightCloseButton.bounds.Contains(x, y))
            {
                this.isVisible = false;
                Game1.activeClickableMenu = null;
                Game1.playSound("bigDeSelect");
            }
            else if (this.commandButton.bounds.Contains(x, y))
            {
                Game1.activeClickableMenu = new NamingMenu(this.textBoxEnter, "Command", "")
                {
                    randomButton = new ClickableTextureComponent(new Rectangle(-100, -100, 0, 0), Game1.mobileSpriteSheet, new Rectangle(87, 22, 20, 20), 4f, false)
                };
                this.isVisible = false;
                Game1.playSound("bigDeSelect");
            }
            else
            {
                this.scrollLastFakeY = y;
                this.scrollLastY = y;
                this.scrolling = true;
                this.scrollbox.receiveLeftClick(x, y);
            }
        }

        public void textBoxEnter(string text)
        {
            string command = text.Trim();
            if (command.Length > 0)
            {
                if (command.EndsWith(";"))
                {
                    command = command.TrimEnd(';');
                    SGame.instance.CommandQueue.Enqueue(command);
                    this.exitThisMenu();
                    return;
                }
                SGame.instance.CommandQueue.Enqueue(command);
            }
            this.isVisible = true;
            Game1.activeClickableMenu = this;
        }

        public override void leftClickHeld(int x, int y)
        {
            if (this.scrolling)
            {
                int tmp = y;
                y = this.scrollLastFakeY + this.scrollLastY - y;
                this.scrollLastY = tmp;
                this.scrollLastFakeY = y;
                this.scrollbox.leftClickHeld(x, y);
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            this.scrolling = false;
            this.scrollbox.releaseLeftClick(x, y);
        }

        internal void WriteLine(string consoleMessage, ConsoleLogLevel level)
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

        private string _parseText(string text)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strings = text.Split("\n");
            foreach (string t in strings)
            {
                string[] wordArray = t.Split(' ');
                foreach (string word in wordArray)
                {
                    if (this.smallFont.MeasureString(line + word).X > this.MaxTextAreaWidth)
                    {
                        returnString = returnString + line + '\n';
                        line = string.Empty;
                    }
                    line = line + word + ' ';
                }
                returnString = returnString + line + '\n';
                line = string.Empty;
            }
            returnString.TrimEnd('\n');
            return returnString;
        }

        public override void draw(SpriteBatch b)
        {
            this.scrollbox.setUpForScrollBoxDrawing(b);
            lock (this.consoleMessageQueue)
            {
                float offset = 0;
                foreach (var log in this.consoleMessageQueue)
                {
                    string text = this._parseText(log.Value);
                    Vector2 size = this.smallFont.MeasureString(text);
                    float y = Game1.game1.screen.Height - size.Y - offset - this.scrollbox.getYOffsetForScroll();
                    offset += size.Y;
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

                    if (offset > this.MaxScrollBoxHeight)
                    {
                        break;
                    }
                }
            }
            this.scrollbox.finishScrollBoxDrawing(b);
            if (Context.IsWorldReady)
            {
                this.upperRightCloseButton.draw(b);
                this.commandButton.draw(b);
            }
        }
    }
}
