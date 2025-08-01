using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private bool isGrounded;
    public float gravity = -9.8f;
    public float speed = 5f;
    public float jumpHeight = 1f;
    public float crouchTimer;

    private bool lerpCrouch;
    private bool crouch;
    private bool sprint;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;
        if (lerpCrouch)
        {
            crouchTimer += Time.deltaTime;
            float p = crouchTimer / 1f;
            p *= p;
            if (crouch)
                characterController.height = Mathf.Lerp(characterController.height, 1f, p);
            else
                characterController.height = Mathf.Lerp(characterController.height, 2f, p);

            if (p > 1)
            {
                lerpCrouch = false;
                crouchTimer = 0f;
            }
        }
    }

    public void ProcessMovement(Vector2 input)
    {
        Vector3 moveDirection = new(input.x, 0, input.y);
        characterController.Move(speed * Time.deltaTime * transform.TransformDirection(moveDirection));
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    public void ProcessJump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3f * gravity);
        }
    }

    public void ProcessCrouch(bool isCrouching)
    {
        crouch = !crouch;
        lerpCrouch = true;
        crouchTimer = 0f;
        if (crouch)
        {
            speed /= 2;
        }
        else
        {
            speed = 5f;
        }
    }

    public void ProcessSprint(bool isSprinting)
    {
        sprint = isSprinting;
        if (sprint)
        {
            speed *= 2;
        }
        else
        {
            speed = 5f;
        }
    }
}
