using NUnit.Framework;
using UnityEngine;

public class LevelSolverTests
{
    private static LevelData Level(params VehicleData[] vehicles)
    {
        return new LevelData
        {
            id = 1,
            name = "Solver",
            gridWidth = 5,
            gridHeight = 5,
            levelUndos = 3,
            exitTiles = new[] { new Vector2Int(4, 2) },
            vehicles = vehicles
        };
    }

    private static VehicleData Car(string id, string orientation, Vector2Int from, Vector2Int to)
    {
        return new VehicleData
        {
            id = id,
            orientation = orientation,
            tiles = new[] { from, to }
        };
    }

    [Test]
    public void EmptyExitRow_SlidesOutAndWins()
    {
        var level = Level(Car("car", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2)));

        Assert.IsTrue(LevelSolver.Solvable(level));
        var moves = LevelSolver.Solve(level);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(0, moves[0].VehicleIndex);
        Assert.AreEqual(Vector2Int.right, moves[0].Direction);
    }

    [Test]
    public void BlockingChain_RequiresOrderedMoves()
    {
        var blocker = Car("blocker", "horizontal", new Vector2Int(2, 2), new Vector2Int(3, 2));
        var rear = Car("rear", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2));
        var level = Level(rear, blocker);

        var moves = LevelSolver.Solve(level);
        Assert.IsNotNull(moves);
        Assert.IsTrue(
            moves.Exists(move => level.vehicles[move.VehicleIndex].id == "blocker" && move.Direction == Vector2Int.right),
            "The near-exit blocker must slide out before the lane opens for the rear car");
    }

    [Test]
    public void WallOfVehicles_ExitDemandsTwoLanes()
    {
        var a = Car("a", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2));
        var b = Car("b", "horizontal", new Vector2Int(2, 2), new Vector2Int(3, 2));
        var level = Level(a, b);

        var moves = LevelSolver.Solve(level);
        Assert.IsNotNull(moves);
        Assert.AreEqual(2, moves.Count);
    }

    [Test]
    public void SealedExit_Unsolved()
    {
        var level = Level(Car("a", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2)));
        level.staticObstacles = new[]
        {
            new StaticObstacleData { tile = new Vector2Int(2, 0) },
            new StaticObstacleData { tile = new Vector2Int(2, 3) }
        };
        var blocker = Car("blocker", "vertical", new Vector2Int(2, 1), new Vector2Int(2, 2));
        level.vehicles = new[] { blocker, level.vehicles[0] };

        Assert.IsFalse(LevelSolver.Solvable(level),
            "The exit lane is sealed: the vertical blocker is pinned by obstacles and the horizontal car cannot pass");
    }

    [Test]
    public void ObstacleBlocksTheExitLane_ButOtherLaneWins()
    {
        var level = Level(Car("car", "horizontal", new Vector2Int(0, 1), new Vector2Int(1, 1)));
        level.staticObstacles = new[] { new StaticObstacleData { tile = new Vector2Int(3, 1) } };

        var moves = LevelSolver.Solve(level);
        Assert.IsNull(moves, "The lane is sealed by the obstacle and the exit needs this lane");
    }

    [Test]
    public void BarrierOnExitTile_DoesNotSealTheLevel()
    {
        var level = Level(Car("car", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2)));
        level.barriers = new[] { new BarrierData { miniGameScene = "MiniGame_Pipes_Easy", tile = new Vector2Int(4, 2) } };

        Assert.IsTrue(LevelSolver.Solvable(level), "The barrier unlocks via a free-retry mini-game, so exit is reachable");
    }

    [Test]
    public void SlideOffTheWrongEdge_DoesNotCountAsExit()
    {
        var blocker = Car("blocker", "vertical", new Vector2Int(3, 2), new Vector2Int(3, 3));
        var rear = Car("rear", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2));
        var level = Level(rear, blocker);

        Assert.IsFalse(LevelSolver.Solvable(level),
            "The exit tile is on the right edge; sliding off the left edge at the same row is not an exit");
    }

    [Test]
    public void SlideOffEdge_ThroughAnExitTile_Exits()
    {
        var level = Level(Car("car", "horizontal", new Vector2Int(2, 2), new Vector2Int(3, 2)));
        level.exitTiles = new[] { new Vector2Int(0, 2) };

        var moves = LevelSolver.Solve(level);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(Vector2Int.left, moves[0].Direction);
    }

    [Test]
    public void VerticalVehicleOnExitTile_Exits()
    {
        var level = Level(Car("car", "vertical", new Vector2Int(4, 0), new Vector2Int(4, 1)));
        level.exitTiles = new[] { new Vector2Int(4, 0) };

        var moves = LevelSolver.Solve(level);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(Vector2Int.down, moves[0].Direction,
            "The exit tile at the bottom edge opens when the car's first tile sits on it");
    }

    [Test]
    public void InvalidLevel_NotSolvable()
    {
        var level = Level(Car("car", "horizontal", new Vector2Int(0, 2), new Vector2Int(1, 2)));
        level.gridWidth = 3;

        Assert.IsFalse(LevelSolver.Solvable(level));
    }
}