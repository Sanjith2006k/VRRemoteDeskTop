using UnityEngine;
using VRRemoteDesktop.Computer;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive workstation gaming chair.
    /// Approaching the chair displays '[E] SIT & USE COMPUTER'.
    /// Pressing 'E' triggers the smooth cinematic camera animation into the seated position
    /// and activates full Computer Interaction Mode.
    /// </summary>
    public class WorkstationChair : InteractiveProp
    {
        [Header("Chair Seated Focus")]
        [Tooltip("Exact transform where the player's head rests while seated in this chair.")]
        [SerializeField] private Transform _seatedHeadPoint;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 2.8f;
            _promptOffset = new Vector3(0f, 0.40f, 0f);
            SetPromptAction("SIT & USE COMPUTER");
        }

        protected override void OnInteract()
        {
            var cim = ComputerInteractionManager.Instance ?? Object.FindAnyObjectByType<ComputerInteractionManager>();
            if (cim != null && !cim.IsInComputerMode)
            {
                Debug.Log("[WorkstationChair] Seating player in chair and initiating smooth computer transition.");
                cim.EnterComputerMode();
            }
        }
    }
}
