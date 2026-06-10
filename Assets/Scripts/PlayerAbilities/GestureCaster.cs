using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PDollarGestureRecognizer; 
using System.IO;                
using TMPro;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))] 
public class GestureCaster : MonoBehaviour
{
    [Header("References")]
    public PlayerAbilities playerAbilities; 
    public Camera mainCamera;
    
    [Tooltip("Drag the object that has your camera look script here to pause it while drawing.")]
    public MonoBehaviour cameraLookScript; 

    // ---> ADDED: Reference to your new fake cursor script <---
    [Header("Virtual Mouse")]
    public VirtualMouse virtualMouse;

    [Header("UI & Timers")] 
    public TMP_Text abilityTimerText; 
    public TMP_Text warningText; 
    
    public float timeStopDuration = 7f;
    public float safeFallDuration = 10f; 
    public float lightDuration = 10f;
    public float speedBoostDuration = 6f; 

    private float timeStopTimer = 0f;
    private float safeFallTimer = 0f;
    private float lightTimer = 0f;
    private float speedBoostTimer = 0f; 
    
    private Coroutine warningRoutine; 

    [Header("Drawing Settings")]
    public float minDistanceBetweenPoints = 5f; 
    public float drawDistance = 2f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip failSound;
    public AudioClip notEnoughManaSound; 
    public AudioClip timeStopSound;
    public AudioClip infiniteStaminaSound;
    public AudioClip healCrossSound;
    public AudioClip manaBurstSound;
    public AudioClip safeFallSound;
    public AudioClip speedBoostSound;
    public AudioClip dashSound;
    public AudioClip jumpSound;
    public AudioClip lightSpellSound;

    private LineRenderer lineRenderer;
    private List<Vector2> screenPoints = new List<Vector2>();
    private List<Gesture> trainingSet = new List<Gesture>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; 
        if (mainCamera == null) mainCamera = Camera.main;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        TextAsset[] gestureFiles = Resources.LoadAll<TextAsset>("GestureSet");
        foreach (TextAsset file in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromXML(file.text));
        }

        if (abilityTimerText != null) abilityTimerText.text = "";
        if (warningText != null) warningText.text = ""; 

        // ---> ADDED: Hide the virtual mouse when the game starts <---
        if (virtualMouse != null) virtualMouse.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleTimers(); 

        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame) StartDrawing();
        if (Mouse.current.rightButton.isPressed) UpdateDrawing();
        if (Mouse.current.rightButton.wasReleasedThisFrame) EndDrawing();
    }

    private void HandleTimers()
    {
        string currentText = "";

        if (playerAbilities != null && playerAbilities.infiniteStaminaRemainingTime > 0)
        {
            currentText += $"Inf Stamina: {playerAbilities.infiniteStaminaRemainingTime:F1}s\n";
        }

        if (timeStopTimer > 0)
        {
            timeStopTimer -= Time.unscaledDeltaTime; 
            if (timeStopTimer > 0) currentText += $"Time Stop: {timeStopTimer:F1}s\n";
        }

        if (safeFallTimer > 0)
        {
            safeFallTimer -= Time.unscaledDeltaTime; 
            if (safeFallTimer > 0) currentText += $"Safe Fall: {safeFallTimer:F1}s\n";
        }

        if (lightTimer > 0)
        {
            lightTimer -= Time.unscaledDeltaTime; 
            if (lightTimer > 0) currentText += $"Light: {lightTimer:F1}s\n";
        }

        if (speedBoostTimer > 0)
        {
            speedBoostTimer -= Time.unscaledDeltaTime;
            if (speedBoostTimer > 0) currentText += $"Speed Boost: {speedBoostTimer:F1}s\n";
        }

        if (abilityTimerText != null)
        {
            abilityTimerText.text = currentText;
        }
    }

    private void StartDrawing()
    {
        screenPoints.Clear();
        lineRenderer.positionCount = 0;
        
        // ---> UPDATED: Keep the real mouse locked and hidden <---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ---> ADDED: Turn on and center the fake UI cursor <---
        if (virtualMouse != null)
        {
            virtualMouse.gameObject.SetActive(true);
            virtualMouse.CenterCursor(); 
        }

        if (cameraLookScript != null) cameraLookScript.enabled = false;
    }

    private void UpdateDrawing()
    {
        // ---> UPDATED: Get position from the fake cursor, NOT the real mouse <---
        Vector2 mousePos = virtualMouse.ScreenPosition;

        if (screenPoints.Count == 0 || Vector2.Distance(mousePos, screenPoints[screenPoints.Count - 1]) > minDistanceBetweenPoints)
        {
            screenPoints.Add(mousePos);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, drawDistance));
            
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, worldPos);
        }
    }

    private void EndDrawing()
    {
        lineRenderer.positionCount = 0;
        
        // ---> UPDATED: Keep the real mouse locked since we are done drawing <---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ---> ADDED: Turn off the fake UI cursor <---
        if (virtualMouse != null) virtualMouse.gameObject.SetActive(false);

        if (cameraLookScript != null) cameraLookScript.enabled = true;

        if (screenPoints.Count < 5) 
        {
            PlayCastSound(failSound);
            return; 
        }

        Point[] pointArray = new Point[screenPoints.Count];
        for (int i = 0; i < screenPoints.Count; i++)
        {
            pointArray[i] = new Point(screenPoints[i].x, -screenPoints[i].y, 0); 
        }

        Gesture playerDrawing = new Gesture(pointArray);

        if (trainingSet.Count > 0)
        {
            Result result = PointCloudRecognizer.Classify(playerDrawing, trainingSet.ToArray());

            if (result.Score > 0.8f && result.GestureClass == "Hourglass")
            {
                if (playerAbilities != null) 
                {
                    if (playerAbilities.TriggerTimeStop())
                    {
                        timeStopTimer = timeStopDuration; 
                        PlayCastSound(timeStopSound);
                    }
                    else TriggerCastFailure();
                }
                return; 
            }

            if (result.Score > 0.9f && result.GestureClass == "infinitestam")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerInfiniteStamina()) PlayCastSound(infiniteStaminaSound);
                    else TriggerCastFailure();
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "HealCross")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerQuickRegen()) PlayCastSound(healCrossSound);
                    else TriggerCastFailure();
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "regenmana")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerManaBurst()) PlayCastSound(manaBurstSound);
                    else TriggerCastFailure();
                }
                return;
            }

            if (result.Score > 0.75f && result.GestureClass == "nofall")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerSafeFall())
                    {
                        safeFallTimer = safeFallDuration; 
                        PlayCastSound(safeFallSound);
                    }
                    else TriggerCastFailure();
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "speed")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerSpeedBoost())
                    {
                        speedBoostTimer = speedBoostDuration; 
                        PlayCastSound(speedBoostSound);
                    }
                    else TriggerCastFailure();
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "light")
            {
                if (playerAbilities != null)
                {
                    if (playerAbilities.TriggerNightLight())
                    {
                        lightTimer = lightDuration; 
                        PlayCastSound(lightSpellSound);
                    }
                    else TriggerCastFailure();
                }
                return;
            }
        }
        else
        {
            Debug.LogWarning("No gestures loaded! Make sure your .xml files are in Assets/Resources/GestureSet");
        }

        Vector2 startPoint = screenPoints[0];
        Vector2 endPoint = screenPoints[screenPoints.Count - 1];
        Vector2 swipeVector = endPoint - startPoint;
        
        if (swipeVector.magnitude < 100f) 
        {
            PlayCastSound(failSound);
            return;
        }

        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (playerAbilities != null)
            {
                if (playerAbilities.TriggerGenjiDash()) PlayCastSound(dashSound);
                else TriggerCastFailure();
            }
        }
        else
        {
            if (playerAbilities != null)
            {
                if (playerAbilities.TriggerSuperJump()) PlayCastSound(jumpSound);
                else TriggerCastFailure();
            }
        }
    }

    private void PlayCastSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void TriggerCastFailure()
    {
        PlayCastSound(notEnoughManaSound);
        
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(ShowWarningMessage("Not Enough Mana!"));
    }

    private IEnumerator ShowWarningMessage(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            yield return new WaitForSeconds(2f);
            warningText.text = "";
        }
    }
}