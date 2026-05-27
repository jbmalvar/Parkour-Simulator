using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Footstep Sounds")]
    public AudioClip[] walkSteps;
    public AudioClip[] sprintSteps;
    [Range(0.1f, 1f)] public float footstepVolume = 0.5f;

    [Header("Ability Sounds")]
    public AudioClip castSound;
    [Range(0.1f, 1f)] public float castVolume = 1.0f;

    void Start()
    {
        // Grabs the AudioSource component off the player
        audioSource = GetComponent<AudioSource>();
    }

    // Call this for walking
    public void PlayWalkStep()
    {
        if (walkSteps.Length > 0)
        {
            AudioClip clip = walkSteps[Random.Range(0, walkSteps.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    // Call this for sprinting
    public void PlaySprintStep()
    {
        if (sprintSteps.Length > 0)
        {
            AudioClip clip = sprintSteps[Random.Range(0, sprintSteps.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    // Call this when the Hourglass triggers
    public void PlayCastSound()
    {
        if (castSound != null)
        {
            audioSource.PlayOneShot(castSound, castVolume);
        }
    }
}