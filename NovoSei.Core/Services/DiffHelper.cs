using System;
using System.Collections.Generic;
using System.Linq;

namespace NovoSei.Core.Services;

public enum DiffType
{
    Unchanged,
    Inserted,
    Deleted
}

public class DiffLine
{
    public DiffType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? OldLineNumber { get; set; }
    public int? NewLineNumber { get; set; }
}

public static class DiffHelper
{
    public static List<DiffLine> GenerateDiff(string oldText, string newText)
    {
        var oldLines = (oldText ?? "").Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var newLines = (newText ?? "").Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        int n = oldLines.Length;
        int m = newLines.Length;

        // LCS Dynamic Programming table
        int[,] dp = new int[n + 1, m + 1];

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (oldLines[i - 1] == newLines[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        // Backtrack to find the diff
        var result = new List<DiffLine>();
        int x = n, y = m;

        while (x > 0 || y > 0)
        {
            if (x > 0 && y > 0 && oldLines[x - 1] == newLines[y - 1])
            {
                result.Insert(0, new DiffLine
                {
                    Type = DiffType.Unchanged,
                    Text = oldLines[x - 1],
                    OldLineNumber = x,
                    NewLineNumber = y
                });
                x--;
                y--;
            }
            else if (y > 0 && (x == 0 || dp[x, y - 1] >= dp[x - 1, y]))
            {
                result.Insert(0, new DiffLine
                {
                    Type = DiffType.Inserted,
                    Text = newLines[y - 1],
                    OldLineNumber = null,
                    NewLineNumber = y
                });
                y--;
            }
            else if (x > 0 && (y == 0 || dp[x, y - 1] < dp[x - 1, y]))
            {
                result.Insert(0, new DiffLine
                {
                    Type = DiffType.Deleted,
                    Text = oldLines[x - 1],
                    OldLineNumber = x,
                    NewLineNumber = null
                });
                x--;
            }
        }

        return result;
    }
}
