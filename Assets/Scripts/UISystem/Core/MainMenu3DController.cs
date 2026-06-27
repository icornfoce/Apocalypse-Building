using UnityEngine;
using System.Collections.Generic;

namespace Simulation.UI
{
    /// <summary>
    /// Controls the 3D main menu buttons.
    /// When followCamera is enabled, this object (parent of all buttons) is
    /// re-parented under the main camera at runtime so buttons naturally
    /// follow the camera's position and rotation (orbit, pan, etc.)
    /// without any per-frame delta tracking.
    /// </summary>
    public class MainMenu3DController : MonoBehaviour
    {
        [Header("Buttons Configuration")]
        [Tooltip("List of 3D buttons in the Main Menu. If empty, it will find them in children automatically.")]
        [SerializeField] private List<MainMenu3DButton> menuButtons = new List<MainMenu3DButton>();

        [Header("Stagger Settings")]
        [Tooltip("If true, buttons will slide in with a stagger effect.")]
        [SerializeField] private bool useStagger = true;
        [SerializeField] private float delayBetweenButtons = 0.15f;
        [SerializeField] private float baseDelay = 0.1f;

        [Header("Follow Camera")]
        [Tooltip("If true, this container will be parented to the main camera so all buttons follow it.")]
        [SerializeField] private bool followCamera = true;

        private void Awake()
        {
            if (menuButtons == null || menuButtons.Count == 0)
            {
                menuButtons = new List<MainMenu3DButton>(GetComponentsInChildren<MainMenu3DButton>(true));
            }
        }

        private void Start()
        {
            if (followCamera)
            {
                AttachToCamera();
            }
        }

        private void OnEnable()
        {
            AnimateAllButtons();
        }

        /// <summary>
        /// Parents this transform under the main camera while preserving
        /// its current world-space position, rotation and scale.
        /// </summary>
        private void AttachToCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[MainMenu3DController] Camera.main not found — cannot follow camera.");
                return;
            }

            // Re-parent while keeping the same world position/rotation/scale
            transform.SetParent(cam.transform, worldPositionStays: true);
        }

        /// <summary>
        /// Triggers the slide entrance animation for all registered 3D buttons.
        /// </summary>
        public void AnimateAllButtons()
        {
            if (menuButtons == null || menuButtons.Count == 0) return;

            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (menuButtons[i] == null) continue;

                // Adjust delay if stagger is enabled
                float delay = baseDelay;
                if (useStagger)
                {
                    delay += i * delayBetweenButtons;
                }

                // Trigger button animation with custom stagger delay
                menuButtons[i].PlayEntranceAnimation(delay);
            }
        }
    }
}
