using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAbilities : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private CharacterController controller;

    [Header("Genji Dash Settings")]
    public float dashSpeed = 35f;
    public float dashDuration = 0.25f;

    [Header("Super Jump Settings")]
    public float superJumpMultiplier = 3f;

    private Coroutine currentAbilityRoutine;

    void Start()
    {
        // Automatically grab the required components on this GameObject
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
    }

    public void TriggerGenjiDash()
    {
        playerMovement.EndCurrentAction(); 
        if (currentAbilityRoutine != null) StopCoroutine(currentAbilityRoutine);
        currentAbilityRoutine = StartCoroutine(PerformGenjiDash());
    }

    public void TriggerSuperJump()
    {
        playerMovement.EndCurrentAction();
        
        // Grab the base jump height and gravity from the movement script
        float baseJumpHeight = playerMovement.jumpHeight;
        float gravity = playerMovement.gravity;

        playerMovement.playerVelocity.y = Mathf.Sqrt((baseJumpHeight * superJumpMultiplier) * -2f * gravity); 
    }

    private IEnumerator PerformGenjiDash()
    {
        float timer = 0f;
        
        // Reset gravity/velocity pull
        playerMovement.playerVelocity = Vector3.zero;
        Vector3 dashDirection = Camera.main.transform.forward;

        while (timer < dashDuration)
        {
            // Move strictly in the dash direction
            controller.Move(dashDirection * dashSpeed * Time.unscaledDeltaTime);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        // Leave a bit of forward momentum when the dash ends
        playerMovement.playerVelocity = dashDirection * playerMovement.walkSpeed; 
    }
}