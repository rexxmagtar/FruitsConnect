using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace JigsawSystem
{
    public class JigsawPuzzleSelectScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform gridParent;
        [SerializeField] private PuzzleButtonUi puzzleButtonPrefab;
        [SerializeField] private Button backButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Sub-Screens")]
        [SerializeField] private PuzzleSolveUI solveUI;

        private List<PuzzleButtonUi> activeButtons = new List<PuzzleButtonUi>();

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => 
                {
                    if (audioSource != null && buttonClickSound != null)
                    {
                        audioSource.PlayOneShot(buttonClickSound);
                    }
                    gameObject.SetActive(false);
                });
            }
        }

        private void OnEnable()
        {
            RefreshList();
        }

        public void RefreshList()
        {
            // Clear existing
            foreach (var btn in activeButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            activeButtons.Clear();

            var puzzles = JigsawPuzzleManager.Instance.Config.Puzzles;
            foreach (var puzzle in puzzles)
            {
                PuzzleButtonUi btn = Instantiate(puzzleButtonPrefab, gridParent);
                btn.Initialize(puzzle, OnPuzzleSelected);
                activeButtons.Add(btn);
            }
        }

        private void OnPuzzleSelected(JigsawPuzzleData data)
        {
            if (solveUI != null)
            {
                solveUI.Open(data);
                // We don't necessarily hide the select screen, 
                // but usually we do or solveUI is a popup on top.
            }
        }
    }
}
