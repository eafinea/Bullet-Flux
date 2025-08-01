using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerHealth : MonoBehaviour, IDamageable
{
    private float health;
    private float lerpTimer;
    [Header("Health Bar")]
    public float maxHealth = 100f;
    public float chipSpeed = 2f;

    public Image frontHealthBar;
    public Image backHealthBar;
    [SerializeField]
    private TextMeshProUGUI healthText;

    [Header("Damage Overlay")]
    public Image damageOverlay;
    public float duration; // Duration of the damage overlay stay opaque
    public float fadeSpeed; // Speed at which the overlay fades out
    private float durationTimer; // Timer to track how long the overlay has been visible

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        damageOverlay.color = new Color(damageOverlay.color.r, damageOverlay.color.g, damageOverlay.color.b, 0);
    }

    // Update is called once per frame
    void Update()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
        if (damageOverlay.color.a > 0f)
        {
            if (health <= 30)
            {
                return;
            }
            durationTimer += Time.deltaTime;
            if(durationTimer > duration)
            {
                // Fade the Image
                float tempAlpha = damageOverlay.color.a;
                tempAlpha -= Time.deltaTime * fadeSpeed;
                damageOverlay.color = new Color(damageOverlay.color.r, damageOverlay.color.g, damageOverlay.color.b, tempAlpha);
            }
            // Disable overlay when fully faded
            if (damageOverlay.color.a <= 0f)
            {
                damageOverlay.gameObject.SetActive(false);
            }
        }
    }
    public void UpdateHealthUI()
    {
        healthText.text = health.ToString() + " / " + maxHealth.ToString();
        float fillFront = frontHealthBar.fillAmount;
        float fillBack = backHealthBar.fillAmount;
        float healthPercentage = health / maxHealth;
        float percentageComplete = lerpTimer / chipSpeed;
        percentageComplete *= percentageComplete; // Squaring the percentage for a smoother transition

        if (fillBack > healthPercentage)
        {
            frontHealthBar.fillAmount = healthPercentage;
            backHealthBar.color = Color.red;
            lerpTimer += Time.deltaTime;
            backHealthBar.fillAmount = Mathf.Lerp(fillBack, healthPercentage, percentageComplete);
        }
        if (fillFront < healthPercentage)
        {
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = healthPercentage;
            lerpTimer += Time.deltaTime;
            frontHealthBar.fillAmount = Mathf.Lerp(fillFront, backHealthBar.fillAmount, percentageComplete);
        }
    }
    public void TakeDamage(float damage)
    {
        damageOverlay.gameObject.SetActive(true);
        health -= damage;
        lerpTimer = 0f;
        durationTimer = 0f;
        damageOverlay.color = new Color(damageOverlay.color.r, damageOverlay.color.g, damageOverlay.color.b, 1);
        Debug.Log("Current Health: " + health);

    }
    public void HealDamge(float healAmount)
    {
        health += healAmount;
        lerpTimer = 0f;
        Debug.Log("Current Health: " + health);
    }
}
