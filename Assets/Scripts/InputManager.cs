using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    public PlayerInput.OnFootActions onFootActions;

    private PlayerMotor playerMotor;
    private PlayerLook playerLook;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();
        onFootActions = playerInput.OnFoot;
        playerMotor = GetComponent<PlayerMotor>();
        playerLook = GetComponent<PlayerLook>();
        onFootActions.Jump.performed += ctx => playerMotor.ProcessJump(); //Anytime onFootActions.Jump is performed, call the ProcessJump method in PlayerMotor using context callback ctx
        //Crouch on button down
        onFootActions.Crouch.started += ctx => playerMotor.ProcessCrouch(true);
        onFootActions.Crouch.canceled += ctx => playerMotor.ProcessCrouch(false);
        //Sprint on button down
        onFootActions.Sprint.started += ctx => playerMotor.ProcessSprint(true);
        onFootActions.Sprint.canceled += ctx => playerMotor.ProcessSprint(false);

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Tell the PlayerMotor to process the movement input
        playerMotor.ProcessMovement(onFootActions.Movement.ReadValue<Vector2>());
    }
    private void LateUpdate()
    {
        //Tell the PlayerLook to process the look input
        playerLook.ProcessLook(onFootActions.Look.ReadValue<Vector2>());
    }
    private void OnEnable()
    {
        onFootActions.Enable();
    }
    private void OnDisable()
    {
        onFootActions.Disable();
    }
}
