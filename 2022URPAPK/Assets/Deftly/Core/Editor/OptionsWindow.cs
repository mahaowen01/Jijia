// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Deftly
{
    public class OptionsWindow : EditorWindow
    {
        // General
        private readonly GUIContent _showTxt =      new GUIContent("Show Floating Damage", "Will floating damage text be shown?");
        private readonly GUIContent _prefab =       new GUIContent("Prefab", "The prefab which contains a GUIText and Floating Damage script.");
        private readonly GUIContent _difficulty =   new GUIContent("Game Difficulty", "The Game Difficulty Level (if used)");
        private readonly GUIContent _pickup =       new GUIContent("Weapon Pickup Auto Switch", "Automatically change to the weapon after picking it up?");
        private readonly GUIContent _rpgStuff =     new GUIContent("RPG Elements", "Use Leveling and Stat features?");
        private readonly GUIContent _friendlyFire = new GUIContent("Friendly Fire", "Subjects of the same team can damage one another?");
        private readonly GUIContent _peaceMode =    new GUIContent("Peaceful Mode", "Intellect's don't attack.");

        public Preferences Prefs;

        // Misc
        public static int ActiveOptionsTab;

        private readonly Color _active = new Color(1, 1, 1, 1);
        private readonly Color _inactive = new Color(1, 1, 1, 0.7f);
        private static List<string> _liveList = new List<string>();
        public static Rect WindowRect = new Rect(150, 150, 400, 400);
        public static OptionsWindow Window;

        [MenuItem("Window/Deftly Global Options")]
        private static void Init()
        {
            Window = (OptionsWindow) GetWindow(typeof (OptionsWindow));
            Window.titleContent.text = "Deftly Options";
            Window.position = WindowRect;
            Window.Show();
        }

        void OnEnable()
        {
            if (!Prefs) Prefs = StaticUtil.GetPrefsInEditor(true);
        }
        void OnGUI()
        {
            GUI.changed = false;

            ShowHeader();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            switch (ActiveOptionsTab)
            {
                case (0):
                    ShowPreferences();
                    break;
                case (1):
                    ShowDamageTypes();
                    break;
                case (2):
                    ShowAbout();
                    break;
            } 
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            ShowFooter();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(Prefs);
            }
        }

        private void ShowHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = ActiveOptionsTab == 0 ? _active : _inactive;
            if (GUILayout.Button("General", EditorStyles.toolbarButton)) ActiveOptionsTab = 0;

            GUI.color = ActiveOptionsTab == 1 ? _active : _inactive;
            if (GUILayout.Button("Damage Types", EditorStyles.toolbarButton)) ActiveOptionsTab = 1;

            GUI.color = ActiveOptionsTab == 2 ? _active : _inactive;
            if (GUILayout.Button("Links", EditorStyles.toolbarButton)) ActiveOptionsTab = 2;
            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
        }
        private void ShowAbout()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Release Notes")) Application.OpenURL("http://www.cleverous.com/#!deftly-release-notes/c1cdb");
            if (GUILayout.Button("Trello")) Application.OpenURL("https://trello.com/b/Wc9Zqt6r/cleverous-tds-kit-deftly");
            if (GUILayout.Button("Jabbr Chat")) Application.OpenURL("https://jabbr.net/#/rooms/Cleverous");
            if (GUILayout.Button("Beta Group")) Application.OpenURL("https://groups.google.com/forum/#!forum/deftly-beta");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Special Thanks to: ", EditorStyles.boldLabel);
            GUI.skin.label.wordWrap = true;
            EditorGUILayout.LabelField(
                "Jean Fabre, Alex Chouls, Sebastain 'SebasRez' Alvarez, Nils '600' Jakrins, Jasper Flick, Emil 'AngryAnt' Johansen, Tony Li and all the nice folks on Unity Forum/Answers/Slack.",
                EditorStyles.wordWrappedMiniLabel);
        }
        private void ShowFooter()
        {
            EditorGUILayout.Space();
            GUI.color = Color.grey;
            EditorGUILayout.LabelField("Deftly™ Version: " + Prefs.AssetVersion + ", © Cleverous™ 2015");
        }
        private void ShowPreferences()
        {
            EditorGUILayout.LabelField("Preferences", EditorStyles.boldLabel);

            Prefs.UseFloatingText = EditorGUILayout.Toggle(_showTxt, Prefs.UseFloatingText);
            EditorGUI.BeginDisabledGroup(!Prefs.UseFloatingText);
            Prefs.TextPrefab = EditorGUILayout.ObjectField(_prefab, Prefs.TextPrefab, typeof(GameObject), true) as GameObject;
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(true);
            Prefs.Difficulty = EditorGUILayout.FloatField(_difficulty, Prefs.Difficulty);
            EditorGUI.EndDisabledGroup();
            Prefs.WeaponPickupAutoSwitch = EditorGUILayout.Toggle(_pickup, Prefs.WeaponPickupAutoSwitch);
            EditorGUI.BeginDisabledGroup(true);
            Prefs.UseRpgElements = EditorGUILayout.Toggle(_rpgStuff, Prefs.UseRpgElements);
            EditorGUI.EndDisabledGroup();
            Prefs.FriendlyFire = EditorGUILayout.Toggle(_friendlyFire, Prefs.FriendlyFire);
            Prefs.PeacefulMode = EditorGUILayout.Toggle(_peaceMode, Prefs.PeacefulMode);
        }
        private void ShowDamageTypes()
        {
            // Initialize if needed.
            if (Prefs.DamageTypes.Length != 10)
            {
                _liveList = new List<string>();
                for (int i = 0; i < 10; i++) _liveList.Add("Nameless Type");
                Prefs.DamageTypes = _liveList.ToArray();
            }

            for (int i = 0; i < Prefs.DamageTypes.Length; i++)
            {
                Prefs.DamageTypes[i] = EditorGUILayout.TextField("Damage Type " + i, Prefs.DamageTypes[i]);
            }
        }
    }
}