#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit batch-only Play Mode smoke test; never runs during normal editing.</summary>
[InitializeOnLoad]
public static class ObjectApproachSmokeCheck
{
    private const string Key = "VR.ObjectApproachSmoke";
    static ObjectApproachSmokeCheck()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Step;
    }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run smoke check in batch mode only.");
        EditorSceneManager.OpenScene("Assets/Scenes/ObjectApproachRoom.unity");
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + ".stage", 0);
        SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup);
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
        EditorApplication.isPlaying = true;
    }
    private static void Press(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) throw new Exception("Missing UI button: " + name);
        var button = go.GetComponent<Button>();
        if (!button.interactable) throw new Exception("Button not interactable: " + name);
        button.onClick.Invoke();
    }
    private static bool Ready(string name)
    {
        var go = GameObject.Find(name);
        return go != null && go.GetComponent<Button>().interactable;
    }
    private static void Step()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".start", 0) > 90)
                throw new Exception("Smoke test timed out");
            if (!EditorApplication.isPlaying) return;
            var task = UnityEngine.Object.FindAnyObjectByType<ObjectApproachController>();
            if (task == null || GameObject.Find("Approach Controls") == null) return;
            if (!string.IsNullOrEmpty(task.LastError)) throw new Exception(task.LastError);
            int stage = SessionState.GetInt(Key + ".stage", 0);
            if (stage == 0)
            {
                task.numberOfTrials = 2; task.countdownSeconds = 0.1f;
                task.trialTimeoutSeconds = 5f; task.minimumRestSeconds = 0;
                task.session.participantId = "SYNTHETIC";
                task.session.condition = "synthetic_editor_smoke";
                Press("Begin session");
                SessionState.SetInt(Key + ".stage", 1);
            }
            else if (stage == 1 && task.Sequence.State == TrialSequence.Phase.Ready && Ready("Ready / next trial"))
            {
                Press("Ready / next trial"); SessionState.SetInt(Key + ".stage", 2);
            }
            else if (stage == 2 && task.Sequence.State == TrialSequence.Phase.Active && Ready("I have finished"))
            {
                if (!task.target.activeSelf) throw new Exception("Target did not appear");
                Press("I have finished"); SessionState.SetInt(Key + ".stage", 3);
            }
            else if (stage == 3 && task.Sequence.State == TrialSequence.Phase.Ready && Ready("Ready / next trial"))
            {
                if (task.target.activeSelf) throw new Exception("Target did not disappear");
                Press("Ready / next trial"); SessionState.SetInt(Key + ".stage", 4);
            }
            else if (stage == 4 && task.Sequence.State == TrialSequence.Phase.Finished)
            {
                if (task.session.IsActive || task.session.HasPendingExport || task.target.activeSelf)
                    throw new Exception("Session or target did not finish cleanly");
                string folder = task.session.LastSessionFolder;
                string csv = File.ReadAllText(Path.Combine(folder, "events.csv"));
                if (!csv.Contains("manual_confirmation") || !csv.Contains("timeout"))
                    throw new Exception("Expected both completion outcomes in events");
                Debug.Log("OBJECT_APPROACH_SMOKE_PASSED " + folder);
                Finish(0);
            }
        }
        catch (Exception ex) { Debug.LogException(ex); Finish(1); }
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Step;
        EditorApplication.Exit(code);
    }
}
#endif
