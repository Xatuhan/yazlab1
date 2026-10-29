using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YazLab.NpcDialogue
{
    // Bitiş ekranı: ne olduğunu (başlık), bekçinin son sözünü ve kaç mesajda bittiğini yazar.
    // Oyun durur, iki düğme çıkar.
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        // Oyuncu kapının açıldığını ve bekçinin son sözünü görsün diye ekran biraz gecikmeyle açılır.
        [SerializeField] private float showDelaySeconds = 2f;

        // Metinler Inspector'dan değişir (hikâye değişirse kod değişmez). {n} = mesaj sayısı.
        [SerializeField] private string unlockedTitle = "Seni tanıdı. Kapı açıldı.";
        [SerializeField] private string unlockedCount = "{n} mesajda onu ikna ettin.";
        [SerializeField] private string defeatedTitle = "Karın saldırıya geçti. Savaşı kaybettin.";
        [SerializeField] private string defeatedCount = "{n} mesajda karını kızdırdın.";

        private void Awake()
        {
            detailText.richText = false; // bekçinin sözü düz metin
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
            // Replace: Inspector'da yazılan metin bozuk olsa da string.Format gibi hata fırlatmaz.
            detailText.text = "Bekçi: \"" + reply + "\"\n" + countFormat.Replace("{n}", turns.ToString());
            panel.SetActive(true);
            Time.timeScale = 0f; // oyun durur (arayüz unscaled zamanla çalışmaya devam eder)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Restart()
        {
            // Önce zaman geri açılır; açılmazsa yeni sahne donuk başlar (timeScale sahneler arası korunur).
            Time.timeScale = 1f;
            int index = SceneManager.GetActiveScene().buildIndex;
            if (index < 0)
            {
                // Sahne Build Profiles listesinde değil (ör. B'nin test sahnesi); çökmek yerine uyar.
                Debug.LogWarning("Yeniden Başla: sahne build listesinde değil, yeniden yüklenemiyor.");
                return;
            }
            SceneManager.LoadScene(index);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // editörde Application.Quit işe yaramaz
#else
            Application.Quit();
#endif
        }
    }
}
