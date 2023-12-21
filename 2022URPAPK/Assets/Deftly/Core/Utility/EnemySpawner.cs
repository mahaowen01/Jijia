using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deftly
{
    public class EnemySpawner : MonoBehaviour
    {
        public enum VictoryStyle { SpawnerIsDead, NoEnemiesLeft, LastWaveNoEnemies }
        public enum PositionStyle { FixedPositions, RandomInRadius }

        public VictoryStyle VictoryType;
        public PositionStyle PositionType;

        // Physical representation of the Spawner itself. Can be destroyed if setup that way. Will require a Subject script.
        public GameObject DestroyableAvatar;
        public LayerMask CanTriggerThis;

        // How many waves and how much time between them. If not using waves, time is used between spawns to climb to MaintainLiveCount.
        public int NumberOfWaves;
        public float TimeBetweenWaves;
        public int MaxEnemyCount;
        public float SpawnRadius;
        public List<Door> DoorsToAffect;

        // Predefined Prefab and Count lists
        public List<GameObject> EnemyPrefabs = new List<GameObject>();
        public List<int> EnemyCounts = new List<int>();

        // Live lists
        public Dictionary<GameObject, int> EnemyReferences;
        public List<GameObject> Positions = new List<GameObject>();

        protected bool ReachedVictory;

        private List<Subject> _remainingEnemies;
        private bool _startedAndWorking;
        private int _currentWaveNumber;
        private float _time;
         
        void Awake()
        {
            _remainingEnemies = new List<Subject>();

            EnemyReferences = new Dictionary<GameObject, int>();
            for (int i = 0; i < EnemyPrefabs.Count; i++) EnemyReferences.Add(EnemyPrefabs[i], EnemyCounts[i]);
        }
        void Start()
        {
            foreach (Door door in DoorsToAffect)
            {
                if (!door.StartClosed)
                {
                    door.Locked = false;
                    door.Open();
                }
            }
        }
        void OnEnable()
        {
            ReachedVictory = false;
            _currentWaveNumber = 0;
        }
        void OnTriggerEnter(Collider col)
        {
            if (_startedAndWorking || !StaticUtil.LayerMatchTest(CanTriggerThis, col.gameObject)) return;
            _startedAndWorking = true;

            // StartCoroutine(CloseDoors());
            // StartCoroutine(VictoryLoop());
        }
        void Update()
        {
            if (!_startedAndWorking | ReachedVictory) return;
            _time += Time.deltaTime;

            if (_time > TimeBetweenWaves)
            {
                CleanList();
                CheckForVictory();

                _time = 0;
                if (ReachedVictory) return;
                
                SpawnWave();
            }
        }

        public void SpawnWave()
        {
            if (_remainingEnemies.Count >= MaxEnemyCount) return;
            switch (PositionType)
            {
                case PositionStyle.FixedPositions:
                    foreach (KeyValuePair<GameObject, int> key in EnemyReferences)
                    {
                        for (int i = 0; i < key.Value; i++)
                        {
                            Transform t = Positions[Random.Range(0, Positions.Count)].transform;
                            AddToEnemies(StaticUtil.Spawn(key.Key, t.position, t.rotation) as GameObject);
                        }
                    }
                    break;
                case PositionStyle.RandomInRadius:
                    foreach (KeyValuePair<GameObject, int> key in EnemyReferences)
                    {
                        for (int i = 0; i < key.Value; i++)
                        {
                            Vector2 t = Random.insideUnitCircle * SpawnRadius;
                            AddToEnemies(StaticUtil.Spawn(key.Key, new Vector3(t.x, transform.position.y, t.y), Quaternion.identity) as GameObject);
                        }
                    }
                    break;
            }
        }

        public void CleanList()
        {
            // Clean Lists
            for (int i = 0; i < _remainingEnemies.Count; i++)
            {
                if (!_remainingEnemies[i] || _remainingEnemies[i].IsDead)
                {
                    _remainingEnemies.RemoveAt(i);
                }
            }
        }
        public void CheckForVictory()
        {
            // Check for victory
            switch (VictoryType)
            {
                case VictoryStyle.SpawnerIsDead:
                    if (!DestroyableAvatar) ReachedVictory = true;
                    break;
                case VictoryStyle.LastWaveNoEnemies:
                    if (_currentWaveNumber >= NumberOfWaves && _remainingEnemies.Count == 0) ReachedVictory = true;
                    break;
                case VictoryStyle.NoEnemiesLeft:
                    if (_remainingEnemies.Count == 0) ReachedVictory = true;
                    break;
            }

            if (ReachedVictory)
            {
                OpenDoors();
                if (VictoryType == VictoryStyle.SpawnerIsDead && DestroyableAvatar != gameObject) Destroy(gameObject);
            }
        }

        public void CloseDoors()
        {
            foreach (Door door in DoorsToAffect)
            {
                door.Locked = true;
                door.Close();
            }
        }
        public void OpenDoors()
        {
            foreach (Door door in DoorsToAffect)
            {
                door.Locked = false;
                door.Open();
            }
        }
        public void AddToEnemies(GameObject go)
        {
            _remainingEnemies.Add(go.GetComponent<Subject>());
        }
    }
}