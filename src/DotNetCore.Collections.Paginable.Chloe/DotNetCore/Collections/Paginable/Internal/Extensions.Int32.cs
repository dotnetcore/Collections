namespace DotNetCore.Collections.Paginable.Internal {
    internal static class Int32Extensions {
        public static bool IsValid(this int? int32Value) {
            return int32Value != null;
        }
    }
}