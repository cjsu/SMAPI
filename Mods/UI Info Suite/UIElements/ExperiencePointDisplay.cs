using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.UIElements
{
    class ExperiencePointDisplay
    {
        private int _alpha = 100;
        private float _experiencePoints;
        private Vector2 _position;

        public ExperiencePointDisplay(float experiencePoints, Vector2 position)
        {
            this._position = position;
            this._experiencePoints = experiencePoints;
        }

        public void Draw()
        {
            this._position.Y -= 0.5f;
            --this._alpha;
            Game1.drawWithBorder(
                "Exp " + this._experiencePoints,
                Color.DarkSlateGray * ((float)this._alpha / 100f),
                Color.PaleTurquoise * ((float)this._alpha / 100f),
                new Vector2(this._position.X - 28, this._position.Y - 130),
                0.0f,
                0.8f,
                0.0f);
        }

        public bool IsInvisible
        {
            get { return this._alpha < 3; }
        }
    }
}
