// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProjectDawn.Navigation;

#if UNITY_EDITOR
using UnityEditor; // wrapped #if to avoid accidental build errors from auto-imported namespaces.
#endif

namespace Deftly
{
    [AddComponentMenu("Deftly/Intellect")]
    [RequireComponent(typeof(Subject))]
    //[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    public class Intellect : MonoBehaviour
    {
        #region ### Variable Definitions
        // DATA STUFF
        public bool LogDebug;
        public bool DrawGizmos;
        public int IgnoreUpdates; // how many *Frames* to skip before running the **Process**.
        public int SenseFrequency; // how many **Processes** to skip between sensory updates.
        public Vector3 MyOriginPoint;

        // PROVOKING AND ALLIES
        public enum ProvokeType { TargetIsInRange, TargetAttackedMe }
        public enum ThreatPriority { Nearest, MostDamage }
        public ProvokeType MyProvokeType;
        public ThreatPriority MyThreatPriority;
        public float ProvokeDelay;
        public bool HelpAllies;
        public enum AllyAwarenessType { InRange, InFov }
        public AllyAwarenessType AllyAwareness;
        public bool RequireLosToAlly;

        public int MaxAllyCount = 10;
        public bool Provoked;
        public int FleeHealthThreshold;
        public float AlertTime;
        public bool UseFov;

        public int RetargetDamageThreshold;

        // JUKE SETUP
        public bool DoesJuke = true;
        public float JukeTime;
        public float JukeFrequency;
        public float JukeFrequencyRandomness;
        private bool _juking;
        private Vector3 _jukeHeading;

        // RANGES AND MASKS
        public float FiringAngle;
        public float FieldOfView;
        public float FovProximity;
        
        public float SightRange;
        public float AttackRange;
        public float EngageThreshold;
        public float StandoffDistance;

        public LayerMask SightMask;
        public LayerMask ThreatMask;

        public float NavSampleSize = 5;

        // WANDER
        public bool DoesWander;
        public bool WanderAtOrigin;
        public float WanderRange = 10;       
        public float WanderTimeMin = 0.5f;
        public float WanderTimeActual = 1;
        public float WanderTimeMax = 10;
        private float _wanderTimer;
        public float SampleRange = 5;

        // PATROL
        public bool DoesPatrol;
        public List<Transform> PatrolPoints;
        public float PatrolWaitTime = 2;
        private int _currentPatrolPt;
        private float _patrolWaitTimer;
        private bool _waitingOnPatrol;
        private bool _movingOnPatrol;

        // NAVIGATION / ANIMATOR
        //public UnityEngine.AI.NavMeshAgent Agent;
        public string AnimatorDirection;
        public string AnimatorSpeed;
        public float AnimatorDampening;
        public float AgentAcceleration;

        // LIVE PUBLIC VARIABLES
        public Dictionary<Subject, int> ThreatList;
        public List<Subject> AllyList;
        public Subject CurrentTarget;
        public enum Mood { Idle, Patrol, Wander, Flee, Alert, Combat, Dead }

        // PRIVATE VARIABLES USED/CACHED INTERNALLY
        private GameObject _go;
        private bool _hasWeapons;
        private Animator _animator;
        private Weapon _weapon;
        private float _weaponSpeed;
        private bool _needToReload;
        private Transform _startTransform;
        private bool _waiting;
        private int _scanClock;
        private Vector3 _victimLastPos;
        //private Rigidbody _rb;
        //private CapsuleCollider _collider;
        private AgentSonarAvoid _collider;
        private Vector3 _myVelocity;
        private Vector3 _myLastPos;

        protected bool NoAmmoLeft;
        protected Subject ThisSubject;
        protected bool Fleeing;
        protected bool MoodSwitch;
        protected Mood MyMood;
        protected Mood MyLastMood;
        #endregion

        // Init and Editor stuff
        void Reset()
        {
            LogDebug = false;
            IgnoreUpdates = 1;
            SenseFrequency = 1;
            
            MyProvokeType = ProvokeType.TargetIsInRange;
            ProvokeDelay = 1f;
            HelpAllies = true;

            DoesJuke = false;
            JukeTime = 2f;
            JukeFrequency = 2f;
            JukeFrequencyRandomness = 1f;

            UseFov = true;
            FieldOfView = 120f;
            FovProximity = 2f;
            FiringAngle = 5f;
            SightRange = 12f;
            EngageThreshold = 1f;
            StandoffDistance = 3f;

            FleeHealthThreshold = 0;

            SightMask = -1;
            ThreatMask = -1;
            AlertTime = 3f;

            WanderRange = 3f;
            WanderTimeMin = 1;
            WanderTimeMax = 4;
            WanderAtOrigin = true;

            DoesPatrol = false;
            PatrolWaitTime = 0;

            AnimatorSpeed = "speed";
            AnimatorDirection = "direction";
            AnimatorDampening = 0.1f;
            AgentAcceleration = 100f;

            MaxAllyCount = 10;
        }
        void OnEnable()
        {
            ThreatList = new Dictionary<Subject, int>();
            AllyList = new List<Subject>();
        }
        void Awake()
        {
            ////_rb = GetComponent<Rigidbody>();
            //_rb.angularDrag = 1000f;
            //_rb.drag = 1000f;
            //_rb.mass = 1f;
            //_rb.interpolation = RigidbodyInterpolation.Interpolate;
            ////_rb.constraints = (RigidbodyConstraints) 84;

            _startTransform = transform;
            ThreatList = new Dictionary<Subject, int>();
            AllyList =  new List<Subject>();
            ThisSubject = GetComponent<Subject>();
            //Agent = AgentGetComponent;
        }
        void Start()
        {
            // _patrolWaitTimer = 9999;
            _go = gameObject;
            _collider = GetComponent<AgentSonarAvoid>();
            MyOriginPoint = transform.position;

            //_curPatrol = 0;
            _hasWeapons = (ThisSubject.WeaponListRuntime.Count > 0);

            Fleeing = false;
            MyMood = Mood.Idle;

           // AgentConfigure();
            
            ThisSubject.OnDeath += Die;
            ThisSubject.OnAttacked += Attacked;
            ThisSubject.OnSwitchedWeapon += UpdateWeapon;

            if (_hasWeapons) UpdateWeapon(ThisSubject.GetCurrentWeaponGo());
          //  Debug.Log(1);
            if (ThisSubject.Stats.UseMecanim)
            {
                _animator = ThisSubject.GetAnimator();
                if (_animator == null) Debug.LogWarning("检查对Animator Host,确认它有一个Animator组件。");
            }

            StartCoroutine(StateMachine());
        }
        void OnDrawGizmosSelected()
        {
            if (!DrawGizmos) return;
            if (!Application.isPlaying) MyOriginPoint = transform.position;

            // SIGHT range
            //
            if (SightRange > 0)
            {
                Gizmos.color = Color.green;
                for (float i = 0; i < 360; i += 0.5f)
                {
                    Gizmos.DrawRay(
                        transform.position + Vector3.up + Quaternion.Euler(0, i, 0) * new Vector3(0, 0, SightRange),
                        Quaternion.Euler(0, i, 0) * new Vector3(0, 0, -0.1f));
                }
            }

            // Firing Angle
            //
            if (FiringAngle > 0)
            {
                Gizmos.color = Color.red;
                Vector3 dirR = Quaternion.AngleAxis(FiringAngle/2, Vector3.up)*transform.forward;
                Vector3 dirL = Quaternion.AngleAxis(-FiringAngle/2, Vector3.up)*transform.forward;
                Gizmos.DrawRay(Vector3.up + transform.position, dirR * SightRange);
                Gizmos.DrawRay(Vector3.up + transform.position, dirL * SightRange);
            }

            // Field Of View
            //
            if (FieldOfView > 0 && UseFov)
            {
                Gizmos.color = Color.blue;
                Vector3 dirR = Quaternion.AngleAxis(FieldOfView/2, Vector3.up)*transform.forward;
                Vector3 dirL = Quaternion.AngleAxis(-FieldOfView/2, Vector3.up)*transform.forward;
                Gizmos.DrawRay(Vector3.up + transform.position, dirR * SightRange);
                Gizmos.DrawRay(Vector3.up + transform.position, dirL * SightRange);

                // FoV Proximity
                //
                if (FovProximity > 0 && UseFov)
                {
                    Gizmos.color = Color.blue;
                    for (int i = 0; i < 360; i += 1)
                    {
                        Gizmos.DrawRay(
                            transform.position + Vector3.up + Quaternion.Euler(0, i, 0) * new Vector3(0, 0, FovProximity),
                            Quaternion.Euler(0, i, 0) * new Vector3(0, 0, -0.1f));
                    }
                }
            }

            // WANDER range
            //
            if (WanderRange > 0 && DoesWander)
            {
                Gizmos.color = Color.grey;
                for (int i = 0; i < 360; i += 1)
                {
                    Gizmos.DrawRay(
                        (WanderAtOrigin ? MyOriginPoint : transform.position) + Vector3.up + Quaternion.Euler(0, i, 0) * new Vector3(0, 0, WanderRange),
                        Quaternion.Euler(0, i, 0) * new Vector3(0, 0, -0.1f));
                }
            }

            // Allies and Threat Indicator Lines
            //
            if (AllyList.Count > 0)
            {
                foreach (var ally in AllyList)
                {
                    Debug.DrawLine(transform.position + Vector3.up, ally.transform.position + Vector3.up, Color.green);
                }
            }

            if (Provoked && CurrentTarget) Debug.DrawLine(transform.position + Vector3.up, CurrentTarget.transform.position + Vector3.up, Color.red);
            if (ThreatList != null && ThreatList.Count > 0)
            {
                foreach (var threat in ThreatList)
                {
                    Debug.DrawLine(transform.position + Vector3.up, threat.Key.transform.position + Vector3.up, Color.yellow);
                }
            }

            // STANDOFF position
            //
            if (CurrentTarget)
            {
                Debug.DrawLine(gameObject.transform.position, GetStandoffPosition(CurrentTarget.gameObject));
            }

            // PATROL lines
            //
            if (DoesPatrol && PatrolPoints.Count > 0)
            {
                if (PatrolPoints.Any(t => t == null))
                {
                    Debug.LogWarning(gameObject.name + " cannot have null patrol points! Make sure all Patrol Points are valid GameObjects.");
                    return;
                }
                Debug.DrawLine(transform.position, PatrolPoints[0].position, Color.grey);
                for (int i = 0; i < PatrolPoints.Count; i++)
                {
                    Debug.DrawLine(PatrolPoints[i].position, i == PatrolPoints.Count - 1 ? PatrolPoints[0].position : PatrolPoints[i + 1].position, Color.black);
                }
            }

            #if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
            #endif
        }

        // Primary loop and context/condition analysis
        void DoLocomotion()
        {
            // aim either at the target or toward our path.
            if (Provoked && _hasWeapons && CurrentTarget && TargetCanBeSeen(CurrentTarget.gameObject))
            {
                LeadTarget(CurrentTarget.gameObject);
            }
            //else if (AgentDesiredVelocity != Vector3.zero)
            //{
            //    LookAt(transform.position + AgentDesiredVelocity);
            //}

            // adjust threshold for tiny distance movement here.
            _myVelocity = (transform.position - _myLastPos).magnitude < 0.01f 
                ? Vector3.zero 
                : (transform.position - _myLastPos).normalized;
            
            _myLastPos = transform.position;

            float speed = 0;
            float angle = 0;

            if (_myVelocity != Vector3.zero)
            {
                speed = _myVelocity.magnitude;
                angle = Vector3.Angle(transform.forward, _myVelocity);
                angle *= Mathf.Deg2Rad;
            }

            _animator.SetFloat(AnimatorDirection, angle, AnimatorDampening, Time.deltaTime);
            _animator.SetFloat(AnimatorSpeed, speed, AnimatorDampening, Time.deltaTime);
        }
        private IEnumerator StateMachine()
        {
            if (ThisSubject.WeaponListRuntime.Count > 0) UpdateWeapon(ThisSubject.GetCurrentWeaponGo()); // Intellect does not get the first weapon update call so we Init here.
            int i = IgnoreUpdates;
            while (true)
            {
                while (ThisSubject.IsDead) { yield return null; }
                DoLocomotion();
                while (i > 0)
                {
                    i--;
                    yield return null;
                }

                i = IgnoreUpdates;

                yield return StartCoroutine(ProcessConditions());

                switch (MyMood)
                {
                    case (Mood.Idle):
                        StartCoroutine(Idle());
                        break;
                    case (Mood.Patrol):
                        StartCoroutine(Patrol());
                        break;
                    case (Mood.Wander):
                        StartCoroutine(Wander());
                        break;
                        /*
                    case (Mood.Alert):
                        StartCoroutine(Alert()); // this isn't working right at the moment.
                        break;
                        */
                    case (Mood.Flee):
                        StartCoroutine(Flee());
                        break;
                    case (Mood.Combat):
                        StartCoroutine(Combat());
                        break;
                    case (Mood.Dead):
                        StartCoroutine(Dead());
                        break;
                }
            }
        }
        private IEnumerator ProcessConditions()
        {
            if (ThisSubject.IsDead) yield break;
            if (MyMood != Mood.Combat && _hasWeapons) _weapon.Attacking = false;
            if (MyProvokeType == ProvokeType.TargetAttackedMe) Fleeing = (ThisSubject.Health <= FleeHealthThreshold && ThisSubject.LastAttacker);
            if (MyProvokeType == ProvokeType.TargetIsInRange) Fleeing = (ThisSubject.Health <= FleeHealthThreshold && CurrentTarget);
            if (_scanClock >= SenseFrequency)
            {
                // NOTE ::::::: CurrentTarget is assigned here ONLY! :::::
                ScanForAllSubjects();
                CurrentTarget = FindThreat().Key;
                _scanClock = 0;
            }
            _scanClock++;

            // NOTE: Order of Mood processing matters here. Upper evaluations have priority over the ones below.
            // If a Mood condition is met then no other moods are processed.
            //
            // ### Mood Conditions (Possible States)
            //

            #region ### Consider Dead
            if (ThisSubject.IsDead)
            {
                Provoked = false;
                yield return MyMood = Mood.Dead;
                yield break;
            }
            #endregion
            #region ### Consider Alert
            /*
            // TODO since we could have non-dangerous threats, this needs to look at the biggest threat value instead.
            if (MyMood == Mood.Combat && ThreatList.Count == 0)
            {
                // just dropped out of combat and there's no dangerous threats, be Alert
                Provoked = false;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Alert;
                yield break;
            }
            if (_waiting) yield break;
            if (Fleeing)
            {
                Provoked = true;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Flee;
                yield break;
            }
             * */
            #endregion
            #region ### Consider Combat
            if (CurrentTarget && Provoked && !StaticUtil.Preferences.PeacefulMode)
            {
                if (MyLastMood != Mood.Combat)
                {
                    StartCoroutine(DelayProvoke());
                }

                Provoked = true;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Combat;
                yield break;
            }
            #endregion

            // Provoked above, unprovoked below
            Provoked = false;
            
            #region ### Consider Patrol
            if (PatrolPoints.Count > 0 && DoesPatrol)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Patrol;
                yield break;
            }
            #endregion
            #region ### Consider Wander
            if (WanderRange > 0f && DoesWander)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Wander;
                yield break;
            }
            #endregion
            #region ### Consider Idle
            if (!Provoked)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Idle;
            }
            #endregion
        }

        // Possible AI states
        private IEnumerator Idle()
        {
            //if (Vector3.Distance(AgentDestination, _startTransform.position) > 0.1)
            //{
            //    MoveTo(_startTransform.position);
            //}
            //else
            //{
            //    AgentResume();
            //}
            //if (LogDebug) Debug.Log(_go.name + " Mood: Idle.");

            yield return null;
        }
        private IEnumerator Patrol()
        {
            if (LogDebug) Debug.Log(_go.name + " Mood: Patrol.");
            if (Provoked) yield break;
            if (PatrolPoints.Count <= 0 || PatrolPoints.Any(point => !point))
            {
                Debug.LogWarning(_go.name + " has no patrol point destinations.");
                yield break;
            }

            // Am I idling at a point? If so, add to the timer.
            if (_waitingOnPatrol) _patrolWaitTimer += Time.deltaTime;

            // Am I idling and the timer is elapsed?
            if (_waitingOnPatrol && _patrolWaitTimer >= PatrolWaitTime)
            {
                _patrolWaitTimer = 0;
                _currentPatrolPt++;
                if (_currentPatrolPt > PatrolPoints.Count-1) _currentPatrolPt = 0;
                MoveTo(PatrolPoints[_currentPatrolPt].position);
            }

            //AgentStoppingDistance = 0.2f;
            bool closeEnough = Vector3.Distance(transform.position, PatrolPoints[_currentPatrolPt].position) <= 1;
            if (closeEnough)
            {
                _movingOnPatrol = false;
                _waitingOnPatrol = true;
            }
            else if (!_movingOnPatrol)
            {
                MoveTo(PatrolPoints[_currentPatrolPt].position);
                _waitingOnPatrol = false;
                _movingOnPatrol = true;
            }
            yield return null;
        }
        private IEnumerator Wander()
        {
            if (LogDebug) Debug.Log(_go.name + " Mood: Wander.");
            _wanderTimer += Time.deltaTime;
            if (_wanderTimer >= WanderTimeActual)
            {
                WanderTimeActual = Random.Range(WanderTimeMin, WanderTimeMax);
                MoveTo(GetPosNearby((WanderAtOrigin ? MyOriginPoint : transform.position), WanderRange, NavSampleSize));
                _wanderTimer = 0;
            }

            yield return null;
        }
        private IEnumerator Flee()
        {
            if (ThisSubject.IsDead) yield break;

            if (_hasWeapons) _weapon.Attacking = false;
            //AgentStoppingDistance = 0;
            if (!TargetIsInRange(CurrentTarget.gameObject, SightRange))
            {
                if (LogDebug) Debug.Log("I'm out of [my] sight range from my target.");
                yield break;
            }
            //if (AgentIsPathStale | AgentRemainingDistance < 1)
            //{
            //    if (LogDebug) Debug.Log("Destination is stale, repathing.");
            //    // TODO better algorithm to decide flee destination
            //    int rng = Random.Range(0, 10);
            //    if (rng > 5) MoveTo(GetPosNearby(transform.position, SightRange, NavSampleSize));
            //    else MoveTo(GetPosFleeing(SightRange));
            //    yield break;
            //}
            //if (AgentRemainingDistance < 1)
            //{
            //    if (LogDebug) Debug.Log("Reached Destination, repathing.");
            //    MoveTo(GetPosNearby(transform.position, 15, NavSampleSize));
            //    yield break;
            //}
            //if (Vector3.Distance(AgentDestination, CurrentTarget.transform.position) <= 5f)
            //{
            //    if (LogDebug) Debug.Log("Destination is too close to the Attacker, repathing.");
            //    MoveTo(GetPosNearby(transform.position, SightRange, NavSampleSize));
            //    yield break;
            //}

            yield return null;
        }
        /*
        private IEnumerator Alert()
        {
            if (LogDebug) Debug.Log(_go.name + " Mood: Alert.");

            if (_waiting) yield break;
            _waiting = true;
            yield return new WaitForSeconds(AlertTime);
            _waiting = false;
            
            AgentStoppingDistance = 0.1f;
            MoveTo(_startTransform.position);
        }
         * */
        private IEnumerator Combat()
        {
            if (LogDebug) Debug.Log(_go.name + " Mood: Combat!...");
            if (!_hasWeapons) yield break;
            if (_waiting) yield break;
            if (_needToReload && !_weapon.IsReloading) yield return _weapon.StartCoroutine("Reload"); // TODO is there a better way to call this so it can refactor clean?
            if (ThisSubject.IsDead) yield break;
            if (!CurrentTarget)
            {
                _weapon.Attacking = false;
                yield break;
            }
            if (CurrentTarget.IsDead)
            {
                //AgentStoppingDistance = AttackRange + EngageThreshold;
                _weapon.Attacking = false;
                yield break;
            }
            if (!CanFire())
            {
                _weapon.Attacking = false;
                yield break;
            }

            // At this point, I know I *could* fire my weapon.
            // If I'm in range, look at the target, if I'm not, Move to it.

            if (TargetCanBeSeen(CurrentTarget.gameObject) && TargetIsInRange(CurrentTarget.gameObject, AttackRange))
            {
                bool moving = false;
                if (_weapon.Stats.WeaponType == WeaponType.Ranged && TooClose(CurrentTarget.gameObject))
                {
                    moving = true;
                    MoveTo(GetStandoffPosition(CurrentTarget.gameObject));
                }
                else if (DoesJuke && !_juking) StartCoroutine(Juke());

                _weapon.Attacking = _weapon.Stats.WeaponType == WeaponType.Melee || TargetIsInAngle(CurrentTarget.gameObject, FiringAngle);
                //AgentStoppingDistance = moving ? 0.1f : AttackRange;
            }
            else // move in
            {
                //AgentStoppingDistance = 0.1f;
                MoveTo(CurrentTarget.transform.position);
                _weapon.Attacking = false;
            }
        }
        private IEnumerator Dead()
        {
            if (LogDebug) Debug.Log("Given the circumstances, I have decided that I am dead.");
            yield break;
        }

        // Callback responses
        public void Die()                                   // callback when we are dead
        {
            // method called from Subject.
            //_rb.velocity = (Vector3.zero);
            if (_hasWeapons) _weapon.Attacking = false;
            AgentEnabled(false);
        }
        public void Revive()                                // callback at resurrection
        {
            // inverse the relevant actions of Die() here.
            AgentEnabled(true);
        }
        void Attacked()                                     // callback when we are hit
        {
            // If its not in the Threat List and it is not an Ally...
            if (!ThreatList.ContainsKey(ThisSubject.LastAttacker) && !AllyList.Contains(ThisSubject.LastAttacker))
            {
                DefineAsThreat(ThisSubject.LastAttacker, ThisSubject.LastDamage);
            }
            // Its already in the Threat List, so just increase its threat level by how much damage it has done.
            else if (!AllyList.Contains(ThisSubject.LastAttacker))
            {
                AddThreatValue(ThisSubject.LastAttacker, ThisSubject.LastDamage);
            }

            if (LogDebug) Debug.Log(gameObject.name + ": " + ThisSubject.LastAttacker.name + " has provoked me by attacking, dealing " + ThisSubject.LastDamage + " damage.");
            Provoked = true;
        }

        // List Management and Queries
        void DefineAsThreat(Subject target, int threat)     // add a subject to the threat list
        {
            if (ThreatList.ContainsKey(target))
            {
                if (target.IsDead) RemoveThreat(target);
                else return;
            }

            ThreatList.Add(target, threat);
            if (LogDebug) Debug.Log(target.name + " was flagged as a Threat with " + threat + " influence.");
            
        }
        void DefineAsAlly(Subject target)                   // add a subject to the ally list
        {
            if (LogDebug) Debug.Log("Flagging " + target + " as ally.");
            if (AllyList.Contains(target)) return;  // already found, discard.
            if (AllyList.Count >= MaxAllyCount) return; // too many friends, discard.

            switch (AllyAwareness)
            {
                case AllyAwarenessType.InRange:
                    if (RequireLosToAlly && TargetCanBeSeen(target.gameObject)) AllyList.Add(target);
                    else if (TargetIsInRange(target.gameObject, SightRange)) AllyList.Add(target);
                    break;
                case AllyAwarenessType.InFov:
                    if (RequireLosToAlly && TargetCanBeSeen(target.gameObject) && TargetIsInFov(target.gameObject)) AllyList.Add(target);
                    else if (TargetIsInFov(target.gameObject)) AllyList.Add(target);
                    break;
            }
        }
        void RemoveThreat(Subject target)                   // remove a subject from the threat list
        {
            ThreatList.Remove(target);
            if (LogDebug) Debug.Log(target.name + " was removed from the Threat List.");
        }
        void AddThreatValue(Subject target, int threat)     // add threat to a specific subject
        {
            ThreatList[target] += threat;
            if (LogDebug) Debug.Log(target.name + "'s threat influence increased by: " + threat);
        }
        void RemoveAlly(Subject target)                     // remove an Ally from the AllyList
        {
            if (LogDebug) Debug.Log(target.name + " [ally] was removed from list.");
            AllyList.Remove(target);
        }                   

        void ScanForAllSubjects()                           // get all subjects in the Sight Range
        {
            Collider[] scanHits = Physics.OverlapSphere(transform.position, SightRange, ThreatMask);

            if (LogDebug) Debug.Log("Scanning " + scanHits.Length + " hits.");
            foreach (Collider thisHit in scanHits)
            {
                if (thisHit.gameObject == gameObject) continue; // is it me?
                Subject otherSubject = thisHit.GetComponent<Subject>(); // TODO this is unfortunately frequent...
                if (!otherSubject) continue; // is it null?
                if (AllyList.Contains(otherSubject) || ThreatList.ContainsKey(otherSubject)) continue; // is it a duplicate?
                if (UseFov && !TargetIsInFov(thisHit.gameObject)) continue; // do I care if I can see it? can i see it? NOTE: This discards the find if it cant see it.

                if (StaticUtil.SameTeam(ThisSubject, otherSubject)) DefineAsAlly(otherSubject);
                else DefineAsThreat(otherSubject, (MyProvokeType == ProvokeType.TargetIsInRange) ? 1 : 0);
            }

            CleanLists();
        }
        void CleanLists()                                   // remove null entries in the lists
        {
            if (ThreatList.Count > 0)
            {
                List<Subject> removals = (from entry in ThreatList where !entry.Key || entry.Key.IsDead select entry.Key).ToList();
                foreach (Subject trash in removals) RemoveThreat(trash);
            }

            if (AllyList.Count > 0)
            {
                List<Subject> removals = (from entry in AllyList where !entry || entry.IsDead select entry).ToList();
                foreach (Subject trash in removals) RemoveAlly(trash);
            }
        }
        KeyValuePair<Subject, int> FindThreat()             // look in the threat list for something to kill
        {
            // no threats and not helping allies?
            if (!ThreatList.Any() && !HelpAllies) return new KeyValuePair<Subject, int>();

            // grab the local threatlist
            Dictionary<Subject, int> allThreats = ThreatList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            // grab the ally's threats
            if (HelpAllies)
            {
                Dictionary<Subject, int> myFriendsThreats = new Dictionary<Subject, int>();

                // look at each ally
                if (AllyList.Count > 0)
                {
                    foreach (var ally in AllyList)
                    {
                        Intellect friend = ally.GetIntellect();
                        if (friend)
                        {
                            // look at each threat in that ally
                            foreach (var threat in friend.ThreatList)
                            {
                                // add that threat to this local list
                                if (!myFriendsThreats.ContainsKey(threat.Key))
                                {
                                    myFriendsThreats.Add(threat.Key, threat.Value);
                                }
                            }
                        }
                    }
                }
                // put any threats from allies into the full list of threats
                if (myFriendsThreats.Any())
                {
                    foreach (KeyValuePair<Subject, int> kvp in myFriendsThreats.Where(kvp => !allThreats.ContainsKey(kvp.Key)))
                    {
                        allThreats.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            // do i want the closest or the biggest threat? Get one.
            KeyValuePair<Subject, int> final = (MyThreatPriority == ThreatPriority.Nearest
                ? GetNearestThreat(allThreats, transform)
                : GetHighestThreat(allThreats));

            if (final.Value > 0 || (HelpAllies && AllyList.Count > 0 && AllyList.Where(ally => ally.GetIntellect() != null).Any(ally => ally.GetIntellect().Provoked))) Provoked = true;
            return final;
        }
        static KeyValuePair<Subject, int> GetNearestThreat(Dictionary<Subject, int> listOfThreats, Transform fromThis)     // find the nearest threat
        {
            if (listOfThreats.Count == 0) return new KeyValuePair<Subject, int>();
            float[] curNearestDistance = {1000f};

            KeyValuePair<Subject, int>[] nearest = {listOfThreats.First()};
            foreach (KeyValuePair<Subject, int> kvp in listOfThreats
                .Where(kvp => Vector3.Distance(kvp.Key.transform.position, fromThis.position) < curNearestDistance[0]))
            {
                curNearestDistance[0] = Vector3.Distance(kvp.Key.transform.position, fromThis.position);
                nearest[0] = kvp;
            }
            return nearest[0];
        }
        static KeyValuePair<Subject, int> GetHighestThreat(Dictionary<Subject, int> listOfThreats)                         // find the highest threat
        {
            if (listOfThreats.Count == 0) return new KeyValuePair<Subject, int>();

            KeyValuePair<Subject, int>[] biggestThreat = {listOfThreats.First()};
            foreach (KeyValuePair<Subject, int> threat in listOfThreats.Where
                (threat => threat.Value > biggestThreat[0].Value))
            {
                biggestThreat[0] = threat;
            }

            return biggestThreat[0];
        }
        public int GetTargetThreatValue()
        {
            return CurrentTarget != null ? ThreatList[CurrentTarget] : 0;
        }

        // Commands
        private void UpdateWeapon(GameObject newWeapon)     // callback when switched weapons
        {
            _weapon = newWeapon.GetComponent<Weapon>();
            _weaponSpeed = _weapon.Stats.WeaponType == WeaponType.Ranged ? _weapon.Stats.FiresProjectile.GetComponent<Projectile>().Stats.Speed : 1000;
            AttackRange = _weapon.Stats.EffectiveRange;
        }
        private void MoveTo(Vector3 position)               // pathfind to a position
        {
            if (ThisSubject.IsDead) return;
            //AgentDestination = position;
            AgentResume();
        }
        public void LookAt(Vector3 position)                // look at a specific position
        {
            if (CurrentTarget && CurrentTarget.IsDead) return;

            Vector3 dir = position - _go.transform.position; dir.y = 0f;
            Quaternion fin = Quaternion.LookRotation(dir);
            _go.transform.rotation = Quaternion.Slerp(_go.transform.rotation, fin, Time.deltaTime * ThisSubject.ControlStats.TurnSpeed);
        }
        public void LeadTarget(GameObject victim)           // lead the target, compensating for their trajectory and projectile speed
        {
            // 在不改变角色方向的情况下，找到一种方法来补偿武器的水平偏移

            // 得到目标的速度. We need to know *direction* and *speed*.
            Vector3 victimVelocty = (victim.transform.position - _victimLastPos) * Time.deltaTime;
            Vector3 intercept = victim.transform.position + (victimVelocty*(Vector3.Distance(CurrentTarget.transform.position, transform.position)/_weaponSpeed)); 
                //+ victim.transform.TransformVector(new Vector3((_weapon.Stats.MountPivot == MountPivot.RightShoulder ? 1f : _weapon.Stats.PositionOffset.x),0,0)));

            _victimLastPos = victim.transform.position; // 代入最后已知的位置(第一次计算总是错误的)
            LookAt(intercept);
        }
        private IEnumerator Juke()                          // 执行旋转动作
        {
            // Juke必须在每一帧做一些事情，直到它到达它的点或不能到达它
            if (_juking) yield break;
            if (LogDebug) Debug.Log("Perform Juke");
            _juking = true;

            // 告诉那个单位别动
            //AgentStoppingDistance = AttackRange;

            //_jukeHeading = GetJukeHeading();

            // 设置下一个点之前的时间
            float wait = Random.Range(
                    JukeFrequency - JukeFrequencyRandomness,
                    JukeFrequency + JukeFrequencyRandomness);

            bool yieldToJukeTime = true;
            float timer = 0;
            while (yieldToJukeTime && !ThisSubject.IsDead)
            {
                //_rb.MovePosition(transform.position + _jukeHeading * .01f);
                timer += Time.deltaTime;
                if (timer >= JukeTime)
                {
                    yieldToJukeTime = false;
                }
                yield return null;
            }
           // yield return new float 1.0f;
            yield return new WaitForSeconds(0.0f);
            _juking = false;
        }
        private Vector3 GetJukeHeading()
        {
            // 将距离设置为每帧juke
            bool r = (Random.value < 0.5);
            _jukeHeading = (r && !Physics.Raycast(transform.position + Vector3.up, Vector3.right, 0.2f, SightMask)
                ? transform.TransformDirection(Vector3.right * ThisSubject.ControlStats.MoveSpeed)
                : transform.TransformDirection(Vector3.left * ThisSubject.ControlStats.MoveSpeed));
            return _jukeHeading;
        }
        private Vector3 GetStandoffPosition(GameObject target)
        {
            Vector3 pos = gameObject.transform.position + (gameObject.transform.position - target.gameObject.transform.position).normalized * StandoffDistance;
            return pos;
        }

        // Logic queries
        bool CanFire()                                      // 检查武器是否可以发射
        {
            if (MagHasAmmo())
            {
                _needToReload = false;
                return true;
            }
            NoAmmoLeft = (_weapon.Stats.CurrentMagazines <= 0 && !MagHasAmmo());
            if (NoAmmoLeft) return false;

            _needToReload = true;
            return false;
        }
        bool MagHasAmmo()                                   // 弹匣里有足够的弹药可以开火吗?
        {
            return _weapon.Stats.CurrentAmmo >= _weapon.Stats.AmmoCost;
        }
        bool TargetCanBeSeen(GameObject interest)           // 光线投射到目标并检查命中
        {
            // NOTE: This assumes a raycast done from 1.5 units on Y from the feet, on the outside of the Subject collider.
            // May have problems if using more than one collider.

            bool inSight = true;
            Vector3 direction = (interest.transform.position - transform.position).normalized;
            Vector3 origin = transform.position + new Vector3(0, 1.5f, 0) + (direction * _collider.Radius);

            RaycastHit hit;
            if (Physics.Raycast(origin, direction.normalized, out hit, SightRange, SightMask)) { inSight = hit.collider.gameObject == interest; }
            return inSight;
        }
        bool TargetIsInFov(GameObject interest)
        {
            return TargetIsInAngle(interest, FieldOfView) && TargetCanBeSeen(interest) || TargetIsInFovProximity(interest);
        }
        bool TargetIsInFovProximity(GameObject interest)
        {
            return Vector3.Distance(transform.position, interest.transform.position) < FovProximity;
        }
        bool TargetIsInAngle(GameObject target, float inputAngle)  // 检查目标是否在给定的角度范围内
        {
            Vector3 direction = target.transform.position - transform.position;
            float angle = Vector3.Angle(direction, transform.forward);
            return angle < inputAngle * 0.5f;
        }
        bool TargetIsInRange(GameObject target, float maxDistance) // 检查到目标的距离是否小于视野范围。
        {
            return Vector3.Distance(target.transform.position, transform.position) < maxDistance;
        }
        bool TooClose(GameObject target)                    // 检查到目标的距离是否小于对峙距离。
        {
            return Vector3.Distance(target.gameObject.transform.position, gameObject.transform.position) < StandoffDistance;
        }

        // NavMesh queries
        private Vector3 GetPosNearby(Vector3 origin, float range, float sampleSize)               // 在附近随机找一个位置
        {
            Vector3 c = Random.insideUnitCircle * range;
            c.z = c.y;
            c.y = 0;

            Vector3 waypoint = origin + c;

            UnityEngine.AI.NavMeshHit hit;
            UnityEngine.AI.NavMesh.SamplePosition(waypoint, out hit, sampleSize, UnityEngine.AI.NavMesh.AllAreas);
            return hit.position;
        }
        private Vector3 GetPosFleeing(float area)              // 找到一个相对于目标的随机位置
        {
            Debug.Log("Get Pos");
            //AgentStoppingDistance = 0;
            Vector3 waypoint = Vector3.Scale
                (transform.position,
                (CurrentTarget.transform.position - transform.position).normalized * area)
                + Random.insideUnitSphere * 2;
            waypoint.y = 0f;

            UnityEngine.AI.NavMeshHit hit;
            UnityEngine.AI.NavMesh.SamplePosition(waypoint, out hit, area, UnityEngine.AI.NavMesh.AllAreas);
            return hit.position;
        }

        // Miscellaneous
        public string GetTargetName()
        {
            return CurrentTarget != null ? CurrentTarget.gameObject.name : "";
        }
        private IEnumerator DelayProvoke()
        {
            if (_waiting) yield break;
            _waiting = true;
            yield return new WaitForSeconds(ProvokeDelay);
            _waiting = false;
        }

        // 寻路部分

        //public UnityEngine.AI.NavMeshAgent AgentGetComponent
        //{
        //    get
        //    {
        //        return GetComponent<UnityEngine.AI.NavMeshAgent>();
        //    }
        //}

        public void AgentConfigure()
        {
            //AgentStoppingDistance = 1;
            //Agent.angularSpeed = 1.0f;
            //Agent.speed = ThisSubject.ControlStats.MoveSpeed;
            //Agent.acceleration = AgentAcceleration;
            //Agent.autoBraking = false;
        }
        public void AgentResume()
        {
            //Agent.Resume();
        }
        public void AgentStop()
        {
            //Agent.Stop();
        }
        public void AgentEnabled(bool status)
        {
            //Agent.enabled = status;
        }
        //public float AgentRemainingDistance
        //{
        //    //get { return Agent.remainingDistance; }
        //}
        //public float AgentStoppingDistance
        //{
        //    get { return Agent.stoppingDistance; }
        //    set { Agent.stoppingDistance = value; }
        //}
        //public Vector3 AgentDesiredVelocity
        //{
        //    get { return Agent.desiredVelocity; }
        //}
        //public Vector3 AgentDestination
        //{
        //    get { return Agent.destination; } 
        //    set { Agent.SetDestination(value); }
        //}
        //public bool AgentIsPathStale
        //{
        //    get { return Agent.isPathStale; }
        //}
    }
}