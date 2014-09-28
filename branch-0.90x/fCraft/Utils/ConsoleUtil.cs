using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides methods for working with command-line input,
    /// common to MapRenderer and MapConverter. </summary>
    public static class ConsoleUtil {
        /// <summary> Prompts user to answer a yes/no question. Repeats the question until user replies.
        /// Question is written to Console.Out, and answer is read from Console.In.
        /// Interprets "no" or "n" as false, and "yes" or "y" as true.
        /// If no more input is available (e.g. user entered Ctrl+D), returns false. </summary>
        /// <param name="prompt"> A composite format string for the question. Same semantics as String.Format(). </param>
        /// <param name="formatArgs"> An array of objects to write using format. </param>
        /// <returns> Answer to the prompt: true or false. </returns>
        /// <exception cref="ArgumentNullException"> prompt or formatArgs is null </exception>
        [StringFormatMethod("prompt")]
        public static bool ShowYesNo([NotNull] string prompt, [NotNull] params object[] formatArgs) {
            if (prompt == null) throw new ArgumentNullException("prompt");
            if (formatArgs == null) throw new ArgumentNullException("formatArgs");
            while (true) {
                Console.Write(prompt + " (Y/N): ", formatArgs);
                string input = Console.ReadLine();

                if (input == null ||
                    input.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("n", StringComparison.OrdinalIgnoreCase)) {
                    return false;
                } else if (input.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                           input.Equals("y", StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
        }
    }
}
