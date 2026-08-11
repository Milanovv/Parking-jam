using System;
using UnityEngine;

public enum LevelValidationErrorKind
{
    MissingLevel,
    BadId,
    BadName,
    GridOutOfRange,
    UndosOutOfRange,
    ConstraintConflict,
    NoExitTiles,
    ExitTileOffBoundary,
    NoVehicles,
    DuplicateVehicleId,
    VehicleTilesOutOfBounds,
    ObstacleOutOfBounds,
    VehicleTilesNotContiguous,
    OrientationMismatch,
    VehicleTooLong,
    TileOverlap,
    PedestrianRouteTooShort,
    PedestrianRouteTooLong,
    PedestrianRouteOutOfBounds,
    BarrierCount,
    BarrierOffExit,
    MiniGameSceneInvalid,
    ExitCurvePointCount
}

public struct LevelValidationError
{
    public LevelValidationErrorKind Kind;
    public string Message;
}

public static class LevelValidator
{
    public const int MinGridSize = 5;
    public const int MaxGridSize = 12;
    public const int MinUndos = 1;
    public const int MaxUndos = 5;
    public const int MaxVehicleLength = 3;
    public const int MaxNameLength = 32;
    public const int MinRouteLength = 2;
    public const int MaxRouteLength = 16;

    public static bool TryValidate(LevelData level, out string error)
    {
        var check = Validate(level);
        error = check != null ? check.Value.Message : null;
        return check == null;
    }

    public static LevelValidationError? Validate(LevelData level)
    {
        if (level == null) return Error(LevelValidationErrorKind.MissingLevel, "Level data is null");
        if (level.id <= 0) return Error(LevelValidationErrorKind.BadId, $"Level id must be positive, got {level.id}");
        if (string.IsNullOrWhiteSpace(level.name)) return Error(LevelValidationErrorKind.BadName, $"Level {level.id} has no name");
        if (level.name.Length > MaxNameLength)
            return Error(LevelValidationErrorKind.BadName, $"Level {level.id} name exceeds {MaxNameLength} characters");
        if (level.gridWidth < MinGridSize || level.gridWidth > MaxGridSize)
            return Error(LevelValidationErrorKind.GridOutOfRange, $"Level {level.id} gridWidth {level.gridWidth} outside {MinGridSize}-{MaxGridSize}");
        if (level.gridHeight < MinGridSize || level.gridHeight > MaxGridSize)
            return Error(LevelValidationErrorKind.GridOutOfRange, $"Level {level.id} gridHeight {level.gridHeight} outside {MinGridSize}-{MaxGridSize}");
        if (level.levelUndos < MinUndos || level.levelUndos > MaxUndos)
            return Error(LevelValidationErrorKind.UndosOutOfRange, $"Level {level.id} levelUndos {level.levelUndos} outside {MinUndos}-{MaxUndos}");
        if (level.moveLimit > 0 && level.timeLimit > 0)
            return Error(LevelValidationErrorKind.ConstraintConflict, $"Level {level.id} sets both moveLimit ({level.moveLimit}) and timeLimit ({level.timeLimit})");

        if (level.exitTiles == null || level.exitTiles.Length == 0)
            return Error(LevelValidationErrorKind.NoExitTiles, $"Level {level.id} has no exitTiles");
        foreach (var tile in level.exitTiles)
        {
            if (!IsOnBoundary(tile, level.gridWidth, level.gridHeight))
                return Error(LevelValidationErrorKind.ExitTileOffBoundary,
                    $"Level {level.id} exit tile ({tile.x},{tile.y}) is not on the grid boundary");
        }

        if (level.vehicles == null || level.vehicles.Length == 0)
            return Error(LevelValidationErrorKind.NoVehicles, $"Level {level.id} has no vehicles");

        var occupied = new bool[level.gridWidth, level.gridHeight];
        for (int i = 0; i < level.vehicles.Length; i++)
        {
            var vehicle = level.vehicles[i];
            for (int j = 0; j < i; j++)
            {
                if (level.vehicles[j].id == vehicle.id)
                    return Error(LevelValidationErrorKind.DuplicateVehicleId, $"Level {level.id} vehicle id '{vehicle.id}' is not unique");
            }

            var integrity = ValidateVehicle(level, vehicle);
            if (integrity != null) return integrity;

            foreach (var tile in vehicle.tiles)
            {
                if (occupied[tile.x, tile.y])
                    return Error(LevelValidationErrorKind.TileOverlap,
                        $"Level {level.id} vehicle '{vehicle.id}' overlaps another entity at ({tile.x},{tile.y})");
            }
            foreach (var tile in vehicle.tiles) occupied[tile.x, tile.y] = true;
        }

        if (level.staticObstacles != null)
        {
            foreach (var obstacle in level.staticObstacles)
            {
                if (!InBounds(obstacle.tile, level.gridWidth, level.gridHeight))
                    return Error(LevelValidationErrorKind.ObstacleOutOfBounds,
                        $"Level {level.id} obstacle at ({obstacle.tile.x},{obstacle.tile.y}) is out of bounds");
                if (occupied[obstacle.tile.x, obstacle.tile.y])
                    return Error(LevelValidationErrorKind.TileOverlap,
                        $"Level {level.id} obstacle at ({obstacle.tile.x},{obstacle.tile.y}) overlaps another entity");
                occupied[obstacle.tile.x, obstacle.tile.y] = true;
            }
        }

        if (level.pedestrians != null)
        {
            foreach (var pedestrian in level.pedestrians)
            {
                var route = pedestrian.route;
                if (route == null || route.Length < MinRouteLength)
                    return Error(LevelValidationErrorKind.PedestrianRouteTooShort,
                        $"Level {level.id} pedestrian route must have at least {MinRouteLength} waypoints");
                if (route.Length > MaxRouteLength)
                    return Error(LevelValidationErrorKind.PedestrianRouteTooLong,
                        $"Level {level.id} pedestrian route exceeds {MaxRouteLength} waypoints");
                for (int w = 0; w < route.Length; w++)
                {
                    var tile = route[w];
                    if (!InBounds(tile, level.gridWidth, level.gridHeight))
                        return Error(LevelValidationErrorKind.PedestrianRouteOutOfBounds,
                            $"Level {level.id} pedestrian waypoint ({tile.x},{tile.y}) is out of bounds");
                    if (occupied[tile.x, tile.y])
                        return Error(LevelValidationErrorKind.TileOverlap,
                            $"Level {level.id} pedestrian route hits occupied tile ({tile.x},{tile.y})");
                }
                foreach (var tile in route) occupied[tile.x, tile.y] = true;
            }
        }

        if (level.barriers != null && level.barriers.Length > 0)
        {
            if (level.barriers.Length > 1)
                return Error(LevelValidationErrorKind.BarrierCount, $"Level {level.id} defines more than one barrier");
            var barrier = level.barriers[0];
            bool onExit = false;
            foreach (var exitTile in level.exitTiles)
            {
                if (exitTile == barrier.tile) onExit = true;
            }
            if (!onExit)
                return Error(LevelValidationErrorKind.BarrierOffExit,
                    $"Level {level.id} barrier at ({barrier.tile.x},{barrier.tile.y}) is not on an exit tile");
            if (!InBounds(barrier.tile, level.gridWidth, level.gridHeight))
                return Error(LevelValidationErrorKind.BarrierOffExit,
                    $"Level {level.id} barrier at ({barrier.tile.x},{barrier.tile.y}) is out of bounds");
            if (occupied[barrier.tile.x, barrier.tile.y])
                return Error(LevelValidationErrorKind.TileOverlap,
                    $"Level {level.id} barrier at ({barrier.tile.x},{barrier.tile.y}) overlaps another entity");
            occupied[barrier.tile.x, barrier.tile.y] = true;

            if (!MiniGameCatalog.TryParseSceneName(barrier.miniGameScene, out _, out _))
                return Error(LevelValidationErrorKind.MiniGameSceneInvalid,
                    $"Level {level.id} miniGameScene '{barrier.miniGameScene}' is not a valid mini-game scene name");
        }

        if (level.exitCurve != null && level.exitCurve.Length != 4)
            return Error(LevelValidationErrorKind.ExitCurvePointCount,
                $"Level {level.id} exitCurve has {level.exitCurve.Length} points; exactly 4 are required");

        return null;
    }

    private static LevelValidationError? ValidateVehicle(LevelData level, VehicleData vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.id))
            return Error(LevelValidationErrorKind.BadName, $"Level {level.id} has a vehicle without an id");
        if (vehicle.tiles == null || vehicle.tiles.Length == 0 || vehicle.tiles.Length > MaxVehicleLength)
            return Error(LevelValidationErrorKind.VehicleTooLong,
                $"Level {level.id} vehicle '{vehicle.id}' has {vehicle.tiles?.Length ?? 0} tiles; 1-{MaxVehicleLength} required");
        foreach (var tile in vehicle.tiles)
        {
            if (!InBounds(tile, level.gridWidth, level.gridHeight))
                return Error(LevelValidationErrorKind.VehicleTilesOutOfBounds,
                    $"Level {level.id} vehicle '{vehicle.id}' tile ({tile.x},{tile.y}) is out of bounds");
        }

        bool horizontal = vehicle.orientation == "horizontal";
        bool vertical = vehicle.orientation == "vertical";
        if (!horizontal && !vertical)
            return Error(LevelValidationErrorKind.OrientationMismatch,
                $"Level {level.id} vehicle '{vehicle.id}' orientation '{vehicle.orientation}' is not horizontal/vertical");

        int axisValue = horizontal ? vehicle.tiles[0].y : vehicle.tiles[0].x;
        var axisTiles = new int[vehicle.tiles.Length];
        for (int i = 0; i < vehicle.tiles.Length; i++)
        {
            int coordinate = horizontal ? vehicle.tiles[i].y : vehicle.tiles[i].x;
            if (coordinate != axisValue)
                return Error(LevelValidationErrorKind.OrientationMismatch,
                    $"Level {level.id} vehicle '{vehicle.id}' mixes {(horizontal ? "Y" : "X")} coordinates");
            axisTiles[i] = horizontal ? vehicle.tiles[i].x : vehicle.tiles[i].y;
        }
        Array.Sort(axisTiles);
        for (int i = 1; i < axisTiles.Length; i++)
        {
            if (axisTiles[i] != axisTiles[i - 1] + 1)
                return Error(LevelValidationErrorKind.VehicleTilesNotContiguous,
                    $"Level {level.id} vehicle '{vehicle.id}' tiles have a gap");
        }
        return null;
    }

    private static bool IsOnBoundary(Vector2Int tile, int width, int height)
    {
        return tile.x == 0 || tile.x == width - 1 || tile.y == 0 || tile.y == height - 1;
    }

    private static bool InBounds(Vector2Int tile, int width, int height)
    {
        return tile.x >= 0 && tile.x < width && tile.y >= 0 && tile.y < height;
    }

    private static LevelValidationError? Error(LevelValidationErrorKind kind, string message)
    {
        return new LevelValidationError { Kind = kind, Message = message };
    }
}