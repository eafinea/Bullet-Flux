using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Death Effects")]
    [SerializeField] private ParticleSystem deathParticles;
    
    [Header("Death Settings")]
    [SerializeField] private float destroyDelay = 2f;
    
    [Header("Damage Flash Effect")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private bool useEmissionFlash = true;
    [SerializeField] private float emissionIntensity = 2f;

    private Animator animator;
    private bool isDead = false;
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Color[] originalColors;
    private Color[] originalEmissionColors;
    private bool[] hasEmission;
    private Coroutine flashCoroutine;

    public System.Action<EnemyHealth> OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        SetupFlashMaterials();
    }

    private void SetupFlashMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return;
        }

        originalMaterials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];
        originalEmissionColors = new Color[renderers.Length];
        hasEmission = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                originalMaterials[i] = renderers[i].material;
                
                if (renderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = renderers[i].material.color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }
                
                if (renderers[i].material.HasProperty("_EmissionColor"))
                {
                    hasEmission[i] = true;
                    originalEmissionColors[i] = renderers[i].material.GetColor("_EmissionColor");
                }
                else
                {
                    hasEmission[i] = false;
                    originalEmissionColors[i] = Color.black;
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;
        
        TriggerDamageFlash();
        
        if (currentHealth <= 0f) 
        {
            Die();
        }
    }

    private void TriggerDamageFlash()
    {
        if (renderers == null || renderers.Length == 0) return;
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashEffect());
    }

    private IEnumerator FlashEffect()
    {
        ApplyFlash(true);
        yield return new WaitForSeconds(flashDuration);
        ApplyFlash(false);
        flashCoroutine = null;
    }

    private void ApplyFlash(bool isFlashing)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].material == null) continue;

            if (isFlashing)
            {
                if (renderers[i].material.HasProperty("_Color"))
                {
                    renderers[i].material.color = flashColor;
                }

                if (useEmissionFlash && hasEmission[i])
                {
                    Color emissionColor = flashColor * emissionIntensity;
                    renderers[i].material.SetColor("_EmissionColor", emissionColor);
                    renderers[i].material.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                if (renderers[i].material.HasProperty("_Color"))
                {
                    renderers[i].material.color = originalColors[i];
                }

                if (useEmissionFlash && hasEmission[i])
                {
                    renderers[i].material.SetColor("_EmissionColor", originalEmissionColors[i]);
                    
                    if (originalEmissionColors[i] == Color.black)
                    {
                        renderers[i].material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        
        ApplyFlash(false);
        
        OnDeath?.Invoke(this);
        animator?.SetTrigger("isDead");

        DisableAIComponents();
        
        if (deathParticles != null)
        {
            deathParticles.Play();
        }
        
        TryDropItems();
        
        StartCoroutine(DestroyAfterDelay());
    }

    private void DisableAIComponents()
    {
        var navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }
        
        var standardAI = GetComponent<StandardAI>();
        if (standardAI != null)
        {
            standardAI.enabled = false;
        }
        
        var droneAI = GetComponent<DroneAI>();
        if (droneAI != null)
        {
            droneAI.enabled = false;
        }
        
        var armouredAI = GetComponent<ArmouredAI>();
        if (armouredAI != null)
        {
            armouredAI.enabled = false;
        }
        
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void TryDropItems()
    {
        var enemyDropper = GetComponent<EnemyDropper>();
        if (enemyDropper != null)
        {
            enemyDropper.TryDrop();
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercentage => currentHealth / maxHealth;

    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    public void TestFlash()
    {
        TriggerDamageFlash();
    }

    private void OnDestroy()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
    }
}
