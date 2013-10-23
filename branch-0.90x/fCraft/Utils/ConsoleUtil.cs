using System;
using JetBrains.Annotations;

namespace fCraft {
    public class ConsoleUtil {
        [StringFormatMethod("prompt")]
        public static bool ShowYesNo([NotNull] string prompt, params object[] formatArgs) {
            if( prompt == null ) throw new ArgumentNullException("prompt");
            while( true ) {
                Console.Write(prompt + " (Y/N): ", formatArgs);
                string input = Console.ReadLine();

                if( input == null ||
                    input.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("n", StringComparison.OrdinalIgnoreCase) ) {
                    return false;
                } else if( input.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                           input.Equals("y", StringComparison.OrdinalIgnoreCase) ) {
                    return true;
                }
            }
        }
    }
}
