using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YazLab.NpcDialogue
{
    // Açılış "görev dosyası": geçmiş, görev ve anılar (oyuncu karısını ikna etmek için anıları bilmeli).
    // Oyun Enter'a basılana kadar durur. Metin intro.txt'ten; dosya yoksa ekran hiç açılmaz.
    public class IntroScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text bodyText;

        private bool _showing;

        private void Start()
        {
            string text = null;
            try
            {
                text = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "NpcDialogue", "intro.txt"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Açılış metni yoksa oyun açılışsız başlar.
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                panel.SetActive(false);
                return;
            }

            bodyText.text = text; // kendi dosyamız: başlıklar için <b> etiketi kullanılır
            panel.SetActive(true);
            Time.timeScale = 0f; // okurken oyuncu ve robot hareket etmez
            _showing = true;
        }

        private void Update()
        {
            // timeScale 0 iken de Update çalışır.
            if (!_showing || Keyboard.current == null)
                return;
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                Close();
        }

        private void Close()
        {
            _showing = false;
            panel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
}
