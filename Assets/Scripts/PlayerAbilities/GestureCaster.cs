using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class GestureCaster : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public Camera mainCamera; 
    
    [Tooltip("Drag the object that has your camera look script (ParkourCamera) here so we can pause it while drawing.")]
    public MonoBehaviour cameraLookScript; 

    [Header("Drawing Settings")]
    public float minDistanceBetweenPoints = 5f; 
    public float drawDistance = 2f; 

    private LineRenderer lineRenderer;
    private List<Vector2> screenPoints = new List<Vector2>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; 

        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. Start Drawing
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            StartDrawing();
        }

        // 2. Continue Drawing
        if (Mouse.current.rightButton.isPressed)
        {
            UpdateDrawing();
        }

        // 3. Stop and Cast Gesture
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            EndDrawing();
        }
    }

    private void StartDrawing()
    {
        screenPoints.Clear();
        lineRenderer.positionCount = 0;

        // NEW: Unlock the mouse and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // NEW: Pause the camera script so moving the mouse doesn't spin your view
        if (cameraLookScript != null) 
        {
            cameraLookScript.enabled = false;
        }
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

        // NEW: Relock the mouse and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // NEW: Turn the camera script back on
        if (cameraLookScript != null) 
        {
            cameraLookScript.enabled = true;
        }

        if (screenPoints.Count < 5) return; 

        Vector2 startPoint = screenPoints[0];
        Vector2 endPoint = screenPoints[screenPoints.Count - 1];
        Vector2 swipeVector = endPoint - startPoint;
        
        if (swipeVector.magnitude < 100f) return;

        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (playerMovement != null) playerMovement.TriggerGenjiDash();
        }
        else
        {
            if (playerMovement != null) playerMovement.TriggerSuperJump();
        }
    }
}