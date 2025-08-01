using UnityEngine;

public class HeatMeterController : MonoBehaviour
{
    [Header("Heat Meter References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Renderer heatMeterRenderer;

    [Header("Heat Color Settings")]
    [SerializeField] private Color coolColor = Color.green;
    [SerializeField] private Color warmColor = Color.yellow;
    [SerializeField] private Color hotColor = new Color(1f, 0.75f, 0.58f, 1f);
    [SerializeField] private Color overheatedColor = Color.red;

    [Header("Heat Thresholds")]
    [SerializeField] private float warmThreshold = 0.3f;
    [SerializeField] private float hotThreshold = 0.6f;
    [SerializeField] private float overheatedThreshold = 0.9f;

    [Header("Material Settings")]
    [SerializeField] private string materialColorProperty = "_BaseColor";
    [SerializeField] private string albedoColorProperty = "_MainColor";
    [SerializeField] private bool useEmissiveColor = true;
    [SerializeField] private string emissiveColorProperty = "_EmissionColor";
    [SerializeField] private float emissiveIntensity = 0.5f;
    [SerializeField] private bool forceAlbedoColor = true;

    [Header("Debug Options")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool testModeActive = false;

    private Material heatMeterMaterial;
    private GunStats currentPistolStats;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeHeatMeter();
        SubscribeToWeaponEvents();
    }

    private void InitializeHeatMeter()
    {
        if (weaponManager == null)
        {
            weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager == null)
            {
                return;
            }
        }

        if (heatMeterRenderer == null)
        {
            GameObject heatMeterObject = GameObject.FindGameObjectWithTag("Heat_Meter");
            if (heatMeterObject != null)
            {
                heatMeterRenderer = heatMeterObject.GetComponent<Renderer>();
                if (heatMeterRenderer == null)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (heatMeterRenderer != null)
        {
            heatMeterMaterial = new Material(heatMeterRenderer.material);
            heatMeterRenderer.material = heatMeterMaterial;

            if (useEmissiveColor)
            {
                heatMeterMaterial.EnableKeyword("_EMISSION");
            }

            if (debugMode)
            {
                var shader = heatMeterMaterial.shader;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var propName = shader.GetPropertyName(i);
                    var propType = shader.GetPropertyType(i);
                }
            }
        }

        isInitialized = true;
    }

    private void SubscribeToWeaponEvents()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched += OnWeaponChanged;
            OnWeaponChanged(weaponManager.CurrentGun);
        }
    }

    private void OnWeaponChanged(Gun newGun)
    {
        if (newGun != null)
        {
            var gunStats = newGun.GetComponent<GunStats>();
            if (gunStats != null && gunStats.CurrentWeaponType == GunStats.WeaponType.Pistol)
            {
                currentPistolStats = gunStats;
                testModeActive = false;
            }
            else
            {
                currentPistolStats = null;
                testModeActive = false;
                if (isInitialized)
                {
                    UpdateHeatMeterColor(0f);
                }
            }
        }
    }

    private void Update()
    {
        if (isInitialized && currentPistolStats != null && !testModeActive)
        {
            UpdateHeatMeter();
        }
    }

    private void UpdateHeatMeter()
    {
        if (currentPistolStats == null || heatMeterMaterial == null)
            return;

        float heatPercentage = currentPistolStats.CurrentHeat / currentPistolStats.MaxHeat;
        UpdateHeatMeterColor(heatPercentage);
    }

    private void UpdateHeatMeterColor(float heatPercentage)
    {
        if (heatMeterMaterial == null)
            return;

        Color targetColor = GetHeatColor(heatPercentage);
        bool colorSet = false;

        if (heatMeterMaterial.HasProperty(materialColorProperty))
        {
            heatMeterMaterial.SetColor(materialColorProperty, targetColor);
            colorSet = true;
        }

        if (forceAlbedoColor && heatMeterMaterial.HasProperty(albedoColorProperty))
        {
            heatMeterMaterial.SetColor(albedoColorProperty, targetColor);
            colorSet = true;
        }

        string[] commonProperties = { "_BaseColor", "_MainColor", "_Albedo", "_Tint" };
        foreach (string prop in commonProperties)
        {
            if (heatMeterMaterial.HasProperty(prop))
            {
                heatMeterMaterial.SetColor(prop, targetColor);
                colorSet = true;
            }
        }

        if (useEmissiveColor && heatMeterMaterial.HasProperty(emissiveColorProperty))
        {
            Color emissiveColor = targetColor * emissiveIntensity;
            heatMeterMaterial.SetColor(emissiveColorProperty, emissiveColor);
        }
    }

    private Color GetHeatColor(float heatPercentage)
    {
        heatPercentage = Mathf.Clamp01(heatPercentage);

        if (heatPercentage >= overheatedThreshold)
        {
            return overheatedColor;
        }
        else if (heatPercentage >= hotThreshold)
        {
            float t = (heatPercentage - hotThreshold) / (overheatedThreshold - hotThreshold);
            return Color.Lerp(hotColor, overheatedColor, t);
        }
        else if (heatPercentage >= warmThreshold)
        {
            float t = (heatPercentage - warmThreshold) / (hotThreshold - warmThreshold);
            return Color.Lerp(warmColor, hotColor, t);
        }
        else
        {
            float t = heatPercentage / warmThreshold;
            return Color.Lerp(coolColor, warmColor, t);
        }
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched -= OnWeaponChanged;
        }

        if (heatMeterMaterial != null)
        {
            Destroy(heatMeterMaterial);
        }
    }

    public void SetHeatMeterColor(Color color)
    {
        if (heatMeterMaterial != null)
        {
            if (heatMeterMaterial.HasProperty(materialColorProperty))
                heatMeterMaterial.SetColor(materialColorProperty, color);

            if (forceAlbedoColor && heatMeterMaterial.HasProperty(albedoColorProperty))
                heatMeterMaterial.SetColor(albedoColorProperty, color);

            if (useEmissiveColor && heatMeterMaterial.HasProperty(emissiveColorProperty))
            {
                Color emissiveColor = color * emissiveIntensity;
                heatMeterMaterial.SetColor(emissiveColorProperty, emissiveColor);
            }
        }
    }

    public void TestHeatLevel(float testHeatPercentage)
    {
        testModeActive = true;
        UpdateHeatMeterColor(testHeatPercentage);
        Invoke(nameof(ExitTestMode), 3f);
    }

    private void ExitTestMode()
    {
        testModeActive = false;
    }

    [ContextMenu("Debug Material Properties")]
    public void DebugMaterialProperties()
    {
        if (heatMeterMaterial == null)
        {
            return;
        }

        var shader = heatMeterMaterial.shader;

        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            var propName = shader.GetPropertyName(i);
            var propType = shader.GetPropertyType(i);
            
            if (propType == UnityEngine.Rendering.ShaderPropertyType.Color)
            {
                Color currentColor = heatMeterMaterial.GetColor(propName);
            }
        }
    }

    public float CurrentHeatPercentage => currentPistolStats != null ?
        (currentPistolStats.CurrentHeat / currentPistolStats.MaxHeat) : 0f;

    public Color CurrentHeatColor => GetHeatColor(CurrentHeatPercentage);

    public bool IsTrackingPistol => currentPistolStats != null;
}