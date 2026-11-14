namespace OldSaber
{
    internal static class SceneCatalog
    {
        internal static string ResourceFor(string sceneName)
        {
            switch (sceneName)
            {
                case "DefaultEnvironment": return "OldSaber.scene.json";
                case "NiceEnvironment": return "OldSaber.NiceEnvironment.json";
                case "TriangleEnvironment": return "OldSaber.TriangleEnvironment.json";
                case "BigMirrorEnvironment": return "OldSaber.BigMirrorEnvironment.json";
                default: return null;
            }
        }
    }
}
