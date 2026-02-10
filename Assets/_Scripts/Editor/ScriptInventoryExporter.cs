
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UltimateDungeon.EditorTools
{
    /// <summary>
    /// ScriptInventoryExporter
    /// =======================
    ///
    /// Exports a comprehensive list of ALL *.cs files under Assets/ into Markdown.
    ///
    /// Why:
    /// - Keeps SCRIPTS.md accurate without manual bookkeeping.
    /// - Helps ChatGPT sessions by attaching a full script inventory.
    ///
    /// Output:
    /// - Assets/SCRIPTS.generated.md
    ///
    /// Notes:
    /// - This does NOT parse code deeply (fast + robust).
    /// - It tries to infer category/status from folder names.
    /// - You can tweak the heuristics to match your folder conventions.
    /// </summary>
    public static class ScriptInventoryExporter
    {
        private const string OutputPath = "Assets/SCRIPTS.generated.md";

        [MenuItem("Tools/Ultimate Dungeon/Export Script Inventory")]
        public static void Export()
        {
            try
            {
                // Collect all C# files under Assets (excluding Packages).
                var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                    .Select(ToAssetsRelativePath)
                    .Where(p => !p.Contains("/Editor Default Resources/"))
                    .OrderBy(p => p)
                    .ToList();

                var sb = new StringBuilder(64 * 1024);

                sb.AppendLine("# SCRIPTS.generated.md — Ultimate Dungeon (Auto-Generated)");
                sb.AppendLine();
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine("This file is auto-generated. Do not hand-edit unless you plan to overwrite changes next export.");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                // Group by inferred category to keep it readable.
                var groups = files
                    .GroupBy(InferCategory)
                    .OrderBy(g => g.Key);

                foreach (var g in groups)
                {
                    sb.AppendLine($"## {g.Key}");
                    sb.AppendLine();
                    sb.AppendLine("| Script | Path | Status | Notes |");
                    sb.AppendLine("|---|---|---|---|");

                    foreach (var path in g)
                    {
                        string script = Path.GetFileNameWithoutExtension(path);
                        string status = InferStatus(path);
                        sb.AppendLine($"| `{script}` | `{path}` | {status} |  |");
                    }

                    sb.AppendLine();
                }

                File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();

                Debug.Log($"[ScriptInventoryExporter] Wrote {files.Count} scripts to: {OutputPath}");
                EditorUtility.RevealInFinder(OutputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScriptInventoryExporter] Export failed: {ex}");
            }
        }

        private static string ToAssetsRelativePath(string absolute)
        {
            // Convert absolute path like C:/Project/Assets/Foo.cs -> Assets/Foo.cs
            absolute = absolute.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!absolute.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return absolute;

            return "Assets" + absolute.Substring(dataPath.Length);
        }

        private static string InferCategory(string path)
        {
            // Heuristics based on common folder naming.
            // Adjust to your folder structure as it evolves.

            string p = path.ToLowerInvariant();

            if (p.Contains("/editor/") || p.EndsWith(".editor.cs")) return "Editor";
            if (p.Contains("/debug/") || p.Contains("/tests/") || p.Contains("/test/")) return "Debug / Tests";
            if (p.Contains("/ui/")) return "UI";
            if (p.Contains("/network") || p.Contains("/netcode") || p.Contains("/networking")) return "Networking";
            if (p.Contains("/items/") || p.Contains("/inventory/") || p.Contains("/affix")) return "Items / Inventory";
            if (p.Contains("/combat/")) return "Combat";
            if (p.Contains("/actors/") || p.Contains("/player")) return "Actors / Players";
            if (p.Contains("/scenes/") || p.Contains("/scene")) return "Scenes";

            return "Other";
        }

        private static string InferStatus(string path)
        {
            // More heuristics. Keep it simple.
            string p = path.ToLowerInvariant();

            if (p.Contains("/legacy/") || p.Contains("_old") || p.Contains("deprecated")) return "Legacy";
            if (p.Contains("/spike/") || p.Contains("_spike") || p.Contains("/temp/")) return "Spike";

            return "Active";
        }
    }
}
#endif