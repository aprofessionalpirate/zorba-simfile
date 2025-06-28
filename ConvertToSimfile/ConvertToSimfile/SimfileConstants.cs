namespace ConvertToSimfile
{
    internal static class SimfileConstants
    {
        public static readonly string[] SINGLE_ARROWS = ["1000", "0100", "0010", "0001"]; // L, D, U, R

        public static readonly string[] JUMP_PATTERNS = [
            "1100", // L+D
            "1010", // L+U  
            "1001", // L+R
            "0110", // D+U
            "0101", // D+R
            "0011"  // U+R
        ];
    }
}
