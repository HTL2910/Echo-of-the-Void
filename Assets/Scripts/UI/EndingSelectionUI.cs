using UnityEngine;
using UnityEngine.UI;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Save;

namespace EchoOfTheVoid.UI
{
    /// <summary>Post-boss ending selection screen (3 paths after defeating Chronos).</summary>
    public class EndingSelectionUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text titleText;
        [SerializeField] private Button[] endingButtons = new Button[3];
        [SerializeField] private Text[] endingLabels = new Text[3];

        private readonly string[] _endingKeys = { "ending.true", "ending.echo", "ending.void" };
        private readonly string[] _endingIds = { "reset", "convergence", "sovereign" };

        private void Start()
        {
            if (titleText != null)
                titleText.text = LocalizationService.Instance?.GetString("ui.choose_ending") ?? "Choose Your Path";

            for (int i = 0; i < endingButtons.Length && i < 3; i++)
            {
                int index = i;
                if (endingButtons[i] != null)
                {
                    endingButtons[i].onClick.AddListener(() => SelectEnding(index));

                    if (endingLabels[i] != null)
                    {
                        string endingText = LocalizationService.Instance?.GetString(_endingKeys[i]) ?? $"Ending {i + 1}";
                        endingLabels[i].text = endingText;
                    }
                }
            }
        }

        private void SelectEnding(int endingIndex)
        {
            if (endingIndex < 0 || endingIndex >= _endingIds.Length) return;

            // Record chosen ending in save data
            GameSession.Current.endingChosen = _endingIds[endingIndex];
            SaveService.Save();

            // Trigger ending sequence
            PlayEnding(_endingIds[endingIndex]);
        }

        private void PlayEnding(string endingId)
        {
            // Load ending scene or trigger cutscene
            // For now, just log that ending was selected
            Debug.Log($"[EndingSelection] Playing ending: {endingId}");

            // TODO: Load ending scene or trigger cutscene/credits
        }
    }
}
