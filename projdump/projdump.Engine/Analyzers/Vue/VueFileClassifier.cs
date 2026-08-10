using projdump.Engine.Core;

namespace projdump.Engine.Analyzers.Vue;

static class VueFileClassifier
{
    static readonly string[] EntryPointNames = ["main.js", "main.ts", "App.vue"];

    static readonly HashSet<string> ConfigFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "vite.config.js",
        "vite.config.ts",
        "vue.config.js",
        "tsconfig.json",
        "tsconfig.app.json",
        "tsconfig.node.json",
        "tailwind.config.js",
        "tailwind.config.ts",
        "postcss.config.js",
        ".eslintrc.json",
        ".eslintrc.js",
        ".env",
        ".env.local",
        ".env.development",
        ".env.production",
    };

    static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".vue", ".js", ".ts", ".jsx", ".tsx", ".css", ".scss", ".sass", ".less" };
    static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".ico", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp", ".avif",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
    };

    public static bool IsCodeFile(FileInfo f) => CodeExtensions.Contains(f.Extension);

    public static bool IsConfigFile(FileInfo f) => ConfigFileNames.Contains(f.Name);

    // Entry points first, then router/store, then components, then everything else.
    public static int CodeFilePriority(FileInfo f)
    {
        if (EntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase)) return 0;

        bool inRouterFolder = f.DirectoryName != null &&
            (f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}router", StringComparison.OrdinalIgnoreCase) ||
             f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}router{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        bool inStoreFolder = f.DirectoryName != null &&
            (f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}store", StringComparison.OrdinalIgnoreCase) ||
             f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}stores", StringComparison.OrdinalIgnoreCase) ||
             f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}stores{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        if (inRouterFolder || inStoreFolder) return 1;

        if (f.Name.EndsWith("Type.ts", StringComparison.OrdinalIgnoreCase) || f.Name.EndsWith("Types.ts", StringComparison.OrdinalIgnoreCase)) return 2;

        bool inComponentsFolder = f.DirectoryName != null &&
            (f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}components", StringComparison.OrdinalIgnoreCase) ||
             f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}components{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        if (inComponentsFolder) return 3;

        bool inComposablesFolder = f.DirectoryName != null &&
            (f.DirectoryName.EndsWith($"{Path.DirectorySeparatorChar}composables", StringComparison.OrdinalIgnoreCase) ||
             f.DirectoryName.Contains($"{Path.DirectorySeparatorChar}composables{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        if (inComposablesFolder) return 4;

        return 5; // everything else
    }

    // No ApiSurface role - Vue only supports "default" mode for now.
    public static FileRole AssignRole(FileInfo f, ITestFileDetector testFileDetector)
    {
        if (testFileDetector.IsTestFile(f))
            return FileRole.Test;

        if (f.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase))
            return FileRole.Build;

        if (f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            return FileRole.Doc;

        if (IsConfigFile(f))
            return FileRole.Config;

        if (EntryPointNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            return FileRole.EntryPoint;

        if (f.Extension.Equals(".vue", StringComparison.OrdinalIgnoreCase))
            return FileRole.Component;

        if (f.Extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals(".scss", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals(".sass", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals(".less", StringComparison.OrdinalIgnoreCase))
            return FileRole.Style;

        if (AssetExtensions.Contains(f.Extension))
            return FileRole.Asset;

        return FileRole.Other;
    }
}