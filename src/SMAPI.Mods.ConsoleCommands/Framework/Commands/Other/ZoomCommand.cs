using System.Linq;
using System.Reflection;
using StardewValley;
using StardewValley.Mobile;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other
{
    /// <summary>A command which sends a debug command to the game.</summary>
    internal class ZoomCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ZoomCommand()
            : base("zoom", "Modify game's zoom level.\n\nUsage: zoom <zoomLevel>\n- zoomLevel: the target zoomLevel (a number).\nFor example, 'zoom 1.5' set zoom level to 1.5 * NativeZoomLevel.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // submit command
            decimal zoomLevel;
            if (!args.Any())
            {
                zoomLevel = 1.0m;
            }
            else if (!args.TryGetDecimal(0, "zoomLevel", out zoomLevel, min: 0.1m, max: 10m))
                return;
            object viewport = typeof(Game1).GetField("viewport", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            PropertyInfo x = viewport.GetType().GetProperty("X");
            PropertyInfo y = viewport.GetType().GetProperty("Y");
            int oldX = (int)x.GetValue(viewport);
            int oldY = (int)y.GetValue(viewport);
            FieldInfo _lastPinchZoomLevel = typeof(PinchZoom).GetField("_lastPinchZoomLevel", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo _pinchZoomLevel = typeof(PinchZoom).GetField("_pinchZoomLevel", BindingFlags.Instance | BindingFlags.NonPublic);
            Game1.options.zoomLevel = Game1.NativeZoomLevel * (float)zoomLevel;
            float oldZoom = (float)_lastPinchZoomLevel.GetValue(PinchZoom.Instance);
            _lastPinchZoomLevel.SetValue(PinchZoom.Instance, _pinchZoomLevel.GetValue(PinchZoom.Instance));
            _pinchZoomLevel.SetValue(PinchZoom.Instance, Game1.options.zoomLevel);
            Game1.game1.refreshWindowSettings();
            PinchZoom.Instance.Center();
            WeatherDebrisManager.Instance.RepositionOnZoomChange(oldX, oldY, (int)x.GetValue(viewport), (int)y.GetValue(viewport), oldZoom, Game1.options.zoomLevel);
            RainManager.Instance.UpdateRainPositionForPinchZoom((float)(oldX - (int)x.GetValue(viewport)), (float)(oldY - (int)y.GetValue(viewport)));
            // show result
            monitor.Log("Zoom level changed.", LogLevel.Info);
        }
    }
}
