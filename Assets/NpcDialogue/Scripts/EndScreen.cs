using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YazLab.NpcDialogue
{
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float showDelaySeconds = 2f;

        [SerializeField] private string unlockedTitle = "Seni tanıdı. Kapı açıldı.";
        [SerializeField] private string unlockedCount = "{n} mesajda onu ikna ettin.";
        [SerializeField] private string defeatedTitle = "Seni tanımadı. Saldırıya geçti.";
        [SerializeField] private string defeatedCount = "{n} mesajda başarısız oldun.";

        private void Awake()
        {
            detailText.richText = false;
            panel.SetActive(false);
            restartButton.onClick.AddListener(Restart);
            quitButton.onClick.AddListener(Quit);
        }

        private void OnEnable()
        {
            NpcEvents.DoorUnlocked += ShowUnlocked;
            NpcEvents.PlayerDefeated += ShowDefeated;
        }

        private void OnDisable()
        {
            NpcEvents.DoorUnlocked -= ShowUnlocked;
            NpcEvents.PlayerDefeated -= ShowDefeated;
        }

        private void ShowUnlocked(string reply, int turns) => StartCoroutine(Show(unlockedTitle, reply, unlockedCount, turns));
        private void ShowDefeated(string reply, int turns) => StartCoroutine(Show(defeatedTitle, reply, defeatedCount, turns));

        private IEnumerator Show(string title, string reply, string countFormat, int turns)
        {
            yield return new WaitForSecondsRealtime(showDelaySeconds);
            titleText.text = title;
            detailText.text = "Güvenlik Şefi: \"" + reply + "\"\n" + countFormat.Replace("{n}", turns.ToString());
            panel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Restart()
        {
            Time.timeScale = 1f; // sahneler arası korunuyor
            int index = SceneManager.GetActiveScene().buildIndex;
            if (index < 0)
            {
                Debug.LogWarning("Yeniden Başla: sahne build listesinde değil, yeniden yüklenemiyor.");
                return;
            }
            SceneManager.LoadScene(index);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
