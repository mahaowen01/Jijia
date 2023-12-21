// (c) Copyright Cleverous 2015. All rights reserved.
using UnityEngine;
using UnityEditor;
using Deftly;

[CustomEditor(typeof(DeftlyCamera))]
public class E_Camera : Editor
{
    private DeftlyCamera _x;

    private readonly GUIContent _following =        new GUIContent("Follow Style", "The 'feel' of how the camera follows the targets");
    private readonly GUIContent _rotation =         new GUIContent("Rotation Style", "Allow or disallow camera rotation to follow targets. Note you may need to enable Editor Tracking ON if so it can calibrate before play.");
    private readonly GUIContent _tracking =         new GUIContent("Tracking Style", "Option to follow the Average Position or the Average 'aiming direction'");
    private readonly GUIContent _trackDistance =    new GUIContent("Track Distance", "The distance from each character that is sampled during tracking");
    private readonly GUIContent _trackSpeed =       new GUIContent("Track Speed", "How fast the camera tracks its targets with Loose mode");
    private readonly GUIContent _offset =           new GUIContent("Position Offset", "The literal positional offset in xyz from the camera's targets");
    private readonly GUIContent _editorTracking =   new GUIContent("In Editor Tracking", "The script will set the position of the Camera in the scene while not in Play Mode.");

    private readonly GUIContent _frustumDebug =     new GUIContent("Show Debug", "Show a debug gizmo indicating the size of the Frustum Confinement area.");
    private readonly GUIContent _frustumUse =       new GUIContent("Use Confinement", "Add colliders at the edges of the screen so players can't walk past them. Useful for preventing 2 or more Targets from separating too much.");
    private readonly GUIContent _frustumLayer =     new GUIContent("Frustum Layer", "The script will set the position of the Camera in the scene while not in Play Mode.");
    private readonly GUIContent _frustumBumper =    new GUIContent("Use Bumpers", "Add a small buffer to the edge of the screen, moving the barriers inward.");
    private readonly GUIContent _frustumBumperSize= new GUIContent("Bumper Size", "How much edge buffering. Values roughly in screen size percentages. Scale X for Horizontal (left/right), Y for Vertical (top/bottom).");

    void OnEnable()
    {
        _x = (DeftlyCamera)target;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("General Configuration", EditorStyles.toolbarButton)) EditorUtils.CameraGeneral = !EditorUtils.CameraGeneral;
        if (EditorUtils.CameraGeneral) ShowGeneral();

        if (GUILayout.Button("Camera Targets", EditorStyles.toolbarButton)) EditorUtils.CameraTargets = !EditorUtils.CameraTargets;
        if (EditorUtils.CameraTargets) ShowTargets();

        if (GUILayout.Button("Player Confinement System", EditorStyles.toolbarButton)) EditorUtils.CameraConfinement = !EditorUtils.CameraConfinement;
        if (EditorUtils.CameraConfinement) ShowConfinement();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_x);
    }

    void ShowGeneral()
    {
        EditorGUILayout.Space();

        _x.FollowingStyle = (DeftlyCamera.MoveStyle) EditorGUILayout.EnumPopup(_following, _x.FollowingStyle);
        _x.RotationStyle = (DeftlyCamera.MoveStyle) EditorGUILayout.EnumPopup(_rotation, _x.RotationStyle);
        _x.Tracking = (DeftlyCamera.TrackingStyle) EditorGUILayout.EnumPopup(_tracking, _x.Tracking);

        _x.InEditorTracking = EditorGUILayout.Toggle(_editorTracking, _x.InEditorTracking);
        _x.TrackDistance = EditorGUILayout.Slider(_trackDistance, _x.TrackDistance, 0f, 10f);
        _x.TrackSpeed = EditorGUILayout.Slider(_trackSpeed, _x.TrackSpeed, 1f, 20f);
        _x.Offset = EditorGUILayout.Vector3Field(_offset, _x.Offset);

        bool noTargets = _x.Targets.Count == 0;
        if (noTargets)
        {
            EditorGUILayout.HelpBox("No Camera Targets are specified! \n\nThe Camera rotation must to be calibrated after any camera transform changes " +
                                    "if you are using Stiff Rotation Style with no predefined targets. ",
                MessageType.Warning);
        }
        EditorGUI.BeginDisabledGroup(_x.Targets.Count > 0);
        GUI.color = noTargets? Color.yellow : Color.grey;
        if (GUILayout.Button("Calibrate Camera Rotation"))
        {
            _x.CalibrateRotation();
        }
        GUI.color = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
    void ShowTargets()
    {
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Targets"), true);
        EditorGUILayout.Space();
    }
    void ShowConfinement()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        _x.UseConfinement = EditorGUILayout.Toggle(_frustumUse, _x.UseConfinement);
        EditorGUI.BeginDisabledGroup(!_x.UseConfinement);
        _x.ShowDebugGizmos = EditorGUILayout.Toggle(_frustumDebug, _x.ShowDebugGizmos);
        EditorGUILayout.EndHorizontal();

        _x.FrustumLayer = EditorGUILayout.LayerField(_frustumLayer, _x.FrustumLayer);
        _x.UseFrustumBumper = EditorGUILayout.Toggle(_frustumBumper, _x.UseFrustumBumper);
        EditorGUI.BeginDisabledGroup(!_x.UseFrustumBumper);
        _x.FrustumBumperSize = EditorGUILayout.Vector2Field(_frustumBumperSize, _x.FrustumBumperSize);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.HelpBox("Note: Make sure the Frustum Layer only collides with Players in the Collision Matrix", MessageType.Info);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }
}