using ConvenientChests.CategorizeChests.Interface.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ConvenientChests.CategorizeChests.Interface
{
    class TooltipManager : ITooltipManager
    {
        private Widget Tooltip;

        public void ShowTooltipThisFrame(Widget tooltip)
        {
            Tooltip = tooltip;
        }

        public void Draw(SpriteBatch batch)
        {
            if (Tooltip != null)
            {
                var mousePosition = Game1.getMousePosition();

                Tooltip.Position = new Point(
                    mousePosition.X + 8 * Game1.pixelZoom,
                    mousePosition.Y + 8 * Game1.pixelZoom
                );

                Tooltip.Draw(batch);

                Tooltip = null;
            }
        }
    }
}