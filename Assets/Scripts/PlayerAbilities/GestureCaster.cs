using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PDollarGestureRecognizer; 
using System.IO;                

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))] 
public class GestureCaster : MonoBehaviour
{
    [Header("References")]
    public PlayerAbilities playerAbilities; 
    public Camera mainCamera;
    
    [Tooltip("Drag the object that has your camera look script here to pause it while drawing.")]
    public MonoBehaviour cameraLookScript; 

    [Header("Drawing Settings")]
    public float minDistanceBetweenPoints = 5f; 
    public float drawDistance = 2f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    [Tooltip("Sound played when an invalid drawing or nothing is recognized.")]
    public AudioClip failSound;
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
        Debug.Log("My hidden folder is: " + Application.persistentDataPath);
        
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; 
        if (mainCamera == null) mainCamera = Camera.main;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        TextAsset[] gestureFiles = Resources.LoadAll<TextAsset>("GestureSet");
        foreach (TextAsset file in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromXML(file.text));
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame) StartDrawing();
        if (Mouse.current.rightButton.isPressed) UpdateDrawing();
        if (Mouse.current.rightButton.wasReleasedThisFrame) EndDrawing();
    }

    private void StartDrawing()
    {
        screenPoints.Clear();
        lineRenderer.positionCount = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraLookScript != null) cameraLookScript.enabled = false;
    }

    private void UpdateDrawing()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

            // ---> UPDATED: All checks now expect PlayerAbilities to return true if successful <---
            if (result.Score > 0.8f && result.GestureClass == "Hourglass")
            {
                if (playerAbilities != null && playerAbilities.TriggerTimeStop()) 
                {
                    PlayCastSound(timeStopSound);
                }
                return; 
            }

            if (result.Score > 0.9f && result.GestureClass == "infinitestam")
            {
                if (playerAbilities != null && playerAbilities.TriggerInfiniteStamina())
                {
                    PlayCastSound(infiniteStaminaSound);
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "HealCross")
            {
                if (playerAbilities != null && playerAbilities.TriggerQuickRegen())
                {
                    PlayCastSound(healCrossSound);
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "regenmana")
            {
                if (playerAbilities != null && playerAbilities.TriggerManaBurst())
                {
                    PlayCastSound(manaBurstSound);
                }
                return;
            }

            if (result.Score > 0.75f && result.GestureClass == "nofall")
            {
                if (playerAbilities != null && playerAbilities.TriggerSafeFall())
                {
                    PlayCastSound(safeFallSound);
                }
                return;
            }

            if (result.Score > 0.9f && result.GestureClass == "speed")
            {
                if (playerAbilities != null && playerAbilities.TriggerSpeedBoost())
                {
                    PlayCastSound(speedBoostSound);
                }
                return;
            }

            // Check for the Light / Night Vision gesture
            if (result.Score > 0.9f && result.GestureClass == "light")
            {
                if (playerAbilities != null && playerAbilities.TriggerNightLight())
                {
                    PlayCastSound(lightSpellSound);
                }
                return;
            }
        }
        else
        {
            Debug.LogWarning("No gestures loaded! Make sure your .xml files are in Assets/Resources/GestureSet");
        }

        // --- 2. Fallback to Simple Swipes ---
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
            if (playerAbilities != null && playerAbilities.TriggerGenjiDash()) 
            {
                PlayCastSound(dashSound);
            }
        }
        else
        {
            if (playerAbilities != null && playerAbilities.TriggerSuperJump()) 
            {
                PlayCastSound(jumpSound);
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
}