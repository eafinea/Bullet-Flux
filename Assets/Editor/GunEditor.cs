using UnityEditor;

[CustomEditor(typeof(Gun))]
public class GunEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Gun Settings Section
        EditorGUILayout.LabelField("Gun Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gunStats"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shootingSystem"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSpawnPoint"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("impactParticleSystem"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletTrail"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shootDelay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hitLayerMask"));

        EditorGUILayout.Space();

        // Bullet Effects Section
        EditorGUILayout.LabelField("Bullet Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletEffects"));

        serializedObject.ApplyModifiedProperties();

    }
}