using UnityEngine;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive VIP Cinema viewing sofa positioned directly in front of the hologram globe facing the theater screen.
    /// Interacting with the sofa (pressing 'E') triggers Cinema Mode:
    /// smoothly lowering the PC workstation setup into the floor and displaying the Welcome Screen.
    /// </summary>
    public class TheaterSofa : InteractiveProp
    {
        public override bool AllowInteractionInCinemaMode => true;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 4.0f;
            SetPromptOffset(new Vector3(0f, 0.65f, 0f));
            SetPromptAction("SIT ON SOFA (THEATER)");
        }

        protected override void Update()
        {
            base.Update();
            bool inCinema = TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode;
            string targetAction = inCinema ? "STAND UP" : "SIT ON SOFA (THEATER)";
            if (PromptAction != targetAction)
            {
                SetPromptAction(targetAction);
            }
        }

        protected override void OnInteract()
        {
            var cinema = TheaterCinemaController.Instance ?? Object.FindAnyObjectByType<TheaterCinemaController>();
            if (cinema != null)
            {
                Debug.Log("[TheaterSofa] Interacted! Toggling Hydraulic Theater Experience.");
                cinema.ToggleTheaterMode();
            }
            else
            {
                Debug.LogWarning("[TheaterSofa] TheaterCinemaController instance not found in scene!");
            }
        }
    }
}
