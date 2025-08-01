using UnityEditor;

[CustomEditor(typeof(GunStats))]
public class GunStatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Weapon Type Selection
        EditorGUILayout.LabelField("Weapon Configuration", EditorStyles.boldLabel);
        SerializedProperty weaponTypeProp = serializedObject.FindProperty("weaponType");
        EditorGUILayout.PropertyField(weaponTypeProp);
        
        GunStats.WeaponType weaponType = (GunStats.WeaponType)weaponTypeProp.enumValueIndex;

        // Basic Stats (always shown)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fireRate"));

        // Weapon-specific sections
        ShowWeaponSpecificUI(weaponType);
        ShowShootingModeUI(weaponType);

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowWeaponSpecificUI(GunStats.WeaponType weaponType)
    {
        EditorGUILayout.Space();

        switch (weaponType)
        {
            case GunStats.WeaponType.Pistol:
                ShowOverheatUI();
                break;

            case GunStats.WeaponType.Shotgun:
                ShowFiniteShotsUI();
                ShowShotgunUI();
                break;

            case GunStats.WeaponType.SMG:
                ShowFiniteShotsUI();
                break;
        }
    }

    private void ShowOverheatUI()
    {
        EditorGUILayout.LabelField("Overheat System", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHeat"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentHeat"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heatPerShot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldownRate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("overheatedCooldownTime"));
        
        EditorGUILayout.HelpBox("Pistol has infinite ammo but overheats with continuous use.", MessageType.Info);
        
        // Heat bar visualization
        float heatPercent = serializedObject.FindProperty("currentHeat").floatValue / 
                           serializedObject.FindProperty("maxHeat").floatValue;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), heatPercent, $"Heat: {heatPercent:P0}");
    }

    private void ShowFiniteShotsUI()
    {
        EditorGUILayout.LabelField("Finite Shot System", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("totalShots"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("remainingShots"));
        EditorGUILayout.HelpBox("Weapon has finite shots. No reloading - only ammo pickups.", MessageType.Info);
    }

    private void ShowShotgunUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shotgun Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shotgunPellets"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fireSpread"));
    }

    private void ShowShootingModeUI(GunStats.WeaponType weaponType)
    {
        EditorGUILayout.Space();
        
        SerializedProperty shootingModeProp = serializedObject.FindProperty("shootingMode");

        // Restrict modes based on weapon type
        switch (weaponType)
        {
            case GunStats.WeaponType.Shotgun:
                // Only allow SemiAuto for shotguns
                int mode = shootingModeProp.enumValueIndex;
                if (mode != 0) mode = 0;
                string[] shotgunModes = new[] {"SemiAuto"};
                int newMode = EditorGUILayout.Popup("Shooting Mode", mode, shotgunModes);
                if (newMode != mode)
                    shootingModeProp.enumValueIndex = newMode;
                EditorGUILayout.HelpBox("Shotgun: Only Semi-Auto available.", MessageType.Info);
                break;

            case GunStats.WeaponType.SMG:
                // SMG typically allows all modes
                EditorGUILayout.PropertyField(shootingModeProp);
                EditorGUILayout.HelpBox("SMG: All firing modes available.", MessageType.Info);
                if (shootingModeProp.enumValueIndex == 2) // Burst
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("burstCooldown"));
                }
                break;

            case GunStats.WeaponType.Pistol:
                EditorGUILayout.PropertyField(shootingModeProp);
                EditorGUILayout.HelpBox("Pistol: All firing modes available.", MessageType.Info);
                if (shootingModeProp.enumValueIndex == 2) // Burst
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("burstCooldown"));
                }
                break;
        }
    }
}