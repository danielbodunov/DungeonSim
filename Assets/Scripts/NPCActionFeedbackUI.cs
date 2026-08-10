using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays pooled, short-lived screen-space feedback above NPC actions.</summary>
[DisallowMultipleComponent]
public sealed class NPCActionFeedbackUI : MonoBehaviour
{
    sealed class Popup
    {
        public GameObject GameObject;
        public RectTransform Rect;
        public CanvasGroup CanvasGroup;
        public TMP_Text Text;
    }

    [SerializeField, Min(0.1f)] float lifetime = 0.9f;
    [SerializeField, Min(0f)] float riseDistance = 75f;
    [SerializeField, Min(1)] int initialPoolSize = 8;

    readonly Queue<Popup> available = new();
    readonly List<Popup> allPopups = new();
    RectTransform canvasRect;
    int popupSequence;

    void Awake()
    {
        BuildCanvas();
        for (int i = 0; i < initialPoolSize; i++)
            available.Enqueue(CreatePopup());
    }

    void OnEnable()
    {
        NPCActionResolver.ActionResolved += ShowResult;
    }

    void OnDisable()
    {
        NPCActionResolver.ActionResolved -= ShowResult;
        StopAllCoroutines();
        available.Clear();
        foreach (Popup popup in allPopups)
        {
            popup.GameObject.SetActive(false);
            available.Enqueue(popup);
        }
    }

    void BuildCanvas()
    {
        GameObject canvasObject = new("NPC Action Feedback", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasObject.GetComponent<RectTransform>();
    }

    Popup CreatePopup()
    {
        GameObject instance = new("NPC Action Popup", typeof(RectTransform));
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.SetParent(canvasRect, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 60f);

        CanvasGroup group = instance.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        TextMeshProUGUI text = instance.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(10, 10, 18, 220);
        instance.SetActive(false);

        var popup = new Popup
        {
            GameObject = instance,
            Rect = rect,
            CanvasGroup = group,
            Text = text
        };
        allPopups.Add(popup);
        return popup;
    }

    void ShowResult(NPCActionResult result)
    {
        Popup popup = available.Count > 0 ? available.Dequeue() : CreatePopup();
        popup.Text.text = BuildText(result);
        popup.Text.color = GetColor(result.Outcome);
        popup.CanvasGroup.alpha = 1f;

        Camera camera = Camera.main;
        Vector2 screenPosition = camera != null
            ? RectTransformUtility.WorldToScreenPoint(camera, result.WorldPosition)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, null, out Vector2 localPosition);
        localPosition.x += (popupSequence++ % 3 - 1) * 28f;
        popup.Rect.anchoredPosition = localPosition;
        popup.GameObject.SetActive(true);
        StartCoroutine(AnimatePopup(popup, localPosition));
    }

    IEnumerator AnimatePopup(Popup popup, Vector2 start)
    {
        float duration = Mathf.Max(0.1f, lifetime);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            popup.Rect.anchoredPosition = start + Vector2.up * (riseDistance * progress);
            popup.CanvasGroup.alpha = 1f - progress;
            yield return null;
        }

        popup.GameObject.SetActive(false);
        available.Enqueue(popup);
    }

    static string BuildText(NPCActionResult result)
    {
        return result.Outcome switch
        {
            NPCActionOutcome.Dodged => "DODGED",
            NPCActionOutcome.Defeated => $"DEFEATED  -{result.AppliedDamage}",
            _ => $"-{result.AppliedDamage}"
        };
    }

    static Color GetColor(NPCActionOutcome outcome)
    {
        return outcome switch
        {
            NPCActionOutcome.Dodged => new Color(0.35f, 0.95f, 0.72f, 1f),
            NPCActionOutcome.Defeated => new Color(1f, 0.28f, 0.24f, 1f),
            _ => new Color(1f, 0.72f, 0.25f, 1f)
        };
    }
}
