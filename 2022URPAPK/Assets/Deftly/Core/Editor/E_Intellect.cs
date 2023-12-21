// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(Intellect))]
public class E_Intellect : Editor
{
    private Subject _s;
    private Intellect _x;
    
    // General
    private readonly GUIContent _ignoreUpdates      = new GUIContent("Ignore Updates", "How many frames to ignore before processing the AI");
    private readonly GUIContent _senseFrequency     = new GUIContent("Sense Frequency", "How many AI process ticks to ignore before updating mood sensory, context and conditions");
    private readonly GUIContent _navSampleSize      = new GUIContent("Nav Sample Size", "How many Units in game space to search around for valid destinations on the NavMesh when evaluating a target position.\n\nLower values mean weaker Nav solutions in complex environments, Higher values can lead to performance drops.");
    private readonly GUIContent _provokeType        = new GUIContent("Provoke Type", "How the AI becomes provoked");
    private readonly GUIContent _threatPriority     = new GUIContent("Threat Priority", "Priority choice for selecting targets");
    private readonly GUIContent _sightMask          = new GUIContent("Sight Mask", "The Layers that the AI is *allowed* to see. Used for navigation and confirming line of sight to targets");
    private readonly GUIContent _threatMask         = new GUIContent("Threat Mask", "An optimization mask, only Subjects which could be threats should be specified");
    private readonly GUIContent _fleeHealth         = new GUIContent("Flee Health", "When health reaches this value, the AI will Flee");
    private readonly GUIContent _alertTime          = new GUIContent("Alert Time", "Time spent in the Alert mood before returning to normal. Disabled until improved implementation.");

    // Targeting
    private readonly GUIContent _provokeDelay       = new GUIContent("Provoke Delay", "After being provoked, how long to wait before taking action");
    private readonly GUIContent _firingAngle        = new GUIContent("Firing Angle", "If the target is inside this range then the AI will fire. Red debug Lines");
    private readonly GUIContent _useFov             = new GUIContent("Use FoV", "Does the AI requires Line Of Sight acquisition to become provoked?");
    private readonly GUIContent _fovAngle           = new GUIContent("FoV Angle", "Field of view for the AI, in angle units facing forward. Blue debug lines.");
    private readonly GUIContent _fovProx            = new GUIContent("FoV Proximity", "A circular proximity around the Subject which is combined with the FOV.");
    private readonly GUIContent _sightRange         = new GUIContent("Sight Range", "The sight range, how far can the AI can see before recognizing Subjects. Green outer debug lines.");
    private readonly GUIContent _standoff           = new GUIContent("Standoff Dist", "During combat, the Intellect will attempt to maintain a gap between itself and the target while using ranged weapons.");

    // Ally Assist
    private readonly GUIContent _helpAllies         = new GUIContent("Help Allies", "If an ally is provoked or has a bigger threat, this Intellect will assist.");
    private readonly GUIContent _helpType           = new GUIContent("Ally Awareness", "Filter for acquiring allies, either strictly in FoV or within the max Sight Range.");
    private readonly GUIContent _requireLos         = new GUIContent("Require LoS", "Line of Sight is required identify something as an ally.");

    // Juke
    private readonly GUIContent _doesJuke           = new GUIContent("Does Juke", "Enable or disable Juking behaviors.");
    private readonly GUIContent _jukeTime           = new GUIContent("Juke Time", "How much time to spend moving in the Juke direction.");
    private readonly GUIContent _jukeFreq           = new GUIContent("Juke Frequency", "How often to do a new juke maneuver.");
    private readonly GUIContent _jukeRng            = new GUIContent("Juke Freq Rng", "Rng +/- range for Juke Time. Recommended to keep this lower than the Frequency value to avoid negative timing frequencies.");

    // Animator Settings
    private readonly GUIContent _animDir            = new GUIContent("Animator Direction", "In the Animator Controller, the name of the Direction parameter");
    private readonly GUIContent _animSpeed          = new GUIContent("Animator Speed", "In the Animator Controller, the name of the Speed parameter");
    private readonly GUIContent _animDamp           = new GUIContent("Animator Dampening", "The dampening of the inputs for locomotion in the Animator.");
    private readonly GUIContent _acelleration       = new GUIContent("Acelleration", "The acelleration of locomotion, used by Unity's Agent and Navigation/Pathfinding.");

    // Wander Settings
    private readonly GUIContent _wanderRange        = new GUIContent("Wander Range", "The nearby distance/range the Intellect will wander in.");
    private readonly GUIContent _doesWander         = new GUIContent("Does Wander", "Toggle Wandering On/Off.");
    private readonly GUIContent _wanderOrigin       = new GUIContent("Wander At Origin", "When ON, Wander Range will only apply to where the intellect spawned which keeps it in a tight area. \n\nWhen OFF, the Wander Range applies to wherever the Intellect is, so it could wander anywhere.");
    private readonly GUIContent _wanderTmin         = new GUIContent("Time Min", "The minimum time between Wander stages.");
    private readonly GUIContent _wanderTmax         = new GUIContent("Time Max", "The maximum time between Wander stages");
    private readonly GUIContent _wanderNavSamples   = new GUIContent("Sample Range", "A random position in range is chosen for a destination. NavMesh will try to sample this position for validity and continue in the Sample Range to try to find a valid point on the NavMesh.");

    // Patrol Settings
    private readonly GUIContent _patrolPoints       = new GUIContent("Patrol Points", "The nodes in sequence which the AI will Patrol");
    private readonly GUIContent _doesPatrol         = new GUIContent("Does Patrol", "When ON, this Intellect will patrol the specified points. Overrides Wandering.");
    private readonly GUIContent _patrolWaitTime     = new GUIContent("Patrol Wait Time", "After reaching a patrol point the Intellect will wait there for these seconds.");
    
    void OnEnable()
    {
        _x = (Intellect)target;
        _s = _x.GetComponent<Subject>();
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("General Settings", EditorStyles.toolbarButton)) EditorUtils.IntellectGeneral = !EditorUtils.IntellectGeneral;
        if (EditorUtils.IntellectGeneral) ShowGeneral();

        if (GUILayout.Button("Targeting and Acquisition", EditorStyles.toolbarButton)) EditorUtils.IntellectTargeting = !EditorUtils.IntellectTargeting;
        if (EditorUtils.IntellectTargeting) ShowTargeting();

        if (GUILayout.Button("Ally Assistance", EditorStyles.toolbarButton)) EditorUtils.IntellectAllyAssist = !EditorUtils.IntellectAllyAssist;
        if (EditorUtils.IntellectAllyAssist) ShowAllyAssist();

        if (GUILayout.Button("Juke Settings", EditorStyles.toolbarButton)) EditorUtils.IntellectJuke = !EditorUtils.IntellectJuke;
        if (EditorUtils.IntellectJuke) ShowJuke();

        if (GUILayout.Button("Animator Settings", EditorStyles.toolbarButton)) EditorUtils.IntellectAnimator = !EditorUtils.IntellectAnimator;
        if (EditorUtils.IntellectAnimator) ShowAnimator();

        if (GUILayout.Button("Patrolling", EditorStyles.toolbarButton)) EditorUtils.IntellectPatrol = !EditorUtils.IntellectPatrol;
        if (EditorUtils.IntellectPatrol) ShowPatrol();

        if (GUILayout.Button("Wandering", EditorStyles.toolbarButton)) EditorUtils.IntellectWander = !EditorUtils.IntellectWander;
        if (EditorUtils.IntellectWander) ShowWander();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_x);
    }

    void ShowGeneral()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 80;
        _x.LogDebug = EditorGUILayout.Toggle("Log Debug", _x.LogDebug);
        _x.DrawGizmos = EditorGUILayout.Toggle("Draw Gizmos", _x.DrawGizmos);
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 125;
        _x.IgnoreUpdates = EditorGUILayout.IntSlider(_ignoreUpdates, _x.IgnoreUpdates, 0, 3);
        _x.SenseFrequency = EditorGUILayout.IntSlider(_senseFrequency, _x.SenseFrequency, 0, 3);
        _x.NavSampleSize = EditorGUILayout.Slider(_navSampleSize, _x.NavSampleSize, 1, 6);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("SightMask"), _sightMask, false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ThreatMask"), _threatMask, false);

        EditorGUILayout.Space();
    }
    void ShowTargeting()
    {
        EditorGUILayout.Space();

        _x.MyProvokeType = (Intellect.ProvokeType)EditorGUILayout.EnumPopup(_provokeType, _x.MyProvokeType);
        _x.MyThreatPriority = (Intellect.ThreatPriority)EditorGUILayout.EnumPopup(_threatPriority, _x.MyThreatPriority);
        _x.ProvokeDelay = EditorGUILayout.Slider(_provokeDelay, _x.ProvokeDelay, 0, 3);
        EditorGUI.BeginDisabledGroup(true);
        _x.AlertTime = EditorGUILayout.Slider(_alertTime, _x.AlertTime, 0, 30);
        EditorGUI.EndDisabledGroup();
        _x.FleeHealthThreshold = EditorGUILayout.IntSlider(_fleeHealth, _x.FleeHealthThreshold, 0, (int)_s.Stats.Health.Max-1);
        _x.StandoffDistance = EditorGUILayout.Slider(_standoff, _x.StandoffDistance, 1, _x.SightRange);

        EditorUtils.AddBlackLine();

        _x.FiringAngle = EditorGUILayout.Slider(_firingAngle, _x.FiringAngle, 1, 359);
        _x.SightRange = EditorGUILayout.Slider(_sightRange, _x.SightRange, 1, 50);
        _x.UseFov = EditorGUILayout.Toggle(_useFov, _x.UseFov);

        EditorGUI.BeginDisabledGroup(!_x.UseFov);
        _x.FieldOfView = EditorGUILayout.Slider(_fovAngle, _x.FieldOfView, 1, 359);
        _x.FovProximity = EditorGUILayout.Slider(_fovProx, _x.FovProximity, 0, _x.SightRange);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
    void ShowAllyAssist()
    {
        EditorGUILayout.Space();

        _x.HelpAllies = EditorGUILayout.Toggle(_helpAllies, _x.HelpAllies);

        EditorGUI.BeginDisabledGroup(!_x.HelpAllies);
        if (!_x.UseFov) _x.AllyAwareness = Intellect.AllyAwarenessType.InRange;
        _x.AllyAwareness = (Intellect.AllyAwarenessType)EditorGUILayout.EnumPopup(_helpType, _x.AllyAwareness);
        _x.RequireLosToAlly = EditorGUILayout.Toggle(_requireLos, _x.RequireLosToAlly);
        _x.MaxAllyCount = EditorGUILayout.IntSlider("Max Allies", _x.MaxAllyCount, 1, 10);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
    void ShowJuke()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("A 'Juke' is a sort of sidestep maneuver.", MessageType.Info);

        _x.DoesJuke = EditorGUILayout.Toggle(_doesJuke, _x.DoesJuke);
        _x.JukeTime = EditorGUILayout.Slider(_jukeTime, _x.JukeTime, 0.2f, 5f);
        _x.JukeFrequency = EditorGUILayout.Slider(_jukeFreq, _x.JukeFrequency, 0.1f, 15);
        _x.JukeFrequencyRandomness = EditorGUILayout.Slider(_jukeRng, _x.JukeFrequencyRandomness, 0.1f, 5);

        EditorGUILayout.Space();
    }
    void ShowAnimator()
    {
        EditorGUILayout.Space();

        _x.AnimatorDirection = EditorGUILayout.TextField(_animDir, _x.AnimatorDirection);
        _x.AnimatorSpeed = EditorGUILayout.TextField(_animSpeed, _x.AnimatorSpeed);
        _x.AnimatorDampening = EditorGUILayout.Slider(_animDamp, _x.AnimatorDampening, 0, 1);
        _x.AgentAcceleration = EditorGUILayout.Slider(_acelleration, _x.AgentAcceleration, 0, 100);

        EditorGUILayout.Space();
    }
    void ShowPatrol()
    {
        EditorGUILayout.Space();

        _x.DoesPatrol = EditorGUILayout.Toggle(_doesPatrol, _x.DoesPatrol);

        EditorGUI.BeginDisabledGroup(!_x.DoesPatrol);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("PatrolPoints"), _patrolPoints, true);
        _x.PatrolWaitTime = EditorGUILayout.Slider(_patrolWaitTime, _x.PatrolWaitTime, 0, 30);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
    void ShowWander()
    {
        EditorGUILayout.Space();

        _x.DoesWander = EditorGUILayout.Toggle(_doesWander, _x.DoesWander);

        EditorGUI.BeginDisabledGroup(!_x.DoesWander);
        _x.WanderAtOrigin = EditorGUILayout.Toggle(_wanderOrigin, _x.WanderAtOrigin);
        _x.WanderRange = EditorGUILayout.Slider(_wanderRange, _x.WanderRange, 1, 20);
        _x.WanderTimeMin = EditorGUILayout.Slider(_wanderTmin, _x.WanderTimeMin, 0.5f, 30);
        if (_x.WanderTimeMax < _x.WanderTimeMin) _x.WanderTimeMax = _x.WanderTimeMin;
        _x.WanderTimeMax = EditorGUILayout.Slider(_wanderTmax, _x.WanderTimeMax, 0.5f, 30);
        if (_x.WanderTimeMin > _x.WanderTimeMax) _x.WanderTimeMin = _x.WanderTimeMax;
        _x.SampleRange = EditorGUILayout.Slider(_wanderNavSamples, _x.SampleRange, 1, 6);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
}