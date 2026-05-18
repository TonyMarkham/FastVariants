using System;

namespace FastVariants.Core.Utilities
{
    public static class GuidUtilities
    {
        public const string EmptyId = "00000000-0000-0000-0000-000000000000";
        
        public static bool IsEmptyId(this string id)
        {
            return string.IsNullOrEmpty(id) || string.Equals(id, EmptyId, StringComparison.Ordinal);
        }

        public static string NewId()
        {
            return Guid.NewGuid().ToString("D");
        }
    }
}
