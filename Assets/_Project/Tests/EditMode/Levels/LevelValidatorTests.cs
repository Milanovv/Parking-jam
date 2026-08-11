using NUnit.Framework;
using UnityEngine;

public class LevelValidatorTests
{
    private static LevelData Valid()
    {
        return new LevelData
        {
            id = 1,
            name = "Test",
            gridWidth = 8,
            gridHeight = 8,
            levelUndos = 4,
            exitTiles = new[] { new Vector2Int(7, 3) },
            vehicles = new[]
            {
                new VehicleData { id = "car_red", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 3), new Vector2Int(1, 3) } }
            }
        };
    }

    [Test]
    public void ValidLevel_Passes()
    {
        Assert.IsTrue(LevelValidator.TryValidate(Valid(), out var error), error);
    }

    [Test]
    public void NullLevel_Fails()
    {
        AssertValidationFails(null, LevelValidationErrorKind.MissingLevel);
    }

    [Test]
    public void NonPositiveId_Fails()
    {
        var level = Valid();
        level.id = 0;
        AssertValidationFails(level, LevelValidationErrorKind.BadId);
    }

    [Test]
    public void MissingName_Fails()
    {
        var level = Valid();
        level.name = "";
        AssertValidationFails(level, LevelValidationErrorKind.BadName);
    }

    [Test]
    public void OverlongName_Fails()
    {
        var level = Valid();
        level.name = new string('x', LevelValidator.MaxNameLength + 1);
        AssertValidationFails(level, LevelValidationErrorKind.BadName);
    }

    [Test]
    public void GridOutsideFiveToTwelve_Fails()
    {
        var level = Valid();
        level.gridWidth = 4;
        AssertValidationFails(level, LevelValidationErrorKind.GridOutOfRange);

        level = Valid();
        level.gridHeight = 13;
        AssertValidationFails(level, LevelValidationErrorKind.GridOutOfRange);
    }

    [Test]
    public void UndosOutsideOneToFive_Fails()
    {
        var level = Valid();
        level.levelUndos = 0;
        AssertValidationFails(level, LevelValidationErrorKind.UndosOutOfRange);

        level = Valid();
        level.levelUndos = 6;
        AssertValidationFails(level, LevelValidationErrorKind.UndosOutOfRange);
    }

    [Test]
    public void MoveAndTimeLimitTogether_Fail()
    {
        var level = Valid();
        level.moveLimit = 10;
        level.timeLimit = 30;
        AssertValidationFails(level, LevelValidationErrorKind.ConstraintConflict);
    }

    [Test]
    public void MissingExitTiles_Fail()
    {
        var level = Valid();
        level.exitTiles = null;
        AssertValidationFails(level, LevelValidationErrorKind.NoExitTiles);
    }

    [Test]
    public void ExitTileOffBoundary_Fails()
    {
        var level = Valid();
        level.exitTiles = new[] { new Vector2Int(4, 4) };
        AssertValidationFails(level, LevelValidationErrorKind.ExitTileOffBoundary);
    }

    [Test]
    public void MissingVehicles_Fail()
    {
        var level = Valid();
        level.vehicles = null;
        AssertValidationFails(level, LevelValidationErrorKind.NoVehicles);
    }

    [Test]
    public void DuplicateVehicleIds_Fail()
    {
        var level = Valid();
        level.vehicles = new[]
        {
            new VehicleData { id = "car", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 3) } },
            new VehicleData { id = "car", orientation = "vertical", tiles = new[] { new Vector2Int(5, 0) } }
        };
        AssertValidationFails(level, LevelValidationErrorKind.DuplicateVehicleId);
    }

    [Test]
    public void VehicleWithGap_Fails()
    {
        var level = Valid();
        level.vehicles[0].tiles = new[] { new Vector2Int(0, 3), new Vector2Int(2, 3) };
        AssertValidationFails(level, LevelValidationErrorKind.VehicleTilesNotContiguous);
    }

    [Test]
    public void VehicleOrientationMismatch_Fails()
    {
        var level = Valid();
        level.vehicles[0].tiles = new[] { new Vector2Int(0, 3), new Vector2Int(0, 4) };
        AssertValidationFails(level, LevelValidationErrorKind.OrientationMismatch);

        level = Valid();
        level.vehicles[0].orientation = "diagonal";
        AssertValidationFails(level, LevelValidationErrorKind.OrientationMismatch);
    }

    [Test]
    public void VehicleLongerThanThree_Fails()
    {
        var level = Valid();
        level.vehicles[0].tiles = new[]
        {
            new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3)
        };
        AssertValidationFails(level, LevelValidationErrorKind.VehicleTooLong);
    }

    [Test]
    public void VehicleTileOutOfBounds_Fails()
    {
        var level = Valid();
        level.vehicles[0].tiles = new[] { new Vector2Int(0, 3), new Vector2Int(0, 9) };
        AssertValidationFails(level, LevelValidationErrorKind.VehicleTilesOutOfBounds);
    }

    [Test]
    public void OverlappingVehicles_Fail()
    {
        var level = Valid();
        level.vehicles = new[]
        {
            new VehicleData { id = "a", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 3), new Vector2Int(1, 3) } },
            new VehicleData { id = "b", orientation = "vertical", tiles = new[] { new Vector2Int(1, 2), new Vector2Int(1, 3) } }
        };
        AssertValidationFails(level, LevelValidationErrorKind.TileOverlap);
    }

    [Test]
    public void OverlappingObstacle_Fails()
    {
        var level = Valid();
        level.staticObstacles = new[] { new StaticObstacleData { tile = new Vector2Int(1, 3) } };
        AssertValidationFails(level, LevelValidationErrorKind.TileOverlap);
    }

    [Test]
    public void PedestrianRouteTooShort_Fails()
    {
        var level = Valid();
        level.pedestrians = new[] { new PedestrianData { route = new[] { new Vector2Int(5, 5) } } };
        AssertValidationFails(level, LevelValidationErrorKind.PedestrianRouteTooShort);
    }

    [Test]
    public void PedestrianRouteOutOfBounds_Fails()
    {
        var level = Valid();
        level.pedestrians = new[] { new PedestrianData { route = new[] { new Vector2Int(5, 5), new Vector2Int(5, 8) } } };
        AssertValidationFails(level, LevelValidationErrorKind.PedestrianRouteOutOfBounds);
    }

    [Test]
    public void PedestrianRouteOverlappingVehicle_Fails()
    {
        var level = Valid();
        level.pedestrians = new[] { new PedestrianData { route = new[] { new Vector2Int(1, 3), new Vector2Int(2, 3) } } };
        AssertValidationFails(level, LevelValidationErrorKind.TileOverlap);
    }

    [Test]
    public void MultipleBarriers_Fail()
    {
        var level = Valid();
        level.barriers = new[]
        {
            new BarrierData { miniGameScene = "MiniGame_Pipes_Easy", tile = new Vector2Int(7, 3) },
            new BarrierData { miniGameScene = "MiniGame_Pipes_Medium", tile = new Vector2Int(7, 4) }
        };
        AssertValidationFails(level, LevelValidationErrorKind.BarrierCount);
    }

    [Test]
    public void BarrierOffExitTile_Fails()
    {
        var level = Valid();
        level.barriers = new[] { new BarrierData { miniGameScene = "MiniGame_Pipes_Easy", tile = new Vector2Int(7, 7) } };
        AssertValidationFails(level, LevelValidationErrorKind.BarrierOffExit);
    }

    [Test]
    public void BarrierOverlappingVehicle_Fails()
    {
        var level = Valid();
        level.barriers = new[] { new BarrierData { miniGameScene = "MiniGame_Pipes_Easy", tile = new Vector2Int(7, 3) } };
        level.vehicles[0].tiles = new[] { new Vector2Int(6, 3), new Vector2Int(7, 3) };
        AssertValidationFails(level, LevelValidationErrorKind.TileOverlap);
    }

    [Test]
    public void InvalidMiniGameScene_Fails()
    {
        var level = Valid();
        level.barriers = new[] { new BarrierData { miniGameScene = "MiniGame_Unknown_Easy", tile = new Vector2Int(7, 3) } };
        AssertValidationFails(level, LevelValidationErrorKind.MiniGameSceneInvalid);
    }

    [Test]
    public void ExitCurveWrongPointCount_Fails()
    {
        var level = Valid();
        level.exitCurve = new[] { new Vector2Int(7, 3), new Vector2Int(8, 3), new Vector2Int(9, 4) };
        AssertValidationFails(level, LevelValidationErrorKind.ExitCurvePointCount);
    }

    [Test]
    public void ExitCurveWithFourPoints_Passes()
    {
        var level = Valid();
        level.exitCurve = new[]
        {
            new Vector2Int(7, 3), new Vector2Int(8, 3), new Vector2Int(9, 4), new Vector2Int(11, 3)
        };
        Assert.IsTrue(LevelValidator.TryValidate(level, out var error), error);
    }

    [Test]
    public void PedestriansFromTutorialToTopEdge_Pass()
    {
        var level = Valid();
        level.pedestrians = new[] { new PedestrianData { route = new[] { new Vector2Int(2, 2), new Vector2Int(2, 6) } } };
        Assert.IsTrue(LevelValidator.TryValidate(level, out var error), error);
    }

    private static void AssertValidationFails(LevelData level, LevelValidationErrorKind expectedKind)
    {
        Assert.IsFalse(LevelValidator.TryValidate(level, out var error), "Expected validation failure");
        var result = LevelValidator.Validate(level);
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedKind, result.Value.Kind);
        Assert.IsNotEmpty(result.Value.Message, "Every failure carries a clear message");
    }
}