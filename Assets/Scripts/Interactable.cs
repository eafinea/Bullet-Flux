using UnityEngine;

public abstract class Interactable : MonoBehaviour //Template for sub-classes
{
    // Add/Remove the InteractEvent component to/from the game object
    public bool useEvent;
    public string promptMessage;

    // This method is called when the player interacts with the object.
    public void BaseInteract()
    {
        if (useEvent)
        {
            GetComponent<InteractionEvent>().onInteract.Invoke();
        }
        Interact();
    }
    protected virtual void Interact()
    {
        // This method will be overridden in derived classes
        // to provide specific interaction behavior.
    }
}
