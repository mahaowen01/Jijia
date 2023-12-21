// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(DeftlyGameManager))]
public class E_DeftlyGameManager : Editor
{
    // Reference to the Class we're editing
    private DeftlyGameManager _x;
    
    // Basic GUIContent
    private readonly GUIContent _victoryType =      new GUIContent("Victory Rules", "The Conditions for Victory.\n\nDestroy a number of specific targets, reach a certain amount of kills or survive for a period of time.");
    private readonly GUIContent _victoryKills =     new GUIContent("Victory Kills", "Victory is True if the players collectively get this many kills.");
    private readonly GUIContent _victoryTime =      new GUIContent("Victory Time", "Victory is True if this many seconds pass without defeat.");
    private readonly GUIContent _vTargetsTooltip =  new GUIContent("Victory Targets", "Runtime targets for the players to destroy.");
    private readonly GUIContent _gameStatus =       new GUIContent("Game Status", "Field will update with the status of the game.\n\nNot Playing, In Progress, Victory or Defeat");
    private readonly GUIContent _cam =              new GUIContent("Main Camera", "Runtime reference to the Main Camera. For flexibility, the camera doesn't persist between scenes.");
    private readonly GUIContent _respawn =          new GUIContent("Respawn Position", "Main Spawn and Respawn position for character(s).");

    public static bool FoldTab1;
    public static bool FoldTab2;
    public static bool FoldTab3;
    public static bool FoldTab4;

    public static bool FoldTargets;
    public static bool Fold2;
    public static bool Fold3;
    public static bool Fold4;

    void OnEnable()
    {
        _x = (DeftlyGameManager)target;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("General", EditorStyles.toolbarButton)) FoldTab1 = !FoldTab1;
        if (FoldTab1) ShowGeneral();

        if (GUILayout.Button("Options", EditorStyles.toolbarButton)) FoldTab2 = !FoldTab2;
        if (FoldTab2) ShowVictory();

        EditorGUILayout.Space();
        
        Save();
    }

    private void Save()
    {
        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_x);
    }
    private void ShowGeneral()
    {
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("This Game Manager's purpose is to setup each individual scene with references to the the scene's Camera and Respawn location." +
                                " It also handles Victory Conditions for this scene. \n\n" +
                                "The required runtime references are things that must exist in the scene and be indentified here so other scripts can look for them on the GameManager. You will get errors if they are not defined.\n\n" + "" +
                                "It is recommended to put this on the main camera, but not required.", MessageType.None);

        EditorGUILayout.Space();
    }
    private void ShowVictory()
    {
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(true);
        _x.GameStatus = (DeftlyGameManager.GameStatusType)EditorGUILayout.EnumPopup(_gameStatus, _x.GameStatus);
        EditorGUI.EndDisabledGroup();


        EditorUtils.AddBlackLine();
        EditorGUILayout.LabelField("This Scene: Victory Conditions", EditorStyles.boldLabel);
        _x.VictoryRules = (DeftlyGameManager.VictoryConditions)EditorGUILayout.EnumPopup(_victoryType, _x.VictoryRules);

        EditorGUI.BeginDisabledGroup(_x.VictoryRules != DeftlyGameManager.VictoryConditions.KillCount);
        _x.VictoryKillCount = EditorGUILayout.IntSlider(_victoryKills, _x.VictoryKillCount, 1, 300);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_x.VictoryRules != DeftlyGameManager.VictoryConditions.Time);
        _x.VictoryTime = EditorGUILayout.FloatField(_victoryTime, _x.VictoryTime);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_x.VictoryRules != DeftlyGameManager.VictoryConditions.TargetsDestroyed);
        EditorUtils.ShowList(_x.VictoryTargets, ref FoldTargets, _vTargetsTooltip);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorUtils.AddBlackLine();

        EditorGUILayout.LabelField("This Scene: Required References", EditorStyles.boldLabel);
        _x.MainDeftlyCamera = EditorGUILayout.ObjectField(_cam, _x.MainDeftlyCamera, typeof(DeftlyCamera), true) as DeftlyCamera;
        _x.MainRespawn = EditorGUILayout.ObjectField(_respawn, _x.MainRespawn, typeof(Transform), true) as Transform;
    }
}