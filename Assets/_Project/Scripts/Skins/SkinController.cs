using System;
using UnityEngine;

public class SkinController : MonoBehaviour
{
    [Serializable]
    public struct PaintSlot
    {
        public string skinId;
        public Material material;
    }

    [SerializeField] private PaintSlot[] _paints = new PaintSlot[0];

    public PaintSlot[] Paints
    {
        get => _paints;
        set => _paints = value ?? new PaintSlot[0];
    }

    public string EquippedSkinId { get; private set; }

    public bool Equip(string skinId)
    {
        foreach (var slot in _paints)
        {
            if (slot.skinId != skinId || slot.material == null) continue;

            EquippedSkinId = skinId;
            Apply(slot.material);
            return true;
        }
        return false;
    }

    private void Apply(Material paint)
    {
        var vehicles = FindObjectsByType<Vehicle>();
        foreach (var vehicle in vehicles)
            vehicle.Recolour(paint);
    }
}