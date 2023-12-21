// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Deftly
{
    [CustomEditor(typeof (EnemySpawner))]
    public class E_EnemySpawner : Editor
    {
        private EnemySpawner _x;
        private static readonly GUIContent ButtonAdd = new GUIContent("+", "Add Enemy");
        private static readonly GUIContent ButtonRemove = new GUIContent("-", "Remove Enemy");

        private readonly GUIContent _mask = new GUIContent("Can Trigger This", "Which Layers can trigger this?");
        private readonly GUIContent _doors = new GUIContent("Doors to affect", "Optionally tell doors to Open when victory conditions are met.");
        private readonly GUIContent _avatar = new GUIContent("Destroyable Avatar", "The physical representation of this Spawner. You can put a Subject, Collider and Rigidbody on it to make the spawner destroyable.");

        private static bool _esFold1;

        private void OnEnable()
        {
            _x = (EnemySpawner) target;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("General", EditorStyles.toolbarButton)) EditorUtils.EnemySpawnerGeneral = !EditorUtils.EnemySpawnerGeneral;
            if (EditorUtils.EnemySpawnerGeneral) ShowGeneral();

            if (GUILayout.Button("Spawn Settings", EditorStyles.toolbarButton)) EditorUtils.EnemySpawnerSettings = !EditorUtils.EnemySpawnerSettings;
            if (EditorUtils.EnemySpawnerSettings) ShowSettings();

            if (GUILayout.Button("Spawnables", EditorStyles.toolbarButton)) EditorUtils.EnemySpawnerSpawnables = !EditorUtils.EnemySpawnerSpawnables;
            if (EditorUtils.EnemySpawnerSpawnables) ShowSpawnables();

            if (GUILayout.Button("Positions", EditorStyles.toolbarButton)) EditorUtils.EnemySpawnerOther = !EditorUtils.EnemySpawnerOther;
            if (EditorUtils.EnemySpawnerOther) ShowOther();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed) EditorUtility.SetDirty(_x);
        }
        private void ShowGeneral()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Enemy Spawner design still in progress. Please report any issues.", MessageType.None);

            _x.VictoryType = (EnemySpawner.VictoryStyle)EditorGUILayout.EnumPopup("Victory Condition", _x.VictoryType);
            _x.PositionType = (EnemySpawner.PositionStyle)EditorGUILayout.EnumPopup("Position Type", _x.PositionType);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CanTriggerThis"), _mask, false);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

        }
        private void ShowSettings()
        {
            EditorGUILayout.Space();

            _x.DestroyableAvatar = EditorGUILayout.ObjectField(_avatar, _x.DestroyableAvatar, typeof(GameObject), false) as GameObject;

            _x.NumberOfWaves = EditorGUILayout.IntSlider("Fixed Wave Count", _x.NumberOfWaves, 1, 90);
            _x.TimeBetweenWaves = EditorGUILayout.Slider("Time Between Waves", _x.TimeBetweenWaves, 1, 60);
            _x.MaxEnemyCount = EditorGUILayout.IntSlider("Max Enemy Count", _x.MaxEnemyCount, 1, 60);

            EditorGUI.BeginDisabledGroup(_x.PositionType == EnemySpawner.PositionStyle.FixedPositions);
            _x.SpawnRadius = EditorGUILayout.Slider("Spawn Radius", _x.SpawnRadius, 1, 60);
            EditorGUI.EndDisabledGroup();

            EditorUtils.AddBlackLine();
            EditorUtils.ShowList(_x.DoorsToAffect, ref _esFold1, _doors);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

        }
        private void ShowSpawnables()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Prefabs that will spawn, and their respective counts.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 1;
            float fw = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.fieldWidth = 5;

            GUI.color = Color.green;
            if (GUILayout.Button(ButtonAdd, GUILayout.Width(20f)))
            {
                _x.EnemyPrefabs.Add(null);
                _x.EnemyCounts.Add(1);
            }
            GUI.color = Color.white;

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("", "Prefab", GUILayout.MaxWidth(130));
            EditorGUILayout.LabelField("", "Count", GUILayout.MaxWidth(70));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _x.EnemyPrefabs.Count; i++)
            {
                DrawEnemy(_x.EnemyPrefabs, _x.EnemyCounts, i);
            }

            EditorGUIUtility.fieldWidth = fw;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
        private void ShowOther()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Possible locations to spawn at, chosen randomly.", MessageType.None);

            GUI.color = Color.green;
            if (GUILayout.Button(ButtonAdd, GUILayout.Width(20f))) _x.Positions.Add(null);
            GUI.color = Color.white;

            for (int i = 0; i < _x.Positions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Point " + (i + 1), EditorStyles.boldLabel);
                _x.Positions[i] = EditorGUILayout.ObjectField(_x.Positions[i], typeof(GameObject), false, GUILayout.MaxWidth(130)) as GameObject;

                GUI.color = Color.red;
                if (GUILayout.Button(ButtonRemove, GUILayout.Width(20f)))
                {
                    _x.Positions.RemoveAt(i);
                    return;
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        private void DrawEnemy(List<GameObject> enemyRefList, List<int> enemyCountList, int index)
        {
            EditorGUIUtility.fieldWidth = 1;
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            EditorGUILayout.LabelField("Enemy " + (index+1), EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 1;

            enemyRefList[index] = EditorGUILayout.ObjectField(enemyRefList[index], typeof (GameObject), false, GUILayout.MaxWidth(130)) as GameObject;
            enemyCountList[index] = EditorGUILayout.IntField(enemyCountList[index], GUILayout.MaxWidth(45));
            GUI.color = Color.red;
            if (GUILayout.Button(ButtonRemove, GUILayout.Width(20f)))
            {
                _x.EnemyPrefabs.RemoveAt(index);
                _x.EnemyCounts.RemoveAt(index);
                return;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }
}