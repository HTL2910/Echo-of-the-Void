using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI
{
    /// <summary>Dialogue display for Iris and story NPCs. Shows speaker name, dialogue text, continues on input.</summary>
    public class DialogueDisplayUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Image dialoguePanel;

        private string[] _dialogueSequence;
        private int _currentLineIndex;
        private System.Action _onDialogueEnd;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(AdvanceLine);

            gameObject.SetActive(false);
        }

        /// <summary>Start a dialogue sequence (list of dialogue keys from localization).</summary>
        public void StartDialogue(string[] dialogueKeys, System.Action onEnd = null)
        {
            _dialogueSequence = dialogueKeys;
            _currentLineIndex = 0;
            _onDialogueEnd = onEnd;

            gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause during dialogue
            DisplayLine(0);
        }

        private void DisplayLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _dialogueSequence.Length)
            {
                EndDialogue();
                return;
            }

            string key = _dialogueSequence[lineIndex];
            string fullText = LocalizationService.Instance?.GetString(key) ?? $"[{key}]";

            // Parse format: "SpeakerName: Dialogue text" or just "Dialogue text"
            var parts = fullText.Split(new[] { ": " }, System.StringSplitOptions.None, 2);

            if (parts.Length == 2 && !parts[0].Contains("\n"))
            {
                // Has speaker
                if (speakerText != null) speakerText.text = parts[0];
                if (dialogueText != null) dialogueText.text = parts[1];
            }
            else
            {
                // No speaker, just dialogue
                if (speakerText != null) speakerText.text = "";
                if (dialogueText != null) dialogueText.text = fullText;
            }

            _currentLineIndex = lineIndex;
        }

        public void AdvanceLine()
        {
            _currentLineIndex++;
            if (_currentLineIndex >= _dialogueSequence.Length)
            {
                EndDialogue();
            }
            else
            {
                DisplayLine(_currentLineIndex);
            }
        }

        private void EndDialogue()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Resume game
            _onDialogueEnd?.Invoke();
        }

        private void Update()
        {
            // Allow advance via Space or E
            if (gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))
                AdvanceLine();

            // Allow close via Esc
            if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                EndDialogue();
        }
    }
}
