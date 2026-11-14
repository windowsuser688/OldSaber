namespace OldSaber
{
    internal static class VisualPolicy
    {
        // Classic source IDs 3/4 are left/right rotating laser groups.
        internal static bool UsesGlowOnly(int sourceLightId) { return sourceLightId == 3 || sourceLightId == 4; }
        internal static bool UsesMirror(string sourceMaterial) { return sourceMaterial == "PlayersPlaceMirror" || sourceMaterial == "TrackMirror"; }
    }
}
