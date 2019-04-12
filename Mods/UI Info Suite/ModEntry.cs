using UIInfoSuite.Options;
using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UIInfoSuite
{
    public class ModEntry : Mod
    {
        private SkipIntro _skipIntro;

        private string _modDataFileName;
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>();

        public static IMonitor MonitorObject { get; private set; }
        public static IModHelper HelperObject { get; private set; }

        private ModOptionsPageHandler _modOptionsPageHandler;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //Helper = helper;
            MonitorObject = this.Monitor;
            HelperObject = this.Helper;
            this._skipIntro = new SkipIntro(helper.Events);

            this.Monitor.Log("starting.", LogLevel.Debug);
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saved += this.OnSaved;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Display.Rendering += IconHandler.Handler.Reset;

            //Resources = new ResourceManager("UIInfoSuite.Resource.strings", Assembly.GetAssembly(typeof(ModEntry)));
            //try
            //{
            //    //Test to make sure the culture specific files are there
            //    Resources.GetString(LanguageKeys.Days, ModEntry.SpecificCulture);
            //}
            //catch
            //{
            //    Resources = Properties.Resources.ResourceManager;
            //}
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            this._modOptionsPageHandler?.Dispose();
            this._modOptionsPageHandler = null;
        }

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaved(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this._modDataFileName))
            {
                if (File.Exists(this._modDataFileName))
                    File.Delete(this._modDataFileName);
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.IndentChars = "  ";
                using (XmlWriter writer = XmlWriter.Create(File.Open(this._modDataFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), settings))
                {
                    writer.WriteStartElement("options");

                    foreach (KeyValuePair<string, string> option in this._options)
                    {
                        writer.WriteStartElement("option");
                        writer.WriteAttributeString("name", option.Key);
                        writer.WriteValue(option.Value);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Close();
                }
            }
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            try
            {
                try
                {
                    this._modDataFileName = Path.Combine(this.Helper.DirectoryPath, Game1.player.Name + "_modData.xml");
                }
                catch
                {
                    this.Monitor.Log("Error: Player name contains character that cannot be used in file name. Using generic file name." + Environment.NewLine +
                        "Options may not be able to be different between characters.", LogLevel.Warn);
                    this._modDataFileName = Path.Combine(this.Helper.DirectoryPath, "default_modData.xml");
                }

                if (File.Exists(this._modDataFileName))
                {
                    XmlDocument document = new XmlDocument();

                    document.Load(this._modDataFileName);
                    XmlNodeList nodes = document.GetElementsByTagName("option");

                    foreach (XmlNode node in nodes)
                    {
                        string key = node.Attributes["name"]?.Value;
                        string value = node.InnerText;

                        if (key != null)
                            this._options[key] = value;
                    }

                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log("Error loading mod config. " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }

            this._modOptionsPageHandler = new ModOptionsPageHandler(this.Helper, this._options);
        }

       
    }
}
