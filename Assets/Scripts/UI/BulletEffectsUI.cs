using UnityEngine;
using UnityEngine.UI;

public class BulletEffectsUI : MonoBehaviour
{
    [Header("Effect Icons")]
    public Image bounceIcon;
    public Image dotIcon;
    public Image slowIcon;

    [Header("Weapon Manager Reference")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Transparency Settings")]
    [SerializeField] private float inactiveAlpha = 100f / 255f; // 100 out of 255
    [SerializeField] private float activeAlpha = 225f / 255f;   // 225 out of 255

    // Current bullet effects reference
    private BulletEffects currentBulletEffects;
    private BulletEffectType lastKnownEffects = BulletEffectType.None;

    private void Awake()
    {
        // Initialize icons to inactive transparency
        UpdateEffectUI(BulletEffectType.None);
    }

    private void Start()
    {
        // Get WeaponManager if not assigned
        if (weaponManager == null)
        {
            weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager == null)
            {
                Debug.LogWarning("WeaponManager not found! Bullet effects UI may not work correctly.");
                return;
            }
        }

        // Subscribe to weapon change events
        weaponManager.OnBulletEffectsChanged += OnBulletEffectsChanged;
        
        // Force initial update - use a small delay to ensure WeaponManager is fully initialized
        Invoke(nameof(ForceInitialUpdate), 0.1f);
    }

    private void ForceInitialUpdate()
    {
        // Get current bullet effects and force update
        currentBulletEffects = weaponManager.GetBulletEffects();
        if (currentBulletEffects != null)
        {
            OnBulletEffectsChanged(currentBulletEffects);
            // Force UI update regardless of last known state
            lastKnownEffects = BulletEffectType.None;
            UpdateEffectUI(currentBulletEffects.CurrentEffects);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (weaponManager != null)
        {
            weaponManager.OnBulletEffectsChanged -= OnBulletEffectsChanged;
        }
    }

    private void OnBulletEffectsChanged(BulletEffects newBulletEffects)
    {
        currentBulletEffects = newBulletEffects;
        lastKnownEffects = BulletEffectType.None; // Force UI update
        
        if (currentBulletEffects != null)
        {
            UpdateEffectUI(currentBulletEffects.CurrentEffects);
            Debug.Log($"BulletEffectsUI updated - Effects: {currentBulletEffects.CurrentEffects}");
        }
    }

    private void Update()
    {
        // Only update if we have valid bullet effects reference
        if (currentBulletEffects == null) return;

        // Get current effects from the centralized bullet effects
        BulletEffectType currentEffects = currentBulletEffects.CurrentEffects;
            
        // Only update UI if effects have changed
        if (currentEffects != lastKnownEffects)
        {
            UpdateEffectUI(currentEffects);
            lastKnownEffects = currentEffects;
        }
    }

    public void UpdateEffectUI(BulletEffectType activeEffects)
    {
        // Check which effects are active
        bool hasBounce = (activeEffects & BulletEffectType.Bounce) == BulletEffectType.Bounce;
        bool hasDOT = (activeEffects & BulletEffectType.DamageOverTime) == BulletEffectType.DamageOverTime;
        bool hasSlow = (activeEffects & BulletEffectType.Slow) == BulletEffectType.Slow;

        // Update transparency instead of enabling/disabling
        if (bounceIcon != null)
        {
            SetIconTransparency(bounceIcon, hasBounce);
        }

        if (dotIcon != null)
        {
            SetIconTransparency(dotIcon, hasDOT);
        }

        if (slowIcon != null)
        {
            SetIconTransparency(slowIcon, hasSlow);
        }

        Debug.Log($"UI Updated - Bounce: {hasBounce}, DOT: {hasDOT}, Slow: {hasSlow}");
    }

    private void SetIconTransparency(Image icon, bool isActive)
    {
        if (icon == null) return;

        Color iconColor = icon.color;
        iconColor.a = isActive ? activeAlpha : inactiveAlpha;
        icon.color = iconColor;
    }

    // Public method to manually refresh UI (useful for debugging)
    public void RefreshUI()
    {
        ForceInitialUpdate();
    }
}
