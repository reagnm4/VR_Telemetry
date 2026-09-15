using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Prototype head-relative controls, operated with existing XR UI rays.</summary>
public class ObjectApproachPanel : MonoBehaviour
{
    public ObjectApproachController task;
    public Transform hmd;
    private Text status;
    private Button begin, next, complete, stop;

    private void Start()
    {
        if (task == null || hmd == null) { enabled = false; return; }
        var go = new GameObject("Approach Controls", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster), typeof(GraphicRaycaster));
        go.transform.SetParent(hmd, false);
        go.transform.localPosition = new Vector3(0, -0.65f, 1.1f);
        go.transform.localScale = Vector3.one * 0.001f;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(760, 360);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = hmd.GetComponent<Camera>();
        canvas.sortingOrder = 10;
        var bg = go.AddComponent<Image>(); bg.color = new Color(0.035f, 0.05f, 0.075f, 0.94f);
        status = Label(rect, "Status", new Vector2(0, 90), new Vector2(710, 155), 25);
        begin = MakeButton(rect, "Begin session", new Vector2(-180, -25), task.BeginSession);
        next = MakeButton(rect, "Ready / next trial", new Vector2(180, -25), task.NextTrial);
        complete = MakeButton(rect, "I have finished", new Vector2(-180, -105), task.CompleteTrial);
        stop = MakeButton(rect, "STOP AND SAVE", new Vector2(180, -105), task.StopTask);
        stop.GetComponent<Image>().color = new Color(0.65f, 0.12f, 0.12f);
    }

    private static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = pos;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    private static Button MakeButton(Transform parent, string caption, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(335, 65); rect.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.10f, 0.25f, 0.36f);
        var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
        Label(rect, "Label", Vector2.zero, rect.sizeDelta, 24).text = caption;
        return button;
    }
    private void Update()
    {
        if (status == null) return;
        var s = task.Sequence;
        bool recording = task.session.IsActive;
        begin.interactable = task.CanBegin;
        next.interactable = recording && s != null && s.State == TrialSequence.Phase.Ready;
        complete.interactable = recording && s != null && s.State == TrialSequence.Phase.Active;
        stop.interactable = recording || task.session.HasPendingExport;
        string line = "Check tracking and floor height.\nWhen comfortable, begin the session.";
        if (s != null)
        {
            int remaining = Mathf.CeilToInt((float)System.Math.Max(0, s.Deadline - task.session.ElapsedSeconds));
            switch (s.State)
            {
                case TrialSequence.Phase.Ready: line = "Return to the start marker at your own pace.\nSelect Ready when prepared."; break;
                case TrialSequence.Phase.Countdown: line = $"Trial {s.Trial}/{s.TrialCount}: starting in {remaining}\nWait for the target."; break;
                case TrialSequence.Phase.Active: line = $"Trial {s.Trial}/{s.TrialCount}: {remaining}s remaining\nApproach and look at the target. Confirm when finished."; break;
                case TrialSequence.Phase.Rest: line = $"Trial ended: {s.LastOutcome.Replace('_', ' ')}\nRest: at least {remaining}s. Next trial starts only when ready."; break;
                case TrialSequence.Phase.Finished: line = "Trials finished. Recording saved.\nYou may end Play Mode."; break;
                case TrialSequence.Phase.Stopped: line = "Session stopped. Recording saved."; break;
            }
        }
        if (recording) line = "RECORDING  •  " + line;
        if (task.session.HasPendingExport) line = "SAVE INCOMPLETE — select STOP AND SAVE to retry.";
        if (!string.IsNullOrEmpty(task.LastError)) line = "Action failed: " + task.LastError;
        status.text = line;
    }
}
