using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
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

    private LineRenderer lineRenderer;
    private List<Vector2> screenPoints = new List<Vector2>();

    void Start()
    {
        Debug.Log("My hidden folder is: " + Application.persistentDataPath);
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; 
        if (mainCamera == null) mainCamera = Camera.main;
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

        if (screenPoints.Count < 5) return; 

        // Get the complex gesture string
        string gestureSequence = GetGestureDirectionString(screenPoints);
        Debug.Log("Gesture Drawn: " + gestureSequence);

        // Check for Hourglass (e.g., Right -> Down-Left -> Right -> Up-Left)
        // We include a few common sloppy variations of drawing an hourglass
        if (gestureSequence.Contains("R-DL-R-UL") || gestureSequence.Contains("R-LD-R-LU") || gestureSequence.Contains("R-D-R-U"))
        {
            if (playerAbilities != null) playerAbilities.TriggerTimeStop();
            return;
        }

        // Fallback to simple swipes if it's not a complex shape
        Vector2 startPoint = screenPoints[0];
        Vector2 endPoint = screenPoints[screenPoints.Count - 1];
        Vector2 swipeVector = endPoint - startPoint;
        
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (playerAbilities != null) playerAbilities.TriggerGenjiDash();
        }
        else
        {
            if (playerAbilities != null) playerAbilities.TriggerSuperJump();
        }
    }

    // NEW: A much more forgiving string generator
    private string GetGestureDirectionString(List<Vector2> points)
    {
        if (points.Count == 0) return "";

        List<string> directions = new List<string>();
        
        // Increasing this number makes it MORE forgiving (ignores larger jitters)
        // Decreasing it makes it MORE exact
        float strokeThreshold = 40f; 
        Vector2 lastAnchor = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            // Only log a direction if the mouse has moved far enough to prove it's a deliberate stroke
            if (Vector2.Distance(points[i], lastAnchor) > strokeThreshold)
            {
                Vector2 dir = (points[i] - lastAnchor).normalized;
                string currentDir = GetDirectionFromVector(dir);
                
                // Only add to the list if it's a NEW direction
                if (directions.Count == 0 || directions[directions.Count - 1] != currentDir)
                {
                    directions.Add(currentDir);
                }
                
                // Move our anchor forward to check the next segment
                lastAnchor = points[i]; 
            }
        }
        
        return string.Join("-", directions);
    }

    // Helper: Maps a 2D vector to an 8-way compass direction
    private string GetDirectionFromVector(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360;

        if (angle >= 337.5f || angle < 22.5f) return "R";
        if (angle >= 22.5f && angle < 67.5f) return "UR";
        if (angle >= 67.5f && angle < 112.5f) return "U";
        if (angle >= 112.5f && angle < 157.5f) return "UL";
        if (angle >= 157.5f && angle < 202.5f) return "L";
        if (angle >= 202.5f && angle < 247.5f) return "DL"; // Down-Left
        if (angle >= 247.5f && angle < 292.5f) return "D";
        if (angle >= 292.5f && angle < 337.5f) return "DR";
        
        return "";
    }
}