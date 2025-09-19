using FlipFlop.Game;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace FlipFlop.Gameplay
{
    public class CinemachineManager : MonoBehaviour
    {
        public bool isPerspective { get; private set; } = true;
        public bool isTransitioning { get; private set; } = false;
        public bool isOrthographic { get; private set; } = false;
        
        private CinemachineBrain cinemachineBrain;
        public CinemachineCamera perspectiveCamera;
        public CinemachineCamera transitionCamera;
        public CinemachineCamera orthographicCamera;

        public PlayableDirector perspectiveToOrthoTimeline;
        public PlayableDirector orthoToPerspectiveTimeline;

        private PlayerInputHandler inputHandler;

        private CinemachinePositionComposer orthographicPerspectiveComposer;
        
        public GameObject[] orthoOnlyColliders;
        
        private void Start()
        {
            DontDestroyOnLoad(transform.parent.gameObject);
            
            cinemachineBrain = GetComponent<CinemachineBrain>();
            DebugUtility.HandleErrorIfNullGetComponent<CinemachineManager, CinemachineBrain>(cinemachineBrain, this, gameObject);

            Debug.Log("Transitioning to perspective");
            perspectiveCamera.gameObject.SetActive(true);
            ToggleOrthoOnlyColliders(false);
            isTransitioning = true;
            orthoToPerspectiveTimeline.Play();
            orthographicCamera.gameObject.SetActive(false);
            isPerspective = true;
            isOrthographic = false;
            isTransitioning = false;
            
            if (PlayerCharacterController.instance != null)
            {
                inputHandler = PlayerCharacterController.instance.inputHandler;
            }
            else
            {
                Debug.LogWarning("PlayerCharacterController.instance or its inputHandler is null.");
                inputHandler = new PlayerInputHandler();
                inputHandler.Movement.Enable();
            }

            inputHandler.Movement.TogglePerspective.started += TogglePerspectiveStarted;

            orthographicPerspectiveComposer = orthographicCamera.GetComponent<CinemachinePositionComposer>();

            orthoOnlyColliders = GameObject.FindGameObjectsWithTag("OrthoOnlyCollider");
            foreach (GameObject collider in orthoOnlyColliders)
            {
                collider.SetActive(orthographicCamera.gameObject.activeSelf);
            }
        }

        private void TogglePerspectiveStarted(InputAction.CallbackContext context)
        {
            if (inputHandler.Movement.TogglePerspective.WasPressedThisFrame())
            {
                if (perspectiveCamera != null && orthographicCamera != null)
                {
                    while (isTransitioning)
                    {
                        var trans = orthographicCamera.transform;
                        Vector3 pos = trans.position + trans.forward *
                            orthographicPerspectiveComposer.CameraDistance;
                        float size = orthographicCamera.Lens.OrthographicSize *
                                     Mathf.Tan(perspectiveCamera.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);

                        float d = Mathf.Max((size / Mathf.Tan(1f * 0.5f)) * Mathf.Rad2Deg, 700f);
                        transitionCamera.transform.position = trans.position + trans.forward * -d;
                    }
                    
                    if (perspectiveCamera.gameObject.activeSelf)
                    {
                        Debug.Log("Transitioning to orthographic");
                        orthographicCamera.gameObject.SetActive(true);
                        ToggleOrthoOnlyColliders(true);
                        isTransitioning = true;
                        perspectiveToOrthoTimeline.Play();
                        perspectiveCamera.gameObject.SetActive(false);
                        isPerspective = false;
                        isOrthographic = true;
                        isTransitioning = false;
                        
                        // Function here
                            //Physics.Raycast(-Vector.up, transform.position + (0,20,0), 25)
                            {
                                //Move character to hit position
                            }
                    }
                    else
                    {
                        Debug.Log("Transitioning to perspective");
                        perspectiveCamera.gameObject.SetActive(true);
                        ToggleOrthoOnlyColliders(false);
                        isTransitioning = true;
                        orthoToPerspectiveTimeline.Play();
                        orthographicCamera.gameObject.SetActive(false);
                        isPerspective = true;
                        isOrthographic = false;
                        isTransitioning = false;
                    }
                }
            }
        }

        private void ToggleOrthoOnlyColliders(bool isOrtho)
        {
            foreach (GameObject collider in orthoOnlyColliders)
            {
                collider.SetActive(isOrtho);
            }
        }
    }
}