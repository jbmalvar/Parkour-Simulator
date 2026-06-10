using UnityEngine;
using UnityEngine.InputSystem; // ---> ADDED for New Input System compatibility

public class VirtualMouse : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RectTransform fakeCursorRect; 
    
    [Header("Settings")]
    public float mouseSensitivity = 1f;

    public Vector2 ScreenPosition { get; private set; }

    public void CenterCursor()
    {
        // Puts the fake cursor exactly in the middle of the screen
        ScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (fakeCursorRect != null) fakeCursorRect.position = ScreenPosition;
    }

    private void Start()
    {
        CenterCursor();
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (Mouse.current == null) return;

        // 1. Get raw hardware movement from New Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // 2. Add the movement to our current position
        Vector2 newPos = ScreenPosition + mouseDelta;

        // 3. Clamp the position so the fake cursor cannot leave the screen bounds
        newPos.x = Mathf.Clamp(newPos.x, 0, Screen.width);
        newPos.y = Mathf.Clamp(newPos.y, 0, Screen.height);

        // 4. Save the clamped position and move the UI element
        ScreenPosition = newPos;
        fakeCursorRect.position = ScreenPosition;
    }
}