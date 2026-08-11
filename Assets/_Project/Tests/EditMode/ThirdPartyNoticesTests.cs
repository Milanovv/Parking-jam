using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

public class ThirdPartyNoticesTests
{
    private const string NoticesPath = "THIRD_PARTY_NOTICES.md";

    private static readonly string[] VendorPackRoots =
    {
        "Assets/BrokenVector",
        "Assets/DenysAlmaral",
        "Assets/Palmov Island",
        "Assets/YughuesFreeConcreteMaterials"
    };

    private static string[][] NoticeRows()
    {
        Assert.IsTrue(File.Exists(NoticesPath), "The third-party notices document exists at the repo root");
        return File.ReadAllLines(NoticesPath)
            .Where(IsTableRow)
            .Select(row => row.Split('|').Select(cell => cell.Trim()).Where(cell => cell.Length > 0).ToArray())
            .ToArray();
    }

    private static bool IsTableRow(string line)
    {
        if (!line.StartsWith("| ")) return false;
        if (line.StartsWith("| ---")) return false;
        if (line.StartsWith("| Asset |")) return false;
        return true;
    }

    private static void AssertNoFiles(string[] roots, string[] patterns, string message)
    {
        string[] offenders = roots
            .SelectMany(root => patterns.SelectMany(pattern => Directory.GetFiles(root, pattern, SearchOption.AllDirectories)))
            .Distinct()
            .ToArray();
        CollectionAssert.IsEmpty(offenders, message + " " + string.Join(", ", offenders));
    }

    [Test]
    public void Notices_RecordEverySourcedPack_WithSourceLicenseAndAuditDate()
    {
        var rows = NoticeRows();
        Assert.That(rows.Length, Is.GreaterThanOrEqualTo(VendorPackRoots.Length),
            "Every imported pack has a notices row");

        foreach (var cells in rows)
        {
            Assert.That(cells.Length, Is.GreaterThanOrEqualTo(4),
                string.Join(" | ", cells) + " has the Asset | Source | License | Audited columns");

            Assert.IsNotEmpty(cells[0], "The Asset cell names the pack");
            Assert.IsNotEmpty(cells[1], "The Source cell records where the pack came from");
            Assert.IsNotEmpty(cells[2], "The License cell records the EULA");

            Assert.IsTrue(DateTime.TryParse(cells[3], out var date),
                cells[3] + " is a real audit date");
            Assert.That(date, Is.LessThanOrEqualTo(DateTime.Today),
                cells[3] + " is not in the future");
        }

        foreach (var root in VendorPackRoots)
        {
            var folderName = Path.GetFileName(root.TrimEnd('/'));
            Assert.IsTrue(rows.Any(cells => cells[0].Contains(folderName)),
                "Rows cover the " + folderName + " pack");
            Assert.IsTrue(Directory.Exists(root),
                "The " + folderName + " pack is present in the project");
        }
    }

    [Test]
    public void VendorPacks_ContainNoScripts()
    {
        AssertNoFiles(VendorPackRoots, new[] { "*.cs" },
            "No pack ships a compiled script into the game assembly (pruned per the notices):");
    }

    [Test]
    public void VendorPacks_ContainNoDemoScenes()
    {
        AssertNoFiles(VendorPackRoots, new[] { "*.unity" },
            "No pack demo scene lands in the project (pruned per the notices):");
    }

    [Test]
    public void VendorPacks_ContainNoAudio()
    {
        AssertNoFiles(VendorPackRoots, new[] { "*.wav", "*.mp3", "*.ogg", "*.aiff", "*.aif" },
            "No pack audio of unverifiable provenance lands in the project (SFX come from Mixkit/CC0 per ADR-0008):");
    }
}