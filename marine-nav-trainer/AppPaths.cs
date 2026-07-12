using System.IO;

namespace marine_nav_trainer {
    // Assets do %AppData%\MarineNavTrainer\Assets - zapis nie wymaga uprawnień adm
    // instalacja Programy do Program Files (read only)
    public static class AppPaths {
        public static string AssetsBase { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarineNavTrainer",
            "Assets");

        public static string MapsDir { get; } = Path.Combine(AssetsBase, "Maps");
        public static string JsonsDir { get; } = Path.Combine(AssetsBase, "Jsons");
        public static string BackgroundDir { get; } = Path.Combine(AssetsBase, "Backgrounds");

        // Szablon domyślnych zasobów obok exe.
        private static string SeedDir { get; } = Path.Combine(AppContext.BaseDirectory, "Assets");

        
        // Tworzy strukturę katalogów w %AppData% i kopiuje domyślne pliki Assets
        public static void EnsureSeeded() {
            Directory.CreateDirectory(MapsDir);
            Directory.CreateDirectory(JsonsDir);
            Directory.CreateDirectory(BackgroundDir);

            if (!Directory.Exists(SeedDir))
                return;

            foreach (string src in Directory.EnumerateFiles(SeedDir, "*", SearchOption.AllDirectories)) {
                string relative = Path.GetRelativePath(SeedDir, src);
                string dest = Path.Combine(AssetsBase, relative);

                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                if (!File.Exists(dest))
                    File.Copy(src, dest);
            }
        }
    }
}
