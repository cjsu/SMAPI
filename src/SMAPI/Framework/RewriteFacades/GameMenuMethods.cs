using System.Reflection;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class GameMenuMethods : GameMenu
    {
        public string HoverTextProp
        {
            get
            {
                return (string)typeof(GameMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(GameMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
    }
}
