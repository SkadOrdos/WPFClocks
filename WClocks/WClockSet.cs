using System;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace WClocks
{
    [Serializable]
    public class WClockSet
    {
        [Serializable]
        public class ColorSet
        {
            [XmlIgnore]
            public Color CurrentColor => Color.FromArgb((byte)this.A, (byte)R, (byte)G, (byte)B);

            int ca;
            [XmlAttribute]
            public int A
            {
                get { return ca; }
                set { ca = ValidateColorValue(value); }
            }

            int cr;
            [XmlAttribute]
            public int R
            {
                get { return cr; }
                set { cr = ValidateColorValue(value); }
            }

            int cg;
            [XmlAttribute]
            public int G
            {
                get { return cg; }
                set { cg = ValidateColorValue(value); }
            }

            int cb;
            [XmlAttribute]
            public int B
            {
                get { return cb; }
                set { cb = ValidateColorValue(value); }
            }


            public ColorSet()
            { }

            public ColorSet(Color color)
            {
                SetColor(color);
            }

            private byte ValidateColorValue(int value)
            {
                return (value <= byte.MinValue || value > byte.MaxValue) ? byte.MaxValue : (byte)value;
            }

            public void SetColor(Color color)
            {
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
            }
        }


        public ColorSet FaceColor { get; set; }

        public string Location { get; set; }

        public Point Position { get; set; }

        public int Size { get; set; } = 100;


        public WClockSet()
        {
            FaceColor = new ColorSet(Color.FromArgb(255, 230, 230, 230));
            Position = new Point(-1, -1);
            Location = Locations.Float;
        }
    }
}
