using System;
using System.Collections.Generic;
using System.Linq;
using ConvenientChests.CategorizeChests.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ConvenientChests.CategorizeChests.Interface.Widgets {
    class CategoryMenu : Widget {
        // Styling settings
        private const  int        MaxItemColumns = 12;
        private static int        Padding    => 2 * Game1.pixelZoom;
        private static SpriteFont HeaderFont => Game1.dialogueFont;

        // Elements
        private Widget                  Body            { get; set; }
        private Widget                  TopRow          { get; set; }
        private LabeledCheckbox         SelectAllButton { get; set; }
        private SpriteButton            CloseButton     { get; set; }
        private Background              Background      { get; set; }
        private Label                   CategoryLabel   { get; set; }
        private SpriteButton            PrevButton      { get; set; }
        private SpriteButton            NextButton      { get; set; }
        private WrapBag                 ToggleBag       { get; set; }
        private IEnumerable<ItemToggle> ItemToggles     => ToggleBag.Children.OfType<ItemToggle>();

        private IItemDataManager ItemDataManager { get; }
        private ITooltipManager  TooltipManager  { get; }
        private ChestData        ChestData       { get; }
        private int              Index           { get; set; }
        private List<string>     Categories      { get; }
        private string           ActiveCategory  => Categories[Index];

        public event Action OnClose;

        public CategoryMenu(ChestData chestData, IItemDataManager itemDataManager, ITooltipManager tooltipManager) {
            ItemDataManager = itemDataManager;
            TooltipManager  = tooltipManager;
            ChestData       = chestData;

            Categories = itemDataManager.Categories.Keys.ToList();
            Categories.Sort();

            BuildWidgets();

            SetCategory(Index);
        }

        private void BuildWidgets() {
            Background = AddChild(new Background(Sprites.MenuBackground));
            Body       = AddChild(new Widget());
            TopRow     = Body.AddChild(new Widget());
            ToggleBag  = Body.AddChild(new WrapBag(MaxItemColumns * Game1.tileSize));

            NextButton         =  TopRow.AddChild(new SpriteButton(Sprites.RightArrow));
            PrevButton         =  TopRow.AddChild(new SpriteButton(Sprites.LeftArrow));
            NextButton.OnPress += () => CycleCategory(1);
            PrevButton.OnPress += () => CycleCategory(-1);

            SelectAllButton          =  TopRow.AddChild(new LabeledCheckbox("All"));
            SelectAllButton.OnChange += OnToggleSelectAll;

            CloseButton         =  AddChild(new SpriteButton(Sprites.ExitButton));
            CloseButton.OnPress += () => OnClose?.Invoke();

            CategoryLabel = TopRow.AddChild(new Label("", Color.Black, HeaderFont));
        }

        private void PositionElements() {
            Body.Position = new Point(Background.Graphic.LeftBorderThickness, Background.Graphic.RightBorderThickness);

            // Figure out width
            Body.Width   = ToggleBag.Width;
            TopRow.Width = Body.Width;
            Width        = Body.Width + Background.Graphic.LeftBorderThickness + Background.Graphic.RightBorderThickness + Padding * 2;

            // Build the top row
            var longestCat  = Categories.OrderByDescending(s => s.Length).First();
            var headerWidth = (int) HeaderFont.MeasureString(longestCat).X;
            NextButton.X = TopRow.Width / 2                    + headerWidth / 2;
            PrevButton.X = TopRow.Width / 2 - PrevButton.Width - headerWidth / 2;

            SelectAllButton.X = Padding;

            CategoryLabel.CenterHorizontally();

            TopRow.Height = TopRow.Children.Max(c => c.Height);

            foreach (var child in TopRow.Children)
                child.Y = TopRow.Height / 2 - child.Height / 2;

            // Figure out height and vertical positioning
            ToggleBag.Y = TopRow.Y + TopRow.Height + Padding;
            Body.Height = ToggleBag.Y              + ToggleBag.Height;
            Height      = (Body.Height + Background.Graphic.TopBorderThickness + Background.Graphic.BottomBorderThickness + Padding * 2);

            Background.Width  = Width;
            Background.Height = Height;

            CloseButton.Position = new Point(Width - CloseButton.Width, 0);
        }

        private void OnToggleSelectAll(bool on) {
            if (on)
                SelectAll();
            else
                SelectNone();
        }

        private void SelectAll() {
            foreach (var toggle in ItemToggles) {
                if (!toggle.Active)
                    toggle.Toggle();
            }
        }

        private void SelectNone() {
            foreach (var toggle in ItemToggles) {
                if (toggle.Active)
                    toggle.Toggle();
            }
        }

        private void CycleCategory(int offset) {
            SetCategory(Utility.Mod(Index + offset, Categories.Count));
        }

        private void SetCategory(int index) {
            Index              = index;
            CategoryLabel.Text = ActiveCategory;

            RecreateItemToggles();

            SelectAllButton.Checked = AreAllSelected();

            PositionElements();
        }

        private void RecreateItemToggles() {
            ToggleBag.RemoveChildren();

            var itemKeys = ItemDataManager.Categories[ActiveCategory];

            foreach (var itemKey in itemKeys) {
                var item   = ItemDataManager.GetItem(itemKey);
                var toggle = ToggleBag.AddChild(new ItemToggle(TooltipManager, item, ChestData.Accepts(itemKey)));
                toggle.OnToggle += () => ToggleItem(itemKey);
            }
        }

        private void ToggleItem(ItemKey itemKey) {
            ChestData.Toggle(itemKey);
            SelectAllButton.Checked = AreAllSelected();
        }

        private bool AreAllSelected() {
            return ItemToggles.Count(t => !t.Active) == 0;
        }

        public override bool ReceiveLeftClick(Point point) {
            PropagateLeftClick(point);
            return true;
        }

        public override bool ReceiveScrollWheelAction(int amount) {
            CycleCategory(amount > 1 ? -1 : 1);
            return true;
        }
    }
}