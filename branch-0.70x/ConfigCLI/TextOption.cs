using System;

namespace fCraft.ConfigCLI {
    class TextOption : ICloneable {
        public static ConsoleColor ForeColorDefault { get; set; }
        public static ConsoleColor BackColorDefault { get; set; }

        static TextOption() {
            ForeColorDefault = ConsoleColor.Gray;
            BackColorDefault = ConsoleColor.Black;
        }

        public TextOption( string label, string text ) {
            Label = label;
            Text = text;
            ForeColor = ConsoleColor.White;
            BackColor = ConsoleColor.Black;
        }
        
        public object Tag { get; set; }
        public string Label { get; set; }
        public string Text { get; set; }
        public ConsoleColor ForeColor { get; set; }
        public ConsoleColor BackColor { get; set; }


        public object Clone() {
            return new TextOption( Label, Text ) {
                ForeColor = ForeColor,
                BackColor = BackColor
            };
        }
    }
}
