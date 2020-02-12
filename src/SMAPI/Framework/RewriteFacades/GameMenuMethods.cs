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

        public GameMenuMethods(bool playOpeningSound = true) : base()
        {
        }

        public GameMenuMethods(int startingTab, int extra = -1, bool playOpeningSound = true) : base(startingTab, extra)
        {
        }
        public void changeTab(int whichTab, bool playSound = true)
        {
            base.changeTab(whichTab);
        }
    }
}
