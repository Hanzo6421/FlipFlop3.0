using FlipFlop.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlipFlop.Gameplay.Puzzle_Elements
{
    public class RotatingBillboard : MonoBehaviour
    {
        private PlayerInputHandler inputHandler;

        public RotatingBillboardTrigger trigger;

        public bool interacting;

        private void Start()
        {
            if (PlayerCharacterController.instance != null)
            {
                inputHandler = PlayerCharacterController.instance.inputHandler;
            }
            else
            {
                Debug.LogWarning("PlayerCharacterController.instance or its inputHandler is null.");
                inputHandler = new PlayerInputHandler();
                inputHandler.Gameplay.Enable();
            }
            
            inputHandler.Gameplay.Interact.started += OnInteractStarted;
            inputHandler.Interacting.Rotate.started += OnRotateStarted;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (!interacting)
            {
                if (trigger.isPlayerInInteractTrigger)
                {
                    inputHandler.Movement.Disable();
                    PlayerCharacterController.instance.inputHandler.Movement.Disable();
                    inputHandler.Interacting.Enable();
                    PlayerCharacterController.instance.inputHandler.Interacting.Enable();
                
                    trigger.collider.includeLayers = LayerMask.GetMask("Default", "Ignore Raycast");
                    trigger.TryMovePlayerOutOfTrigger();
                    interacting = true;
                    
                    Debug.Log("Started interaction");
                }
            }
            else
            {
                inputHandler.Interacting.Disable();
                PlayerCharacterController.instance.inputHandler.Interacting.Disable();
                inputHandler.Movement.Enable();
                PlayerCharacterController.instance.inputHandler.Movement.Enable();
                
                trigger.collider.includeLayers = LayerMask.GetMask();
                interacting = false;
                
                Debug.Log("Stopped interaction");
            }
        }

        private void OnRotateStarted(InputAction.CallbackContext context)
        {
            transform.Rotate(0f, inputHandler.Interacting.Rotate.ReadValue<float>() * 90f, 0f);
        }
    }
}