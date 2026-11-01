using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace YazLab.NpcDialogue.Tests
{
    public class ItemCatalogTests
    {
        private static string Item(string id, string label, string eventText) =>
            "{\"id\":\"" + id + "\",\"label\":\"" + label + "\",\"eventText\":\"" + eventText + "\"}";

        private static string File(params string[] items) => "{\"items\":[" + string.Join(",", items) + "]}";

        // Oyunla giden gerçek eşya dosyası geçerli: 3 eşya, olay metinleri "[Olay]" ile başlıyor.
        [Test]
        public void ShippedFile_IsValid()
        {
            string json = System.IO.File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "NpcDialogue", "items.json"));

            Assert.IsTrue(ItemCatalog.TryParse(json, out ItemCatalog catalog, out string error), error);
            Assert.AreEqual(3, catalog.Items.Count);
            Assert.IsTrue(catalog.TryGet("ring", out ItemData ring));
            StringAssert.StartsWith("[Olay]", ring.eventText);
        }

        [Test]
        public void TryGet_UnknownId_False()
        {
            ItemCatalog.TryParse(File(Item("ring", "Yüzük", "[Olay] a")), out ItemCatalog catalog, out _);

            Assert.IsFalse(catalog.TryGet("yok", out _));
            Assert.IsFalse(catalog.TryGet(null, out _));
        }

        // Olay metni oyun tarafından yazılmış gibi görünmeli; öneksiz metin kabul edilmez.
        [TestCase("[Olay] a", "", "boş ad")]
        [TestCase("Olay: a", "Yüzük", "öneksiz")]
        [TestCase("", "Yüzük", "boş olay")]
        public void InvalidItem_Fails(string eventText, string label, string why)
        {
            Assert.IsFalse(ItemCatalog.TryParse(File(Item("ring", label, eventText)), out _, out string error), why);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void DuplicateId_Fails()
        {
            Assert.IsFalse(ItemCatalog.TryParse(File(Item("a", "A", "[Olay] a"), Item("a", "B", "[Olay] b")), out _, out _));
        }

        [Test]
        public void TooManyOrNone_Fails()
        {
            Assert.IsFalse(ItemCatalog.TryParse(File(), out _, out _));
            var six = new string[6];
            for (int i = 0; i < 6; i++)
                six[i] = Item("i" + i, "E", "[Olay] x");
            Assert.IsFalse(ItemCatalog.TryParse(File(six), out _, out _));
        }

        [TestCase("{\"items\":[")]
        [TestCase("")]
        public void BrokenJson_Fails(string json)
        {
            Assert.IsFalse(ItemCatalog.TryParse(json, out _, out string error));
            Assert.IsNotEmpty(error);
        }
    }
}
