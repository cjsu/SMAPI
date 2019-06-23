using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class HUDMessageMethods : HUDMessage
    {
        public HUDMessageMethods(string message, int whatType)
            : base(message, whatType, -1)
        {
        }

    }
}
