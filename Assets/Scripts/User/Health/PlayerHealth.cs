using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float health;

    [Header("Health Bar")]
    public float chipSpeed = 2f;
    public Image frontHealthBar;
    public Image backHealthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Damage Overlay")]
    public Image damageOverlay;
    public float duration;     // How long the overlay stays fully opaque
    public float fadeSpeed;    // How fast it fades out
    private float durationTimer;

    [Header("Progress Management")]
    [SerializeField] private bool clearProgressOnDeath = true;

    private float lerpTimer;

    // Events
    public static event Action<float> OnHealthChanged;
    public static event Action OnPlayerDied;

    private bool isDead = false;

    void Start()
    {
        health = maxHealth;
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(
                damageOverlay.color.r,
                damageOverlay.color.g,
                damageOverlay.color.b,
                0f
            );
            damageOverlay.gameObject.SetActive(false);
        }
        Debug.Log("PlayerHealth initialized with health: " + health);
    }

    void Update()
    {
        // clamp and update visuals every frame
        health = Mathf.Clamp(health, 0f, maxHealth);
        UpdateHealthUI();

        // 1) Death check always first
        if (health <= 0f && !isDead)
        {
            HandleDeath();
            return; // nothing else matters once dead
        }

        // 2) Overlay fade logic
        if (damageOverlay != null && damageOverlay.color.a > 0f)
        {
            // only bail out if alive but low on health
            if (health > 0f && health <= 30f)
                return;

            durationTimer += Time.deltaTime;
            if (durationTimer > duration)
            {
                float alpha = damageOverlay.color.a - Time.deltaTime * fadeSpeed;
                damageOverlay.color = new Color(
                    damageOverlay.color.r,
                    damageOverlay.color.g,
                    damageOverlay.color.b,
                    alpha
                );
            }

            if (damageOverlay.color.a <= 0f)
            {
                damageOverlay.gameObject.SetActive(false);
            }
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        Debug.Log("=== PLAYER DEATH DETECTED ===");
        
        // Clear progress on death if enabled
        if (clearProgressOnDeath && GameProgressManager.Instance != null)
        {
            Debug.Log("Clearing progress due to player death...");
            GameProgressManager.Instance.ClearProgress();
        }
        
        Debug.Log("Triggering death event...");
        OnPlayerDied?.Invoke();
        Debug.Log("Death event invoked!");

        // primary fallback to PauseMenuManager singleton
        if (PauseMenuManager.Instance != null)
        {
            Debug.Log("Calling PauseMenuManager directly as fallback...");
            PauseMenuManager.Instance.TriggerGameOver();
            return;
        }

        // last‐ditch: search scene for it
        var pauseManager = FindFirstObjectByType<PauseMenuManager>();
        if (pauseManager != null)
        {
            Debug.Log("Found PauseMenuManager manually, triggering game over...");
            pauseManager.TriggerGameOver();
        }
        else
        {
            Debug.LogError("Could not find PauseMenuManager at all!");
        }
    }

    [ContextMenu("Test Player Death")]
    public void TestPlayerDeath()
    {
        Debug.Log("TestPlayerDeath called manually");
        if (!isDead)
        {
            health = 0f;
            HandleDeath();
        }
        else
        {
            Debug.Log("Player is already dead – resetting.");
            ResetHealth();
        }
    }

    [ContextMenu("Set Health to Zero")]
    public void SetHealthToZero()
    {
        Debug.Log("Setting health to zero for testing");
        health = 0f;
    }

    public void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"{health:F0} / {maxHealth:F0}";

        if (frontHealthBar != null && backHealthBar != null)
        {
            float fillFront = frontHealthBar.fillAmount;
            float fillBack = backHealthBar.fillAmount;
            float targetPct = health / maxHealth;
            lerpTimer += Time.deltaTime;
            float t = Mathf.Pow(lerpTimer / chipSpeed, 2f);

            if (fillBack > targetPct)
            {
                frontHealthBar.fillAmount = targetPct;
                backHealthBar.color = Color.red;
                backHealthBar.fillAmount = Mathf.Lerp(fillBack, targetPct, t);
            }
            else if (fillFront < targetPct)
            {
                backHealthBar.color = Color.green;
                backHealthBar.fillAmount = targetPct;
                frontHealthBar.fillAmount = Mathf.Lerp(fillFront, backHealthBar.fillAmount, t);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (damageOverlay != null)
        {
            damageOverlay.gameObject.SetActive(true);
            damageOverlay.color = new Color(
                damageOverlay.color.r,
                damageOverlay.color.g,
                damageOverlay.color.b,
                1f
            );
            durationTimer = 0f;
        }

        health -= damage;
        lerpTimer = 0f;
        OnHealthChanged?.Invoke(health);
        Debug.Log($"Player took {damage} damage. Current health: {health}");
    }

    public void HealDamage(float healAmount)
    {
        if (isDead) return;

        health += healAmount;
        lerpTimer = 0f;
        OnHealthChanged?.Invoke(health);
        Debug.Log($"Player healed {healAmount}. Current health: {health}");
    }

    public void ResetHealth()
    {
        health = maxHealth;
        isDead = false;
        lerpTimer = durationTimer = 0f;

        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(
                damageOverlay.color.r,
                damageOverlay.color.g,
                damageOverlay.color.b,
                0f
            );
            damageOverlay.gameObject.SetActive(false);
        }

        OnHealthChanged?.Invoke(health);
        Debug.Log("PlayerHealth reset to: " + health);
    }

    // Public read‐only properties
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
}
