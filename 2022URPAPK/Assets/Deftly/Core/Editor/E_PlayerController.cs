// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(PlayerController))]
public class E_PlayerController : Editor
{
    protected PlayerController _x;
    
    // Basic Stats
    protected readonly GUIContent _aimControl =       new GUIContent("Aim Control Feel", "How the character aims in response to input");
    protected readonly GUIContent _moveControl =      new GUIContent("Move Control Feel", "~~Not working yet.~~");
    protected readonly GUIContent _mask =             new GUIContent("Mask", "The layers used in raycasts. Typically just 'floor'");
    protected readonly GUIContent _logDebug =         new GUIContent("Log Debug", "Show/Hide debug information");
    protected readonly GUIContent _rootMotion =       new GUIContent("Root Motion", "Use root motion data from Mecanim to drive Subject locomotion. Note that your animations must strongly support this.");
    protected readonly GUIContent _inControl =        new GUIContent("Use InControl", "Use InControl plugin for peripheral controller support");
    protected readonly GUIContent _inControlDevice =  new GUIContent("Device ID", "InControl provides Device IDs, assign them on a per character basis here. Note InControl docs on best practice doing this.");
    protected readonly GUIContent _manualDevice =     new GUIContent("Manually Assign Device", "This makes the script not assign a Device at all. No controls will work until you supply an InControl.Device to the Device variable in a script.");
    protected readonly GUIContent _horizontal =       new GUIContent("Horizontal", "Input Manager Name Reference");
    protected readonly GUIContent _vertical =         new GUIContent("Vertical", "Input Manager Name Reference");
    protected readonly GUIContent _fire1 =            new GUIContent("Fire 1", "Input Manager Name Reference");
    protected readonly GUIContent _fire2 =            new GUIContent("Fire 2", "Input Manager Name Reference");
    protected readonly GUIContent _reload =           new GUIContent("Reload", "Input Manager Name Reference");
    protected readonly GUIContent _interact =         new GUIContent("Interact", "Input Manager Name Reference");
    protected readonly GUIContent _dropWeapon =       new GUIContent("Drop Weapon", "Input Manager Name Reference");
    protected readonly GUIContent _changeWeapon =     new GUIContent("Change Weapon", "Input Manager Name Reference");
    protected readonly GUIContent _walkAnimSpeed =    new GUIContent("Walk Speed", "Root Motion: Multiplier for inputs, but does not change the animator speeds.\nNon Root Motion: Multiplies the inputs and moves the character faster.");
    protected readonly GUIContent _runAnimSpeed =     new GUIContent("Run Speed", "Root Motion: Multiplier for inputs, but does not change the animator speeds.\nNon Root Motion: Multiplies the inputs and moves the character faster.");
    protected readonly GUIContent _animDampening =    new GUIContent("Animator Damper", "Dampening for the inputs going to the animator. Might help with jerky animations.");
    
    void OnEnable()
    {
        _x = (PlayerController)target;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("General", EditorStyles.toolbarButton)) EditorUtils.PlayControlsGeneral = !EditorUtils.PlayControlsGeneral;
        if (EditorUtils.PlayControlsGeneral) ShowGeneral();

        if (GUILayout.Button("Input References", EditorStyles.toolbarButton)) EditorUtils.PlayControlsLabels = !EditorUtils.PlayControlsLabels;
        if (EditorUtils.PlayControlsLabels) ShowLabels();

        if (GUILayout.Button("Peripheral Config", EditorStyles.toolbarButton)) EditorUtils.PlayControlsPeriph = !EditorUtils.PlayControlsPeriph;
        if (EditorUtils.PlayControlsPeriph) ShowControllerConfig();

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

        _x.AimControlFeel = (PlayerController.ControlFeel)EditorGUILayout.EnumPopup(_aimControl, _x.AimControlFeel);
        EditorGUI.BeginDisabledGroup(true);
        _x.MoveControlFeel = (PlayerController.ControlFeel)EditorGUILayout.EnumPopup(_moveControl, _x.MoveControlFeel);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Mask"), _mask, false);

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 70;
        _x.LogDebug = EditorGUILayout.Toggle(_logDebug, _x.LogDebug);
        _x.UseRootMotion = EditorGUILayout.Toggle(_rootMotion, _x.UseRootMotion);
        EditorGUIUtility.labelWidth = 110;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        _x.AnimatorDampening = EditorGUILayout.Slider(_animDampening, _x.AnimatorDampening, 0, 1);
        _x.SprintSpeed = EditorGUILayout.Slider(_runAnimSpeed, _x.SprintSpeed, 1, 3);
        _x.WalkSpeed = EditorGUILayout.Slider(_walkAnimSpeed, _x.WalkSpeed, 1, 3);

        EditorGUILayout.Space();
    }
    private void ShowLabels()
    {
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("These names correspond to the Inputs in the Input Manager.", MessageType.None);

        _x.Horizontal = EditorGUILayout.TextField(_horizontal, _x.Horizontal);
        _x.Vertical = EditorGUILayout.TextField(_vertical, _x.Vertical);
        _x.Fire1 = EditorGUILayout.TextField(_fire1, _x.Fire1);
        _x.Fire2 = EditorGUILayout.TextField(_fire2, _x.Fire2);
        _x.Reload = EditorGUILayout.TextField(_reload, _x.Reload);
        _x.Interact = EditorGUILayout.TextField(_interact, _x.Interact);
        _x.DropWeapon = EditorGUILayout.TextField(_dropWeapon, _x.DropWeapon);
        _x.ChangeWeapon = EditorGUILayout.TextField(_changeWeapon, _x.ChangeWeapon);
        _x.Sprint = EditorGUILayout.TextField("Sprint", _x.Sprint);

        EditorGUILayout.Space();
    }
    public virtual void ShowControllerConfig()
    {
        GUIStyle myStyle = GUI.skin.GetStyle("HelpBox");
        myStyle.richText = true;
        EditorGUILayout.HelpBox("" +
            "\nTo use InControl: \n\n"+
            "<b>1..</b> Import InControl\n"+
            "<b>2..</b> Import the Co-Op Demo from the Addons folder.\n" +
            "<b>3..</b> Add a *InController* script to your character (below this script)\n" +
            "<b>4..</b> Migrate any values from this script to the new one (InController).\n" +
            "<b>5..</b> Remove this script since InController is replacing it.\n"
            , MessageType.Info);

        _x.UseInControl = false;

        EditorGUILayout.Space();
    }
}