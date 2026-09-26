using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI
{
    /// <summary>In-world monolith UI: opens as overlay when Kael approaches/interacts with a Monolith.</summary>
    public class MonolithDisplayUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text monolithTitleText;
        [SerializeField] private Text monolithBodyText;
        [SerializeField] private Button closeButton;

        private int _currentMonolithId;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        /// <summary>Display a monolith entry by ID (1-12).</summary>
        public void DisplayMonolith(int monolithId)
        {
            _currentMonolithId = monolithId;

            string key = $"monolith.entry_{monolithId:D2}";
            string title = LocalizationService.Instance?.GetString($"{key}.title") ?? $"Monolith #{monolithId}";
            string body = LocalizationService.Instance?.GetString(key) ?? "[Monolith text not found]";

            if (monolithTitleText != null)
                monolithTitleText.text = title;

            if (monolithBodyText != null)
                monolithBodyText.text = body;

            gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause game while reading
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }

        private void Update()
        {
            // Allow close via Esc or E key
            if (gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
                Close();
        }
    }
}
