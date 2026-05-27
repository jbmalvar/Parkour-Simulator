using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    private AudioSource audioSource;
    private CharacterController controller;
    private PlayerMovement playerMovement; 

    [Header("Footstep Sounds")]
    public AudioClip[] walkSteps;
    public AudioClip[] sprintSteps;
    [Range(0.1f, 1f)] public float footstepVolume = 0.5f;

    [Header("Footstep Timing")]
    public float walkInterval = 0.5f;   
    public float sprintInterval = 0.3f; 
    private float stepTimer = 0f;

    // NEW: Variables to track your true position manually
    private Vector3 lastPosition;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        controller = GetComponentInParent<CharacterController>(); 
        playerMovement = GetComponentInParent<PlayerMovement>();
        
        stepTimer = walkInterval; 
        
        // Record our starting position
        lastPosition = transform.position;
    }

    void Update()
    {
        if (controller == null || playerMovement == null) return;

        // NEW: Calculate your TRUE velocity by comparing where you are now vs last frame
        Vector3 trueVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position; // Update for the next frame

        // Isolate horizontal movement
        Vector3 horizontalVelocity = new Vector3(trueVelocity.x, 0, trueVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Now we use our custom currentSpeed instead of controller.velocity!
        if (controller.isGrounded && currentSpeed > 0.5f 
            && !playerMovement.IsSliding && !playerMovement.IsRolling && !playerMovement.IsWallRunning)
        {
            bool isSprinting = currentSpeed > 6f; 
            float currentInterval = isSprinting ? sprintInterval : walkInterval;

            stepTimer += Time.deltaTime;

            if (stepTimer >= currentInterval)
            {
                if (isSprinting) PlaySprintStep();
                else PlayWalkStep();
                
                stepTimer = 0f; 
            }
        }
        else if (currentSpeed < 0.1f)
        {
            stepTimer = walkInterval; 
        }
    }

    private void PlayWalkStep()
    {
        if (walkSteps.Length > 0)
        {
            AudioClip clip = walkSteps[Random.Range(0, walkSteps.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    private void PlaySprintStep()
    {
        if (sprintSteps.Length > 0)
        {
            AudioClip clip = sprintSteps[Random.Range(0, sprintSteps.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }
}