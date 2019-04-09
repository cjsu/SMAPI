using ConvenientChests.CategorizeChests.Interface.Widgets;
using Microsoft.Xna.Framework.Graphics;

namespace ConvenientChests.CategorizeChests.Interface
{
    interface ITooltipManager
    {
        void ShowTooltipThisFrame(Widget tooltip);
        void Draw(SpriteBatch batch);
    }
}