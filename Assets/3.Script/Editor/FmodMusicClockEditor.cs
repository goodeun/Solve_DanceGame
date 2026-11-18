using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FmodMusicClock))]
public sealed class FmodMusicClockEditor : Editor
{
    private double nextRepaintTime;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live FMOD", EditorStyles.boldLabel);
            FmodMusicClock clock = (FmodMusicClock)target;
            EditorGUILayout.LabelField("Current BPM", $"{clock.BPM:0.##}");
            EditorGUILayout.LabelField("Bar", clock.CurrentBar.ToString());
            EditorGUILayout.LabelField("Beat", clock.CurrentBeat.ToString());

            if (EditorApplication.timeSinceStartup >= nextRepaintTime)
            {
                nextRepaintTime = EditorApplication.timeSinceStartup + 0.1d;
                Repaint();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Live BPM is shown while the game is running.", MessageType.Info);
        }
    }
}
