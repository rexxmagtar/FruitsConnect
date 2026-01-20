using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JigsawSystem
{
    public class PuzzleButtonUi : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image puzzleImage;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private GameObject solvedCheckbox;
        [SerializeField] private Button button;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;

        private JigsawPuzzleData data;
        private System.Action<JigsawPuzzleData> onClick;

        public void Initialize(JigsawPuzzleData puzzleData, System.Action<JigsawPuzzleData> clickCallback)
        {
            data = puzzleData;
            onClick = clickCallback;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (puzzleImage != null) puzzleImage.sprite = data.fullImage;
            if (rewardText != null) rewardText.text = data.completionReward.ToString();

            Refresh();

            if (button != null)
            {
                button.onClick.AddListener(() => 
                {
                    if (audioSource != null && clickSound != null)
                    {
                        audioSource.PlayOneShot(clickSound);
                    }
                    onClick?.Invoke(data);
                });
            }
        }

        public void Refresh()
        {
            int collected = JigsawPuzzleManager.Instance.GetCollectedPieceCount(data.puzzleId);
            bool isSolved = JigsawPuzzleManager.Instance.IsPuzzleSolved(data.puzzleId);

            if (progressText != null) progressText.text = $"{collected}/9";
            if (solvedCheckbox != null) solvedCheckbox.SetActive(isSolved);
            
            rewardText.transform.parent.gameObject.SetActive(!isSolved);

            bool isLocked = (collected == 0);
            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
            
            if (button != null) button.interactable = !isLocked;
        }
    }
}
