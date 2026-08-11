using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PipeBoardTests
{
    [Test]
    public void PipeDirections_Straight_RotationZeroIsHorizontal_OneIsVertical()
    {
        Assert.AreEqual(PipeDirections.Left | PipeDirections.Right, PipeDirections.OpenMask(PipeTileType.Straight, 0));
        Assert.AreEqual(PipeDirections.Up | PipeDirections.Down, PipeDirections.OpenMask(PipeTileType.Straight, 1));
    }

    [Test]
    public void PipeDirections_Corner_RotationAdvancesClockwise()
    {
        Assert.AreEqual(PipeDirections.Up | PipeDirections.Right, PipeDirections.OpenMask(PipeTileType.Corner, 0));
        Assert.AreEqual(PipeDirections.Right | PipeDirections.Down, PipeDirections.OpenMask(PipeTileType.Corner, 1));
        Assert.AreEqual(PipeDirections.Down | PipeDirections.Left, PipeDirections.OpenMask(PipeTileType.Corner, 2));
        Assert.AreEqual(PipeDirections.Left | PipeDirections.Up, PipeDirections.OpenMask(PipeTileType.Corner, 3));
    }

    [Test]
    public void PipeDirections_TJunction_OpensThreeOfFourDirections()
    {
        Assert.AreEqual(PipeDirections.Up | PipeDirections.Right | PipeDirections.Left, PipeDirections.OpenMask(PipeTileType.T, 0));
        Assert.AreEqual(PipeDirections.Left | PipeDirections.Up | PipeDirections.Down, PipeDirections.OpenMask(PipeTileType.T, 3));
    }

    [Test]
    public void SolvePath_StraightLineOfCornerConnectedTiles_ConnectsSourceToSink()
    {
        var board = new PipeBoard(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0));
        board.SetTile(0, 0, PipeTileType.Source, 0);
        board.SetTile(1, 0, PipeTileType.Straight, 0);
        board.SetTile(2, 0, PipeTileType.Sink, 0);

        var path = board.SolvePath();
        Assert.IsNotNull(path);
        Assert.AreEqual(3, path.Count);
        Assert.AreEqual(new Vector2Int(0, 0), path[0]);
        Assert.AreEqual(new Vector2Int(2, 0), path[2]);
    }

    [Test]
    public void SolvePath_MisorientedStraight_BreaksThePath()
    {
        var board = new PipeBoard(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0));
        board.SetTile(0, 0, PipeTileType.Source, 0);
        board.SetTile(1, 0, PipeTileType.Straight, 1);
        board.SetTile(2, 0, PipeTileType.Sink, 0);

        Assert.IsFalse(board.IsConnected());
    }

    [Test]
    public void TryRotate_RepairsThePath_AndSecondRotationBreaksItAgain()
    {
        var board = new PipeBoard(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0));
        board.SetTile(0, 0, PipeTileType.Source, 0);
        board.SetTile(1, 0, PipeTileType.Straight, 1);
        board.SetTile(2, 0, PipeTileType.Sink, 0);
        board.SetRotatable(1, 0, true);

        Assert.IsTrue(board.TryRotate(1, 0));
        Assert.IsTrue(board.IsConnected());
        Assert.IsTrue(board.TryRotate(1, 0));
        Assert.IsFalse(board.IsConnected());
    }

    [Test]
    public void TryRotate_NonRotatableTile_DoesNothing()
    {
        var board = new PipeBoard(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0));
        board.SetTile(1, 0, PipeTileType.Straight, 0);

        Assert.IsFalse(board.TryRotate(1, 0));
        Assert.AreEqual(0, board.Tile(1, 0).Rotation);
    }

    [Test]
    public void Generate_AllSpecSizes_SpawnUnsolvableButReachable()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            GenerateAndAssert(3, 3, 3, seed);
            GenerateAndAssert(4, 3, 5, seed);
        }

        for (int seed = 0; seed < 3; seed++)
            GenerateAndAssert(4, 4, 8, seed);
    }

    private static void GenerateAndAssert(int width, int height, int rotatableCount, int seed)
    {
        var board = PipeBoard.Generate(width, height, rotatableCount, new System.Random(seed));
        Assert.IsNotNull(board);
        Assert.IsFalse(board.IsConnected(), "Board " + width + "x" + height + " seed " + seed + " must not spawn solved");
        Assert.AreEqual(rotatableCount, board.RotatableCount);
        Assert.IsTrue(ReachableSolved(board), "Board " + width + "x" + height + " seed " + seed + " must be solvable by rotation");
    }

    private static bool ReachableSolved(PipeBoard board)
    {
        var rotatables = new List<Vector2Int>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (board.IsRotatable(x, y)) rotatables.Add(new Vector2Int(x, y));
            }
        }

        int total = 1 << (2 * rotatables.Count);
        for (int state = 0; state < total; state++)
        {
            for (int i = 0; i < rotatables.Count; i++)
            {
                var tile = rotatables[i];
                var current = board.Tile(tile.x, tile.y);
                current.Rotation = (state >> (2 * i)) & 3;
                board.SetTile(tile.x, tile.y, current.Type, current.Rotation);
            }

            if (board.IsConnected()) return true;
        }

        return false;
    }

    [Test]
    public void Generate_IsDeterministicForASeed()
    {
        var a = PipeBoard.Generate(4, 4, 8, new System.Random(42));
        var b = PipeBoard.Generate(4, 4, 8, new System.Random(42));

        foreach (var pair in PipeBoard.SnapshotRotations(a))
            Assert.AreEqual(pair.Value, b.Tile(pair.Key.x, pair.Key.y).Rotation);
    }

    [Test]
    public void Generate_DifferentSeeds_DifferOnAtLeastOneRotatableTile()
    {
        var a = PipeBoard.Generate(4, 4, 8, new System.Random(1));
        var b = PipeBoard.Generate(4, 4, 8, new System.Random(2));

        int differences = 0;
        for (int x = 0; x < a.Width; x++)
        {
            for (int y = 0; y < a.Height; y++)
            {
                if (a.IsRotatable(x, y) && a.Tile(x, y).Rotation != b.Tile(x, y).Rotation) differences++;
            }
        }
        Assert.Greater(differences, 0);
    }

    [Test]
    public void HintTile_GeneratedBoard_ReturnsARotatableTile()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var board = PipeBoard.Generate(4, 4, 8, new System.Random(seed));
            var hint = board.HintTile();
            Assert.IsNotNull(hint, "Seed " + seed + " must offer a hint");
            Assert.IsTrue(board.IsRotatable(hint.Value.x, hint.Value.y), "Hint must be a rotatable tile");
        }
    }

    [Test]
    public void HintTile_ManualPath_FlagsTheMisorientedTileOnly()
    {
        var board = new PipeBoard(3, 1, new Vector2Int(0, 0), new Vector2Int(2, 0));
        board.SetTile(0, 0, PipeTileType.Source, 0);
        board.SetTile(1, 0, PipeTileType.Straight, 1);
        board.SetTile(2, 0, PipeTileType.Sink, 0);
        board.SetRotatable(1, 0, true);

        Assert.AreEqual(new Vector2Int(1, 0), board.HintTile());
    }
}