using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YazLab.NpcDialogue
{
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
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                panel.SetActive(false);
                return;
            }

            bodyText.text = text;
            panel.SetActive(true);
            Time.timeScale = 0f; // okurken oyun durur
            _showing = true;
        }

        private void Update()
        {
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
