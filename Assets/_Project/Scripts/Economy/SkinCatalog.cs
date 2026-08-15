public static class SkinCatalog
{
    public struct Entry
    {
        public string Id;
        public string DisplayName;
        public bool Exclusive;
    }

    public static readonly Entry[] All =
    {
        new Entry { Id = "Red", DisplayName = "Red", Exclusive = false },
        new Entry { Id = "Green", DisplayName = "Green", Exclusive = false },
        new Entry { Id = "Blue", DisplayName = "Blue", Exclusive = false },
        new Entry { Id = "Purple", DisplayName = "Purple", Exclusive = true },
        new Entry { Id = "Silver", DisplayName = "Silver", Exclusive = true },
        new Entry { Id = "Yellow", DisplayName = "Yellow", Exclusive = true }
    };

    public static Entry Find(string id)
    {
        foreach (var entry in All)
        {
            if (entry.Id == id) return entry;
        }
        return default;
    }

    public static bool Contains(string id)
    {
        foreach (var entry in All)
        {
            if (entry.Id == id) return true;
        }
        return false;
    }
}
