using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows which items an NPC likes receiving.</summary>
    internal class CharacterGiftTastesField : GenericField
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="giftTastes">The items by how much this NPC likes receiving them.</param>
        /// <param name="showTaste">The gift taste to show.</param>
        public CharacterGiftTastesField(GameHelper gameHelper, string label, IDictionary<GiftTaste, Item[]> giftTastes, GiftTaste showTaste)
            : base(gameHelper, label, CharacterGiftTastesField.GetText(gameHelper, giftTastes, showTaste)) { }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the text to display.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="giftTastes">The items by how much this NPC likes receiving them.</param>
        /// <param name="showTaste">The gift taste to show.</param>
        private static IEnumerable<IFormattedText> GetText(GameHelper gameHelper, IDictionary<GiftTaste, Item[]> giftTastes, GiftTaste showTaste)
        {
            if (!giftTastes.ContainsKey(showTaste))
                yield break;

            // get item data
            Item[] ownedItems = gameHelper.GetAllOwnedItems().ToArray();
            Item[] inventory = Game1.player.Items.Where(p => p != null).ToArray();
            var items =
                (
                    from item in giftTastes[showTaste]
                    let isInventory = inventory.Any(p => p.ParentSheetIndex == item.ParentSheetIndex && p.Category == item.Category)
                    let isOwned = ownedItems.Any(p => p.ParentSheetIndex == item.ParentSheetIndex && p.Category == item.Category)
                    orderby isInventory descending, isOwned descending, item.DisplayName
                    select new { Item = item, IsInventory = isInventory, IsOwned = isOwned }
                )
                .ToArray();

            // generate text
            for (int i = 0, last = items.Length - 1; i <= last; i++)
            {
                var entry = items[i];
                string text = i != last
                    ? entry.Item.DisplayName + ","
                    : entry.Item.DisplayName;

                if (entry.IsInventory)
                    yield return new FormattedText(text, Color.Green);
                else if (entry.IsOwned)
                    yield return new FormattedText(text, Color.Black);
                else
                    yield return new FormattedText(text, Color.Gray);
            }
        }
    }
}
