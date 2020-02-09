using System;
using System.Collections.Generic;
using System.Reflection;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class ShopMenuMethods : ShopMenu
    {
        public ISalable HeldItemProp
        {
            get
            {
                return (ISalable)typeof(ShopMenu).GetField("heldItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(ShopMenu).GetField("heldItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
        public ISalable HoveredItemProp
        {
            get
            {
                return (ISalable)typeof(ShopMenu).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(ShopMenu).GetField("hoveredItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
        public int HoverPriceProp
        {
            get
            {
                return (int)typeof(ShopMenu).GetField("hoverPrice", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(ShopMenu).GetField("hoverPrice", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
        public string HoverTextProp
        {
            get
            {
                return (string)typeof(ShopMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(ShopMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
        public List<int> CategoriesToSellHereProp
        {
            get
            {
                return (List<int>)typeof(ShopMenu).GetField("categoriesToSellHere", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(this);
            }
            set
            {
                typeof(ShopMenu).GetField("categoriesToSellHere", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(this, value);
            }
        }
        public ShopMenuMethods(Dictionary<ISalable, int[]> itemPriceAndStock, int currency = 0, string who = null, Func<ISalable, Farmer, int, bool> on_purchase = null, Func<ISalable, bool> on_sell = null, string context = null) : base(itemPriceAndStock, currency, who, on_purchase, on_sell, context)
        {
        }
    }
}
