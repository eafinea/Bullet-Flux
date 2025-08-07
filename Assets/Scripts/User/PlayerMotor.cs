using UnityEngine;
using FMOD.Studio;

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

    // Footstep sound
    private EventInstance playerFootsteps;
    [Header("Footstep Rate")]
    [SerializeField] private float walkRate = 1f;
    [SerializeField] private float runRate = 2f;
    [SerializeField] private float crouchRate = 0.2f;
    
    // Movement tracking
    private Vector2 currentInput;
    private bool footstepsPlaying = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerFootsteps = AudioManager.instance.CreateInstance(FMODEvents.instance.playerFootsteps);
        
        // Attach footsteps to player for 3D positioning
        if (playerFootsteps.isValid())
        {
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(playerFootsteps, gameObject);
        }
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;
        
        // Update footstep audio
        UpdateSound();
        
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
        currentInput = input;
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
            AudioManager.instance.PlayOneShot(FMODEvents.instance.playerJump, transform.position);
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
        UpdateFootstepRate();
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
        UpdateFootstepRate();
    }

    private void UpdateSound()
    {
        if (!playerFootsteps.isValid())
            return;

        bool shouldPlay = ShouldPlayFootsteps();

        if (shouldPlay && !footstepsPlaying)
        {
            StartFootsteps();
        }
        else if (!shouldPlay && footstepsPlaying)
        {
            StopFootsteps();
        }
    }

    private bool ShouldPlayFootsteps()
    {
        // Play footsteps if grounded and moving
        return isGrounded && currentInput.magnitude > 0.1f;
    }

    private void StartFootsteps()
    {
        PLAYBACK_STATE playbackState;
        playerFootsteps.getPlaybackState(out playbackState);
        
        if (playbackState == PLAYBACK_STATE.STOPPED)
        {
            playerFootsteps.start();
            footstepsPlaying = true;
            UpdateFootstepRate();
        }
    }

    private void StopFootsteps()
    {
        playerFootsteps.stop(STOP_MODE.ALLOWFADEOUT);
        footstepsPlaying = false;
    }

    private void UpdateFootstepRate()
    {
        if (!playerFootsteps.isValid() || !footstepsPlaying)
            return;

        float targetRate;

        // Determine playback rate based on movement state
        if (crouch)
        {
            targetRate = crouchRate; // Slower for crouching
        }
        else if (sprint)
        {
            targetRate = runRate; // Faster for sprinting
        }
        else
        {
            targetRate = walkRate; // Normal walking speed
        }

        // Set the playback rate using setPitch
        FMOD.RESULT result = playerFootsteps.setPitch(targetRate);
        
        if (result == FMOD.RESULT.OK)
        {
            string state = crouch ? "Crouch" : (sprint ? "Sprint" : "Walk");
            Debug.Log($"[PlayerMotor] Footstep rate set to {targetRate} ({state})");
        }
        else
        {
            Debug.LogWarning($"[PlayerMotor] Failed to set footstep rate: {result}");
        }
    }

    private void OnDestroy()
    {
        // Clean up FMOD event instance
        if (playerFootsteps.isValid())
        {
            playerFootsteps.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            playerFootsteps.release();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && footstepsPlaying)
        {
            StopFootsteps();
        }
    }
}
