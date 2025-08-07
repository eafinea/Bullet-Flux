using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Weapon Sounds")]
    [field: SerializeField] public EventReference pistolShoot { get; private set; }
    [field: SerializeField] public EventReference pistolOverheat { get; private set; }
    [field: SerializeField] public EventReference shotgunShoot { get; private set; }
    [field: SerializeField] public EventReference smgShoot { get; private set; }
    [field: SerializeField] public EventReference pickupSound { get; private set; }

    [field: Header("Player Sounds")]
    [field: SerializeField] public EventReference playerFootsteps { get; private set; }
    [field: SerializeField] public EventReference playerJump { get; private set; }

    [field: Header("Enemy Sounds")]
    [field: SerializeField] public EventReference enemyFootsteps { get; private set; }
    [field: SerializeField] public EventReference enemyFlying { get; private set; }

    [field: Header("UI Sounds")]
    [field: SerializeField] public EventReference UIButtonClick { get; private set; }
    [field: SerializeField] public EventReference UIButtonHover { get; private set; }
    [field: SerializeField] public EventReference UIMusic { get; private set; }

    [field: Header("Rest Point Sounds")]
    [field: SerializeField] public EventReference restPointActivate { get; private set; }
    [field: SerializeField] public EventReference restPointMusic { get; private set; }
    public static FMODEvents instance { get; private set; }
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("There is more than one FMODEvents instance in the scene.");
        }
        instance = this;
    }
}
