// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deftly
{
    /// <summary>
    /// Designed to deal with level entry and exit on a per-scene basis.
    /// Manages level-specific data and victory conditions. Should not be persistent between levels but instead should 
    /// define each level's unique references and win conditions.
    /// </summary>
    public class DeftlyGameManager : MonoBehaviour
    {
        public static DeftlyGameManager GameManager;

        public enum VictoryConditions { TargetsDestroyed, KillCount, Time }
        public VictoryConditions VictoryRules;

        public enum GameStatusType { NotPlaying, InProgress, Victory, Defeat }
        public GameStatusType GameStatus = GameStatusType.NotPlaying;

        // Victory Data
        public int VictoryKillCount;
        public float VictoryTime;

        // UI Data
        public string MissionText1;
        public string MissionText2;
        public string ObjectiveText1;

        // Predefined Scene
        public DeftlyCamera MainDeftlyCamera;
        public Transform MainRespawn;
        public List<GameObject> VictoryTargets;

        // Runtime Scene
        public List<Subject> RuntimePlayerList;

        public int ThisSceneIndex;

        void Awake()
        {
            ThisSceneIndex = SceneManager.GetActiveScene().buildIndex;
            GameStatus = GameStatusType.InProgress;
            GameManager = this;
        }
        void Update()
        {
            if (GameStatus != GameStatusType.InProgress) return;
            CheckForDefeat();
            CheckForVictory();
        }

        private void CheckForVictory()
        {
            switch (VictoryRules)
            {
                case VictoryConditions.KillCount:
                    if (GetTotalKillCount() >= VictoryKillCount) GameStatus = GameStatusType.Victory;
                    break;
                case VictoryConditions.TargetsDestroyed:
                    if (CheckIfTargetsAreDestroyed()) GameStatus = GameStatusType.Victory;
                    break;
                case VictoryConditions.Time:
                    if (Time.time >= VictoryTime) GameStatus = GameStatusType.Victory;
                    break;
            }
        }
        private void CheckForDefeat()
        {
            if (RuntimePlayerList.Count <= 0) return;
            int deadCount = 0;
            foreach (Subject x in RuntimePlayerList)
            {
                if (x.IsDead) deadCount++;
            }
            if (deadCount >= RuntimePlayerList.Count) GameStatus = GameStatusType.Defeat;
        }
        private bool CheckIfTargetsAreDestroyed()
        {
            return VictoryTargets.TrueForAll(x => !x);
        }
        private int GetTotalKillCount()
        {
            return RuntimePlayerList.Sum(player => player.Stats.Kills);
        }
    }
}