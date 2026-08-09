using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MoveResolverTests
{
    private const int GridWidth = 8;
    private const int GridHeight = 8;

    private static Mover MoverAt(Vector3Int position, Orientation orientation, int length)
    {
        return new Mover { Position = position, Orientation = orientation, Length = length };
    }

    private static MoveRequest Request(
        Mover mover,
        Vector3Int direction,
        IReadOnlyCollection<Vector3Int> exitTiles = null,
        GateState gate = null)
    {
        return new MoveRequest
        {
            Mover = mover,
            Direction = direction,
            GridSize = new Vector2Int(GridWidth, GridHeight),
            ExitTiles = exitTiles,
            Gate = gate
        };
    }

    private static TestOccupant HorizontalOccupant(Vector3Int start, int length)
    {
        var tiles = new Vector3Int[length];
        for (int i = 0; i < length; i++)
            tiles[i] = new Vector3Int(start.x + i, start.y, 0);
        return new TestOccupant(tiles);
    }

    private static TestOccupant VerticalOccupant(Vector3Int start, int length)
    {
        var tiles = new Vector3Int[length];
        for (int i = 0; i < length; i++)
            tiles[i] = new Vector3Int(start.x, start.y + i, 0);
        return new TestOccupant(tiles);
    }

    [Test]
    public void Resolve_FreeSlide_CompletesToGridEdge()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0)));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(6, outcome.Steps);
        Assert.AreEqual(new Vector3Int(6, 0, 0), outcome.Destination);
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(1, resolver.Tick, "A freely-slid drag advances the tick by one");
        Assert.AreEqual(3, resolver.UndoBalance, "A free slide consumes no undo");
    }

    [Test]
    public void Resolve_LeftwardFreeSlide_CompletesToGridEdge()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(4, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(4, 0, 0), Orientation.Horizontal, 2), new Vector3Int(-1, 0, 0)));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(4, outcome.Steps);
        Assert.AreEqual(new Vector3Int(0, 0, 0), outcome.Destination);
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_ConsecutiveFreeSlides_AdvanceTickPerMove()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var request = Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0));

        resolver.Resolve(map, request);
        resolver.Resolve(map, request);

        Assert.AreEqual(2, resolver.Tick, "One tick per completed Move");
    }

    [Test]
    public void Resolve_ZeroSlideAgainstGridEdge_CompletesWithoutMovingOrTicking()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(6, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(6, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0)));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(0, outcome.Steps);
        Assert.AreEqual(new Vector3Int(6, 0, 0), outcome.Destination);
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(0, resolver.Tick, "A drag that slides no tiles is not a Move and does not tick");
        Assert.AreEqual(3, resolver.UndoBalance);
    }

    [Test]
    public void Resolve_NoSlideAgainstOccupant_CompletesWithoutCost()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(3, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(5, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(3, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0)));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(0, outcome.Steps);
        Assert.AreEqual(StopReason.NoSlide, outcome.StopReason);
        Assert.AreEqual(0, resolver.Tick, "A drag that cannot slide is not a Move and does not tick");
        Assert.AreEqual(3, resolver.UndoBalance, "A drag that cannot slide does not collide and spends nothing");
    }

    [Test]
    public void Resolve_BlockedByOccupantWithSlide_CancelsAndSpendsUndo()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(4, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0)));

        Assert.AreEqual(MoveOutcomeKind.Cancelled, outcome.Kind);
        Assert.AreEqual(0, outcome.Steps, "A cancelled move restores the vehicle: nothing applied");
        Assert.AreEqual(new Vector3Int(0, 0, 0), outcome.Destination);
        Assert.AreEqual(StopReason.Blocked, outcome.StopReason);
        Assert.AreEqual(2, resolver.UndoBalance, "Exactly one undo spent per collision");
        Assert.AreEqual(0, resolver.Tick, "A cancelled move does not advance the tick");
    }

    [Test]
    public void Resolve_CancelledMove_SnapshotCapturesPreMoveWorld()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(4, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 3);

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0)));

        WorldSnapshot snapshot = outcome.Snapshot;
        CollectionAssert.AreEqual(
            new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) },
            snapshot.OccupantPositions[mover],
            "Snapshot holds the mover's pre-move tiles"
        );
        CollectionAssert.AreEqual(
            new[] { new Vector3Int(4, 0, 0) },
            snapshot.OccupantPositions[blocker],
            "Snapshot holds every other occupant's pre-move tiles"
        );
        Assert.AreEqual(0, snapshot.Tick);
        Assert.AreEqual(3, snapshot.UndoBalance, "Snapshot is taken before the undo is spent");
    }

    [Test]
    public void Resolve_LockedBarrier_BumperToGate_StopsOneShortWithoutSpendingUndo()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var barrier = new Barrier(new Vector3Int(4, 0, 0));
        map.Place(mover);
        map.Place(barrier);
        var resolver = new MoveResolver(authoredUndos: 3);
        var gate = new GateState();

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), gate: gate));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(2, outcome.Steps);
        Assert.AreEqual(new Vector3Int(2, 0, 0), outcome.Destination);
        Assert.AreEqual(StopReason.BarrierBlocked, outcome.StopReason);
        Assert.AreEqual(3, resolver.UndoBalance, "A bumper-to-gate stop consumes nothing");
        Assert.AreEqual(1, resolver.Tick, "The slide to the barrier is a completed Move");
    }

    [Test]
    public void Resolve_ExitTileOpenGate_CrossingEdge_ResolvesExit()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind, "Crossing the exit edge with an open gate leaves the lot");
        Assert.AreEqual(7, outcome.Destination.x, "The exit destination places the leading tile at the exit lane");
        Assert.AreEqual(1, resolver.Tick, "An exit move completes and advances the tick");
        Assert.AreEqual(3, resolver.UndoBalance, "Leaving the lot costs nothing");
    }

    [Test]
    public void Resolve_ExitTile_ParkedAtBoundary_ResolvesExit()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(6, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(6, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind, "A vehicle at the boundary exits when pushed outward");
        Assert.AreEqual(7, outcome.Destination.x, "One outward push lands the leading tile in the exit lane");
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_ExitedLeftward_DestinationIsOffGridOnTheLeft()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(1, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(0, 0, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(1, 0, 0), Orientation.Horizontal, 2), new Vector3Int(-1, 0, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind);
        Assert.AreEqual(-1, outcome.Destination.x, "The destination sits fully off the left edge");
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_VerticalExitTile_ColumnMatch_ResolvesExit()
    {
        var map = new OccupancyMap();
        var mover = VerticalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(0, 7, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Vertical, 2), new Vector3Int(0, 1, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind, "Exit tiles match columns for vertical movers");
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_NonExitBoundary_CompletesOrdinaryStop()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 3, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind, "A non-exit boundary never yields an exit verdict");
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_ExitOutcome_LeavesNoOccupantWhenApplied()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind);
        map.Remove(mover);
        Assert.IsTrue(map.IsTileFree(new Vector3Int(0, 0, 0)), "The caller removes the vehicle from the grid on exit");
        Assert.IsTrue(map.IsTileFree(new Vector3Int(1, 0, 0)));
    }

    [Test]
    public void Resolve_LockedGate_ExitTile_StopsAtBoundaryWithoutExiting()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };
        var gate = new GateState();

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles, gate));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind, "A locked gate seals every exit tile");
        Assert.AreEqual(6, outcome.Steps);
        Assert.AreEqual(new Vector3Int(6, 0, 0), outcome.Destination, "The drag stops on the last inner-grid tile");
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(3, resolver.UndoBalance, "The sealed edge is a hard stop: no undo consumed");
        Assert.AreEqual(1, resolver.Tick, "The slide to the sealed edge is a completed Move");
    }

    [Test]
    public void Resolve_LockedGate_ParkedAtBoundary_NeverExits()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(6, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };
        var gate = new GateState();

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(6, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles, gate));

        Assert.AreEqual(MoveOutcomeKind.Completed, outcome.Kind);
        Assert.AreEqual(StopReason.GridEdge, outcome.StopReason);
        Assert.AreEqual(0, resolver.Tick, "Parking at the sealed edge without sliding ticks nothing");
    }

    [Test]
    public void Resolve_UnlockedGate_ExitTile_ResolvesExit()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        map.Place(mover);
        var resolver = new MoveResolver(authoredUndos: 3);
        var exitTiles = new List<Vector3Int> { new Vector3Int(7, 0, 0) };
        var gate = new GateState();
        gate.Unlock();

        MoveOutcome outcome = resolver.Resolve(map, Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0), exitTiles, gate));

        Assert.AreEqual(MoveOutcomeKind.Exited, outcome.Kind, "After unlock the exit verdict returns");
        Assert.AreEqual(1, resolver.Tick);
    }

    [Test]
    public void Resolve_CollisionWhenPoolEmpty_RestartsLevelAsFreshAttempt()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(4, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 1);
        var request = Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0));

        MoveOutcome first = resolver.Resolve(map, request);

        Assert.AreEqual(MoveOutcomeKind.Cancelled, first.Kind);
        Assert.AreEqual(0, resolver.UndoBalance, "The last authored undo was spent on the first collision");

        MoveOutcome second = resolver.Resolve(map, request);

        Assert.AreEqual(MoveOutcomeKind.Restarted, second.Kind);
        Assert.AreEqual(0, resolver.Tick, "A restart resets the tick to zero");
        Assert.AreEqual(1, resolver.UndoBalance, "A restart refills the authored pool");
        Assert.AreEqual(1, resolver.AuthoredRemaining);
        CollectionAssert.AreEqual(
            new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0) },
            second.Snapshot.OccupantPositions[mover],
            "The restart snapshot records the attempt's pre-move world"
        );
    }

    [Test]
    public void Resolve_CollisionsSpendBonusUndosBeforeAuthored()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(4, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 3);
        resolver.AddBonusUndos(2);
        var request = Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0));

        resolver.Resolve(map, request);
        Assert.AreEqual(1, resolver.BonusUndos, "First collision spends a bonus undo");
        Assert.AreEqual(3, resolver.AuthoredRemaining, "Authored undos untouched while bonuses remain");

        resolver.Resolve(map, request);
        Assert.AreEqual(0, resolver.BonusUndos);
        Assert.AreEqual(3, resolver.AuthoredRemaining);
        Assert.AreEqual(3, resolver.UndoBalance, "Only then does the authored stock carry the pool");
    }

    [Test]
    public void Resolve_Restart_RefillsAuthoredAndKeepsUnspentBonus()
    {
        var map = new OccupancyMap();
        var mover = HorizontalOccupant(new Vector3Int(0, 0, 0), 2);
        var blocker = HorizontalOccupant(new Vector3Int(4, 0, 0), 1);
        map.Place(mover);
        map.Place(blocker);
        var resolver = new MoveResolver(authoredUndos: 1);
        resolver.AddBonusUndos(1);
        var request = Request(MoverAt(new Vector3Int(0, 0, 0), Orientation.Horizontal, 2), new Vector3Int(1, 0, 0));

        resolver.Resolve(map, request);
        resolver.Resolve(map, request);

        MoveOutcome restart = resolver.Resolve(map, request);

        Assert.AreEqual(MoveOutcomeKind.Restarted, restart.Kind);
        Assert.AreEqual(1, resolver.AuthoredRemaining, "Authored undos refill on restart");
        Assert.AreEqual(0, resolver.BonusUndos, "Bonus stock is never reset or spent by restart itself");
        Assert.AreEqual(1, resolver.UndoBalance);

        resolver.Resolve(map, request);
        MoveOutcome secondRestart = resolver.Resolve(map, request);

        Assert.AreEqual(MoveOutcomeKind.Restarted, secondRestart.Kind);
        Assert.AreEqual(1, resolver.AuthoredRemaining, "Every restart refills the authored pool");
        Assert.AreEqual(0, resolver.BonusUndos, "No bonus balance leaks across restarts");
        Assert.AreEqual(1, resolver.UndoBalance);
    }

    private class TestOccupant : IOccupant
    {
        public Vector3Int[] OccupiedTiles { get; }

        public TestOccupant(Vector3Int[] tiles)
        {
            OccupiedTiles = tiles;
        }
    }
}