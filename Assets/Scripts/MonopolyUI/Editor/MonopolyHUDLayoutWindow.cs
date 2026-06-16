using System.IO;
using UnityEditor;
using UnityEngine;

public class MonopolyHUDLayoutWindow : EditorWindow
{
    private const string DefaultSettingsPath = "Assets/Scripts/MonopolyUI/MonopolyHUDLayoutSettings.asset";

    private MonopolyHUD targetHUD;
    private MonopolyHUDLayoutSettings settings;
    private SerializedObject serializedSettings;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Food Monopoly/UI Layout Editor")]
    public static void Open()
    {
        GetWindow<MonopolyHUDLayoutWindow>("Monopoly UI");
    }

    private void OnEnable()
    {
        FindSceneHUD();
        FindExistingSettings();
        RefreshSerializedSettings();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        DrawToolbar();
        EditorGUILayout.Space(8f);

        targetHUD = (MonopolyHUD)EditorGUILayout.ObjectField("Scene HUD", targetHUD, typeof(MonopolyHUD), true);

        EditorGUI.BeginChangeCheck();
        settings = (MonopolyHUDLayoutSettings)EditorGUILayout.ObjectField("Layout Settings", settings, typeof(MonopolyHUDLayoutSettings), false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshSerializedSettings();
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Default Settings", GUILayout.Height(28f)))
            {
                CreateDefaultSettingsAsset();
            }

            using (new EditorGUI.DisabledScope(targetHUD == null || settings == null))
            {
                if (GUILayout.Button("Assign To HUD", GUILayout.Height(28f)))
                {
                    AssignSettingsToHUD();
                }
            }
        }

        if (settings == null)
        {
            EditorGUILayout.HelpBox("Create or assign a MonopolyHUDLayoutSettings asset first.", MessageType.Info);
            return;
        }

        if (serializedSettings == null)
        {
            RefreshSerializedSettings();
        }

        serializedSettings.Update();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawSection("Canvas", "referenceResolution", "matchWidthOrHeight", "sortingOrder");
        DrawSection("Colors", "panelColor", "buttonColor", "buttonHighlightColor", "buttonPressedColor", "buttonDisabledColor", "textColor", "outlineColor", "outlineDistance");
        DrawLayoutSection("Stats Panel", "statsPanel");
        DrawTextSection("Money Text", "moneyText");
        DrawTextSection("Turn Text", "turnText");
        DrawTextSection("Income Text", "incomeText");
        DrawLayoutSection("Info Panel", "infoPanel");
        DrawTextSection("Info Text", "infoText");
        DrawLayoutSection("Build Button", "buildButton");
        DrawTextSection("Build Button Text", "buildButtonText");
        DrawLayoutSection("Upgrade Button", "upgradeButton");
        DrawTextSection("Upgrade Button Text", "upgradeButtonText");
        DrawLayoutSection("Dice Button", "diceButton");
        DrawTextSection("Dice Button Text", "diceButtonText");

        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset Settings To Defaults", GUILayout.Height(26f)))
            {
                Undo.RecordObject(settings, "Reset Monopoly HUD Layout");
                settings.ResetToDefaults();
                EditorUtility.SetDirty(settings);
                RefreshSerializedSettings();
            }

            if (GUILayout.Button("Save Asset", GUILayout.Height(26f)))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        serializedSettings.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Find HUD", EditorStyles.toolbarButton))
            {
                FindSceneHUD();
            }

            if (GUILayout.Button("Find Settings", EditorStyles.toolbarButton))
            {
                FindExistingSettings();
                RefreshSerializedSettings();
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawSection(string title, params string[] propertyNames)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                EditorGUILayout.PropertyField(serializedSettings.FindProperty(propertyNames[i]));
            }
        }
    }

    private void DrawLayoutSection(string title, string propertyName)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        SerializedProperty property = serializedSettings.FindProperty(propertyName);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("anchorMin"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("anchorMax"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("pivot"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("anchoredPosition"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("sizeDelta"));
        }
    }

    private void DrawTextSection(string title, string propertyName)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        SerializedProperty property = serializedSettings.FindProperty(propertyName);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("defaultText"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("fontSize"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("alignment"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("padding"), true);
        }
    }

    private void CreateDefaultSettingsAsset()
    {
        EnsureDirectory(Path.GetDirectoryName(DefaultSettingsPath));

        MonopolyHUDLayoutSettings asset = MonopolyHUDLayoutSettings.CreateDefaultInstance();
        string path = AssetDatabase.GenerateUniqueAssetPath(DefaultSettingsPath);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        settings = asset;
        RefreshSerializedSettings();
        Selection.activeObject = asset;
    }

    private void AssignSettingsToHUD()
    {
        SerializedObject hudObject = new SerializedObject(targetHUD);
        hudObject.FindProperty("layoutSettings").objectReferenceValue = settings;
        hudObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetHUD);
    }

    private void FindSceneHUD()
    {
        targetHUD = FindObjectOfType<MonopolyHUD>();
    }

    private void FindExistingSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:MonopolyHUDLayoutSettings");
        if (guids.Length == 0)
        {
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        settings = AssetDatabase.LoadAssetAtPath<MonopolyHUDLayoutSettings>(path);
    }

    private void RefreshSerializedSettings()
    {
        serializedSettings = settings == null ? null : new SerializedObject(settings);
    }

    private void EnsureDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path);
        string folderName = Path.GetFileName(path);
        EnsureDirectory(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
