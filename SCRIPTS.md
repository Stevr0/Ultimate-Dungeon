# SCRIPTS.md — Ultimate Dungeon (Comprehensive Script Inventory)

Version: 1.7  
Last Updated: 2026-02-02

---

## PURPOSE (LOCKED)

This document lists **ALL scripts in the Unity project** (not just high-level gameplay systems).

It exists so you can:
- Attach it to Project Files for each chat
- Quickly locate scripts by name/path
- Track what is **active**, **spike**, **legacy**, or **editor-only**
- Avoid “ghost scripts” silently hanging around

This document is **NOT authoritative design**.

Authoritative behavior/rules live in design docs:
- `DOCUMENTS_INDEX.md`
- `ACTOR_MODEL.md`
- `TARGETING_MODEL.md`
- `COMBAT_CORE.md`
- `SCENE_RULE_PROVIDER.md`
- `PLAYER_DEFINITION.md`

---

## HOW THIS FILE SHOULD BE MAINTAINED

### Source of truth
This file should be **generated** (or at least partially generated) from the project.
Manual edits are fine for:
- “Status” (Active / Spike / Legacy)
- Short purpose notes
- Wiring notes (which prefab / scene uses it)

### Update cadence
Update after most sessions (your current workflow), so it stays accurate.

---

## REQUIRED COLUMNS

Every script entry must include:
- **Script** (class/file name)
- **Path** (within Assets)
- **Category** (Runtime / UI / Editor / Debug / Data / Networking / Tests)
- **Status** (Active / Spike / Legacy / Deprecated)
- **Purpose** (1 line)
- **Notes** (optional)

---

## AUTOMATION (RECOMMENDED)

You do *not* want to maintain a comprehensive list by hand.
Below is a small **Editor utility** that exports a full inventory of `*.cs` files under `Assets/` into a Markdown file.

### Setup
1. Create folder: `Assets/_Scripts/Editor/`
2. Add file: `ScriptInventoryExporter.cs`
3. Paste the code below.

### Usage
In Unity:
- Menu: **Tools → Ultimate Dungeon → Export Script Inventory**

It writes:
- `Assets/SCRIPTS.generated.md`

Then you can copy/paste that content into this `SCRIPTS.md` (or we can treat the generated file as the attachment).

> Tip: You can keep this `SCRIPTS.md` as the human-friendly version and attach `SCRIPTS.generated.md` for each chat.

---

## ScriptInventoryExporter.cs (Editor)

```csharp
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
```

---

## COMPREHENSIVE SCRIPT LIST

> This section is intended to be the **fully enumerated list**.
> Once you run the exporter, copy the generated Markdown here (or attach `SCRIPTS.generated.md` to chats).

(Generated list goes here)

