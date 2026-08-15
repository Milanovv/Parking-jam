using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    public struct CardState
    {
        public string Id;
        public bool Owned;
        public bool Equipped;
        public bool Exclusive;
    }

    private Transform _cardsParent;
    private readonly List<CardState> _cards = new List<CardState>();

    public IReadOnlyList<CardState> Cards => _cards;
    public int CardCount => _cards.Count;

    public void Build(SaveData save, Transform cardsParent, Action<string> buyOrEquip)
    {
        ClearCards();
        _cards.Clear();
        _cardsParent = cardsParent;
        if (save == null || cardsParent == null) return;

        foreach (var entry in SkinCatalog.All)
        {
            bool owned = save.IsSkinUnlocked(entry.Id) || save.equippedVehicleSkinId == entry.Id;
            bool equipped = save.equippedVehicleSkinId == entry.Id;
            _cards.Add(new CardState
            {
                Id = entry.Id,
                Owned = owned,
                Equipped = equipped,
                Exclusive = entry.Exclusive
            });

            string captured = entry.Id;
            GameUiFactory.CreateShopCard(cardsParent, entry, owned, equipped, () => buyOrEquip?.Invoke(captured));
        }
    }

    public void Refresh(SaveData save, Action<string> buyOrEquip)
    {
        Build(save, _cardsParent, buyOrEquip);
    }

    private void ClearCards()
    {
        if (_cardsParent == null) return;
        for (int i = _cardsParent.childCount - 1; i >= 0; i--)
        {
            var child = _cardsParent.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }
}
