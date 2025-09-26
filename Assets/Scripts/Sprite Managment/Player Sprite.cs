using FlipFlop.Gameplay;
using UnityEngine;

public class PlayerSprite : MonoBehaviour
{
    private PlayerCharacterController characterController;
    public Transform playerTransform;
    public Animator playerSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = FindAnyObjectByType<PlayerCharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
        transform.position = playerTransform.position;

        if (!characterController.isGrounded)
        {
            playerSprite.Play("Magician_Anim_Jump");
        }
        else if (Vector3Int.RoundToInt(characterController.characterVelocity) != Vector3.zero) 
        {
            playerSprite.Play("Magician_Anim_Run");
        }
        else
        {
            playerSprite.Play("Magician_Anim_Idle");
        }
    }
}
