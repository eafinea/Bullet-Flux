using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Camera playerCamera;
    // The distance from the player to the interactable object
    [SerializeField]
    private float distance = 3f;
    [SerializeField]
    private LayerMask layerMask; // Layer mask to filter the objects that can be interacted with
    private PlayerUI playerUI;
    private InputManager inputManager;
    private Outline lastOutline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = GetComponent<PlayerLook>().playerCamera;
        playerUI = GetComponent<PlayerUI>();
        inputManager = GetComponent<InputManager>();
    }

    // Update is called once per frame
    void Update()
    {
        playerUI.UpdateText(""); // Clear the UI text at the start of each frame
        
        // Creating a ray from the camera to the point in the world where the camera is looking
        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.yellow); // Visualize the ray in the editor
        RaycastHit hitInfo; // Variable to store information about the object hit by the ray

        // Check if the ray hits an object within the specified distance and layer mask
        if (Physics.Raycast(ray, out hitInfo, distance, layerMask))
        {
            // Check if the hit object has an Interactable component
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                // Update UI with prompt message
                playerUI.UpdateText(interactable.promptMessage);
                
                // Handle outline management
                Outline currentOutline = interactable.GetComponent<Outline>();
                
                // If we hit a different interactable or this is the first one
                if (lastOutline != currentOutline)
                {
                    // Disable the previous outline if it exists
                    if (lastOutline != null)
                    {
                        lastOutline.enabled = false;
                    }
                    
                    // Enable the new outline if it exists
                    if (currentOutline != null)
                    {
                        currentOutline.enabled = true;
                    }
                    
                    // Update the reference
                    lastOutline = currentOutline;
                }
                
                // Handle interaction input
                if (inputManager.onFootActions.Interact.triggered)
                {
                    interactable.BaseInteract();
                }
                
                return; // Exit early since we found an interactable
            }
        }
        
        // No interactable found - disable any active outline
        if (lastOutline != null)
        {
            lastOutline.enabled = false;
            lastOutline = null;
        }
    }
}
