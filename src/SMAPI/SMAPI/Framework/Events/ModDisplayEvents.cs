using System;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events related to UI and drawing to the screen.</summary>
    internal class ModDisplayEvents : ModEventsBase, IDisplayEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        public event EventHandler<MenuChangedEventArgs> MenuChanged
        {
            add => this.EventManager.MenuChanged.Add(value);
            remove => this.EventManager.MenuChanged.Remove(value);
        }

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        public event EventHandler<RenderingEventArgs> Rendering
        {
            add => this.EventManager.Rendering.Add(value);
            remove => this.EventManager.Rendering.Remove(value);
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
        public event EventHandler<RenderedEventArgs> Rendered
        {
            add => this.EventManager.Rendered.Add(value);
            remove => this.EventManager.Rendered.Remove(value);
        }

        /// <summary>Raised before the game world is drawn to the screen. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        public event EventHandler<RenderingWorldEventArgs> RenderingWorld
        {
            add => this.EventManager.RenderingWorld.Add(value);
            remove => this.EventManager.RenderingWorld.Remove(value);
        }

        /// <summary>Raised after the game world is drawn to the sprite patch, before it's rendered to the screen. Content drawn to the sprite batch at this point will be drawn over the world, but under any active menu, HUD elements, or cursor.</summary>
        public event EventHandler<RenderedWorldEventArgs> RenderedWorld
        {
            add => this.EventManager.RenderedWorld.Add(value);
            remove => this.EventManager.RenderedWorld.Remove(value);
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised before that menu is drawn to the screen. This includes the game's internal menus like the title screen. Content drawn to the sprite batch at this point will appear under the menu.</summary>
        public event EventHandler<RenderingActiveMenuEventArgs> RenderingActiveMenu
        {
            add => this.EventManager.RenderingActiveMenu.Add(value);
            remove => this.EventManager.RenderingActiveMenu.Remove(value);
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen. Content drawn to the sprite batch at this point will appear over the menu and menu cursor.</summary>
        public event EventHandler<RenderedActiveMenuEventArgs> RenderedActiveMenu
        {
            add => this.EventManager.RenderedActiveMenu.Add(value);
            remove => this.EventManager.RenderedActiveMenu.Remove(value);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
        public event EventHandler<RenderingHudEventArgs> RenderingHud
        {
            add => this.EventManager.RenderingHud.Add(value);
            remove => this.EventManager.RenderingHud.Remove(value);
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear over the HUD.</summary>
        public event EventHandler<RenderedHudEventArgs> RenderedHud
        {
            add => this.EventManager.RenderedHud.Add(value);
            remove => this.EventManager.RenderedHud.Remove(value);
        }

        /// <summary>Raised after the game window is resized.</summary>
        public event EventHandler<WindowResizedEventArgs> WindowResized
        {
            add => this.EventManager.WindowResized.Add(value);
            remove => this.EventManager.WindowResized.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModDisplayEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
