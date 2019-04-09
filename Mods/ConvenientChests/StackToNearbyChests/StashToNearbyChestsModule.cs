using ConvenientChests.CategorizeChests.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ConvenientChests.StackToNearbyChests {
    public class StashToNearbyChestsModule : Module {
        public StackLogic.AcceptingFunction AcceptingFunction { get; private set; }

        public StashToNearbyChestsModule(ModEntry modEntry) : base(modEntry) { }

        public override void Activate() {
            IsActive = true;

            // Acceptor
            AcceptingFunction = CreateAcceptingFunction();

            // Events
            this.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private StackLogic.AcceptingFunction CreateAcceptingFunction() {
            if (Config.CategorizeChests && Config.StashToExistingStacks)
                return (chest, item) => ModEntry.CategorizeChests.ChestAcceptsItem(chest, item) || chest.ContainsItem(item);

            if (Config.CategorizeChests)
                return (chest, item) => ModEntry.CategorizeChests.ChestAcceptsItem(chest, item);

            if (Config.StashToExistingStacks)
                return (chest, item) => chest.ContainsItem(item);

            return (chest, item) => false;
        }

        public override void Deactivate() {
            IsActive = false;

            // Events
            this.Events.Input.ButtonPressed -= OnButtonPressed;
        }

        private void TryStashNearby() {
            if (Game1.player.currentLocation == null)
                return;

            if (Game1.activeClickableMenu is ItemGrabMenu m && m.behaviorOnItemGrab?.Target is Chest c)
                StackLogic.StashToChest(c, AcceptingFunction);

            else
                StackLogic.StashToNearbyChests(Config.StashRadius, AcceptingFunction);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (e.Button == Config.StashKey || e.Button == Config.StashButton)
                TryStashNearby();
        }
    }
}