using System;
using System.Collections.Generic;
using UnityEngine;

public enum PipeTileType
{
    Straight,
    Corner,
    T,
    Source,
    Sink
}

public struct PipeTile
{
    public PipeTileType Type;
    public int Rotation;
}

public static class PipeDirections
{
    public const int Up = 1;
    public const int Right = 2;
    public const int Down = 4;
    public const int Left = 8;

    public static readonly Vector2Int[] All =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    public static Vector2Int Vector(int dir)
    {
        switch (dir)
        {
            case Up: return new Vector2Int(0, 1);
            case Right: return new Vector2Int(1, 0);
            case Down: return new Vector2Int(0, -1);
            default: return new Vector2Int(-1, 0);
        }
    }

    public static int Opposite(int dir)
    {
        switch (dir)
        {
            case Up: return Down;
            case Right: return Left;
            case Down: return Up;
            default: return Right;
        }
    }

    public static int OpenMask(PipeTileType type, int rotation)
    {
        switch (type)
        {
            case PipeTileType.Source:
            case PipeTileType.Sink:
                return Up | Right | Down | Left;
            case PipeTileType.Straight:
                return (rotation % 2 == 0) ? (Left | Right) : (Up | Down);
            case PipeTileType.Corner:
                switch (rotation & 3)
                {
                    case 0: return Up | Right;
                    case 1: return Right | Down;
                    case 2: return Down | Left;
                    default: return Left | Up;
                }
            default:
                switch (rotation & 3)
                {
                    case 0: return Up | Right | Left;
                    case 1: return Right | Down | Up;
                    case 2: return Down | Left | Right;
                    default: return Left | Up | Down;
                }
        }
    }

    public static bool Contains(int mask, int dir)
    {
        return (mask & dir) != 0;
    }
}

public class PipeBoard
{
    public int Width { get; }
    public int Height { get; }
    public Vector2Int Source { get; }
    public Vector2Int Sink { get; }
    public int RotatableCount { get; private set; }

    private readonly PipeTile[,] _tiles;
    private readonly bool[,] _rotatable;

    public PipeBoard(int width, int height, Vector2Int source, Vector2Int sink)
    {
        Width = width;
        Height = height;
        Source = source;
        Sink = sink;
        _tiles = new PipeTile[width, height];
        _rotatable = new bool[width, height];
    }

    public PipeTile Tile(int x, int y) => _tiles[x, y];
    public bool IsRotatable(int x, int y) => _rotatable[x, y];

    public void SetTile(int x, int y, PipeTileType type, int rotation)
    {
        _tiles[x, y] = new PipeTile { Type = type, Rotation = rotation };
    }

    public void SetRotatable(int x, int y, bool rotatable)
    {
        _rotatable[x, y] = rotatable;
    }

    public bool TryRotate(int x, int y)
    {
        if (!_rotatable[x, y]) return false;
        var tile = _tiles[x, y];
        tile.Rotation = (tile.Rotation + 1) & 3;
        _tiles[x, y] = tile;
        return true;
    }

    public bool IsConnected()
    {
        return SolvePath() != null;
    }

    public List<Vector2Int> SolvePath()
    {
        if (!InBounds(Source) || !InBounds(Sink)) return null;

        var queue = new Queue<Vector2Int>();
        var visited = new bool[Width, Height];
        var parents = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(Source);
        visited[Source.x, Source.y] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == Sink)
            {
                var path = new List<Vector2Int> { Sink };
                var walk = current;
                while (parents.TryGetValue(walk, out var parent))
                {
                    path.Add(parent);
                    walk = parent;
                }
                path.Reverse();
                return path;
            }

            int mask = PipeDirections.OpenMask(_tiles[current.x, current.y].Type, _tiles[current.x, current.y].Rotation);
            foreach (var dir in PipeDirections.All)
            {
                var neighbor = current + dir;
                if (!InBounds(neighbor) || visited[neighbor.x, neighbor.y]) continue;
                if (!PipeDirections.Contains(mask, MaskOf(dir))) continue;

                int neighborMask = PipeDirections.OpenMask(_tiles[neighbor.x, neighbor.y].Type, _tiles[neighbor.x, neighbor.y].Rotation);
                if (!PipeDirections.Contains(neighborMask, PipeDirections.Opposite(MaskOf(dir)))) continue;

                visited[neighbor.x, neighbor.y] = true;
                parents[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        return null;
    }

    public Vector2Int? HintTile()
    {
        var path = SolvePath();
        if (path != null)
        {
            foreach (var tile in path)
            {
                if (!_rotatable[tile.x, tile.y]) continue;
                if (RequiresOtherRotation(tile)) return tile;
            }

            foreach (var tile in path)
            {
                if (_rotatable[tile.x, tile.y]) return tile;
            }
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_rotatable[x, y]) return new Vector2Int(x, y);
            }
        }

        return null;
    }

    private bool RequiresOtherRotation(Vector2Int tile)
    {
        var path = SolvePath();
        int index = path.IndexOf(tile);
        if (index < 0) return false;

        int required = 0;
        if (index > 0)
            required |= MaskOf(path[index - 1] - tile);
        if (index < path.Count - 1)
            required |= MaskOf(path[index + 1] - tile);

        int open = PipeDirections.OpenMask(_tiles[tile.x, tile.y].Type, _tiles[tile.x, tile.y].Rotation);
        return (open & required) != required;
    }

    private static int MaskOf(Vector2Int delta)
    {
        if (delta == new Vector2Int(0, 1)) return PipeDirections.Up;
        if (delta == new Vector2Int(0, -1)) return PipeDirections.Down;
        if (delta == new Vector2Int(1, 0)) return PipeDirections.Right;
        return PipeDirections.Left;
    }

    private bool InBounds(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < Width && tile.y >= 0 && tile.y < Height;
    }

    public static PipeBoard Generate(int width, int height, int rotatableCount, System.Random rng)
    {
        var source = new Vector2Int(0, 0);
        var sink = new Vector2Int(width - 1, height - 1);

        var board = new PipeBoard(width, height, source, sink);
        var interior = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (new Vector2Int(x, y) == source || new Vector2Int(x, y) == sink) continue;
                board.SetTile(x, y, PipeTileType.T, rng.Next(4));
                interior.Add(new Vector2Int(x, y));
            }
        }

        board.SetTile(source.x, source.y, PipeTileType.Source, 0);
        board.SetTile(sink.x, sink.y, PipeTileType.Sink, 0);

        var rotatables = PickDistinct(interior, Mathf.Min(rotatableCount, interior.Count), rng);
        foreach (var tile in rotatables)
            board.SetRotatable(tile.x, tile.y, true);
        board.RotatableCount = rotatables.Count;

        for (int rebuild = 0; rebuild < 20; rebuild++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board._rotatable[x, y]) continue;
                    if (new Vector2Int(x, y) == source || new Vector2Int(x, y) == sink) continue;
                    board._tiles[x, y].Rotation = rng.Next(4);
                }
            }

            for (int attempt = 0; attempt < 100 && !board.IsConnected(); attempt++)
            {
                foreach (var tile in rotatables)
                    board._tiles[tile.x, tile.y].Rotation = rng.Next(4);
            }
            if (!board.IsConnected()) continue;

            var solved = SnapshotRotations(board);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                foreach (var tile in rotatables)
                    board._tiles[tile.x, tile.y].Rotation = rng.Next(4);

                if (!board.IsConnected() && DiffersFrom(board, solved)) return board;
            }

            if (TryAlternateRotations(board, rotatables, solved)) return board;
        }

        return board;
    }

    private static bool TryAlternateRotations(PipeBoard board, List<Vector2Int> rotatables, Dictionary<Vector2Int, int> solved)
    {
        foreach (var tile in rotatables)
        {
            int original = board._tiles[tile.x, tile.y].Rotation;
            for (int rotation = 1; rotation < 4; rotation++)
            {
                board._tiles[tile.x, tile.y].Rotation = (original + rotation) & 3;
                if (!board.IsConnected() && DiffersFrom(board, solved)) return true;
            }
            board._tiles[tile.x, tile.y].Rotation = original;
        }
        return false;
    }

    private static bool DiffersFrom(PipeBoard board, Dictionary<Vector2Int, int> snapshot)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (board._tiles[x, y].Rotation != snapshot[new Vector2Int(x, y)]) return true;
            }
        }
        return false;
    }

    private static List<Vector2Int> PickDistinct(List<Vector2Int> pool, int count, System.Random rng)
    {
        var copy = new List<Vector2Int>(pool);
        var picked = new List<Vector2Int>();
        while (picked.Count < count && copy.Count > 0)
        {
            int index = rng.Next(copy.Count);
            picked.Add(copy[index]);
            copy.RemoveAt(index);
        }
        return picked;
    }

    public static Dictionary<Vector2Int, int> SnapshotRotations(PipeBoard board)
    {
        var snapshot = new Dictionary<Vector2Int, int>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
                snapshot[new Vector2Int(x, y)] = board._tiles[x, y].Rotation;
        }
        return snapshot;
    }
}