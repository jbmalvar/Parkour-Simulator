using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonInteract : MonoBehaviour
{
    public GameObject door;
    private bool isOpen = false;
    public float interactDistance = 3f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    private InputAction clickAction;

    void Start()
    {
        clickAction = InputSystem.actions.FindAction("Attack");
        if (clickAction == null)
            clickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        clickAction.Enable();

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (clickAction.WasPressedThisFrame())
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                if (hit.transform == transform)
                    ToggleDoor();
            }
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        if (door != null)
            door.SetActive(!isOpen);

        if (isOpen && openSound != null)
            audioSource.PlayOneShot(openSound);
        else if (!isOpen && closeSound != null)
            audioSource.PlayOneShot(closeSound);
    }
}