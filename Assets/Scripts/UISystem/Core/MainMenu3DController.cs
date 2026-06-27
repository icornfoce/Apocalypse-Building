using UnityEngine;
using System.Collections.Generic;

namespace Simulation.UI
{
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

        private void Awake()
        {
            if (menuButtons == null || menuButtons.Count == 0)
            {
                menuButtons = new List<MainMenu3DButton>(GetComponentsInChildren<MainMenu3DButton>(true));
            }
        }

        private void OnEnable()
        {
            AnimateAllButtons();
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
