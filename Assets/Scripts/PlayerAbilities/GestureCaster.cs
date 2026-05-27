using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PDollarGestureRecognizer; // Required for PDollar
using System.IO;                // Required for file loading

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

    // NEW: List to hold our loaded PDollar gestures
    private List<Gesture> trainingSet = new List<Gesture>();

    void Start()
    {
        // I left your debug log here just in case you ever need to find the folder again!
        Debug.Log("My hidden folder is: " + Application.persistentDataPath);
        
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; 
        if (mainCamera == null) mainCamera = Camera.main;

        // NEW: Load all the .xml gesture files from your Assets/Resources/GestureSet folder
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

        if (screenPoints.Count < 5) return; 

        // --- 1. PDollar Shape Recognition ---
        Point[] pointArray = new Point[screenPoints.Count];
        for (int i = 0; i < screenPoints.Count; i++)
        {
            // PDollar flips the Y axis mathematically, so we feed it a negative Y value
            pointArray[i] = new Point(screenPoints[i].x, -screenPoints[i].y, 0); 
        }

        Gesture playerDrawing = new Gesture(pointArray);

        // Only try to classify if we successfully loaded XML files
        if (trainingSet.Count > 0)
        {
            Result result = PointCloudRecognizer.Classify(playerDrawing, trainingSet.ToArray());
            Debug.Log("Drew: " + result.GestureClass + " | Accuracy: " + result.Score);

            // If the player drew an Hourglass with at least 80% accuracy, cast it and STOP
            if (result.Score > 0.8f && result.GestureClass == "Hourglass")
            {
                if (playerAbilities != null) playerAbilities.TriggerTimeStop();
                return; 
            }
        }
        else
        {
            Debug.LogWarning("No gestures loaded! Make sure your .xml files are in Assets/Resources/GestureSet");
        }

        // --- 2. Fallback to Simple Swipes ---
        // If the drawing wasn't a complex shape, handle it like a standard dash or jump
        Vector2 startPoint = screenPoints[0];
        Vector2 endPoint = screenPoints[screenPoints.Count - 1];
        Vector2 swipeVector = endPoint - startPoint;
        
        if (swipeVector.magnitude < 100f) return;

        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (playerAbilities != null) playerAbilities.TriggerGenjiDash();
        }
        else
        {
            if (playerAbilities != null) playerAbilities.TriggerSuperJump();
        }
    }
}