using System;
using FlipFlop.Game;
using FlipFlop.Game.Managers;
using FlipFlop.Game.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FlipFlop.Gameplay
{
    [RequireComponent(typeof(CharacterController), typeof(AudioSource), typeof(Health))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")] [HideInInspector]
        public static PlayerCharacterController instance { get; private set; }
        
        [Tooltip("Reference to the main camera used for the player")]
        public Camera playerCamera;
        
        [Tooltip("Audio source for footsteps, jumps, etc.")]
        public AudioSource audioSource;

        [Header("General")] [Tooltip("Force applied downward when in the air")]
        public float gravity = 20f;
        
        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask groundCheckLayers = -1;
        
        [Tooltip("Distance from the bottom of the character controller capsule to test for ground")]
        public float groundCheckDistance = 0.05f;

        [Header("Movement")] [Tooltip("Speed of the player when moving")]
        public float moveSpeed = 10f;
        
        [Tooltip("Sharpness for the movement when grounded." +
                 "A low value will make the player accelerate and decelerate slowly." +
                 "A high value will make the player accelerate and decelerate quickly.")]
        public float groundMovementSharpness = 15f;
        
        [Tooltip("Max movement speed when crouching")] [Range(0, 1)]
        public float crouchSpeedRatio = 0.5f;
        
        [Tooltip("Max movement speed when not grounded")]
        public float airMoveSpeed = 5f;
        
        [Tooltip("Acceleration speed when in the air")]
        public float airAccelerationSpeed = 25f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float killHeight = -50f;

        [Header("Rotation")] [Tooltip("Rotation speed used for the player")]
        public float rotationSpeed = 200f;
        
        [Header("Jump")] [Tooltip("Force applied when jumping")]
        public float jumpForce = 9f;

        [Header("Stance")] [Tooltip("Height of the player capsule when standing")]
        public float capsuleStandHeight = 2f;
        
        [Tooltip("Height of the player capsule when crouching")]
        public float capsuleCrouchHeight = 0.9f;
        
        [Tooltip("Speed of the crouching transition")]
        public float crouchTransitionSpeed = 10f;
        
        [Header("Audio")] [Tooltip("Amount of footstep sounds played per meter")]
        public float footstepSfxPerMeter = 1f;
        
        [Tooltip("Sound played for footsteps")]
        public AudioClip footstepSfx;
        
        [Tooltip("Sound played when jumping")]
        public AudioClip jumpSfx;
        [Tooltip("Sound played when landing")]
        public AudioClip landSfx;
        
        [Tooltip("Sound played when the player dies from a fall")]
        public AudioClip fallDamageSfx;
        
        [Header("Fall Damage")] [Tooltip("Whether the player will receive fall damage")]
        public bool fallDamage = true;
        
        [Tooltip("Fall speed for receiving minimum fall damage")]
        public float minFallDamageSpeed = 10f;
        
        [Tooltip("Fall speed for receiving maximum fall damage")]
        public float maxFallDamageSpeed = 40f;
        
        [Tooltip("Damage received when falling at the minimum speed")]
        public float minFallDamage = 10f;
        
        [Tooltip("Damage received when falling at the maximum speed")]
        public float maxFallDamage = 75f;

        public UnityAction<bool> onStanceChanged;
        
        public Vector3 characterVelocity { get; set; }
        public bool isGrounded { get; private set; }
        public bool hasJumpedThisFrame { get; private set; }
        public bool isDead { get; private set; }
        public bool isCrouching { get; private set; }
        
        public Health health;
        public PlayerInputHandler inputHandler;
        private CharacterController controller;
        private Vector3 groundNormal;
        private Vector3 latestImpactSpeed;
        private float lastTimeJumped;
        private float cameraVerticalAngle;
        private float footstepDistanceCounter;
        private float targetCharacterHeight;

        private const float jumpGroundingPreventionTime = 0.2f;
        private const float groundCheckDistanceInAir = 0.081f;

        private void Start()
        {
            if (instance != null && instance != this)
            {
                Debug.LogException(new Exception("There can only be one instance of PlayerCharacterController!"));
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(this); // I don't think this does anything but we really can't have this script being destroyed
            
            controller = GetComponent<CharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(controller, this, gameObject);

            //jumpForce = 9f;
            
            if (inputHandler == null)
            {
                inputHandler = new PlayerInputHandler();
            }
            inputHandler.Gameplay.Enable();
            inputHandler.Movement.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(health, this, gameObject);

            controller.enableOverlapRecovery = true;
            health.OnDie += OnDie;
            
            // Force the crouch state to false when starting
            SetCrouchState(false, true);
            //UpdateCharacterHeight(true);
        }

        private void OnEnable()
        {
            inputHandler?.Gameplay.Enable();
            inputHandler?.Movement.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private void OnDisable()
        {
            inputHandler?.Gameplay.Disable();
            inputHandler?.Movement.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // Check for Y kill
            if (!isDead && transform.position.y < killHeight)
            {
                health.Kill();
            }

            hasJumpedThisFrame = false;
            
            bool wasGrounded = isGrounded;
            GroundCheck();
            
            // Landing
            if (isGrounded && !wasGrounded)
            {
                if (fallDamage)
                {
                    float fallSpeed = -Mathf.Min(characterVelocity.y, latestImpactSpeed.y);
                    float fallSpeedRatio = (fallSpeed - minFallDamageSpeed) /
                                           (maxFallDamageSpeed - minFallDamageSpeed);
                    if (fallSpeedRatio > 0f)
                    {
                        float damageFromFall = Mathf.Lerp(minFallDamage, maxFallDamage, fallSpeedRatio);
                        health.TakeDamage(damageFromFall, null);
                    
                        if (audioSource != null && fallDamageSfx != null)
                        {
                            // Play fall damage SFX
                            audioSource.PlayOneShot(fallDamageSfx);
                        }
                    }
                    else
                    {
                        if (audioSource != null && landSfx != null)
                        {
                            // Play landing SFX
                            audioSource.PlayOneShot(landSfx);
                        }
                    }
                }
            }
            
            // Crouching
            if (inputHandler.Movement.Crouch.WasPressedThisFrame())
            {
                SetCrouchState(!isCrouching, false);
            }

            //UpdateCharacterHeight(false);
            
            HandleCharacterMovement();
        }

        // Doesn't currently do anything
        void OnDie()
        {
            isDead = true;
            
            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck()
        {
            float groundCheckDistanceToUse = isGrounded ? (controller.skinWidth + groundCheckDistance) : groundCheckDistanceInAir;
        
            if (Time.time >= lastTimeJumped + jumpGroundingPreventionTime)
            {
                Vector3 capsuleBottom = GetCapsuleBottomHemisphere();
                Vector3 capsuleTop = GetCapsuleTopHemisphere(controller.height);
        
                Debug.DrawRay(capsuleBottom, Vector3.down * groundCheckDistanceToUse, Color.green);
        
                if (Physics.CapsuleCast(
                        capsuleBottom, capsuleTop, controller.radius,
                        Vector3.down, out RaycastHit hit, groundCheckDistanceToUse, groundCheckLayers, QueryTriggerInteraction.Ignore))
                {
                    groundNormal = hit.normal;
        
                    // Consider grounded if the hit normal is mostly upwards and slope is valid
                    if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(hit.normal))
                    {
                        isGrounded = true;
        
                        // Snap to ground if needed
                        if (hit.distance > controller.skinWidth)
                        {
                            controller.Move(Vector3.down * (hit.distance - controller.skinWidth));
                        }
                    }
                }
                else
                {
                    isGrounded = false;
                    groundNormal = Vector3.up;
                }
            }
        }

        private void HandleCharacterMovement()
        {
            // Character movement handling
            Vector3 cameraForward = playerCamera.transform.forward.normalized;
            Vector3 cameraRight = playerCamera.transform.right.normalized;
            
            Vector3 moveInput = inputHandler.Movement.Move.ReadValue<Vector2>();
            float horizontalMoveInput = moveInput.x;
            float verticalMoveInput = moveInput.y;

            var cameraInput = (cameraRight * horizontalMoveInput + cameraForward * verticalMoveInput);
            cameraInput.y = 0;
            Vector3 worldSpaceMoveInput = cameraInput.normalized;
            
            // Rotate the player to face the move direction
            if (worldSpaceMoveInput.sqrMagnitude > 0f)
            {
                Vector3 lookDirection = new Vector3(worldSpaceMoveInput.x, 0f, worldSpaceMoveInput.z);
                if (lookDirection.sqrMagnitude > 0f)
                {
                    Vector3 newForward = Vector3.RotateTowards(transform.forward, lookDirection.normalized, rotationSpeed * Mathf.Deg2Rad * Time.deltaTime * 3, 1);
                    transform.forward = newForward;
                }
            }
            
            if (isGrounded)
            {
                // Calculate the desired velocity from inputs, max speed and current slope
                Vector3 targetVelocity = worldSpaceMoveInput * moveSpeed;
                
                // Reduce speed if crouching by crouch speed ratio
                if (isCrouching)
                {
                    targetVelocity *= crouchSpeedRatio;
                }

                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, groundNormal) * targetVelocity.magnitude;
                
                // Smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, groundMovementSharpness * Time.deltaTime);
                
                // Jumping
                if (isGrounded && inputHandler.Movement.Jump.WasPressedThisFrame())
                {
                    // Force the crouch state to false
                    if (SetCrouchState(false, false))
                    {
                        // Start by cancelling out the vertical component of our velocity
                        characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                        
                        // Then, add the jumpForce value upwards
                        characterVelocity += Vector3.up * jumpForce;

                        if (audioSource != null && jumpSfx != null)
                        {
                            // Play jump SFX
                            audioSource.PlayOneShot(jumpSfx);
                        }

                        // Remember the last time we jumped as we need to prevent snapping to ground for a short time
                        lastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;
                        
                        // Force grounding to false
                        isGrounded = false;
                        groundNormal = Vector3.up;
                    }
                }
                
                // Footsteps SFX
                if (footstepDistanceCounter >= 1f / footstepSfxPerMeter)
                {
                    footstepDistanceCounter = 0f;
                    if (footstepSfx != null && audioSource != null)
                    {
                        // Play the footstep sound effect
                        audioSource.PlayOneShot(footstepSfx);
                    }
                }
                
                // Keep track of the distance travelled for footstep SFX
                footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
            }
            else // Handle air movement
            {
                
                // Add air acceleration
                characterVelocity += worldSpaceMoveInput * (airAccelerationSpeed * Time.deltaTime);
                
                // Limit the horizontal air speed to a maximum
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, airMoveSpeed);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
                
                // Apply the gravity to the velocity
                characterVelocity += Vector3.down * (gravity * Time.deltaTime);
            }
            
            // Apply the final calculated velocity as a character movement
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(controller.height);
            controller.Move(characterVelocity * Time.deltaTime);
            
            // Detect obstructions to adjust velocity accordingly
            latestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleTopBeforeMove, capsuleTopBeforeMove, controller.radius,
                    characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime,
                    -1, QueryTriggerInteraction.Ignore))
            {
                // We remember the last impact speed because the fall damage logic might need it
                latestImpactSpeed = characterVelocity;
                
                characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
            }
        }
        
        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= controller.slopeLimit;
        }
        
        // Gets the center point of the bottom hemisphere of the character controller capsule
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * (controller.center.y - controller.height * 0.5f + controller.radius));
        }
        
        // Gets the center point of the top hemisphere of the character controller capsule
        Vector3 GetCapsuleTopHemisphere(float height)
        {
            return transform.position + (transform.up * (controller.center.y + height * 0.5f - controller.radius));
        }
        
        // Gets a reoriented direction that is tangent to the given slope
        Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // Update height instantly
            if (force)
            {
                controller.height = targetCharacterHeight;
                controller.center = Vector3.up * (controller.height * 0.5f);
            }
            else // Update smooth height
            {
                // Resize the capsule and adjust camera position
                controller.height = Mathf.Lerp(controller.height, targetCharacterHeight, crouchTransitionSpeed * Time.deltaTime);
                controller.center = Vector3.up * (controller.height * 0.5f);
            }
        }
        
        // Returns false if there is an obstruction
        bool SetCrouchState(bool crouch, bool force)
        {
            // Set appropriate heights
            if (crouch)
            {
                targetCharacterHeight = capsuleCrouchHeight;
            }
            else
            {
                // Detect obstructions
                if (!force)
                {
                    Collider[] standingOverlaps = Array.Empty<Collider>();
                    var size = Physics.OverlapCapsuleNonAlloc(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(capsuleStandHeight), controller.radius, standingOverlaps, -1, QueryTriggerInteraction.Ignore);
                    // ReSharper disable once LocalVariableHidesMember
                    foreach (Collider collider in standingOverlaps)
                    {
                        if (collider != controller)
                        {
                            return false;
                        }
                    }
                }

                targetCharacterHeight = capsuleStandHeight;
            }

            if (onStanceChanged != null)
            {
                onStanceChanged.Invoke(crouch);
            }

            isCrouching = crouch;
            return true;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Debug.Log("testsdrdsfds");
        }
    }
}