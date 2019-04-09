using System;
using ConvenientChests.StackToNearbyChests;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ConvenientChests.CategorizeChests.Interface.Widgets {
    internal class ChestOverlay : Widget {
        private ItemGrabMenu           ItemGrabMenu   { get; }
        private CategorizeChestsModule Module         { get; }
        private Chest                  Chest          { get; }
        private ITooltipManager        TooltipManager { get; }

        private readonly InventoryMenu                   InventoryMenu;
        private readonly InventoryMenu.highlightThisItem DefaultChestHighlighter;
        private readonly InventoryMenu.highlightThisItem DefaultInventoryHighlighter;

        private TextButton   OpenButton   { get; set; }
        private TextButton   StashButton  { get; set; }
        private CategoryMenu CategoryMenu { get; set; }

        public ChestOverlay(CategorizeChestsModule module, Chest chest, ItemGrabMenu menu, ITooltipManager tooltipManager) {
            Module         = module;
            Chest          = chest;
            ItemGrabMenu   = menu;
            InventoryMenu  = menu.ItemsToGrabMenu;
            TooltipManager = tooltipManager;

            DefaultChestHighlighter     = ItemGrabMenu.inventory.highlightMethod;
            DefaultInventoryHighlighter = InventoryMenu.highlightMethod;

            AddButtons();
        }

        protected override void OnParent(Widget parent) {
            base.OnParent(parent);

            if (parent == null) return;
            Width  = parent.Width;
            Height = parent.Height;
        }

        private void AddButtons() {
            OpenButton         =  new TextButton("Categorize", Sprites.LeftProtrudingTab);
            OpenButton.OnPress += ToggleMenu;
            AddChild(OpenButton);

            StashButton         =  new TextButton(ChooseStashButtonLabel(), Sprites.LeftProtrudingTab);
            StashButton.OnPress += StashItems;
            AddChild(StashButton);

            PositionButtons();
        }

        private void PositionButtons() {
            StashButton.Width = OpenButton.Width = Math.Max(StashButton.Width, OpenButton.Width);

            OpenButton.Position = new Point(
                ItemGrabMenu.xPositionOnScreen + ItemGrabMenu.width / 2 - OpenButton.Width - 112 * Game1.pixelZoom,
                ItemGrabMenu.yPositionOnScreen                                             + 22  * Game1.pixelZoom
            );

            StashButton.Position = new Point(
                OpenButton.Position.X + OpenButton.Width  - StashButton.Width,
                OpenButton.Position.Y + OpenButton.Height - 0
            );
        }

        private string ChooseStashButtonLabel() {
            return Module.Config.StashKey == SButton.None
                       ? "Stash"
                       : $"Stash ({Module.Config.StashKey})";
        }

        private void ToggleMenu() {
            if (CategoryMenu == null)
                OpenCategoryMenu();

            else
                CloseCategoryMenu();
        }

        private void OpenCategoryMenu() {
            var chestData = Module.ChestDataManager.GetChestData(Chest);
            CategoryMenu = new CategoryMenu(chestData, Module.ItemDataManager, TooltipManager);
            CategoryMenu.Position = new Point(
                ItemGrabMenu.xPositionOnScreen + ItemGrabMenu.width / 2 - CategoryMenu.Width / 2 - 6  * Game1.pixelZoom,
                ItemGrabMenu.yPositionOnScreen                                                   - 10 * Game1.pixelZoom
            );
            CategoryMenu.OnClose += CloseCategoryMenu;
            AddChild(CategoryMenu);

            SetItemsClickable(false);
        }

        private void CloseCategoryMenu() {
            RemoveChild(CategoryMenu);
            CategoryMenu = null;

            SetItemsClickable(true);
        }

        private void StashItems() => StackLogic.StashToChest(Chest, ModEntry.StashNearby.AcceptingFunction);

        public override bool ReceiveLeftClick(Point point) {
            var hit = PropagateLeftClick(point);
            if (!hit && CategoryMenu != null)
                // Are they clicking outside the menu to try to exit it?
                CloseCategoryMenu();

            return hit;
        }

        private void SetItemsClickable(bool clickable) {
            if (clickable) {
                ItemGrabMenu.inventory.highlightMethod = DefaultChestHighlighter;
                InventoryMenu.highlightMethod          = DefaultInventoryHighlighter;
            }
            else {
                ItemGrabMenu.inventory.highlightMethod = i => false;
                InventoryMenu.highlightMethod          = i => false;
            }
        }
    }
}