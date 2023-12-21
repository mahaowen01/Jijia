// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(Subject))]
//[CanEditMultipleObjects]
public class E_Subject : Editor
{
    private Subject _x;
    public static Texture2D TeamTex;
    private static readonly GUIStyle TeamGuiStyle = new GUIStyle();
    private static Color _teamColor;

    private static readonly GUIContent ButtonAdd = new GUIContent("+", "Add weapon");
    private static readonly GUIContent ButtonRemove = new GUIContent("-", "Remove this weapon");

    // Basic data
    private readonly GUIContent _subjectGroup =     new GUIContent("Subject Group", "Is this a Character or is it a Prop?");
    private readonly GUIContent _teamId =           new GUIContent("Team ID", "What team is this Subject on?");
    private readonly GUIContent _title =            new GUIContent("Title", "The name of this Subject");
    private readonly GUIContent _godMode =          new GUIContent("God Mode", "All incoming damage is ignored");
    private readonly GUIContent _unlimitedMags =    new GUIContent("Unlimited Mags", "Magazines are not consumed when reloading");
    private readonly GUIContent _hitReaction =      new GUIContent("Hit Reaction", "When hit/damaged, this sound effect plays");
    private readonly GUIContent _crippledTime =     new GUIContent("Crippled Time", "After losing all health, enter 'downed' status where it can be revived. After this time, the Subject will turn into a corpse.");
    private readonly GUIContent _corpseTime =       new GUIContent("Corpse Time", "The model will disappear after this time (begins after Crippled ends)");

    // Base Stats
    private readonly GUIContent _armorType =        new GUIContent("Armor Type");

    // Weapon Data
    private readonly GUIContent _useMecanim =       new GUIContent("Use Mecanim", "Toggle the use of Mecanim and IK features");
    private readonly GUIContent _changeWeapon =     new GUIContent("Change Weapon", "This sound plays every time you swap weapons");
    private readonly GUIContent _useWeaponIk =      new GUIContent("Use IK", "When on, Mecanim will try to use IK targets. See documentation for more info");
    private readonly GUIContent _rhIk =             new GUIContent("Hand[R]", "The Right Hand goal will be handled");
    private readonly GUIContent _lhIk =             new GUIContent("Hand[L]", "The Left Hand goal will be handled");

    // Mecanim data
    private readonly GUIContent _animHost =         new GUIContent("Animator Host Obj", "The sub-object with the Animator component");
    private readonly GUIContent _horizParam =       new GUIContent("Horizontal Param", "The name of the Horizontal parameter in the targetted Animator Controller");
    private readonly GUIContent _vertiParam =       new GUIContent("Vertical Param", "The name of the Vertical parameter in the targetted Animator Controller");
    private readonly GUIContent _deathParam =       new GUIContent("Death Trigger", "The name of the Trigger which fires the Death animation in the Animator Controller");
    private readonly GUIContent _reviveParam =      new GUIContent("Revive Trigger", "The name of the Trigger which fires the Revive animation in the Animator Controller");
    private readonly GUIContent _reloadParam =      new GUIContent("Reload Param", "The name of the Bool which fires the Reload Weapon animation in the Animator Controller");
    private readonly GUIContent _swapParam =        new GUIContent("Swap Param", "The name of the Bool which fires the Swap Weapon animation in the Animator Controller");
    private readonly GUIContent _wTypeParam =       new GUIContent("Weapon Id Param", "The name of the Int which tells the Animator Controller which type of Weapon the player is holding. \n\nThis could be setup a variety of ways depending on your Controller. ie 1 for melee, 2 for ranged, or unique numbers to change the state for each weapon prefab.");
    private readonly GUIContent _cScale =           new GUIContent("Character Scale", "Average character is typically 2m tall. Increase/decrease this number to multiply the IK offset.");
    private readonly GUIContent _thumbDir =         new GUIContent("Thumb Direction", "The local axis of the hand that the thumb is pointing on.");
    private readonly GUIContent _palmDir =          new GUIContent("Palm Direction", "The local axis of the hand that the palm is facing.");
    private readonly GUIContent _flipX =            new GUIContent("Flip X", "Some rigs have this axis inverted. If the X axis or the Hand Bone points UP THE ARM then toggle this on.");
    private readonly GUIContent _lhIndex =          new GUIContent("IK Layer[L]", "The layer ID IK will be called on for the Left Hand.\n\nNOTE: Lower numbers are called first, keep this in mind for dependencies such as RH processed before LH.");
    private readonly GUIContent _rhIndex =          new GUIContent("IK Layer[R]", "The layer ID IK will be called on for the Right Hand.\n\nNOTE: Lower numbers are called first, keep this in mind for dependencies such as RH processed before LH.");
    private readonly GUIContent _wpnInHandPos =     new GUIContent("Weapon (In Hand) Offset", "The offset from the local 0,0,0 bone position\n\nThis is for moving where the weapon rests in the Hand. Some 'hand' bones are closer to the wrist than others. Correct that here.\n\nThese are relative to the hand, so if the palm is facing Y, use Y here to move in that facing direction.");
    private readonly GUIContent _rigType =          new GUIContent("Rig Type", "Bipedal Subject's are Human-like. If you intend to use the IK features of Deftly it is necessary to have a Humanoid Mecanim Rig Type and choosing Bipedal in this field.\n\nUsing Non Bipedal rigs with Mecanim will work, you just need to specify a GameObject for the Weapon to Mount to instead of Mecanim's Hand Bone for this rig.");
    private readonly GUIContent _overrideLayer =    new GUIContent("Override", "The layer ID which is used for showing animations such as Reloading and Swapping.\n\nThis is necessary to adjust the weight so it doesn't interfere unless necessary.\n\nTo see the Layer name you must select a Scene object because Mecanim Layers cannot be queried on Prefab objects.");

    
    // Control speeds
    private readonly GUIContent _turnSpeed =        new GUIContent("Turn Speed", "The max rate at which the character turns");
    private readonly GUIContent _moveSpeed =        new GUIContent("Move Speed", "The max rate at which the character moves");

    // Misc Titles
    private readonly GUIContent _dmgMult =          new GUIContent("*  ", "Incoming damage of this type is multiplied by this value.\n\n For example *20* incoming damage of this type with this multiplier at 0.8 would be reduced to *16*.");

    // Local Options Copy
    public Preferences Prefs;

    void OnEnable()
    {
        if (!Prefs) Prefs = StaticUtil.GetPrefsInEditor(true);
        TeamTex = new Texture2D(1, 1, TextureFormat.RGBA32, false) {hideFlags = HideFlags.HideAndDontSave};
        TeamGuiStyle.normal.background = TeamTex;
        _x = (Subject)target;
    }

    public override void OnInspectorGUI()
    {  
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();


        if (GUILayout.Button("General Information", EditorStyles.toolbarButton)) EditorUtils.SubjectGeneral = !EditorUtils.SubjectGeneral;
        if (EditorUtils.SubjectGeneral) ShowGeneral();

        if (GUILayout.Button("Primary Stats", EditorStyles.toolbarButton)) EditorUtils.SubjectStats = !EditorUtils.SubjectStats;
        if (EditorUtils.SubjectStats) ShowStats();

        if (GUILayout.Button("Damage Types", EditorStyles.toolbarButton)) EditorUtils.SubjectDmgTypes = !EditorUtils.SubjectDmgTypes;
        if (EditorUtils.SubjectDmgTypes) ShowDmgTypes();

        if (_x.Stats.SubjectGroup != SubjectGroup.Other)
        {
            if (GUILayout.Button("Weapons", EditorStyles.toolbarButton))
                EditorUtils.SubjectWeaponData = !EditorUtils.SubjectWeaponData;
            if (EditorUtils.SubjectWeaponData) ShowWeapons();
        }

        if (GUILayout.Button("Mecanim Ik Configuration", EditorStyles.toolbarButton)) EditorUtils.SubjectIk = !EditorUtils.SubjectIk;
        if (EditorUtils.SubjectIk) ShowIk();

        if (_x.Stats.SubjectGroup != SubjectGroup.Other)
        {
            if (GUILayout.Button("Controller/Animator Data", EditorStyles.toolbarButton))
                EditorUtils.SubjectControls = !EditorUtils.SubjectControls;
            if (EditorUtils.SubjectControls) ShowControls();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_x);
    }

    private void ShowGeneral()
    {
        EditorGUILayout.Space();

        switch (_x.Stats.TeamId)
        {
            case 0:
                _teamColor = Color.red;
                break;
            case 1:
                _teamColor = Color.blue;
                break;
            case 2:
                _teamColor = Color.cyan;
                break;
            case 3:
                _teamColor = Color.magenta;
                break;
            case 4:
                _teamColor = Color.yellow;
                break;
            case 5:
                _teamColor = Color.green;
                break;
            case 6:
                _teamColor = Color.black;
                break;
            case 7:
                _teamColor = Color.grey;
                break;
            case 8:
                _teamColor = Color.white;
                break;
        }
        if (TeamTex != null)
        {
            TeamTex.SetPixel(0, 0, _teamColor);
            TeamTex.Apply();
        }
        GUILayout.BeginVertical();
        GUILayout.Label(TeamTex, TeamGuiStyle, GUILayout.MaxWidth(1000), GUILayout.Height(5));
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        _x.Stats.SubjectGroup = (SubjectGroup) EditorGUILayout.EnumPopup(_subjectGroup, _x.Stats.SubjectGroup);
        _x.Stats.TeamId = EditorGUILayout.IntSlider(_teamId, _x.Stats.TeamId, 0, 8);
        _x.Stats.Title = EditorGUILayout.TextField(_title, _x.Stats.Title);
        _x.Stats.HitSound = EditorGUILayout.ObjectField(_hitReaction, _x.Stats.HitSound, typeof (AudioClip), false) as AudioClip;
        _x.Stats.ReactOnNoDmg = EditorGUILayout.Toggle("React To 0 Dmg Hit", _x.Stats.ReactOnNoDmg);

        EditorGUILayout.Space();

        _x.Stats.CrippledTime = EditorGUILayout.Slider(_crippledTime, _x.Stats.CrippledTime, 0f, 30f);
        _x.Stats.CorpseTime = EditorGUILayout.Slider(_corpseTime, _x.Stats.CorpseTime, 0f, 60f);
        _x.Stats.SpawnFx = EditorGUILayout.ObjectField("Spawn Fx", _x.Stats.SpawnFx, typeof(GameObject), false) as GameObject;
        _x.Stats.LevelUpFx = EditorGUILayout.ObjectField("LevelUp Fx", _x.Stats.LevelUpFx, typeof (GameObject), false) as GameObject;
        _x.Stats.DeathFx = EditorGUILayout.ObjectField("Death Fx", _x.Stats.DeathFx, typeof(GameObject), false) as GameObject;

        EditorGUILayout.Space();

        _x.Stats.RigType = (AnimationRigType)EditorGUILayout.EnumPopup(_rigType, _x.Stats.RigType);
        _x.Stats.UseMecanim = EditorGUILayout.Toggle(_useMecanim, _x.Stats.UseMecanim);

        EditorGUILayout.Space();

        _x.GodMode = EditorGUILayout.Toggle(_godMode, _x.GodMode);
        _x.UnlimitedAmmo = EditorGUILayout.Toggle(_unlimitedMags, _x.UnlimitedAmmo);
        _x.LogDebug = EditorGUILayout.Toggle("Log Debug", _x.LogDebug);

        EditorGUILayout.Space();
    }
    private void ShowStats()
    {
        EditorGUILayout.Space();
        _x.Stats.ArmorType = (ArmorType)EditorGUILayout.EnumPopup(_armorType, _x.Stats.ArmorType);
        EditorGUILayout.HelpBox(_x.Stats.ArmorType == ArmorType.Absorb
                ? "'Absorb' armor will absorb damage until drained. \nArmor degrades."
                : "'Nullify' armor reduces damage by specified amount. \nArmor does not degrade.", MessageType.None);
        
        EditorGUILayout.BeginHorizontal();

        EditorGUIUtility.labelWidth = 1;
        float fw = EditorGUIUtility.fieldWidth;
        EditorGUIUtility.fieldWidth = 5;
        EditorGUILayout.LabelField("");

        EditorGUILayout.LabelField("", "Base", GUILayout.MaxWidth(45));
        EditorGUILayout.LabelField("", "Min", GUILayout.MaxWidth(45));
        EditorGUILayout.LabelField("", "Max", GUILayout.MaxWidth(45));
        EditorGUILayout.LabelField("", "Per Lvl", GUILayout.MaxWidth(45));
        EditorGUILayout.LabelField("", "Actual", GUILayout.MaxWidth(45));
        EditorGUILayout.EndHorizontal();

        DrawStat(_x.Stats.Level, "Level");
        DrawStat(_x.Stats.Experience, "Xp");
        DrawStat(_x.Stats.XpReward, "Kill Xp");
        EditorGUILayout.Space();
        DrawStat(_x.Stats.Health, "Health");
        DrawStat(_x.Stats.Armor, "Armor");
        DrawStat(_x.Stats.Agility, "AGI");
        DrawStat(_x.Stats.Dexterity, "DEX");
        DrawStat(_x.Stats.Endurance, "END");
        DrawStat(_x.Stats.Strength, "STR");

        EditorGUIUtility.fieldWidth = fw;

        if (GUILayout.Button("Reset Stat Values")) { Subject.ResetCharacterStatValues(_x.Stats); }
        EditorGUILayout.Space();
    }
    private void ShowDmgTypes()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset", GUILayout.MaxWidth(50))) _x.Stats.DamageTypeMultipliers = StaticUtil.ResetDamageMultipliers();
        EditorGUILayout.HelpBox("Modify the names in the options menu \n(Window>Deftly Global Options)", MessageType.None);
        EditorGUILayout.EndHorizontal();
        if (_x.Stats.DamageTypeMultipliers.Length != Prefs.DamageTypes.Length)
        {
            _x.Stats.DamageTypeMultipliers = StaticUtil.ResetDamageMultipliers();
        }
        for (int i = 0; i < Prefs.DamageTypes.Length; i++)
        {
            EditorGUIUtility.labelWidth = 20;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Prefs.DamageTypes[i], GUILayout.MaxWidth(100));
            _x.Stats.DamageTypeMultipliers[i] = EditorGUILayout.Slider(_dmgMult, _x.Stats.DamageTypeMultipliers[i], 0, 3);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 1;
        }
        EditorGUILayout.Space();
    }
    private void ShowWeapons()
    {
        EditorGUILayout.Space();

        // show a runtime list of actual weapons
        // if (Application.isPlaying) EditorGUILayout.PropertyField(serializedObject.FindProperty("WeaponListRuntime"), new GUIContent("Runtime Weapon List"), true);
        

        GUI.color = Color.green;
        if (GUILayout.Button(ButtonAdd, GUILayout.Width(20f))) _x.WeaponListEditor.Add(null);
        GUI.color = Color.white;


        // Handle Weapon List
        if (_x.WeaponListEditor.Count > 0)
        {
            for (int i = 0; i < _x.WeaponListEditor.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel = 1;
                EditorGUIUtility.labelWidth = 120;
                Weapon w = null;
                if (_x.WeaponListEditor[i] != null) w = _x.WeaponListEditor[i].GetComponent<Weapon>();
                _x.WeaponListEditor[i] = EditorGUILayout.ObjectField(i + ". " + (w != null ? w.Stats.Title : "Blank"), _x.WeaponListEditor[i], typeof (GameObject), true) as GameObject;
                
                
                GUI.color = Color.red;
                if (GUILayout.Button(ButtonRemove, GUILayout.Width(20f)))
                {
                    _x.WeaponListEditor.RemoveAt(i);
                    return;
                }
                EditorGUI.indentLevel = 0;
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;


                if (_x.WeaponListEditor[i] == null) EditorGUILayout.HelpBox("Cannot have null weapon slots... (yet)", MessageType.Error);
            }
        }

        _x.SwapSound = EditorGUILayout.ObjectField(_changeWeapon, _x.SwapSound, typeof (AudioClip), false) as AudioClip;

        EditorGUILayout.Space();
    }
    private void ShowIk()
    {
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(_x.Stats.UseWeaponIk);
        _x.Stats.WeaponMountPoint = EditorGUILayout.ObjectField("Weapon Mount", _x.Stats.WeaponMountPoint, typeof (GameObject), true) as GameObject;
        EditorGUI.EndDisabledGroup();



        EditorGUIUtility.labelWidth = 50;

        EditorGUI.BeginDisabledGroup(!_x.Stats.UseMecanim);
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 50;
        _x.Stats.UseWeaponIk = EditorGUILayout.Toggle(_useWeaponIk, _x.Stats.UseWeaponIk);
        
        EditorGUI.BeginDisabledGroup(!_x.Stats.UseWeaponIk);
        _x.Stats.UseRightHandIk = EditorGUILayout.Toggle(_rhIk, _x.Stats.UseRightHandIk);
        _x.Stats.UseLeftHandIk = EditorGUILayout.Toggle(_lhIk, _x.Stats.UseLeftHandIk);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _x.InvertHandForward = EditorGUILayout.Toggle(_flipX, _x.InvertHandForward);
        _x.ShowGunDebug = EditorGUILayout.Toggle("Debug", _x.ShowGunDebug);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 75;
        EditorGUIUtility.fieldWidth = 10;
        _x.LeftHandIkLayer = EditorGUILayout.IntField(_lhIndex, _x.LeftHandIkLayer);
        _x.RightHandIkLayer = EditorGUILayout.IntField(_rhIndex, _x.RightHandIkLayer);
        _x.ControlStats.AnimatorOverrideLayerId = EditorGUILayout.IntField(_overrideLayer, _x.ControlStats.AnimatorOverrideLayerId);
        EditorGUILayout.EndHorizontal();
        EditorGUIUtility.fieldWidth = 50;
        if (_x.GetInstanceID() > 0)
        {
            EditorGUILayout.LabelField("Override Layer Name: Only shown on Scene objects.", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField(_x.MyAnimator != null ? "Name: ' " + _x.MyAnimator.GetLayerName(_x.ControlStats.AnimatorOverrideLayerId) + " '" : "Failed to query the Animator!", EditorStyles.boldLabel);
        }

        EditorGUILayout.Space();
        EditorGUIUtility.labelWidth = 100;
        _x.Stats.CharacterScale = EditorGUILayout.Slider(_cScale, _x.Stats.CharacterScale, 0.1f, 2f);

        EditorGUILayout.Space();
        EditorUtils.AddBlackLine();
        EditorUtils.AddBlackLine();
        EditorGUILayout.Space();



        if (GUILayout.Button("Select Right Hand Bone")) Selection.activeGameObject = _x.GetAnimator().GetBoneTransform(HumanBodyBones.RightHand).gameObject;

        EditorGUIUtility.labelWidth = 110;
        bool wm = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true;
        _x.ThumbDirection = EditorGUILayout.Vector3Field(_thumbDir, _x.ThumbDirection);
        _x.PalmDirection = EditorGUILayout.Vector3Field(_palmDir, _x.PalmDirection);
        EditorGUIUtility.wideMode = wm;

        EditorGUIUtility.labelWidth = 200;
        float fw = EditorGUIUtility.fieldWidth;
        EditorGUIUtility.fieldWidth = 20;

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.wideMode = false;
        GUILayout.Label(EditorGUIUtility.IconContent("MoveTool"));
        _x.DominantHandPosCorrection = EditorGUILayout.Vector3Field("Dominant Hand Additional Position",
            _x.DominantHandPosCorrection);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.IconContent("RotateTool"));
        _x.DominantHandRotCorrection = EditorGUILayout.Vector3Field("Dominant Hand Additional Rotation", _x.DominantHandRotCorrection);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.IconContent("ViewToolMove"));
        _x.WeaponPositionInHandCorrection = EditorGUILayout.Vector3Field(_wpnInHandPos, _x.WeaponPositionInHandCorrection);
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.wideMode = wm;
        EditorGUIUtility.fieldWidth = fw;


        EditorGUILayout.Space();
        EditorUtils.AddBlackLine();
        EditorUtils.AddBlackLine();
        EditorGUILayout.Space();



        if (GUILayout.Button("Select Left Hand Bone")) Selection.activeGameObject = _x.GetAnimator().GetBoneTransform(HumanBodyBones.LeftHand).gameObject;

        fw = EditorGUIUtility.fieldWidth;
        EditorGUIUtility.fieldWidth = 20;

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.wideMode = false;
        GUILayout.Label(EditorGUIUtility.IconContent("MoveTool"));
        _x.NonDominantHandPosCorrection = EditorGUILayout.Vector3Field("Non Dominant Hand Additional Position", _x.NonDominantHandPosCorrection);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.IconContent("RotateTool"));
        _x.NonDominantHandRotCorrection = EditorGUILayout.Vector3Field("Non Dominant Hand Additional Rotation", _x.NonDominantHandRotCorrection);
        EditorGUIUtility.wideMode = wm;
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.fieldWidth = fw;

        if (!_x.Stats.UseWeaponIk)
        {
            //reset these because some stuff targets them directly and hiding the inspector isn't enough.
            _x.Stats.UseLeftHandIk = false;
            _x.Stats.UseRightHandIk = false;
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
    }
    private void ShowControls()
    {
        EditorGUILayout.Space();

        EditorGUIUtility.labelWidth = 120;
        EditorGUI.indentLevel = 1;
        if (_x.Stats.UseMecanim)
        {
            EditorGUI.indentLevel = 0;
            _x.Stats.AnimatorHostObj = EditorGUILayout.ObjectField(_animHost, _x.Stats.AnimatorHostObj, typeof(GameObject), true) as GameObject;

            if (_x.Stats.AnimatorHostObj != null)
            {
                Animator anim = _x.Stats.AnimatorHostObj.GetComponent<Animator>();
                if (!anim) EditorGUILayout.HelpBox("No Animator component found on the Host Obj!", MessageType.Error);
                else _x.MyAnimator = anim;
            }

            EditorGUI.indentLevel = 1;
            if (_x.Stats.AnimatorHostObj == null) EditorGUILayout.HelpBox( "You must have an Animator. Specify the GameObject that has the Animator component on it.", MessageType.Error);

            if (_x.Stats.SubjectGroup == SubjectGroup.Intellect) EditorGUILayout.HelpBox("H/V params overidden by Intellect script.", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(_x.Stats.SubjectGroup == SubjectGroup.Intellect);
            _x.ControlStats.AnimatorHorizontal = EditorGUILayout.TextField(_horizParam, _x.ControlStats.AnimatorHorizontal);
            _x.ControlStats.AnimatorVertical = EditorGUILayout.TextField(_vertiParam, _x.ControlStats.AnimatorVertical);
            EditorGUI.EndDisabledGroup();

            _x.ControlStats.AnimatorDie = EditorGUILayout.TextField(_deathParam, _x.ControlStats.AnimatorDie);
            _x.ControlStats.AnimatorRevive = EditorGUILayout.TextField(_reviveParam, _x.ControlStats.AnimatorRevive);
            _x.ControlStats.AnimatorReload = EditorGUILayout.TextField(_reloadParam, _x.ControlStats.AnimatorReload);
            _x.ControlStats.AnimatorSwap = EditorGUILayout.TextField(_swapParam, _x.ControlStats.AnimatorSwap);
            _x.ControlStats.AnimatorWeaponType = EditorGUILayout.TextField(_wTypeParam, _x.ControlStats.AnimatorWeaponType);
        }

        EditorGUILayout.Space();

        EditorGUI.indentLevel = 0;
        EditorGUILayout.HelpBox("Note that Move Speed is overridden if using Root Motion. See Walk/Run Speed in PlayerController.", MessageType.None);
        _x.ControlStats.TurnSpeed = EditorGUILayout.Slider(_turnSpeed, _x.ControlStats.TurnSpeed, 1, 50);
        _x.ControlStats.MoveSpeed = EditorGUILayout.Slider(_moveSpeed, _x.ControlStats.MoveSpeed, 1, 35);

        EditorGUILayout.Space();

    }
    private void DrawStat(Stat s, string label)
    {
        EditorGUIUtility.fieldWidth = 1;
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 20;
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUIUtility.labelWidth = 1;

        s.Base = EditorGUILayout.IntField("Base", (int)s.Base, GUILayout.MaxWidth(45));
        s.Min = EditorGUILayout.IntField((int)s.Min, GUILayout.MaxWidth(45));    
        s.Max = EditorGUILayout.IntField((int)s.Max, GUILayout.MaxWidth(45));
        s.IncreasePerLevel = EditorGUILayout.FloatField(s.IncreasePerLevel, GUILayout.MaxWidth(45));
        s.Actual = EditorGUILayout.FloatField(s.Actual, GUILayout.MaxWidth(45));
        EditorGUILayout.EndHorizontal();
    }
}