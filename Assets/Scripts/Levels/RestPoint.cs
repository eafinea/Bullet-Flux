using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestPoint : MonoBehaviour
{
    private Collider doorTrigger;
    private void Awake()
    {
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CompareTag("ContinueDoor"))
            {
                SceneManager.LoadScene("Arena");
            }
            else if (CompareTag("MenuDoor"))
            {
                SceneManager.LoadScene("Main Menu");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
