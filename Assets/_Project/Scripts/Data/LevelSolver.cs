using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public struct SolvableMove
{
    public int VehicleIndex;
    public Vector2Int Direction;
}

public static class LevelSolver
{
    private const int MaxExpandedStates = 250000;
    private const int ExitedSentinel = int.MinValue;

    public static bool Solvable(LevelData level)
    {
        return Solve(level) != null;
    }

    public static List<SolvableMove> Solve(LevelData level)
    {
        if (level == null || level.vehicles == null || level.vehicles.Length == 0) return null;
        if (!LevelValidator.TryValidate(level, out _)) return null;

        var board = new Board(level);
        var initial = new SolverState(board);
        var frontier = new Queue<SolverState>();
        var visited = new HashSet<string> { initial.Key };
        frontier.Enqueue(initial);

        int expanded = 0;
        while (frontier.Count > 0 && expanded < MaxExpandedStates)
        {
            var current = frontier.Dequeue();
            expanded++;
            if (current.AllExited) return Reconstruct(current);

            for (int index = 0; index < current.Positions.Length; index++)
            {
                if (current.Positions[index] == ExitedSentinel) continue;
                foreach (var direction in DirectionsFor(board.Data.vehicles[index].orientation))
                {
                    var next = current.Slide(board, index, direction);
                    if (next == null) continue;
                    if (visited.Add(next.Key))
                        frontier.Enqueue(next);
                }
            }
        }
        return null;
    }

    private static Vector2Int[] DirectionsFor(string orientation)
    {
        if (orientation == "vertical") return new[] { Vector2Int.up, Vector2Int.down };
        return new[] { Vector2Int.right, Vector2Int.left };
    }

    private static List<SolvableMove> Reconstruct(SolverState final)
    {
        var moves = new List<SolvableMove>();
        for (var current = final; current.Parent != null; current = current.Parent)
            moves.Add(current.AppliedMove);
        moves.Reverse();
        return moves;
    }

    private sealed class Board
    {
        public readonly LevelData Data;

        public Board(LevelData data)
        {
            Data = data;
        }

        public bool IsExitTile(int x, int y)
        {
            foreach (var tile in Data.exitTiles)
            {
                if (tile.x == x && tile.y == y) return true;
            }
            return false;
        }
    }

    private sealed class SolverState
    {
        private readonly Board _board;
        public readonly int[] Positions;
        public readonly SolverState Parent;
        public readonly SolvableMove AppliedMove;

        public string Key { get; private set; }
        public bool AllExited { get; private set; }

        public SolverState(Board board)
        {
            _board = board;
            Positions = new int[board.Data.vehicles.Length];
            for (int i = 0; i < Positions.Length; i++)
            {
                var first = board.Data.vehicles[i].tiles[0];
                Positions[i] = Pack(first.x, first.y);
            }
            ComputeKey();
        }

        private SolverState(Board board, int[] positions, SolverState parent, SolvableMove applied)
        {
            _board = board;
            Positions = positions;
            Parent = parent;
            AppliedMove = applied;
            ComputeKey();
        }

        public SolverState Slide(Board board, int index, Vector2Int direction)
        {
            var vehicle = board.Data.vehicles[index];
            int x = UnpackX(Positions[index]);
            int y = UnpackY(Positions[index]);
            bool horizontal = direction.x != 0;
            bool positive = horizontal ? direction.x > 0 : direction.y > 0;
            int axis = horizontal ? x : y;
            int limit = horizontal ? board.Data.gridWidth : board.Data.gridHeight;
            int length = vehicle.tiles.Length;

            int start = axis;
            while (true)
            {
                int probe = positive ? start + length : start - 1;
                if (probe < 0 || probe >= limit)
                {
                    int lastAxis = positive ? Math.Min(start + length - 1, limit - 1) : start;
                    bool crossed = horizontal
                        ? board.IsExitTile(lastAxis, y)
                        : board.IsExitTile(x, lastAxis);
                    if (crossed)
                    {
                        var exited = (int[])Positions.Clone();
                        exited[index] = ExitedSentinel;
                        return new SolverState(board, exited, this, new SolvableMove { VehicleIndex = index, Direction = direction });
                    }
                    break;
                }
                if (IsBlocked(board, index, horizontal, probe, y, x)) break;
                start = positive ? start + 1 : start - 1;
            }

            if (start == axis) return null;
            var positions = (int[])Positions.Clone();
            positions[index] = Pack(horizontal ? start : x, horizontal ? y : start);
            return new SolverState(board, positions, this, new SolvableMove { VehicleIndex = index, Direction = direction });
        }

        private bool IsBlocked(Board board, int moverIndex, bool horizontal, int probe, int y, int x)
        {
            var level = board.Data;
            int probeX = horizontal ? probe : x;
            int probeY = horizontal ? y : probe;
            if (probeX < 0 || probeX >= level.gridWidth || probeY < 0 || probeY >= level.gridHeight) return true;

            if (level.staticObstacles != null)
            {
                foreach (var obstacle in level.staticObstacles)
                {
                    if (obstacle.tile.x == probeX && obstacle.tile.y == probeY) return true;
                }
            }
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == moverIndex || Positions[i] == ExitedSentinel) continue;
                int otherX = UnpackX(Positions[i]);
                int otherY = UnpackY(Positions[i]);
                var other = level.vehicles[i];
                int length = other.tiles.Length;
                if (other.orientation == "horizontal")
                {
                    if (otherY != probeY) continue;
                    if (probeX >= otherX && probeX < otherX + length) return true;
                }
                else
                {
                    if (otherX != probeX) continue;
                    if (probeY >= otherY && probeY < otherY + length) return true;
                }
            }
            return false;
        }

        private void ComputeKey()
        {
            var builder = new StringBuilder();
            bool any = false;
            for (int i = 0; i < Positions.Length; i++)
            {
                if (Positions[i] == ExitedSentinel) continue;
                if (any) builder.Append('|');
                builder.Append(Positions[i]);
                any = true;
            }
            Key = builder.ToString();
            AllExited = !any;
        }

        private static int Pack(int x, int y)
        {
            return y << 8 | x;
        }

        private static int UnpackX(int packed)
        {
            return packed & 0xFF;
        }

        private static int UnpackY(int packed)
        {
            return packed >> 8;
        }
    }
}