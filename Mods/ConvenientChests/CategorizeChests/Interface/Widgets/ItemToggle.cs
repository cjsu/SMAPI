using System;
using ConvenientChests.CategorizeChests.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ConvenientChests.CategorizeChests.Interface.Widgets {
    /// <summary>
    /// A toggle button corresponding to a given kind of item, appearing as
    /// the icon for that item with an appropriate tooltip.
    /// </summary>
    class ItemToggle : Widget {
        public  Item            Item           { get; }
        private ITooltipManager TooltipManager { get; }
        private Widget          Tooltip        { get; }
        public  bool            Active;

        public event Action OnToggle;

        public ItemToggle(ITooltipManager tooltipManager, Item item, bool active) {
            TooltipManager = tooltipManager;
            Item           = item;
            Active         = active;
            Tooltip        = new ItemTooltip(item.DisplayName);
            Width          = Game1.tileSize;
            Height         = Game1.tileSize;
        }

        public override void Draw(SpriteBatch batch) {
            var alpha = Active ? 1.0f : 0.25f;

            Item.drawInMenu(batch, new Vector2(GlobalPosition.X, GlobalPosition.Y), 1, alpha, 1, false);

            if (GlobalBounds.Contains(Game1.getMousePosition()))
                TooltipManager.ShowTooltipThisFrame(Tooltip);
        }

        public void Toggle() {
            Active = !Active;
            OnToggle?.Invoke();
        }

        public override bool ReceiveLeftClick(Point point) {
            Toggle();
            return true;
        }
    }
}