using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>World-space health and stamina bars displayed above an NPC.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NPCCharacter))]
public class NPCStatusBars : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] Vector3 worldOffset = new(0f, 0.38f, 0f);
    [SerializeField, Min(0.0001f)] float worldScale = 0.002f;
    [SerializeField, Min(1f)] float barWidth = 200f;
    [SerializeField, Min(1f)] float barHeight = 8f;
    [SerializeField, Min(0f)] float barSpacing = 5f;
    [SerializeField, Min(1f)] float nameHeight = 24f;
    [SerializeField, Min(0f)] float nameSpacing = 2f;

    [Header("Colors")]
    [SerializeField] Color healthColor = new(0.15f, 0.85f, 0.2f, 1f);
    [SerializeField] Color staminaColor = new(0.95f, 0.75f, 0.1f, 1f);
    [SerializeField] Color backgroundColor = new(0.04f, 0.04f, 0.04f, 0.9f);
    [SerializeField] Color nameColor = Color.white;

    NPCCharacter character;
    Camera targetCamera;
    Canvas canvas;
    Image healthFill;
    Image staminaFill;
    TMP_Text nameLabel;

    void Awake()
    {
        character = GetComponent<NPCCharacter>();
        BuildBars();
        RefreshBars(character);
    }

    void OnEnable()
    {
        if (character == null)
            character = GetComponent<NPCCharacter>();
        character.ProgressChanged += RefreshBars;
        RefreshBars(character);
    }

    void OnDisable()
    {
        if (character != null)
            character.ProgressChanged -= RefreshBars;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera != null && canvas != null)
            canvas.transform.rotation = targetCamera.transform.rotation;
    }

    void BuildBars()
    {
        var canvasObject = new GameObject("NPC Status Bars", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = worldOffset;
        canvasObject.transform.localScale = Vector3.one * worldScale;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float barsHeight = barHeight * 2f + barSpacing;
        float totalHeight = nameHeight + nameSpacing + barsHeight;
        canvasRect.sizeDelta = new Vector2(barWidth, totalHeight);

        nameLabel = CreateNameLabel(canvasRect, totalHeight * 0.5f - nameHeight * 0.5f);
        float healthY = totalHeight * 0.5f - nameHeight - nameSpacing - barHeight * 0.5f;
        healthFill = CreateBar(canvasRect, "Health", healthY, healthColor);
        staminaFill = CreateBar(canvasRect, "Stamina", healthY - barHeight - barSpacing, staminaColor);
    }

    TMP_Text CreateNameLabel(RectTransform parent, float y)
    {
        var labelObject = new GameObject("Adventurer Name", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 18f;
        label.fontStyle = FontStyles.Bold;
        label.color = nameColor;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 18f;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(barWidth, nameHeight);
        return label;
    }

    Image CreateBar(RectTransform parent, string barName, float y, Color fillColor)
    {
        Image background = CreateImage(parent, barName + " Background", backgroundColor);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = new Vector2(0f, y);
        backgroundRect.sizeDelta = new Vector2(barWidth, barHeight);

        Image fill = CreateImage(backgroundRect, barName + " Fill", fillColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one;
        fillRect.offsetMax = -Vector2.one;
        // A runtime Image without a source sprite ignores Image.fillAmount.
        // Scaling around a left-side pivot works for the generated color quad.
        fillRect.pivot = new Vector2(0f, 0.5f);
        return fill;
    }

    static Image CreateImage(Transform parent, string objectName, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void RefreshBars(NPCCharacter source)
    {
        if (source == null)
            return;
        if (nameLabel != null)
            nameLabel.text = source.CharacterName;
        if (healthFill != null)
            SetBarAmount(healthFill, source.MaxHealth > 0
                ? Mathf.Clamp01(source.CurrentHealth / (float)source.MaxHealth)
                : 0f);
        if (staminaFill != null)
            SetBarAmount(staminaFill, source.MaxStamina > 0f
                ? Mathf.Clamp01(source.CurrentStamina / source.MaxStamina)
                : 0f);
    }

    static void SetBarAmount(Image bar, float amount)
    {
        Vector3 scale = bar.rectTransform.localScale;
        scale.x = Mathf.Clamp01(amount);
        bar.rectTransform.localScale = scale;
    }
}
