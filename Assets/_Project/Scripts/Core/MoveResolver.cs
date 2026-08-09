using UnityEngine;

public enum StopReason
{
    GridEdge,
    NoSlide,
    Blocked,
    BarrierBlocked
}

public enum MoveOutcomeKind
{
    Completed,
    Cancelled,
    Restarted,
    Exited
}

public struct Mover
{
    public Vector3Int Position;
    public Orientation Orientation;
    public int Length;
}

public struct MoveRequest
{
    public Mover Mover;
    public Vector3Int Direction;
    public Vector2Int GridSize;
    public System.Collections.Generic.IReadOnlyCollection<Vector3Int> ExitTiles;
    public GateState Gate;
}

public class MoveOutcome
{
    public MoveOutcomeKind Kind;
    public StopReason StopReason;
    public Vector3Int Destination;
    public int Steps;
    public WorldSnapshot Snapshot;
}

public class WorldSnapshot
{
    public System.Collections.Generic.Dictionary<IOccupant, Vector3Int[]> OccupantPositions;
    public int Tick;
    public int UndoBalance;
    public float? TimerSeconds;
    public System.Collections.Generic.Dictionary<int, Vector3Int> PedestrianPositions;
}

public class MoveResolver
{
    private readonly int _authoredTotal;
    private int _authoredRemaining;
    private int _bonusUndos;
    private int _tick;

    public int Tick => _tick;
    public int UndoBalance => _authoredRemaining + _bonusUndos;
    public int AuthoredRemaining => _authoredRemaining;
    public int BonusUndos => _bonusUndos;

    public MoveResolver(int authoredUndos)
    {
        _authoredTotal = authoredUndos;
        _authoredRemaining = authoredUndos;
    }

    public void AddBonusUndos(int count)
    {
        _bonusUndos += count;
    }

    public MoveOutcome Resolve(OccupancyMap map, MoveRequest request)
    {
        var snapshot = CaptureSnapshot(map);
        (int steps, StopReason reason) = Sweep(map, request.Mover, request.Direction, request.GridSize);

        var outcome = new MoveOutcome
        {
            Kind = MoveOutcomeKind.Completed,
            StopReason = reason,
            Destination = request.Mover.Position + request.Direction * steps,
            Steps = steps,
            Snapshot = snapshot
        };

        bool exitEdge = reason == StopReason.GridEdge && CrossesExitEdge(request);
        if (exitEdge && IsGateOpen(request.Gate))
        {
            outcome.Kind = MoveOutcomeKind.Exited;
            outcome.Destination = MoveOffGrid(request.Mover, request.Direction, request.GridSize);
        }

        if (reason == StopReason.Blocked)
        {
            if (UndoBalance > 0)
            {
                SpendUndo();
                outcome.Kind = MoveOutcomeKind.Cancelled;
                outcome.Destination = request.Mover.Position;
                outcome.Steps = 0;
            }
            else
            {
                RestartLevel();
                outcome.Kind = MoveOutcomeKind.Restarted;
                outcome.Destination = request.Mover.Position;
                outcome.Steps = 0;
            }
            return outcome;
        }

        if (steps > 0 || outcome.Kind == MoveOutcomeKind.Exited) _tick++;
        return outcome;
    }

    private static bool IsGateOpen(GateState gate)
    {
        return gate == null || !gate.Locked;
    }

    private static bool CrossesExitEdge(MoveRequest request)
    {
        if (request.ExitTiles == null || request.ExitTiles.Count == 0) return false;

        bool horizontal = request.Mover.Orientation == Orientation.Horizontal;
        int dir = horizontal ? request.Direction.x : request.Direction.y;
        if (dir == 0) return false;

        int cross = horizontal ? request.Mover.Position.y : request.Mover.Position.x;
        foreach (var tile in request.ExitTiles)
        {
            if (horizontal ? tile.y == cross : tile.x == cross) return true;
        }
        return false;
    }

    private static Vector3Int MoveOffGrid(Mover mover, Vector3Int direction, Vector2Int gridSize)
    {
        bool horizontal = mover.Orientation == Orientation.Horizontal;
        int dir = horizontal ? direction.x : direction.y;
        if (dir > 0)
        {
            int limit = horizontal ? gridSize.x : gridSize.y;
            int crossing = limit - mover.Length + 1;
            return horizontal
                ? new Vector3Int(crossing, mover.Position.y, 0)
                : new Vector3Int(mover.Position.x, crossing, 0);
        }
        return horizontal
            ? new Vector3Int(-1, mover.Position.y, 0)
            : new Vector3Int(mover.Position.x, -1, 0);
    }

    private void SpendUndo()
    {
        if (_bonusUndos > 0)
            _bonusUndos--;
        else
            _authoredRemaining--;
    }

    private void RestartLevel()
    {
        _tick = 0;
        _authoredRemaining = _authoredTotal;
    }

    private WorldSnapshot CaptureSnapshot(OccupancyMap map)
    {
        var positions = new System.Collections.Generic.Dictionary<IOccupant, Vector3Int[]>();
        foreach (var occupant in map.GetOccupants())
            positions[occupant] = occupant.OccupiedTiles;

        return new WorldSnapshot
        {
            OccupantPositions = positions,
            Tick = _tick,
            UndoBalance = UndoBalance
        };
    }

    private static (int Steps, StopReason Reason) Sweep(OccupancyMap map, Mover mover, Vector3Int direction, Vector2Int gridSize)
    {
        bool horizontal = mover.Orientation == Orientation.Horizontal;
        int dir = horizontal ? direction.x : direction.y;
        if (dir == 0) return (0, StopReason.NoSlide);

        int axis = horizontal ? mover.Position.x : mover.Position.y;
        int cross = horizontal ? mover.Position.y : mover.Position.x;
        int limit = horizontal ? gridSize.x : gridSize.y;

        int steps = 0;
        while (true)
        {
            int leading = dir > 0 ? axis + mover.Length - 1 + steps + 1 : axis - (steps + 1);
            if (leading < 0 || leading >= limit)
                return (steps, StopReason.GridEdge);

            var tile = horizontal
                ? new Vector3Int(leading, cross, 0)
                : new Vector3Int(cross, leading, 0);

            if (!map.IsTileFree(tile))
            {
                if (steps == 0) return (0, StopReason.NoSlide);
                if (map.TryGetOccupant(tile, out var blocker) && !blocker.CausesCollision)
                    return (steps, StopReason.BarrierBlocked);
                return (steps, StopReason.Blocked);
            }

            steps++;
        }
    }
}
