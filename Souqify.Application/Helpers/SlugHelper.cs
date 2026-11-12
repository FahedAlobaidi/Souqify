
using System.Text.RegularExpressions;

namespace Souqify.Application.Helpers
{
    public static class SlugHelper
    {
        // "PCIe 4.0" -> "pcie-4-0", "Memory (RAM)" -> "memory-ram".
        // Every run of "not a letter or digit" becomes one dash, then dashes at the ends are cut.
        // Returns "" when the text has no a-z or 0-9 (for example an Arabic value), so callers must check it.
        public static string Create(string text)
        {
            var slug = Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", "-");

            return slug.Trim('-');
        }
    }
}
