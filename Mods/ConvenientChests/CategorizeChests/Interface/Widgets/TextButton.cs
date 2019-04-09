using Microsoft.Xna.Framework;

namespace ConvenientChests.CategorizeChests.Interface.Widgets
{
    /// <summary>
    /// A button shown as text on a background.
    /// </summary>
    class TextButton : Button
    {
        private readonly Background Background;
        private readonly Label Label;

        private int LeftPadding => Background.Graphic.LeftBorderThickness;
        private int RightPadding => Background.Graphic.RightBorderThickness;
        private int TopPadding => Background.Graphic.TopBorderThickness;
        private int BottomPadding => Background.Graphic.BottomBorderThickness;

        public TextButton(string text, NineSlice backgroundTexture)
        {
            Label = new Label(text, Color.Black);
            Background = new Background(backgroundTexture);

            Width = Background.Width = Label.Width + LeftPadding + RightPadding;
            Height = Background.Height = Label.Height + TopPadding + BottomPadding;

            AddChild(Background);
            AddChild(Label);

            CenterLabel();
        }

        protected override void OnDimensionsChanged()
        {
            base.OnDimensionsChanged();

            if (Background != null)
            {
                Background.Width = Width;
                Background.Height = Height;
            }

            if (Label != null)
                CenterLabel();
        }

        private void CenterLabel()
        {
            Label.Position = new Point(
                LeftPadding + (Width - RightPadding - LeftPadding) / 2 - Label.Width / 2,
                TopPadding + (Height - BottomPadding - TopPadding) / 2 - Label.Height / 2
            );
        }
    }
}