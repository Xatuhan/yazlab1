using System;
using System.Collections.Generic;
using UnityEngine;

namespace YazLab.NpcDialogue
{
    [Serializable]
    public class ItemData
    {
        public string id;
        public string label;
        public string eventText;
    }

    public class ItemCatalog
    {
        public const string EventPrefix = "[Olay]"; // oyuncu "[" yazamaz
        private const int MaxItems = 5;
        private const int MaxEventChars = 400;

        [Serializable]
        private class CatalogFile
        {
            public ItemData[] items;
        }

        private readonly Dictionary<string, ItemData> _byId;

        private ItemCatalog(ItemData[] items)
        {
            Items = items;
            _byId = new Dictionary<string, ItemData>();
            foreach (ItemData item in items)
                _byId[item.id] = item;
        }

        public IReadOnlyList<ItemData> Items { get; }

        public bool TryGet(string id, out ItemData item) => _byId.TryGetValue(id ?? string.Empty, out item);

        public static bool TryParse(string json, out ItemCatalog catalog, out string error)
        {
            catalog = null;
            CatalogFile file;
            try
            {
                file = JsonUtility.FromJson<CatalogFile>(json);
            }
            catch (Exception)
            {
                error = "Eşya dosyası okunamadı (bozuk JSON).";
                return false;
            }

            if (file?.items == null || file.items.Length == 0 || file.items.Length > MaxItems)
            {
                error = "Eşya dosyasında 1 ile " + MaxItems + " arasında eşya olmalı.";
                return false;
            }

            var seen = new HashSet<string>();
            foreach (ItemData item in file.items)
            {
                bool valid = item != null
                    && !string.IsNullOrWhiteSpace(item.id) && seen.Add(item.id)
                    && !string.IsNullOrWhiteSpace(item.label)
                    && item.eventText != null && item.eventText.StartsWith(EventPrefix, StringComparison.Ordinal)
                    && item.eventText.Length <= MaxEventChars;
                if (!valid)
                {
                    error = "Eşya dosyasında geçersiz bir eşya var (kimlik, ad ya da olay metni).";
                    return false;
                }
            }

            catalog = new ItemCatalog(file.items);
            error = null;
            return true;
        }
    }
}
