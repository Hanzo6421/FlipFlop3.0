using UnityEngine;

namespace FlipFlop.Gameplay.Puzzle_Elements
{
    public class RotatingBillboardTrigger : MonoBehaviour
    {
        public new Collider collider;

        [HideInInspector]
        public bool isPlayerInInteractTrigger { get; private set; } = false;

        private void Start()
        {
            if (collider == null)
            {
                Debug.LogWarning("Collider not assigned in RotatingBillboardTrigger. Attempting to get Collider component from the GameObject.");
                collider = GetComponent<Collider>();
                if (collider == null)
                {
                    Debug.LogError("No Collider component found on the GameObject. Please assign a Collider.");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerInInteractTrigger = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerInInteractTrigger = false;
            }
        }
        
        public void TryMovePlayerOutOfTrigger()
        {
            GameObject player = PlayerCharacterController.instance.gameObject;
            if (player != null && collider.bounds.Contains(player.transform.position))
            {
                Vector3 direction = (player.transform.position - collider.bounds.center);
                direction.y = 0;
                direction.Normalize();
                
                Vector3 outsidePosition = collider.bounds.center + direction * (collider.bounds.extents.magnitude + 1f);
                outsidePosition.y = player.transform.position.y; // Maintain the player's original Y position
                
                player.transform.position = outsidePosition;
            }
        }
    }
}