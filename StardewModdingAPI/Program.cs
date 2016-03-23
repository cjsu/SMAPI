using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Inheritance;
using StardewModdingAPI.Inheritance.Menus;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    public class Program
    {
        private static List<string> _modPaths;
        //private static List<string> _modContentPaths;

        public static Texture2D DebugPixel { get; private set; }

        public static SGame gamePtr;
        public static bool ready;

        public static Assembly StardewAssembly;
        public static Type StardewProgramType;
        public static FieldInfo StardewGameInfo;
        public static Form StardewForm;

        public static Thread gameThread;
        public static Thread consoleInputThread;

        public static bool StardewInjectorLoaded { get; private set; }
        public static Mod StardewInjectorMod { get; private set; }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Main method holding the API execution
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            try
            {
                ConfigureUI();
                ConfigurePaths();
                ConfigureSDV();

                GameRunInvoker();
            }
            catch (Exception e)
            {
                // Catch and display all exceptions. 
                Log.Error("Critical error: " + e);
            }

            Log.Comment("The API will now terminate. Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Set up the console properties
        /// </summary>
        private static void ConfigureUI()
        {
            Console.Title = Constants.ConsoleTitle;

#if DEBUG
            Console.Title += " - DEBUG IS NOT FALSE, AUTHOUR NEEDS TO REUPLOAD THIS VERSION";
#endif
        }

        /// <summary>
        /// Setup the required paths and logging
        /// </summary>
        private static void ConfigurePaths()
        {
            Log.Info("Validating api paths...");

            _modPaths = new List<string>();
            //_modContentPaths = new List<string>();

            //TODO: Have an app.config and put the paths inside it so users can define locations to load mods from
            _modPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Mods"));
            _modPaths.Add(Path.Combine(Constants.ExecutionPath, "Mods"));

            //Mods need to make their own content paths, since we're doing a different, manifest-driven, approach.
            //_modContentPaths.Add(Path.Combine(Constants.ExecutionPath, "Mods", "Content"));
            //_modContentPaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Mods", "Content"));

            //Checks that all defined modpaths exist as directories
            _modPaths.ForEach(path => VerifyPath(path));
            //_modContentPaths.ForEach(path => VerifyPath(path));
            VerifyPath(Constants.LogPath);

            Log.Initialize(Constants.LogPath);

            if (!File.Exists(Constants.ExecutionPath + "\\Stardew Valley.exe"))
            {
                throw new FileNotFoundException(string.Format("Could not found: {0}\\Stardew Valley.exe", Constants.ExecutionPath));
            }
        }

        /// <summary>
        /// Load Stardev Valley and control features
        /// </summary>
        private static void ConfigureSDV()
        {
            Log.Info("Initializing SDV Assembly...");

            // Load in the assembly - ignores security
            StardewAssembly = Assembly.UnsafeLoadFrom(Constants.ExecutionPath + "\\Stardew Valley.exe");
            StardewProgramType = StardewAssembly.GetType("StardewValley.Program", true);
            StardewGameInfo = StardewProgramType.GetField("gamePtr");

            // Change the game's version
            Log.Verbose("Injecting New SDV Version...");
            Game1.version += string.Format("-Z_MODDED | SMAPI {0}", Constants.Version.VersionString);

            // Create the thread for the game to run in.
            gameThread = new Thread(RunGame);
            Log.Info("Starting SDV...");
            gameThread.Start();

            // Wait for the game to load up
            while (!ready) ;

            //SDV is running
            Log.Comment("SDV Loaded Into Memory");

            //Create definition to listen for input
            Log.Verbose("Initializing Console Input Thread...");
            consoleInputThread = new Thread(ConsoleInputThread);

            // The only command in the API (at least it should be, for now)
            Command.RegisterCommand("help", "Lists all commands | 'help <cmd>' returns command description").CommandFired += help_CommandFired;
            //Command.RegisterCommand("crash", "crashes sdv").CommandFired += delegate { Game1.player.draw(null); };

            //Subscribe to events
            ControlEvents.KeyPressed += Events_KeyPressed;
            GameEvents.LoadContent += Events_LoadContent;
            //Events.MenuChanged += Events_MenuChanged; //Idk right now

            Log.Verbose("Applying Final SDV Tweaks...");
            StardewInvoke(() =>
            {
                gamePtr.IsMouseVisible = false;
                gamePtr.Window.Title = "Stardew Valley - Version " + Game1.version;
                StardewForm.Resize += GraphicsEvents.InvokeResize;
            });
        }

        /// <summary>
        /// Wrap the 'RunGame' method for console output
        /// </summary>
        private static void GameRunInvoker()
        {
            //Game's in memory now, send the event
            Log.Verbose("Game Loaded");
            GameEvents.InvokeGameLoaded();

            Log.Comment("Type 'help' for help, or 'help <cmd>' for a command's usage");
            //Begin listening to input
            consoleInputThread.Start();


            while (ready)
            {
                //Check if the game is still running 10 times a second
                Thread.Sleep(1000 / 10);
            }

            //abort the thread, we're closing
            if (consoleInputThread != null && consoleInputThread.ThreadState == ThreadState.Running)
                consoleInputThread.Abort();

            Log.Verbose("Game Execution Finished");
            Log.Verbose("Shutting Down...");
            Thread.Sleep(100);
            Environment.Exit(0);
        }

        /// <summary>
        /// Create the given directory path if it does not exist
        /// </summary>
        /// <param name="path">Desired directory path</param>
        private static void VerifyPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not create a path: " + path + "\n\n" + ex);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void RunGame()
        {
            Application.ThreadException += Log.Application_ThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += Log.CurrentDomain_UnhandledException;

            try
            {
                gamePtr = new SGame();
                Log.Verbose("Patching SDV Graphics Profile...");
                Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;
                LoadMods();

                StardewForm = Control.FromHandle(gamePtr.Window.Handle).FindForm();
                StardewForm.Closing += StardewForm_Closing;

                ready = true;

                StardewGameInfo.SetValue(StardewProgramType, gamePtr);
                gamePtr.Run();
            }
            catch (Exception ex)
            {
                Log.Error("Game failed to start: " + ex);
            }
        }

        static void StardewForm_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            if (true || MessageBox.Show("Are you sure you would like to quit Stardew Valley?\nUnsaved progress will be lost!", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                gamePtr.Exit();
                gamePtr.Dispose();
                StardewForm.Hide();
                ready = false;
            }
        }

        public static void LoadMods()
        {
            Log.Verbose("LOADING MODS");
            foreach (string ModPath in _modPaths)
            {
                foreach (string d in Directory.GetDirectories(ModPath))
                {
                    foreach (string s in Directory.GetFiles(d, "manifest.json"))
                    {
                        if (s.Contains("StardewInjector"))
                            continue;
                        Log.Success("Found Manifest: " + s);
                        Manifest manifest = new Manifest();
                        try
                        {
                            string t = File.ReadAllText(s);
                            if (string.IsNullOrEmpty(t))
                            {
                                Log.Error("Failed to read mod manifest '{0}'. Manifest is empty!", s);
                                continue;
                            }

                            manifest = manifest.InitializeConfig(s);

                            if (string.IsNullOrEmpty(manifest.EntryDll))
                            {
                                Log.Error("Failed to read mod manifest '{0}'. EntryDll is empty!", s);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to read mod manifest '{0}'. Exception details:\n" + ex, s);
                            continue;
                        }
                        try
                        {
                            if (manifest.PerSaveConfigs)
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(s)))
                                    Directory.CreateDirectory(Path.GetDirectoryName(s));

                                if (!Directory.Exists(Path.GetDirectoryName(s)))
                                {
                                    Log.Error("Failed to create psconfigs directory '{0}'. No exception occured.", Path.GetDirectoryName(s));
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to create psconfigs directory '{0}'. Exception details:\n" + ex, Path.GetDirectoryName(s));
                            continue;
                        }
                        try
                        {
                            string targDll = Path.Combine(Path.GetDirectoryName(s), manifest.EntryDll);
                            if (!File.Exists(targDll))
                            {
                                Log.Error("Failed to load mod '{0}'. File {1} does not exist!", s, targDll);
                                continue;
                            }

                            Assembly mod = Assembly.UnsafeLoadFrom(targDll);

                            if (mod.DefinedTypes.Count(x => x.BaseType == typeof (Mod)) > 0)
                            {
                                Log.Verbose("Loading Mod DLL...");
                                TypeInfo tar = mod.DefinedTypes.First(x => x.BaseType == typeof (Mod));
                                Mod m = (Mod) mod.CreateInstance(tar.ToString());
                                m.PathOnDisk = Path.GetDirectoryName(s);
                                m.Manifest = manifest;
                                Log.Success("LOADED MOD: {0} by {1} - Version {2} | Description: {3} (@ {4})", m.Manifest.Name, m.Manifest.Authour, m.Manifest.Version, m.Manifest.Description, targDll);
                                Constants.ModsLoaded += 1;
                                m.Entry();
                            }
                            else
                            {
                                Log.Error("Invalid Mod DLL");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to load mod '{0}'. Exception details:\n" + ex, s);
                        }
                    }
                }
            }
            Log.Success("LOADED {0} MODS", Constants.ModsLoaded);
            Console.Title = Constants.ConsoleTitle;
        }

        public static void ConsoleInputThread()
        {
            string input = string.Empty;

            while (true)
            {
                Command.CallCommand(Console.ReadLine());
            }
        }

        static void Events_LoadContent(object o, EventArgs e)
        {
            Log.Info("Initializing Debug Assets...");
            DebugPixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            DebugPixel.SetData(new[] { Color.White });

#if DEBUG
            StardewModdingAPI.Log.Verbose("REGISTERING BASE CUSTOM ITEM");
            SObject so = new SObject();
            so.Name = "Mario Block";
            so.CategoryName = "SMAPI Test Mod";
            so.Description = "It's a block from Mario!\nLoaded in realtime by SMAPI.";
            so.Texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, new FileStream(_modContentPaths[0] + "\\Test.png", FileMode.Open));
            so.IsPassable = true;
            so.IsPlaceable = true;
            StardewModdingAPI.Log.Verbose("REGISTERED WITH ID OF: " + SGame.RegisterModItem(so));

            //StardewModdingAPI.Log.Verbose("REGISTERING SECOND CUSTOM ITEM");
            //SObject so2 = new SObject();
            //so2.Name = "Mario Painting";
            //so2.CategoryName = "SMAPI Test Mod";
            //so2.Description = "It's a painting of a creature from Mario!\nLoaded in realtime by SMAPI.";
            //so2.Texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, new FileStream(_modContentPaths[0] + "\\PaintingTest.png", FileMode.Open));
            //so2.IsPassable = true;
            //so2.IsPlaceable = true;
            //StardewModdingAPI.Log.Verbose("REGISTERED WITH ID OF: " + SGame.RegisterModItem(so2));

            Command.CallCommand("load");
#endif
        }

        static void Events_KeyPressed(object o, EventArgsKeyPressed e)
        {

        }

        static void Events_MenuChanged(IClickableMenu newMenu)
        {
            Log.Verbose("NEW MENU: " + newMenu.GetType());
            if (newMenu is GameMenu)
            {
                Game1.activeClickableMenu = SGameMenu.ConstructFromBaseClass(Game1.activeClickableMenu as GameMenu);
            }
        }

        static void Events_LocationsChanged(List<GameLocation> newLocations)
        {
#if DEBUG
            SGame.ModLocations = SGameLocation.ConstructFromBaseClasses(Game1.locations);
#endif
        }

        static void Events_CurrentLocationChanged(GameLocation newLocation)
        {
            //SGame.CurrentLocation = null;
            //System.Threading.Thread.Sleep(10);
#if DEBUG
            Console.WriteLine(newLocation.name);
            SGame.CurrentLocation = SGame.LoadOrCreateSGameLocationFromName(newLocation.name);
#endif
            //Game1.currentLocation = SGame.CurrentLocation;
            //Log.LogComment(((SGameLocation) newLocation).name);
            //Log.LogComment("LOC CHANGED: " + SGame.currentLocation.name);
        }

        public static void StardewInvoke(Action a)
        {
            StardewForm.Invoke(a);
        }

        static void help_CommandFired(object o, EventArgsCommand e)
        {
            if (e.Command.CalledArgs.Length > 0)
            {
                Command fnd = Command.FindCommand(e.Command.CalledArgs[0]);
                if (fnd == null)
                    Log.Error("The command specified could not be found");
                else
                {
                    if (fnd.CommandArgs.Length > 0)
                        Log.Info("{0}: {1} - {2}", fnd.CommandName, fnd.CommandDesc, fnd.CommandArgs.ToSingular());
                    else
                        Log.Info("{0}: {1}", fnd.CommandName, fnd.CommandDesc);
                }
            }
            else
                Log.Info("Commands: " + Command.RegisteredCommands.Select(x => x.CommandName).ToSingular());
        }
    }
}