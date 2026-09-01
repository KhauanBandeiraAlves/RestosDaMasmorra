using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using RestosDaMasmorra.Dungeon;
using Debug = UnityEngine.Debug;

namespace RestosDaMasmorra.EditorTools
{
    public static class DungeonValidationTool
    {
        const string DefinitionPath = "Assets/_Project/ScriptableObjects/Dungeon/PrototypeDungeonDefinition.asset";
        const string ReportPath = "Docs/Validation/DungeonGenerationReport.md";
        const int SeedCount = 5000;
        const float PreviousAverageBranches = 0.22f;

        public static void RunSeedValidation()
        {
            DungeonDefinition definition = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(DefinitionPath);
            if (definition == null)
            {
                Debug.LogError($"DungeonValidationTool: could not load DungeonDefinition at {DefinitionPath}");
                return;
            }

            int successCount = 0;
            int failCount = 0;
            List<int> roomCounts = new List<int>();
            List<int> branchCounts = new List<int>();
            List<(int seed, string reason)> failures = new List<(int, string)>();
            int determinismChecks = 0;
            int determinismFailures = 0;

            Stopwatch sw = Stopwatch.StartNew();

            for (int seed = 1; seed <= SeedCount; seed++)
            {
                DungeonLayoutResult result = DungeonGenerator.Generate(definition, seed);

                if (!result.Success)
                {
                    failCount++;
                    failures.Add((seed, result.FailureReason));
                    continue;
                }

                bool entranceOk = result.Entrance != null && result.Entrance.Definition.RoomType == RoomType.Entrance;
                bool bossOk = result.Boss != null && result.Boss.Definition.RoomType == RoomType.Boss;
                bool reachableOk = DungeonGenerator.IsBossReachable(result);
                bool noOverlap = !DungeonGenerator.HasAnyOverlap(result.Rooms, out string overlapReason);
                bool noNulls = result.Rooms.All(r => r.Prefab != null && r.Definition != null);

                if (!entranceOk || !bossOk || !reachableOk || !noOverlap || !noNulls)
                {
                    failCount++;
                    string reason = !entranceOk ? "missing Entrance" :
                        !bossOk ? "missing Boss" :
                        !reachableOk ? "Boss unreachable" :
                        !noOverlap ? overlapReason :
                        "null reference in placed room";
                    failures.Add((seed, reason));
                    continue;
                }

                successCount++;
                roomCounts.Add(result.Rooms.Count);
                branchCounts.Add(result.BranchCount);

                // Determinism spot-check every 50th seed.
                if (seed % 50 == 0)
                {
                    determinismChecks++;
                    DungeonLayoutResult repeat = DungeonGenerator.Generate(definition, seed);
                    bool same = repeat.Success && repeat.Rooms.Count == result.Rooms.Count &&
                        !repeat.Rooms.Where((room, i) => room.Position != result.Rooms[i].Position || room.YawDegrees != result.Rooms[i].YawDegrees).Any();
                    if (!same) determinismFailures++;
                }
            }

            sw.Stop();

            var sb = new StringBuilder();
            sb.AppendLine("# Dungeon Generation Report");
            sb.AppendLine();
            sb.AppendLine($"- Total seeds tested: {SeedCount}");
            sb.AppendLine($"- Successes: {successCount}");
            sb.AppendLine($"- Failures: {failCount}");
            sb.AppendLine($"- Success rate: {(successCount / (float)SeedCount * 100f):F2}%");
            sb.AppendLine($"- Average rooms per dungeon: {(roomCounts.Count > 0 ? roomCounts.Average() : 0):F2}");
            sb.AppendLine($"- Min rooms: {(roomCounts.Count > 0 ? roomCounts.Min() : 0)}");
            sb.AppendLine($"- Max rooms: {(roomCounts.Count > 0 ? roomCounts.Max() : 0)}");
            float avgBranches = branchCounts.Count > 0 ? (float)branchCounts.Average() : 0;
            sb.AppendLine($"- Average branches: {avgBranches:F2}");
            sb.AppendLine($"- Total generation time: {sw.Elapsed.TotalSeconds:F3}s ({sw.Elapsed.TotalMilliseconds / SeedCount:F3}ms/seed avg)");
            sb.AppendLine($"- Determinism spot-checks: {determinismChecks} (failures: {determinismFailures})");
            sb.AppendLine();

            sb.AppendLine("## Branch variety (Phase B.1)");
            sb.AppendLine();
            sb.AppendLine("| | Average branches/dungeon |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| ANTES (Phase B) | {PreviousAverageBranches:F2} |");
            sb.AppendLine($"| DEPOIS (Phase B.1) | {avgBranches:F2} |");
            sb.AppendLine();
            int b0 = branchCounts.Count(b => b == 0);
            int b1 = branchCounts.Count(b => b == 1);
            int b2 = branchCounts.Count(b => b == 2);
            int bOther = branchCounts.Count - b0 - b1 - b2;
            sb.AppendLine("Branch count distribution:");
            sb.AppendLine();
            sb.AppendLine($"- 0 branches: {b0} ({(b0 / (float)branchCounts.Count * 100f):F1}%)");
            sb.AppendLine($"- 1 branch: {b1} ({(b1 / (float)branchCounts.Count * 100f):F1}%)");
            sb.AppendLine($"- 2 branches: {b2} ({(b2 / (float)branchCounts.Count * 100f):F1}%)");
            if (bOther > 0) sb.AppendLine($"- other: {bOther}");
            sb.AppendLine();

            if (failures.Count > 0)
            {
                sb.AppendLine("## Problem seeds");
                sb.AppendLine();
                foreach ((int seed, string reason) in failures.Take(50))
                {
                    sb.AppendLine($"- seed {seed}: {reason}");
                }
                if (failures.Count > 50) sb.AppendLine($"- ... and {failures.Count - 50} more.");
            }
            else
            {
                sb.AppendLine($"No problem seeds found. {SeedCount}/{SeedCount} valid.");
            }

            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText(ReportPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"DungeonValidationTool: {successCount}/{SeedCount} seeds valid. Report written to {ReportPath}.");
        }
    }
}
