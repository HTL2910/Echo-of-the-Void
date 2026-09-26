using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace EchoOfTheVoid.UI
{
    /// <summary>Auto-generated credits from Assets/CREDITS.md. Displays in scrollable overlay after game completion.</summary>
    public class CreditsUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Text creditsText;
        [SerializeField] private Button skipButton;

        private const string CREDITS_PATH = "Assets/CREDITS.md";
        private float _displayDuration = 0f;

        private void Start()
        {
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipCredits);

            LoadCreditsFromFile();
            gameObject.SetActive(false);
        }

        /// <summary>Display credits screen and auto-scroll.</summary>
        public void ShowCredits(float duration = 30f)
        {
            _displayDuration = duration;
            gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause during credits

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f; // Start at top
        }

        private void LoadCreditsFromFile()
        {
            // Load CREDITS.md from Resources or project root
            var asset = Resources.Load<TextAsset>("CREDITS");
            if (asset == null)
            {
                // Fallback: try loading from editor path
                #if UNITY_EDITOR
                var path = "Assets/CREDITS.md";
                if (System.IO.File.Exists(path))
                {
                    var content = System.IO.File.ReadAllText(path);
                    ParseAndDisplayCredits(content);
                    return;
                }
                #endif

                creditsText.text = "Credits file not found.";
                return;
            }

            ParseAndDisplayCredits(asset.text);
        }

        private void ParseAndDisplayCredits(string content)
        {
            if (creditsText == null) return;

            // Simple markdown parsing: remove markdown syntax, preserve structure
            string formatted = content;
            formatted = Regex.Replace(formatted, @"^# ", "\n\n<size=40>", RegexOptions.Multiline);
            formatted = Regex.Replace(formatted, @"^## ", "\n<size=32>", RegexOptions.Multiline);
            formatted = Regex.Replace(formatted, @"^### ", "\n<size=28>", RegexOptions.Multiline);
            formatted = Regex.Replace(formatted, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            formatted = Regex.Replace(formatted, @"\[([^\]]+)\]\(([^)]+)\)", "$1");  // Remove links

            creditsText.text = formatted;
        }

        private void Update()
        {
            if (_displayDuration > 0f)
            {
                _displayDuration -= Time.unscaledDeltaTime;

                // Auto-scroll down
                if (scrollRect != null && _displayDuration > 0f)
                {
                    float progress = 1f - (_displayDuration / 30f);
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(1f - progress);
                }
            }

            // Allow skip via Esc
            if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                SkipCredits();
        }

        public void SkipCredits()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Resume game
            _displayDuration = 0f;
        }
    }
}
