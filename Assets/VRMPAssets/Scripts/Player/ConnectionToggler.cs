using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// A very simple script that will enable or disable objects based on the Network Connection State.
    /// </summary>
    public class ConnectionToggler : MonoBehaviour
    {
        /// <summary>
        /// Enables all objects on connect.
        /// Disables all objects on disconnect.
        /// </summary>
        [SerializeField] GameObject[] objectsToEnableOnline;

        /// <summary>
        /// Enables all objects on disconnect.
        /// Disables all objects on connect.
        /// </summary>
        [SerializeField] GameObject[] objectsToEnableOffline;

        // Store the event handler so we can properly unsubscribe
        private System.Action<string> m_ConnectionFailedHandler;

        /// <inheritdoc/>
        void OnEnable()
        {
            if (XRINetworkGameManager.Connected != null)
            {
                XRINetworkGameManager.Connected.Subscribe(ToggleNetworkObjects);
                ToggleNetworkObjects(XRINetworkGameManager.Connected.Value);
            }
        }

        void Start()
        {
            if (XRINetworkGameManager.Instance != null)
            {
                m_ConnectionFailedHandler = (reason) =>
                {
                    ToggleNetworkObjects(false);
                };
                XRINetworkGameManager.Instance.OnConnectionFailedAction += m_ConnectionFailedHandler;
            }
        }

        void OnDestroy()
        {
            if (XRINetworkGameManager.Instance != null && m_ConnectionFailedHandler != null)
            {
                XRINetworkGameManager.Instance.OnConnectionFailedAction -= m_ConnectionFailedHandler;
            }
        }

        /// <inheritdoc/>
        void OnDisable()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ToggleNetworkObjects);
        }

        /// <summary>
        /// Toggles objects on or off based on whether or not connected.
        /// <see cref="m_Connected"/>
        /// </summary>
        /// <param name="online">
        /// Whether or not players are connected to a networked game.
        /// </param>
        protected virtual void ToggleNetworkObjects(bool online)
        {
            foreach (GameObject g in objectsToEnableOnline)
            {
                if (g == null) continue;
                g.SetActive(online);
            }

            foreach (GameObject g in objectsToEnableOffline)
            {
                if (g == null) continue;
                g.SetActive(!online);
            }
        }
    }
}
