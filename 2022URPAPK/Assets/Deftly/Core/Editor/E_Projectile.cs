// (c) Copyright Cleverous 2015. All rights reserved.

using System;
using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(Projectile))]
public class E_Projectile : Editor
{
    private Projectile _x;
    private static readonly GUIContent ButtonAdd = new GUIContent("+", "Add new tag filter");
    private static readonly GUIContent ButtonRemove = new GUIContent("-", "Remove this tag filter");

    // Basic Options
    private readonly GUIContent _title =        new GUIContent("Title", "The name of this projectile");
    private readonly GUIContent _weaponType =   new GUIContent("Weapon Type", "The manner in which the weapon is fired. Check the documentation for details");
    private readonly GUIContent _impactStyle =  new GUIContent("Impact Style", "How impact effect orientation is handled");
    private readonly GUIContent _mask =         new GUIContent("Hit Mask", "The Layers this projectile is allowed to hit, and damage.");
    private readonly GUIContent _stopMask =     new GUIContent("Stop Mask", "The piercing will stop if it hits any of these layers.");

    // Stats
    private readonly GUIContent _speed =        new GUIContent("Speed", "Speed of the projectile. High speeds are safe from passing through thin colliders");
    private readonly GUIContent _continuous =   new GUIContent("Constant", "Is speed continuous or a 'one shot' push?");
    private readonly GUIContent _damage =       new GUIContent("Damage", "Amount of damage this will inflict");
    private readonly GUIContent _aoeCaused =    new GUIContent("AoE", "Does this weapon have an AoE damage effect?");
    private readonly GUIContent _aoeFx =        new GUIContent("AoE Fx", "Prefab of the AoE effect which spawns at the point of detonation");
    private readonly GUIContent _aoeRadius =    new GUIContent("Radius", "Radius of the AoE damage");
    private readonly GUIContent _aoeForce =     new GUIContent("Force", "Explosion force caused by AoE");
    private readonly GUIContent _maxTravel =    new GUIContent("Max Travel", "The max distance this can travel before being destroyed");
    private readonly GUIContent _lifetime =     new GUIContent("Lifetime", "The amount of time this can exist before being destroyed");
    private readonly GUIContent _bouncer =      new GUIContent("Bouncer", "If on: Does not detonate on impact, waits for Lifetime to expire and causes AoE");
    private readonly GUIContent _usePhysics =   new GUIContent("Locomotion", "Translate does not require Rigidbody's to use Continuous and uses Transform.Translate");

    // Advanced
    private readonly GUIContent _detach =       new GUIContent("Detach On Destroy", "This object will be detached on a hit. Useful for having Particle Effects outlive the projectile mesh");
    private readonly GUIContent _muzzleFlash =  new GUIContent("Muzzle Flash", "This will be instantiated at the Spawn Pt when fired, usually a particle system prefab");
    private readonly GUIContent _lineRen =      new GUIContent("Line Renderer", "Using Raycast style weapons this is the trail effect to be rendered where the ray was fired.");
    private readonly GUIContent _lineAlphaS =   new GUIContent("Line Alpha Start", "How the alpha is affected over the life of the trail. (Start of trail)");
    private readonly GUIContent _lineAlphaE =   new GUIContent("Line Alpha End", "How the alpha is affected over the life of the trail. (End of trail)");
    private readonly GUIContent _lineUvSpeed =  new GUIContent("Line UV Speed", "How fast the UV pans over the life of the trail.");
    private readonly GUIContent _offsetRng =    new GUIContent("Offset Rng", "Random offset added to the regular offset to add some diversity to the appearance of the trail.");

    // Local Options Copy
    public Preferences Prefs;

    private const int _offsetMinLimit = 0;
    private const int _offsetMaxLimit = 10;

    void OnEnable()
    {
        if (!Prefs) Prefs = StaticUtil.GetPrefsInEditor(true);
        _x = (Projectile)target;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("General Information", EditorStyles.toolbarButton)) EditorUtils.ProjectileGeneral = !EditorUtils.ProjectileGeneral;
        if (EditorUtils.ProjectileGeneral) ShowBasic();

        if (GUILayout.Button("Effects", EditorStyles.toolbarButton)) EditorUtils.ProjectileEffects = !EditorUtils.ProjectileEffects;
        if (EditorUtils.ProjectileEffects) ShowEffects();

        if (GUILayout.Button("Impact Tags", EditorStyles.toolbarButton)) EditorUtils.ProjectileTags = !EditorUtils.ProjectileTags;
        if (EditorUtils.ProjectileTags) ShowTags();

        EditorGUILayout.Space();
        EditorGUILayout.Space(); 

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_x);
    }

    void ShowBasic()
    {        
        EditorGUILayout.Space();
        
        _x.LogDebug = EditorGUILayout.Toggle("Log Debug", _x.LogDebug);
        _x.Stats.Title = EditorGUILayout.TextField(_title, _x.Stats.Title);
        _x.Stats.weaponType = (ProjectileStats.WeaponType)EditorGUILayout.EnumPopup(_weaponType, _x.Stats.weaponType);
        _x.Stats.DamageType = EditorGUILayout.Popup("Damage Type", _x.Stats.DamageType, Prefs.DamageTypes);
        _x.ImpactStyle = (Projectile.ImpactType)EditorGUILayout.EnumPopup(_impactStyle, _x.ImpactStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Mask"), _mask, false);

        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType == ProjectileStats.WeaponType.Standard);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("StopMask"), _stopMask, false);
        EditorGUI.EndDisabledGroup();



        EditorGUILayout.Space();



        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType != ProjectileStats.WeaponType.Standard);
        EditorGUILayout.BeginHorizontal();

        EditorGUIUtility.labelWidth = 60;
        _x.Stats.Speed =            EditorGUILayout.Slider(_speed, _x.Stats.Speed, 0.5f, 80);
        EditorGUI.BeginDisabledGroup(_x.Stats.MoveStyle != ProjectileStats.ProjectileLocomotion.Physics);
        _x.Stats.ConstantForce =    EditorGUILayout.Toggle(_continuous, _x.Stats.ConstantForce);

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();
        _x.Stats.Damage =           EditorGUILayout.IntSlider(_damage, _x.Stats.Damage, 0, 200);
        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType != ProjectileStats.WeaponType.Standard);
        _x.Stats.CauseAoeDamage =   EditorGUILayout.Toggle(_aoeCaused, _x.Stats.CauseAoeDamage);
        if (_x.Stats.CauseAoeDamage)
        {
            EditorGUILayout.EndHorizontal();
            _x.Stats.AoeEffect =    EditorGUILayout.ObjectField(_aoeFx, _x.Stats.AoeEffect, typeof(GameObject), false) as GameObject;
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 45;
            _x.Stats.AoeRadius =    EditorGUILayout.FloatField(_aoeRadius, _x.Stats.AoeRadius);
            _x.Stats.AoeForce =     EditorGUILayout.FloatField(_aoeForce, _x.Stats.AoeForce);
            EditorGUIUtility.labelWidth = 60;
            _x.Stats.Bouncer =      EditorGUILayout.Toggle(_bouncer, _x.Stats.Bouncer);
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("NOTE: AoE Explosion Force does not affect Rigidbodies that have no active colliders. eg, dying Subjects.", EditorStyles.helpBox);
            GUI.color = Color.white;
        }
        else
        {
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUIUtility.labelWidth = 120;


        _x.Stats.MaxDistance =  EditorGUILayout.Slider(_maxTravel, _x.Stats.MaxDistance, 0.1f, 100);
        _x.Stats.Lifetime =     EditorGUILayout.Slider(_lifetime, _x.Stats.Lifetime, 0.1f, 10);
        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType != ProjectileStats.WeaponType.PiercingRaycast);
        _x.Stats.BeamRadius =   EditorGUILayout.Slider("Beam Radius", _x.Stats.BeamRadius, 0.1f, 5);
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType != ProjectileStats.WeaponType.Standard);
        _x.Stats.MoveStyle =    (ProjectileStats.ProjectileLocomotion)EditorGUILayout.EnumPopup(_usePhysics, _x.Stats.MoveStyle);
        EditorGUI.EndDisabledGroup();
        _x.Stats.UsePhysics = _x.Stats.MoveStyle == ProjectileStats.ProjectileLocomotion.Physics;

        EditorGUILayout.Space();

    }
    void ShowTags()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUI.color = Color.green;
        if (GUILayout.Button(ButtonAdd, GUILayout.Width(20f)))
        {
            _x.ImpactTagNames.Add(null);
            _x.ImpactEffects.Add(null);
            _x.ImpactSounds.Add(null);
        }
        GUI.color = Color.white;
        EditorGUIUtility.labelWidth = 300;
        EditorGUILayout.HelpBox("Note: 0) should be named 'Miss', sound/prefab optional but 'Miss' is required in case projectiles time out.", MessageType.None);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        DisplayCompounds();

        EditorGUILayout.Space();
    }
    void ShowEffects()
    {
        EditorGUILayout.Space();

        _x.DetachOnDestroy = EditorGUILayout.ObjectField(_detach, _x.DetachOnDestroy, typeof(GameObject), true) as GameObject;
        _x.AttackEffect = EditorGUILayout.ObjectField(_muzzleFlash, _x.AttackEffect, typeof(GameObject), false) as GameObject;

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType == ProjectileStats.WeaponType.Standard);
        _x.LineRenderer = EditorGUILayout.ObjectField(_lineRen, _x.LineRenderer, typeof(LineRenderer), true) as LineRenderer;
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_x.Stats.weaponType == ProjectileStats.WeaponType.Standard);
        _x.TrailAlphaStart = EditorGUILayout.CurveField(_lineAlphaS, _x.TrailAlphaStart);
        _x.TrailAlphaEnd = EditorGUILayout.CurveField(_lineAlphaE, _x.TrailAlphaEnd);
        _x.TrailUvCurve = EditorGUILayout.CurveField(_lineUvSpeed, _x.TrailUvCurve);
        _x.TrailUvSpeed = EditorGUILayout.Slider("UV Speed Scale", _x.TrailUvSpeed, 0, 5);
        _x.TrailTiling = EditorGUILayout.Vector2Field("UV Tiling", _x.TrailTiling);
        _x.TrailStartColor = EditorGUILayout.ColorField("Start Color", _x.TrailStartColor);
        _x.TrailEndColor = EditorGUILayout.ColorField("End Color", _x.TrailEndColor);
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 120;
        EditorGUILayout.MinMaxSlider(_offsetRng, ref _x.OffsetRngMin, ref _x.OffsetRngMax, _offsetMinLimit, _offsetMaxLimit);
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
    void DisplayCompounds()
    {
        for (int i = 0; i < _x.ImpactTagNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel = 1;
            EditorGUIUtility.labelWidth = 95;

            _x.ImpactTagNames[i] = EditorGUILayout.TextField(i + ")Tag Name", _x.ImpactTagNames[i]);

            EditorGUILayout.EndHorizontal();

            if (_x.ImpactTagNames.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 30;
                _x.ImpactSounds[i] = EditorGUILayout.ObjectField("♫", _x.ImpactSounds[i], typeof(AudioClip), false) as AudioClip;
                _x.ImpactEffects[i] = EditorGUILayout.ObjectField("☼", _x.ImpactEffects[i], typeof(GameObject), false) as GameObject;
                GUI.color = Color.red;
                if (GUILayout.Button(ButtonRemove, GUILayout.Width(20f)))
                {
                    _x.ImpactTagNames.RemoveAt(i);
                    _x.ImpactEffects.RemoveAt(i);
                    _x.ImpactSounds.RemoveAt(i);
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }
    }
}