using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Creates the prototype gameplay HUD at runtime with no scene setup.</summary>
[DisallowMultipleComponent]
public class GameplayLoopUI : MonoBehaviour
{
    static readonly Color Ink = new(0.05f, 0.07f, 0.18f, 1f);
    static readonly Color Panel = new(0.86f, 0.84f, 0.87f, 0.96f);
    static readonly Color ButtonColor = new(0.16f, 0.12f, 0.29f, 1f);
    static readonly Color Accent = new(0.91f, 0.58f, 0.18f, 1f);
    static readonly Color ExpansionColor = new(0.39f, 0.72f, 0.48f, 0.97f);
    static readonly Color ExploringColor = new(0.18f, 0.22f, 0.43f, 0.97f);

    GameplayLoopController loop;
    GameSaveManager saveManager;
    TilePlacement placement;
    InputManager inputManager;
    NPCTraversal recoveryTraversal;
    GameObject expansionPalette;
    GameObject openDungeonButton;
    GameObject explorationPanel;
    Image phasePanelImage;
    TMP_Text phaseTitle;
    TMP_Text phaseDetails;
    TMP_Text explorationDetails;
    TMP_Text pauseButtonText;
    TMP_Text saveStatusText;
    Button loadLastSaveButton;
    GameObject saveMenuOverlay;
    TMP_InputField saveNameInput;
    TMP_Text menuStatusText;
    Button namedSaveButton;
    RectTransform saveListContent;
    bool wasPausedBeforeSaveMenu;
    Image removeTrapButtonImage;
    Image removeEntranceButtonImage;
    Image toggleWallButtonImage;
    TMP_Text trapCandidateText;
    readonly Dictionary<float, Image> speedButtonImages = new();
    readonly Dictionary<int, Image> paletteButtonImages = new();
    readonly Dictionary<CellWidthIntent, Image> widthButtonImages = new();
    GameObject recoveryPanel;
    TMP_Text recoveryInventoryText;
    TMP_Text selectedRecoveryText;
    TMP_Text recoveryActionText;
    TMP_Text dreadActionText;
    Button previousRecoveryButton;
    Button nextRecoveryButton;
    Button focusRecoveryButton;
    Button recoverSelectedButton;
    string selectedRecoveryDropId;
    RecoverableLootWorldDrop selectedRecoveryWorldDrop;
    CameraFollow focusedRecoveryCamera;

    void Awake()
    {
        loop = GetComponent<GameplayLoopController>();
        if (loop == null)
            loop = FindAnyObjectByType<GameplayLoopController>();
        saveManager = loop != null ? loop.GetComponent<GameSaveManager>() : null;
        placement = FindAnyObjectByType<TilePlacement>();
        inputManager = InputManager.Instance ?? FindAnyObjectByType<InputManager>();
        recoveryTraversal = FindAnyObjectByType<NPCTraversal>();

        EnsureEventSystem();
        BuildInterface();
    }

    void OnEnable()
    {
        if (loop != null)
        {
            loop.StateChanged += Refresh;
            loop.LootRecovered += OnPlayerLootRecovered;
        }
        if (saveManager != null)
            saveManager.StatusChanged += OnSaveStatusChanged;
        if (recoveryTraversal != null)
        {
            recoveryTraversal.RecoverableLootCreated += OnRecoverableLootChanged;
            recoveryTraversal.RecoverableLootClaimed += OnRecoverableLootChanged;
        }
    }

    void OnDisable()
    {
        if (loop != null)
        {
            loop.StateChanged -= Refresh;
            loop.LootRecovered -= OnPlayerLootRecovered;
        }
        if (saveManager != null)
            saveManager.StatusChanged -= OnSaveStatusChanged;
        if (recoveryTraversal != null)
        {
            recoveryTraversal.RecoverableLootCreated -= OnRecoverableLootChanged;
            recoveryTraversal.RecoverableLootClaimed -= OnRecoverableLootChanged;
        }
        ClearRecoverySelection(true);
        if (saveMenuOverlay != null && saveMenuOverlay.activeSelf &&
            loop != null && !wasPausedBeforeSaveMenu && loop.IsPaused)
        {
            loop.SetPaused(false);
        }
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if (inputManager == null)
            inputManager = InputManager.Instance;
        if (inputManager != null && inputManager.EscapePressed)
            ToggleSaveMenu();
        RefreshDynamicText();
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    void BuildInterface()
    {
        GameObject canvasObject = new("Gameplay HUD");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        BuildPhasePanel(canvasRect);
        BuildDebugPanel(canvasRect);
        BuildExpansionPalette(canvasRect);
        BuildOpenDungeonButton(canvasRect);
        BuildRecoveryPanel(canvasRect);
        BuildExplorationPanel(canvasRect);
        BuildSaveLoadMenu(canvasRect);
    }

    void BuildPhasePanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel(
            "Phase", parent, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(360f, 112f), new Vector2(-18f, -18f),
            ExpansionColor);
        phasePanelImage = panel.GetComponent<Image>();
        phaseTitle = CreateText(
            "Title", panel, "EXPANSION", 38, FontStyles.Bold,
            TextAlignmentOptions.TopRight, Ink);
        SetRect(phaseTitle.rectTransform, new Vector2(0f, 0.42f), Vector2.one,
            new Vector2(8f, 0f), new Vector2(-16f, -8f));
        phaseDetails = CreateText(
            "Details", panel, string.Empty, 16, FontStyles.Normal,
            TextAlignmentOptions.BottomRight, Ink);
        SetRect(phaseDetails.rectTransform, Vector2.zero, new Vector2(1f, 0.48f),
            new Vector2(8f, 8f), new Vector2(-16f, 0f));
    }

    void BuildDebugPanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel(
            "Debug", parent, Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(210f, 388f), new Vector2(18f, 18f), Panel);
        CreateLabel(panel, "DEBUG", 22, new Vector2(14f, -12f), new Vector2(182f, 30f));

        CreateButton(panel, "Set Day", new Vector2(14f, -52f), new Vector2(86f, 36f),
            () => loop.SetExpansion());
        CreateButton(panel, "Set Night", new Vector2(110f, -52f), new Vector2(86f, 36f),
            () => loop.SetExploring());

        CreateLabel(panel, "GAMEPLAY SPEED", 13, new Vector2(14f, -100f), new Vector2(182f, 24f));
        CreateSpeedButton(panel, "X1", 1f, new Vector2(14f, -130f));
        CreateSpeedButton(panel, "X2", 2f, new Vector2(76f, -130f));
        CreateSpeedButton(panel, "X3", 3f, new Vector2(138f, -130f));

        Button pause = CreateButton(
            panel,
            "Pause Simulation",
            new Vector2(14f, -178f),
            new Vector2(182f, 38f),
            () => loop.TogglePause());
        pauseButtonText = pause.GetComponentInChildren<TMP_Text>();
        CreateButton(panel, "Clear NPCs", new Vector2(14f, -226f), new Vector2(182f, 38f),
            () => loop.ClearAdventurers());

        CreateLabel(panel, "SAVE GAME", 13, new Vector2(14f, -274f), new Vector2(182f, 24f));
        loadLastSaveButton = CreateButton(
            panel, "Load Last Save", new Vector2(14f, -304f), new Vector2(182f, 36f),
            () => saveManager?.LoadLastSave());
        saveStatusText = CreateText(
            "Save Status", panel, string.Empty, 11, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, Ink);
        SetRect(saveStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -382f), new Vector2(196f, -346f));
    }

    void BuildExpansionPalette(RectTransform parent)
    {
        RectTransform panel = CreatePanel(
            "Expansion Palette", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(1520f, 196f), new Vector2(0f, 18f), Panel);
        expansionPalette = panel.gameObject;
        CreateLabel(panel, "BUILD PALETTE", 18, new Vector2(14f, -10f), new Vector2(170f, 28f));
        CreateLabel(panel, "CELL WIDTH", 14, new Vector2(205f, -13f), new Vector2(105f, 26f));
        CreateWidthButton(panel, "Auto", CellWidthIntent.Auto, new Vector2(315f, -8f));
        CreateWidthButton(panel, "Narrow", CellWidthIntent.Narrow, new Vector2(415f, -8f));
        CreateWidthButton(panel, "Wide", CellWidthIntent.Wide, new Vector2(535f, -8f));
        CreateLabel(panel, "WALLS", 14, new Vector2(650f, -13f), new Vector2(62f, 26f));
        Button toggleWall = CreateButton(
            panel, "Toggle Wall", new Vector2(716f, -8f), new Vector2(120f, 34f),
            () => placement?.StartEdgeToggle());
        toggleWallButtonImage = toggleWall.GetComponent<Image>();
        CreateLabel(
            panel, "Click a shared boundary", 12,
            new Vector2(850f, -13f), new Vector2(145f, 26f));
        trapCandidateText = CreateText(
            "Trap Candidate", panel, string.Empty, 12, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, Ink);
        SetTopLeftRect(
            trapCandidateText.rectTransform,
            new Vector2(1000f, -8f),
            new Vector2(235f, 42f));
        dreadActionText = CreateText(
            "Dread Action",
            panel,
            string.Empty,
            12,
            FontStyles.Normal,
            TextAlignmentOptions.TopRight,
            Ink);
        SetTopLeftRect(
            dreadActionText.rectTransform,
            new Vector2(1240f, -8f),
            new Vector2(260f, 52f));

        RectTransform row = CreateRect("Items", panel);
        SetRect(row, Vector2.zero, Vector2.one, new Vector2(14f, 12f), new Vector2(-14f, -78f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        IReadOnlyList<ObjectData> objects = placement != null
            ? placement.AvailableObjects
            : System.Array.Empty<ObjectData>();
        for (int i = 0; i < objects.Count; i++)
        {
            ObjectData item = objects[i];
            bool manifestsTreasure =
                item.PlacementType == ObjectPlacementType.FloorProp &&
                item.Prefab != null &&
                item.Prefab.GetComponent<TreasureProp>() != null;
            string label = manifestsTreasure
                ? $"Manifest Treasure\n{loop.TreasureManifestationDreadCost} Dread"
                : item.PlacementType == ObjectPlacementType.DungeonTile
                    ? $"{item.Name}\n1 Construction Material"
                    : item.Name;
            Button button = CreateButton(
                row,
                label,
                Vector2.zero,
                new Vector2(150f, 62f),
                manifestsTreasure
                    ? () => loop.BeginTreasureManifestation(item.ID)
                    : () => placement.StartPlacement(item.ID));
            paletteButtonImages[item.ID] = button.GetComponent<Image>();
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 150f;
        }

        Button removeTrap = CreateButton(
            row, "Remove Trap", Vector2.zero, new Vector2(140f, 62f),
            () => placement?.StartTrapRemoval());
        removeTrapButtonImage = removeTrap.GetComponent<Image>();
        removeTrap.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        Button removeEntrance = CreateButton(
            row, "Remove Entrance", Vector2.zero, new Vector2(160f, 62f),
            () => placement?.StartEntranceRemoval());
        removeEntranceButtonImage = removeEntrance.GetComponent<Image>();
        removeEntrance.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        Button cancel = CreateButton(row, "Cancel Build", Vector2.zero, new Vector2(130f, 62f),
            () => placement?.StopPlacement());
        cancel.GetComponent<Image>().color = new Color(0.35f, 0.31f, 0.39f, 1f);
        cancel.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;
    }

    void BuildOpenDungeonButton(RectTransform parent)
    {
        Button button = CreateButton(
            parent, "OPEN DUNGEON", Vector2.zero, new Vector2(230f, 74f),
            () => loop.OpenDungeon());
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-18f, -148f);
        button.GetComponent<Image>().color = Accent;
        button.GetComponentInChildren<TMP_Text>().color = Ink;
        button.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
        openDungeonButton = button.gameObject;
    }

    void BuildRecoveryPanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel(
            "Physical Loot Recovery",
            parent,
            Vector2.one,
            Vector2.one,
            Vector2.one,
            new Vector2(360f, 410f),
            new Vector2(-18f, -240f),
            Panel);
        recoveryPanel = panel.gameObject;
        CreateLabel(
            panel,
            "PHYSICAL LOOT RECOVERY",
            18,
            new Vector2(14f, -12f),
            new Vector2(332f, 28f));

        recoveryInventoryText = CreateText(
            "Dungeon Storage",
            panel,
            string.Empty,
            14,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            Ink);
        SetTopLeftRect(
            recoveryInventoryText.rectTransform,
            new Vector2(14f, -48f),
            new Vector2(332f, 52f));

        previousRecoveryButton = CreateButton(
            panel,
            "Previous",
            new Vector2(14f, -110f),
            new Vector2(94f, 36f),
            () => SelectRecoveryOffset(-1));
        nextRecoveryButton = CreateButton(
            panel,
            "Next",
            new Vector2(116f, -110f),
            new Vector2(94f, 36f),
            () => SelectRecoveryOffset(1));
        focusRecoveryButton = CreateButton(
            panel,
            "Focus",
            new Vector2(218f, -110f),
            new Vector2(128f, 36f),
            FocusSelectedRecoveryDrop);

        selectedRecoveryText = CreateText(
            "Selected Drop",
            panel,
            string.Empty,
            14,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            Ink);
        SetTopLeftRect(
            selectedRecoveryText.rectTransform,
            new Vector2(14f, -156f),
            new Vector2(332f, 122f));

        recoverSelectedButton = CreateButton(
            panel,
            "Recover Selected Drop",
            new Vector2(14f, -290f),
            new Vector2(332f, 44f),
            RecoverSelectedDrop);
        recoverSelectedButton.GetComponent<Image>().color = Accent;
        recoverSelectedButton.GetComponentInChildren<TMP_Text>().color = Ink;
        recoverSelectedButton.GetComponentInChildren<TMP_Text>().fontStyle =
            FontStyles.Bold;

        recoveryActionText = CreateText(
            "Recovery Status",
            panel,
            "Click a bag in the dungeon to recover it. Unrecovered bags remain for the next expedition.",
            12,
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft,
            Ink);
        SetTopLeftRect(
            recoveryActionText.rectTransform,
            new Vector2(14f, -344f),
            new Vector2(332f, 48f));
    }

    void BuildExplorationPanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel(
            "Exploration Status", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(610f, 92f), new Vector2(0f, 18f),
            new Color(0.10f, 0.12f, 0.24f, 0.96f));
        explorationPanel = panel.gameObject;
        TMP_Text title = CreateText(
            "Title", panel, "DUNGEON OPEN", 22, FontStyles.Bold,
            TextAlignmentOptions.Top, Color.white);
        SetRect(title.rectTransform, new Vector2(0f, 0.48f), Vector2.one,
            new Vector2(12f, 0f), new Vector2(-12f, -8f));
        explorationDetails = CreateText(
            "Details", panel, string.Empty, 17, FontStyles.Normal,
            TextAlignmentOptions.Bottom, new Color(0.92f, 0.89f, 0.78f, 1f));
        SetRect(explorationDetails.rectTransform, Vector2.zero, new Vector2(1f, 0.52f),
            new Vector2(12f, 8f), new Vector2(-12f, 0f));
    }

    void BuildSaveLoadMenu(RectTransform parent)
    {
        RectTransform overlay = CreateRect("Save Load Overlay", parent);
        SetRect(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.02f, 0.06f, 0.78f);
        saveMenuOverlay = overlay.gameObject;

        RectTransform panel = CreatePanel(
            "Save Load Menu", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(900f, 720f), Vector2.zero, Panel);
        CreateLabel(panel, "SAVE / LOAD", 32, new Vector2(24f, -18f), new Vector2(500f, 44f));
        CreateButton(panel, "Close", new Vector2(774f, -20f), new Vector2(102f, 40f),
            CloseSaveMenu);

        saveNameInput = CreateInputField(
            panel, "Save Name", new Vector2(24f, -80f), new Vector2(620f, 48f));
        saveNameInput.onSubmit.AddListener(_ => SaveNamedGame());
        namedSaveButton = CreateButton(
            panel, "Save Game", new Vector2(660f, -80f), new Vector2(216f, 48f),
            SaveNamedGame);

        menuStatusText = CreateText(
            "Menu Status", panel, string.Empty, 14, FontStyles.Normal,
            TextAlignmentOptions.Left, Ink);
        SetTopLeftRect(
            menuStatusText.rectTransform,
            new Vector2(24f, -136f),
            new Vector2(852f, 28f));

        CreateLabel(panel, "SAVED GAMES", 16, new Vector2(24f, -174f), new Vector2(300f, 28f));
        BuildSaveScrollView(panel);

        TMP_Text hint = CreateText(
            "Hint", panel,
            "Press Escape to close. Saving is available between dungeon visits.",
            13, FontStyles.Normal, TextAlignmentOptions.Center, Ink);
        SetTopLeftRect(hint.rectTransform, new Vector2(24f, -686f), new Vector2(852f, 24f));

        saveMenuOverlay.SetActive(false);
    }

    void BuildSaveScrollView(RectTransform parent)
    {
        RectTransform scrollRoot = CreateRect("Save Browser", parent);
        SetTopLeftRect(scrollRoot, new Vector2(24f, -208f), new Vector2(852f, 462f));
        scrollRoot.gameObject.AddComponent<Image>().color = new Color(0.72f, 0.70f, 0.75f, 1f);
        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 34f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-24f, -8f));
        viewport.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.91f, 0.93f, 1f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        saveListContent = CreateRect("Save Slots", viewport);
        saveListContent.anchorMin = new Vector2(0f, 1f);
        saveListContent.anchorMax = new Vector2(1f, 1f);
        saveListContent.pivot = new Vector2(0.5f, 1f);
        saveListContent.anchoredPosition = Vector2.zero;
        saveListContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = saveListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = saveListContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform scrollbarRect = CreateRect("Scrollbar", scrollRoot);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-18f, 8f);
        scrollbarRect.offsetMax = new Vector2(-6f, -8f);
        scrollbarRect.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.28f, 0.34f, 1f);
        Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform handle = CreateRect("Handle", scrollbarRect);
        SetRect(handle, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Accent;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;

        scrollRect.viewport = viewport;
        scrollRect.content = saveListContent;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    TMP_InputField CreateInputField(
        RectTransform parent,
        string placeholderValue,
        Vector2 position,
        Vector2 size)
    {
        RectTransform root = CreateRect("Save Name Input", parent);
        SetTopLeftRect(root, position, size);
        root.gameObject.AddComponent<Image>().color = Color.white;
        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        input.characterLimit = 64;
        input.lineType = TMP_InputField.LineType.SingleLine;

        RectTransform viewport = CreateRect("Text Area", root);
        SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(12f, 5f), new Vector2(-12f, -5f));
        viewport.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = (TextMeshProUGUI)CreateText(
            "Placeholder", viewport, placeholderValue, 18, FontStyles.Italic,
            TextAlignmentOptions.Left, new Color(0.38f, 0.38f, 0.42f, 0.72f));
        SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TextMeshProUGUI value = (TextMeshProUGUI)CreateText(
            "Text", viewport, string.Empty, 18, FontStyles.Normal,
            TextAlignmentOptions.Left, Ink);
        SetRect(value.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        input.textViewport = viewport;
        input.textComponent = value;
        input.placeholder = placeholder;
        return input;
    }

    void ToggleSaveMenu()
    {
        if (saveMenuOverlay == null)
            return;
        if (saveMenuOverlay.activeSelf)
            CloseSaveMenu();
        else
            OpenSaveMenu();
    }

    void OpenSaveMenu()
    {
        saveMenuOverlay.SetActive(true);
        wasPausedBeforeSaveMenu = loop != null && loop.IsPaused;
        if (loop != null && !wasPausedBeforeSaveMenu)
            loop.SetPaused(true);

        if (saveNameInput != null && string.IsNullOrWhiteSpace(saveNameInput.text))
            saveNameInput.text = BuildDefaultSaveName();
        if (menuStatusText != null && saveManager != null)
            menuStatusText.text = saveManager.LastStatus;
        RebuildSaveSlotList();
        Refresh();
        if (EventSystem.current != null && saveNameInput != null)
        {
            EventSystem.current.SetSelectedGameObject(saveNameInput.gameObject);
            saveNameInput.ActivateInputField();
        }
    }

    void CloseSaveMenu()
    {
        if (saveMenuOverlay == null || !saveMenuOverlay.activeSelf)
            return;
        saveMenuOverlay.SetActive(false);
        if (loop != null && !wasPausedBeforeSaveMenu && loop.IsPaused)
            loop.SetPaused(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void SaveNamedGame()
    {
        if (saveManager == null || saveNameInput == null)
            return;
        if (saveManager.SaveGame(saveNameInput.text))
            saveNameInput.text = BuildDefaultSaveName();
    }

    void LoadSaveSlot(SaveSlotInfo slot)
    {
        if (saveManager != null && saveManager.LoadGame(slot))
            CloseSaveMenu();
    }

    void OnSaveStatusChanged()
    {
        if (menuStatusText != null && saveManager != null)
            menuStatusText.text = saveManager.LastStatus;
        if (saveMenuOverlay != null && saveMenuOverlay.activeSelf)
            RebuildSaveSlotList();
        Refresh();
    }

    void RebuildSaveSlotList()
    {
        if (saveListContent == null || saveManager == null)
            return;

        for (int i = saveListContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = saveListContent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        List<SaveSlotInfo> slots = saveManager.GetSaveSlots();
        if (slots.Count == 0)
        {
            TMP_Text empty = CreateText(
                "No Saves", saveListContent,
                "No saved games yet. Enter a name above to create one.",
                18, FontStyles.Normal, TextAlignmentOptions.Center, Ink);
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            return;
        }

        foreach (SaveSlotInfo slot in slots)
            CreateSaveSlotRow(slot);
    }

    void CreateSaveSlotRow(SaveSlotInfo slot)
    {
        RectTransform row = CreateRect($"Save - {slot.SaveName}", saveListContent);
        row.gameObject.AddComponent<Image>().color = new Color(0.82f, 0.80f, 0.84f, 1f);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        TMP_Text name = CreateText(
            "Name", row, slot.SaveName, 21, FontStyles.Bold,
            TextAlignmentOptions.Left, Ink);
        SetTopLeftRect(name.rectTransform, new Vector2(14f, -8f), new Vector2(610f, 30f));

        string savedTime = FormatSaveTime(slot.SavedAtUtc, slot.SortTimeUtc);
        TMP_Text details = CreateText(
            "Details", row,
            $"{savedTime}   •   Opened {slot.DungeonOpenCount} days   •   " +
            $"Level {slot.DungeonLevel}   •   {slot.Dread} Dread   •   " +
            $"Loot {slot.RecoveredLootValue}   •   " +
            $"{slot.AdventurerCount} NPCs   •   {slot.TileCellCount} cells",
            13, FontStyles.Normal, TextAlignmentOptions.Left, Ink);
        SetTopLeftRect(details.rectTransform, new Vector2(14f, -42f), new Vector2(650f, 25f));

        SaveSlotInfo selectedSlot = slot;
        CreateButton(row, "Load", new Vector2(680f, -20f), new Vector2(104f, 42f),
            () => LoadSaveSlot(selectedSlot));
    }

    static string BuildDefaultSaveName()
    {
        return $"Dungeon {DateTime.Now:yyyy-MM-dd HH-mm}";
    }

    static string FormatSaveTime(string savedAtUtc, DateTime fallbackUtc)
    {
        DateTime value = DateTime.TryParse(
            savedAtUtc,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out DateTime parsed)
                ? parsed
                : fallbackUtc;
        return value.ToLocalTime().ToString("g");
    }

    void CreateSpeedButton(RectTransform parent, string label, float speed, Vector2 position)
    {
        Button button = CreateButton(parent, label, position, new Vector2(52f, 36f),
            () => loop.SetGameplaySpeed(speed));
        speedButtonImages[speed] = button.GetComponent<Image>();
    }

    void CreateWidthButton(
        RectTransform parent,
        string label,
        CellWidthIntent intent,
        Vector2 position)
    {
        Button button = CreateButton(parent, label, position, new Vector2(90f, 34f),
            () => placement?.SetWidthIntent(intent));
        widthButtonImages[intent] = button.GetComponent<Image>();
    }

    void OnRecoverableLootChanged(RecoverableLootDrop _)
    {
        RefreshRecoveryPanel();
    }

    void OnPlayerLootRecovered(PlayerLootRecoveryRecord recovery)
    {
        if (recoveryActionText == null || recovery == null)
            return;
        recoveryActionText.text =
            $"Recovered {recovery.RecoveredItemCount} item(s), value " +
            $"{recovery.RecoveredValue}. Dungeon treasure " +
            $"{recovery.DungeonTreasureValue}; adventurer spoils " +
            $"{recovery.AdventurerLootValue}.";
    }

    void SelectRecoveryOffset(int offset)
    {
        if (loop == null || loop.Phase != DungeonPhase.Expansion ||
            recoveryTraversal == null)
        {
            return;
        }

        IReadOnlyList<RecoverableLootDrop> drops =
            recoveryTraversal.RecoverableLootDrops;
        if (drops.Count == 0)
            return;
        int selectedIndex = FindSelectedRecoveryIndex(drops);
        if (selectedIndex < 0)
            selectedIndex = 0;
        int nextIndex = (selectedIndex + offset) % drops.Count;
        if (nextIndex < 0)
            nextIndex += drops.Count;
        SetRecoverySelection(drops[nextIndex]?.DropId, true);
        RefreshRecoveryPanel();
    }

    void FocusSelectedRecoveryDrop()
    {
        if (string.IsNullOrWhiteSpace(selectedRecoveryDropId))
            return;
        SetRecoverySelection(selectedRecoveryDropId, true);
    }

    void RecoverSelectedDrop()
    {
        if (loop == null || string.IsNullOrWhiteSpace(selectedRecoveryDropId))
            return;

        string dropId = selectedRecoveryDropId;
        ClearRecoverySelection(true);
        if (!loop.TryRecoverLootDrop(
                dropId,
                out _,
                out string failure))
        {
            recoveryActionText.text = failure;
            SetRecoverySelection(dropId, false);
        }
        RefreshRecoveryPanel();
    }

    void RefreshRecoveryPanel()
    {
        if (recoveryPanel == null || loop == null)
            return;

        bool expansion = loop.Phase == DungeonPhase.Expansion;
        recoveryPanel.SetActive(expansion);
        if (!expansion)
        {
            ClearRecoverySelection(true);
            return;
        }
        if (recoveryTraversal == null)
            recoveryTraversal = FindAnyObjectByType<NPCTraversal>();

        IReadOnlyList<RecoverableLootDrop> drops = recoveryTraversal != null
            ? recoveryTraversal.RecoverableLootDrops
            : Array.Empty<RecoverableLootDrop>();
        int selectedIndex = FindSelectedRecoveryIndex(drops);
        if (drops.Count == 0)
        {
            ClearRecoverySelection(true);
            selectedRecoveryText.text =
                "No physical loot bags are waiting in the dungeon.";
        }
        else
        {
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
                SetRecoverySelection(drops[0]?.DropId, false);
            }

            RecoverableLootDrop selected = drops[selectedIndex];
            if (selected == null)
            {
                ClearRecoverySelection(true);
                selectedRecoveryText.text =
                    "The selected loot bag is no longer available.";
                RefreshRecoveryControls(drops.Count, false);
                return;
            }
            GetDropOriginValues(
                selected,
                out int dungeonValue,
                out int adventurerValue);
            selectedRecoveryText.text =
                $"Bag {selectedIndex + 1}/{drops.Count}: {selected.DropId}\n" +
                $"Cell {selected.DropCell}  |  {selected.ItemCount} item(s)  |  " +
                $"value {selected.TotalValue}\n" +
                $"Dungeon treasure {dungeonValue}  |  Adventurer spoils " +
                $"{adventurerValue}\nDropped by {selected.SourceAdventurerName}\n" +
                $"Contents: {BuildDropContentsSummary(selected)}";
        }

        recoveryInventoryText.text =
            $"Waiting bags: {drops.Count}\n" +
            $"Dungeon storage: {loop.RecoveredLootItemCount} item(s), value " +
            $"{loop.RecoveredLootValue}  |  Treasure " +
            $"{loop.RecoveredDungeonTreasureValue}  |  Spoils " +
            $"{loop.RecoveredAdventurerLootValue}";
        bool hasSelection = drops.Count > 0 && selectedIndex >= 0;
        RefreshRecoveryControls(drops.Count, hasSelection);
    }

    void RefreshRecoveryControls(int dropCount, bool hasSelection)
    {
        previousRecoveryButton.interactable = dropCount > 1;
        nextRecoveryButton.interactable = dropCount > 1;
        focusRecoveryButton.interactable = hasSelection;
        recoverSelectedButton.interactable = hasSelection;
    }

    int FindSelectedRecoveryIndex(IReadOnlyList<RecoverableLootDrop> drops)
    {
        if (drops == null || string.IsNullOrWhiteSpace(selectedRecoveryDropId))
            return -1;
        for (int i = 0; i < drops.Count; i++)
            if (drops[i] != null && drops[i].DropId == selectedRecoveryDropId)
                return i;
        return -1;
    }

    void SetRecoverySelection(string dropId, bool focus)
    {
        if (selectedRecoveryDropId != dropId)
            ClearRecoverySelection(true);
        selectedRecoveryDropId = dropId;
        if (recoveryTraversal == null || string.IsNullOrWhiteSpace(dropId) ||
            !recoveryTraversal.TryGetRecoverableLootWorldDrop(
                dropId, out selectedRecoveryWorldDrop))
        {
            selectedRecoveryWorldDrop = null;
            return;
        }

        selectedRecoveryWorldDrop.SetPlayerSelected(true);
        if (!focus)
            return;
        focusedRecoveryCamera = Camera.main != null
            ? Camera.main.GetComponent<CameraFollow>()
            : null;
        if (focusedRecoveryCamera == null)
            focusedRecoveryCamera = FindAnyObjectByType<CameraFollow>();
        focusedRecoveryCamera?.FocusTarget(selectedRecoveryWorldDrop.transform);
    }

    void ClearRecoverySelection(bool clearCamera)
    {
        Transform selectedTransform = selectedRecoveryWorldDrop != null
            ? selectedRecoveryWorldDrop.transform
            : null;
        if (selectedRecoveryWorldDrop != null)
            selectedRecoveryWorldDrop.SetPlayerSelected(false);
        if (clearCamera && focusedRecoveryCamera != null &&
            focusedRecoveryCamera.FocusedTarget == selectedTransform)
        {
            focusedRecoveryCamera.ClearFocus();
        }

        selectedRecoveryDropId = null;
        selectedRecoveryWorldDrop = null;
        focusedRecoveryCamera = null;
    }

    static void GetDropOriginValues(
        RecoverableLootDrop drop,
        out int dungeonValue,
        out int adventurerValue)
    {
        dungeonValue = 0;
        adventurerValue = 0;
        if (drop == null)
            return;
        for (int i = 0; i < drop.Items.Count; i++)
        {
            RecoverableLootItem item = drop.Items[i];
            if (item == null)
                continue;
            if (item.Origin == RecoverableLootOrigin.DungeonTreasure)
                dungeonValue += item.Value;
            else
                adventurerValue += item.Value;
        }
    }

    static string BuildDropContentsSummary(RecoverableLootDrop drop)
    {
        if (drop == null || drop.ItemCount == 0)
            return "none";

        var summary = new System.Text.StringBuilder();
        int displayed = Mathf.Min(3, drop.Items.Count);
        for (int i = 0; i < displayed; i++)
        {
            RecoverableLootItem item = drop.Items[i];
            if (item == null)
                continue;
            if (summary.Length > 0)
                summary.Append(", ");
            summary.Append(item.ItemId);
            if (item.IsPhysicalResource)
            {
                summary.Append(" x");
                summary.Append(item.ResourceQuantity);
                summary.Append(" ");
                summary.Append(item.ResourceCategory);
            }
            summary.Append(" (");
            summary.Append(item.Value);
            summary.Append(')');
        }
        if (drop.Items.Count > displayed)
        {
            summary.Append(" +");
            summary.Append(drop.Items.Count - displayed);
            summary.Append(" more");
        }
        return summary.Length > 0 ? summary.ToString() : "none";
    }

    void Refresh()
    {
        if (loop == null)
            return;

        bool expansion = loop.Phase == DungeonPhase.Expansion;
        expansionPalette.SetActive(expansion);
        openDungeonButton.SetActive(expansion);
        explorationPanel.SetActive(!expansion);
        phasePanelImage.color = expansion ? ExpansionColor : ExploringColor;
        phaseTitle.text = expansion ? "EXPANSION" : "EXPLORING";
        phaseTitle.color = expansion ? Ink : Color.white;
        phaseDetails.color = expansion ? Ink : new Color(0.87f, 0.89f, 1f, 1f);
        pauseButtonText.text = loop.IsPaused
            ? "Resume Simulation"
            : "Pause Simulation";
        if (loadLastSaveButton != null && saveManager != null)
            loadLastSaveButton.interactable = saveManager.HasSave;
        if (saveStatusText != null && saveManager != null)
            saveStatusText.text = saveManager.LastStatus;
        if (namedSaveButton != null)
            namedSaveButton.interactable = loop.Phase == DungeonPhase.Expansion;

        foreach (KeyValuePair<float, Image> pair in speedButtonImages)
            pair.Value.color = Mathf.Approximately(pair.Key, loop.SelectedSpeed)
                ? Accent
                : ButtonColor;

        RefreshDynamicText();
    }

    void RefreshDynamicText()
    {
        if (loop == null || phaseDetails == null)
            return;

        RefreshPaletteSelection();
        RefreshRecoveryPanel();
        if (dreadActionText != null)
            dreadActionText.text = string.IsNullOrEmpty(loop.LastBuildActionMessage)
                ? loop.LastDreadActionMessage
                : loop.LastBuildActionMessage;

        string pause = loop.IsPaused ? "  •  PAUSED" : string.Empty;
        if (loop.Phase == DungeonPhase.Expansion)
        {
            phaseDetails.text =
                $"Rating {BuildRating(loop.DungeonRating)}  •  " +
                $"Level {loop.DungeonLevel}  •  {loop.Dread} Dread  •  " +
                $"Materials {loop.ConstructionMaterials}  •  " +
                $"Trap Components {loop.TrapComponents}  •  " +
                $"Arcane Components {loop.ArcaneComponents}  •  " +
                $"Opened {loop.DungeonOpenCount} days  •  Build enabled{pause}";
            return;
        }

        int seconds = Mathf.CeilToInt(loop.ExplorationTimeRemaining);
        phaseDetails.text = $"Dungeon open  •  {seconds / 60:00}:{seconds % 60:00}{pause}";
        explorationDetails.text =
            $"Rating {BuildRating(loop.DungeonRating)}     Adventurers " +
            $"{loop.ActiveAdventurers}/{loop.MaximumAdventurers}     " +
            $"Dread {loop.Dread} (+{loop.PendingVisitDread})     " +
            $"Closes in {seconds / 60:00}:{seconds % 60:00}";
    }

    void RefreshPaletteSelection()
    {
        int selectedId = placement != null ? placement.SelectedObjectId : -1;
        foreach (KeyValuePair<int, Image> pair in paletteButtonImages)
            pair.Value.color = pair.Key == selectedId ? Accent : ButtonColor;

        if (removeTrapButtonImage != null)
            removeTrapButtonImage.color = placement != null && placement.IsRemovingTraps
                ? Accent
                : ButtonColor;
        if (removeEntranceButtonImage != null)
            removeEntranceButtonImage.color = placement != null && placement.IsRemovingEntrance
                ? Accent
                : ButtonColor;

        CellWidthIntent selectedWidth = placement != null
            ? placement.WidthIntent
            : CellWidthIntent.Auto;
        foreach (KeyValuePair<CellWidthIntent, Image> pair in widthButtonImages)
            pair.Value.color = pair.Key == selectedWidth ? Accent : ButtonColor;

        bool placingTrap = placement != null &&
            placement.IsTrapPlacementActive;
        if (trapCandidateText != null)
        {
            trapCandidateText.gameObject.SetActive(placingTrap);
            if (!placingTrap)
                trapCandidateText.text = string.Empty;
            else if (!placement.HasSelectedTrapCandidate)
                trapCandidateText.text = string.IsNullOrWhiteSpace(
                        placement.TrapPlacementFailure)
                    ? "No valid adjacent corridor"
                    : placement.TrapPlacementFailure;
            else
            {
                TrapAttachmentPlacement candidate =
                    placement.SelectedTrapCandidate;
                string cycleHint = placement.TrapCandidateCount > 1
                    ? "  R: Change side"
                    : string.Empty;
                trapCandidateText.text =
                    $"Target {candidate.TargetCell.x},{candidate.TargetCell.y}  " +
                    $"{candidate.Surface}{cycleHint}";
            }
        }

        if (toggleWallButtonImage != null)
            toggleWallButtonImage.color = placement != null && placement.IsEditingEdges
                ? Accent
                : ButtonColor;
    }

    string BuildRating(int rating)
    {
        return new string('\u2605', rating) + new string('\u2606', Mathf.Max(0, 5 - rating));
    }

    RectTransform CreatePanel(
        string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 size, Vector2 position, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    TMP_Text CreateLabel(RectTransform parent, string value, int size, Vector2 position, Vector2 dimensions)
    {
        TMP_Text label = CreateText(
            "Label", parent, value, size, FontStyles.Bold,
            TextAlignmentOptions.Left, Ink);
        label.rectTransform.anchorMin = new Vector2(0f, 1f);
        label.rectTransform.anchorMax = new Vector2(0f, 1f);
        label.rectTransform.pivot = new Vector2(0f, 1f);
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = dimensions;
        return label;
    }

    Button CreateButton(
        RectTransform parent, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(label, parent);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(action);

        TMP_Text text = CreateText(
            "Text", rect, label, 16, FontStyles.Normal,
            TextAlignmentOptions.Center, Color.white);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 3f), new Vector2(-5f, -3f));
        return button;
    }

    TMP_Text CreateText(
        string name, RectTransform parent, string value, int size,
        FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    void SetRect(
        RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    void SetTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
