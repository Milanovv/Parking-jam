using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SkinControllerTests : PlayModeTestBase
{
    private const string RedId = "Red";
    private const string BlueId = "Blue";

    private Material _redPaint;
    private Material _bluePaint;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.IsNotNull(shader, "The paint test needs the URP lit shader");
        _redPaint = new Material(shader);
        _bluePaint = new Material(shader);
        yield return null;
    }

    [TearDown]
    public void DestroyPaints()
    {
        if (_redPaint != null) Object.Destroy(_redPaint);
        if (_bluePaint != null) Object.Destroy(_bluePaint);
    }

    private SkinController SpawnController()
    {
        var controllerGo = new GameObject("SkinController");
        var controller = controllerGo.AddComponent<SkinController>();
        controller.Paints = new[]
        {
            new SkinController.PaintSlot { skinId = RedId, material = _redPaint },
            new SkinController.PaintSlot { skinId = BlueId, material = _bluePaint }
        };
        return controller;
    }

    private Vehicle SpawnVehicle(string id, int length)
    {
        var vehicleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleGo.name = id;
        vehicleGo.transform.localScale = new Vector3(length, 1f, 1f);
        var vehicle = vehicleGo.AddComponent<Vehicle>();
        vehicle.Initialize(id, Orientation.Horizontal, Vector3Int.zero, length);
        return vehicle;
    }

    [UnityTest]
    public IEnumerator EquippingASkin_RecoloursEveryVehicleInTheLevel_RegardlessOfModel()
    {
        var controller = SpawnController();
        var car = SpawnVehicle("car_1_tile", 1);
        var truck = SpawnVehicle("truck_2_tiles", 2);
        var bus = SpawnVehicle("bus_3_tiles", 3);

        yield return null;

        bool equipped = controller.Equip(BlueId);

        Assert.IsTrue(equipped, "A paint on the shelf equips");
        Assert.AreEqual(BlueId, controller.EquippedSkinId, "The controller records the equipped skin");
        foreach (var vehicle in new[] { car, truck, bus })
        {
            Assert.AreSame(_bluePaint, vehicle.GetComponent<MeshRenderer>().sharedMaterial,
                vehicle.name + " is re-coloured level-wide regardless of model");
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator EquippingASkin_PaintsEveryRenderableChildUnderTheVehicle()
    {
        var controller = SpawnController();
        var vehicle = SpawnVehicle("car", 1);
        var modelChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
        modelChild.name = "Model";
        modelChild.transform.SetParent(vehicle.transform, false);

        yield return null;

        controller.Equip(BlueId);

        Assert.AreSame(_bluePaint, vehicle.GetComponent<MeshRenderer>().sharedMaterial, "The vehicle root is re-coloured");
        Assert.AreSame(_bluePaint, modelChild.GetComponent<MeshRenderer>().sharedMaterial, "Nested model children are re-coloured too");
        yield break;
    }

    [UnityTest]
    public IEnumerator UnknownSkinId_LeavesEveryVehicleUnpainted()
    {
        var controller = SpawnController();
        var vehicle = SpawnVehicle("car", 1);
        var original = vehicle.GetComponent<MeshRenderer>().sharedMaterial;

        yield return null;

        bool equipped = controller.Equip("Nonexistent");

        Assert.IsFalse(equipped, "No shelf slot carries that skin id");
        Assert.IsNull(controller.EquippedSkinId, "The equipped skin stays unset when nothing equips");
        Assert.AreSame(original, vehicle.GetComponent<MeshRenderer>().sharedMaterial, "Vehicles keep their authored paint");
        yield break;
    }

    [UnityTest]
    public IEnumerator EmptyShelf_EquipsNothing()
    {
        var controllerGo = new GameObject("SkinController");
        var controller = controllerGo.AddComponent<SkinController>();

        yield return null;

        Assert.IsFalse(controller.Equip(BlueId), "An unwired shelf has nothing to equip");
        yield break;
    }
}