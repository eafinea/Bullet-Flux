using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public Camera playerCamera;
    public float xRotation = 0f;

    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    private bool cursorLocked = true;

    void Start()
    {
        SetCursorLock(true);
    }
    void Update()
    {
        // Unlock cursor for dev purposes
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleCursorLock();
        }
    }

    public void ProcessLook(Vector2 input)
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        return;

        float mouseX = input.x;
        float mouseY = input.y;
        //Calculate the rotation
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        //Apply the rotation
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        //Rotate the player
        transform.Rotate((mouseX * Time.deltaTime) * xSensitivity * Vector3.up);
    }
    private void SetCursorLock(bool shouldLock)
    {
        cursorLocked = shouldLock;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
    private void ToggleCursorLock()
    {
        SetCursorLock(!cursorLocked);
    }
}
