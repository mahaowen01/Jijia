// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorUtils : Editor
{
    //
    // Try not to rename this stuff.
    // This keeps a memory of which foldouts are opened/closed.
    // Add a new bool for each new foldout required.
    //



    // Subject.cs Foldout memory
    public static bool SubjectGeneral {     get { return EditorPrefs.GetBool("Deftly_SubjectGeneral"); }     set { EditorPrefs.SetBool("Deftly_SubjectGeneral", value); } }
    public static bool SubjectStats {       get { return EditorPrefs.GetBool("Deftly_SubjectStats"); }       set { EditorPrefs.SetBool("Deftly_SubjectStats", value); } }
    public static bool SubjectDmgTypes {    get { return EditorPrefs.GetBool("Deftly_SubjectDmgTypes"); }    set { EditorPrefs.SetBool("Deftly_SubjectDmgTypes", value); } }
    public static bool SubjectWeaponData {  get { return EditorPrefs.GetBool("Deftly_SubjectWeaponData"); }  set { EditorPrefs.SetBool("Deftly_SubjectWeaponData", value); } }
    public static bool SubjectIk {          get { return EditorPrefs.GetBool("Deftly_SubjectIk"); }          set { EditorPrefs.SetBool("Deftly_SubjectIk", value); } }
    public static bool SubjectControls {    get { return EditorPrefs.GetBool("Deftly_SubjectControls"); }    set { EditorPrefs.SetBool("Deftly_SubjectControls", value); } }

    // Weapon.cs Foldout memory
    public static bool WeaponStats {        get { return EditorPrefs.GetBool("Deftly_WeaponStats"); }        set { EditorPrefs.SetBool("Deftly_WeaponStats", value); } }
    public static bool WeaponSoundsAndTiming { get { return EditorPrefs.GetBool("Deftly_WeaponSoundsAndTiming"); } set { EditorPrefs.SetBool("Deftly_WeaponSoundsAndTiming", value); } }
    public static bool WeaponAttacks {      get { return EditorPrefs.GetBool("Deftly_WeaponAttacks"); }      set { EditorPrefs.SetBool("Deftly_WeaponAttacks", value); } }
    public static bool WeaponSpawns {       get { return EditorPrefs.GetBool("Deftly_WeaponSpawns"); }       set { EditorPrefs.SetBool("Deftly_WeaponSpawns", value); } }
    public static bool WeaponAmmo {         get { return EditorPrefs.GetBool("Deftly_WeaponAmmo"); }         set { EditorPrefs.SetBool("Deftly_WeaponAmmo", value); } }
    public static bool WeaponIk {           get { return EditorPrefs.GetBool("Deftly_WeaponIk"); }           set { EditorPrefs.SetBool("Deftly_WeaponIk", value); } }
    public static bool WeaponImpactTags {   get { return EditorPrefs.GetBool("Deftly_WeaponImpactTags"); }   set { EditorPrefs.SetBool("Deftly_WeaponImpactTags", value); } }

    // Intellect.cs Foldout memory
    public static bool IntellectGeneral {   get { return EditorPrefs.GetBool("Deftly_IntellectGeneral"); }      set { EditorPrefs.SetBool("Deftly_IntellectGeneral", value); } }
    public static bool IntellectTargeting { get { return EditorPrefs.GetBool("Deftly_IntellectTargeting"); }    set { EditorPrefs.SetBool("Deftly_IntellectTargeting", value); } }
    public static bool IntellectAllyAssist {get { return EditorPrefs.GetBool("Deftly_IntellectAllyAssist"); }   set { EditorPrefs.SetBool("Deftly_IntellectAllyAssist", value); } }
    public static bool IntellectJuke {      get { return EditorPrefs.GetBool("Deftly_IntellectJuke"); }         set { EditorPrefs.SetBool("Deftly_IntellectJuke", value); } }
    public static bool IntellectAnimator {  get { return EditorPrefs.GetBool("Deftly_IntellectAnimator"); }     set { EditorPrefs.SetBool("Deftly_IntellectAnimator", value); } }
    public static bool IntellectWander {    get { return EditorPrefs.GetBool("Deftly_IntellectWander"); }       set { EditorPrefs.SetBool("Deftly_IntellectWander", value); } }
    public static bool IntellectPatrol {    get { return EditorPrefs.GetBool("Deftly_IntellectPatrol"); }       set { EditorPrefs.SetBool("Deftly_IntellectPatrol", value); } }

    // Projectile.cs Foldout memory
    public static bool ProjectileGeneral {  get { return EditorPrefs.GetBool("Deftly_ProjectileGeneral"); }     set { EditorPrefs.SetBool("Deftly_ProjectileGeneral", value); } }
    public static bool ProjectileTags {     get { return EditorPrefs.GetBool("Deftly_ProjectileTags"); }        set { EditorPrefs.SetBool("Deftly_ProjectileTags", value); } }
    public static bool ProjectileEffects {  get { return EditorPrefs.GetBool("Deftly_ProjectileEffects"); }     set { EditorPrefs.SetBool("Deftly_ProjectileEffects", value); } }

    // PlayerController.cs Foldout memory
    public static bool PlayControlsGeneral {    get { return EditorPrefs.GetBool("Deftly_PlayControlsGeneral"); }   set { EditorPrefs.SetBool("Deftly_PlayControlsGeneral", value); } }
    public static bool PlayControlsLabels {     get { return EditorPrefs.GetBool("Deftly_PlayControlsLabels"); }    set { EditorPrefs.SetBool("Deftly_PlayControlsLabels", value); } }
    public static bool PlayControlsPeriph {     get { return EditorPrefs.GetBool("Deftly_PlayControlsPeriph"); }    set { EditorPrefs.SetBool("Deftly_PlayControlsPeriph", value); } }

    // Spawner.cs Foldout memory
    public static bool SpawnerBasic {       get { return EditorPrefs.GetBool("Deftly_SpawnerBasic"); }      set { EditorPrefs.SetBool("Deftly_SpawnerBasic", value); } }
    public static bool SpawnerPrefabs {     get { return EditorPrefs.GetBool("Deftly_SpawnerPrefabs"); }    set { EditorPrefs.SetBool("Deftly_SpawnerPrefabs", value); } }
    public static bool SpawnerPoints {      get { return EditorPrefs.GetBool("Deftly_SpawnerPoints"); }     set { EditorPrefs.SetBool("Deftly_SpawnerPoints", value); } }

    // DeftlyCamera.cs Foldout memory
    public static bool CameraGeneral {      get { return EditorPrefs.GetBool("Deftly_CameraGeneral"); }     set { EditorPrefs.SetBool("Deftly_CameraGeneral", value); } }
    public static bool CameraTargets {      get { return EditorPrefs.GetBool("Deftly_CameraTargets"); }     set { EditorPrefs.SetBool("Deftly_CameraTargets", value); } }
    public static bool CameraConfinement {  get { return EditorPrefs.GetBool("Deftly_CameraConfinement"); } set { EditorPrefs.SetBool("Deftly_CameraConfinement", value); } }

    // EnemySpawner.cs Foldout memory
    public static bool EnemySpawnerGeneral {    get { return EditorPrefs.GetBool("Deftly_EnemySpawnerGeneral"); }       set { EditorPrefs.SetBool("Deftly_EnemySpawnerGeneral", value); } }
    public static bool EnemySpawnerSettings {   get { return EditorPrefs.GetBool("Deftly_EnemySpawnerSettings"); }      set { EditorPrefs.SetBool("Deftly_EnemySpawnerSettings", value); } }
    public static bool EnemySpawnerSpawnables { get { return EditorPrefs.GetBool("Deftly_EnemySpawnerSpawnables"); }    set { EditorPrefs.SetBool("Deftly_EnemySpawnerSpawnables", value); } }
    public static bool EnemySpawnerOther {      get { return EditorPrefs.GetBool("Deftly_EnemySpawnerOther"); }         set { EditorPrefs.SetBool("Deftly_EnemySpawnerOther", value); } }

    private static readonly GUIContent ButtonAdd = new GUIContent("+", "Add");
    private static readonly GUIContent ButtonRemove = new GUIContent("-", "Remove");

    public static void AddBlackLine()
    {
        GUI.color = Color.black;
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUI.color = Color.white;
    }
    public static void ShowList<T>(List<T> targetList, ref bool foldStatus, GUIContent label = null) where T : class
    {
        EditorGUILayout.Space();
        foldStatus = EditorGUILayout.Foldout(foldStatus, label);
        if (!foldStatus) return;

        EditorGUI.indentLevel++;
        GUI.color = Color.green; 
        if (GUILayout.Button(ButtonAdd, GUILayout.Width(20))) targetList.Add(default(T));
        GUI.color = Color.white;

        if (targetList.Count > 0)
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel++;
                EditorGUIUtility.labelWidth = 120;
                targetList[i] = EditorGUILayout.ObjectField(i + ". ", targetList[i] as Object, typeof(T), true) as T;

                GUI.color = Color.red;
                if (GUILayout.Button(ButtonRemove, GUILayout.Width(20f)))
                {
                    targetList.RemoveAt(i);
                    return;
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndHorizontal();
                GUI.color = Color.white;
            }
        }
        EditorGUI.indentLevel--;
    }
}