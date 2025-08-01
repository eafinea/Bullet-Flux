using UnityEngine;

public class Door : MonoBehaviour
{
    private Animator animator;
    private Collider doorTrigger;
    
    // Cached animator parameter hashes for better performance
    private int isOpenHash;
    private int isCloseHash;
    
    private void Awake()
    {
        // Get Animator component
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[Door] {name}: No Animator component found on the Door object!");
            return;
        }
        
        // Get Collider component and ensure it's a trigger
        doorTrigger = GetComponent<Collider>();
        if (doorTrigger == null)
        {
            Debug.LogError($"[Door] {name}: No Collider component found on the Door object!");
            return;
        }
        
        if (!doorTrigger.isTrigger)
        {
            doorTrigger.isTrigger = true;
        }
        
        // Cache animator parameter hashes for better performance
        isOpenHash = Animator.StringToHash("isOpen");
        isCloseHash = Animator.StringToHash("isClose");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Reset close trigger and set open trigger using hashed parameters
            animator.ResetTrigger(isCloseHash);
            animator.SetTrigger(isOpenHash);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Reset open trigger and set close trigger using hashed parameters
            animator.ResetTrigger(isOpenHash);
            animator.SetTrigger(isCloseHash);
        }
    }
    
    // Method to manually open door (useful for testing or scripted events)
    public void OpenDoor()
    {
        if (animator != null)
        {
            animator.ResetTrigger(isCloseHash);
            animator.SetTrigger(isOpenHash);
        }
    }
    
    // Method to manually close door (useful for testing or scripted events)
    public void CloseDoor()
    {
        if (animator != null)
        {
            animator.ResetTrigger(isOpenHash);
            animator.SetTrigger(isCloseHash);
        }
    }
}
