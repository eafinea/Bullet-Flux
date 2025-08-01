using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HeatMeterController))]
public class HeatMeterControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        HeatMeterController heatMeter = (HeatMeterController)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Heat Meter Testing", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            // Show current status
            EditorGUILayout.LabelField($"Tracking Pistol: {(heatMeter.IsTrackingPistol ? "Yes" : "No")}");
            EditorGUILayout.LabelField($"Current Heat: {heatMeter.CurrentHeatPercentage:P1}");

            EditorGUILayout.Space();

            // Test buttons for different heat levels
            EditorGUILayout.LabelField("Test Heat Levels:", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cool (0%)"))
                heatMeter.TestHeatLevel(0f);
            if (GUILayout.Button("Warm (30%)"))
                heatMeter.TestHeatLevel(0.3f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hot (60%)"))
                heatMeter.TestHeatLevel(0.6f);
            if (GUILayout.Button("Overheated (100%)"))
                heatMeter.TestHeatLevel(1f);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Color testing
            EditorGUILayout.LabelField("Current Color:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ColorField("Heat Color", heatMeter.CurrentHeatColor);
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test heat meter functionality", MessageType.Info);
        }
    }
}