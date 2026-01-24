using UnityEngine;
using System.Linq;

namespace Tutorial
{
    /// <summary>
    /// Tutorial script for Level 1.
    /// Manages cursor visibility based on whether a producer is connected to any node.
    /// </summary>
    public class TutorialLevel1 : MonoBehaviour
    {
        [Header("Tutorial References")]
        [Tooltip("The cursor/indicator to show until a connection is made.")]
        [SerializeField] private GameObject cursor1;
        [Tooltip("The first hint to show until a connection is made.")]
        [SerializeField] private GameObject hint1;
        [Tooltip("The second hint to show after a connection is made.")]
        [SerializeField] private GameObject hint2;

        private bool levelStarted = false;

        private void Awake()
        {
            // Disable all tutorial objects by default
            if (cursor1 != null) cursor1.SetActive(false);
            if (hint1 != null) hint1.SetActive(false);
            if (hint2 != null) hint2.SetActive(false);
        }

        private void Update()
        {
            // Wait for level to start via GameController
            if (!levelStarted && GameController.Instance != null && GameController.Instance.GameplayEnabled)
            {
                levelStarted = true;
                HandleConnectionsChanged();
            }
        }

        private void OnEnable()
        {
            // Subscribe to connection change events
            ConnectionManager.OnConnectionsChanged += HandleConnectionsChanged;
            
            // Initial check in case something is already connected
            HandleConnectionsChanged();
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            ConnectionManager.OnConnectionsChanged -= HandleConnectionsChanged;
        }

        private void HandleConnectionsChanged()
        {
            // Only execute logic if the level has actually started
            if (!levelStarted) return;

            // Check if ConnectionManager instance exists
            if (ConnectionManager.Instance == null) return;

            // Get all active connections
            var activeConnections = ConnectionManager.Instance.GetActiveConnections();

            // Check if there's any connection starting from a ProducerNode
            // We use 'is ProducerNode' to identify producers
            bool isProducerConnected = activeConnections.Any(c => c != null && c.FromNode is ProducerNode);

            // Toggle indicators based on connection state
            if (isProducerConnected)
            {
                // After connected: deactivate cursor1 and hint1, activate hint2
                if (cursor1 != null && cursor1.activeSelf) cursor1.SetActive(false);
                if (hint1 != null && hint1.activeSelf) hint1.SetActive(false);
                if (hint2 != null && !hint2.activeSelf) hint2.SetActive(true);
            }
            else
            {
                // Until connected: activate cursor1 and hint1, deactivate hint2
                if (cursor1 != null && !cursor1.activeSelf) cursor1.SetActive(true);
                if (hint1 != null && !hint1.activeSelf) hint1.SetActive(true);
                if (hint2 != null && hint2.activeSelf) hint2.SetActive(false);
            }
        }
    }
}
