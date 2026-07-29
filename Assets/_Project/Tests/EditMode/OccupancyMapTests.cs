using NUnit.Framework;
using UnityEngine;

public class OccupancyMapTests
{
    private OccupancyMap _map;

    [SetUp]
    public void Setup()
    {
        _map = new OccupancyMap();
    }

    [Test]
    public void IsTileFree_EmptyMap_ReturnsTrue()
    {
        bool free = _map.IsTileFree(new Vector3Int(0, 0, 0));
        Assert.IsTrue(free);
    }

    [Test]
    public void Place_SingleTile_OccupiesTile()
    {
        var occupant = new TestOccupant(new Vector3Int(2, 3, 0));
        _map.Place(occupant);

        Assert.IsFalse(_map.IsTileFree(new Vector3Int(2, 3, 0)));
    }

    [Test]
    public void Place_MultiTileVehicle_OccupiesAllTiles()
    {
        var tiles = new[]
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(2, 0, 0)
        };
        var occupant = new TestOccupant(tiles);
        _map.Place(occupant);

        Assert.IsFalse(_map.IsTileFree(new Vector3Int(0, 0, 0)));
        Assert.IsFalse(_map.IsTileFree(new Vector3Int(1, 0, 0)));
        Assert.IsFalse(_map.IsTileFree(new Vector3Int(2, 0, 0)));
    }

    [Test]
    public void Remove_Vehicle_FreesTiles()
    {
        var occupant = new TestOccupant(new Vector3Int(3, 3, 0));
        _map.Place(occupant);
        _map.Remove(occupant);

        Assert.IsTrue(_map.IsTileFree(new Vector3Int(3, 3, 0)));
    }

    [Test]
    public void Remove_MultiTileVehicle_FreesAllTiles()
    {
        var tiles = new[]
        {
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 2, 0)
        };
        var occupant = new TestOccupant(tiles);
        _map.Place(occupant);
        _map.Remove(occupant);

        Assert.IsTrue(_map.IsTileFree(new Vector3Int(1, 1, 0)));
        Assert.IsTrue(_map.IsTileFree(new Vector3Int(1, 2, 0)));
    }

    [Test]
    public void TwoVehicles_DoNotOverlap_EachOnOwnTile()
    {
        var v1 = new TestOccupant(new Vector3Int(0, 0, 0));
        var v2 = new TestOccupant(new Vector3Int(1, 0, 0));

        _map.Place(v1);
        _map.Place(v2);

        Assert.IsFalse(_map.IsTileFree(new Vector3Int(0, 0, 0)));
        Assert.IsFalse(_map.IsTileFree(new Vector3Int(1, 0, 0)));
    }

    [Test]
    public void Clear_RemovesAllOccupants()
    {
        _map.Place(new TestOccupant(new Vector3Int(0, 0, 0)));
        _map.Place(new TestOccupant(new Vector3Int(1, 1, 0)));
        _map.Clear();

        Assert.IsTrue(_map.IsTileFree(new Vector3Int(0, 0, 0)));
        Assert.IsTrue(_map.IsTileFree(new Vector3Int(1, 1, 0)));
    }

    [Test]
    public void Place_TwoVehiclesSameTile_Overwrites()
    {
        var v1 = new TestOccupant(new Vector3Int(0, 0, 0));
        var v2 = new TestOccupant(new Vector3Int(0, 0, 0));

        _map.Place(v1);
        _map.Place(v2);

        _map.TryGetOccupant(new Vector3Int(0, 0, 0), out var occupant);
        Assert.AreSame(v2, occupant);
    }

    [Test]
    public void TryGetOccupant_EmptyTile_ReturnsFalse()
    {
        bool found = _map.TryGetOccupant(new Vector3Int(99, 99, 0), out _);
        Assert.IsFalse(found);
    }

    [Test]
    public void TryGetOccupant_OccupiedTile_ReturnsTrue()
    {
        var occupant = new TestOccupant(new Vector3Int(5, 5, 0));
        _map.Place(occupant);

        bool found = _map.TryGetOccupant(new Vector3Int(5, 5, 0), out var result);
        Assert.IsTrue(found);
        Assert.AreSame(occupant, result);
    }

    private class TestOccupant : IOccupant
    {
        public Vector3Int[] OccupiedTiles { get; }

        public TestOccupant(Vector3Int singleTile)
        {
            OccupiedTiles = new[] { singleTile };
        }

        public TestOccupant(Vector3Int[] tiles)
        {
            OccupiedTiles = tiles;
        }
    }
}
