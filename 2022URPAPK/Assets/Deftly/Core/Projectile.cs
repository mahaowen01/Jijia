// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Deftly
{
    [AddComponentMenu("Deftly/Projectile")]
    public class Projectile : MonoBehaviour
    {
        // Main configuration stuff
        //
        public ProjectileStats Stats;
        public LayerMask Mask;
        public LayerMask StopMask;
        public GameObject AttackEffect;

        public List<string> ImpactTagNames = new List<string>();
        public List<AudioClip> ImpactSounds = new List<AudioClip>();
        public List<GameObject> ImpactEffects = new List<GameObject>();

        public enum ImpactType { ReflectOffHit, HitPointNormal, InLineWithShot }
        public ImpactType ImpactStyle = ImpactType.InLineWithShot;

        public Subject Owner;
        public Collider OwnerCollider;
        public GameObject DetachOnDestroy;
        public LineRenderer LineRenderer;

        // Misc private stuff
        //
        private GameObject _go;         // shortcut to gameObject
        private Vector3 _startPoint;    // start point for trail or wherever the projectile was born
        private Vector3 _endPoint;      // end point of trail or wherever the final hit took place
        private Vector3 _endNormal;     // final hit normal
        private Subject _victim;        // victim that was hit (not good for Pierced type)
        private GameObject _victimGo;   // same as victim but a GO

        // Raycasted Trail Line Renderer variables
        //
        public AnimationCurve TrailAlphaStart = new AnimationCurve();
        public AnimationCurve TrailAlphaEnd = new AnimationCurve();
        public AnimationCurve TrailUvCurve = new AnimationCurve();
        public Color TrailStartColor = Color.white;
        public Color TrailEndColor = Color.white;
        public float TrailUvSpeed = 5;
        public Vector2 TrailTiling = new Vector2(1,1);
        public float OffsetRngMin = 0;
        public float OffsetRngMax = 10;
        private float _offsetRng;

        private Material _trailMat;
        private Color _liveStartColor = Color.white;
        private Color _liveEndColor = Color.white;
        private float _timer;

        // Misc stuff
        //
        private Collider _myCollider;
        private Vector3 _velocityCache;

        private bool _despawning;
        private bool _canContinue;
        public bool LogDebug = false;

        // Mover stuff
        private bool _fireAsStandard;

        private float _distance;
        private Vector3 _heading;
        private bool _firstShot;

        private Vector3 _lastPos;
        private Vector3 _thisPos;
        private Rigidbody _rb;
        private bool _doneApplyingForces;

        void Awake()
        {
            // Awake runs before the Owner var gets passed in.
            if (!_canContinue || !Owner) return;
            if (LineRenderer) _trailMat = LineRenderer.material;
            _fireAsStandard = false;
            _go = gameObject;
            Lifetimer.AddTimer(gameObject, Stats.Lifetime, true); // not sure if pool friendly?
                                                                  
            // Mover stuff
            _firstShot = true;
            _rb = GetComponent<Rigidbody>();
            if (Stats.weaponType == ProjectileStats.WeaponType.Standard && _rb == null)
            {
                Debug.LogError("You must have a Rigidbody on the Projectile!");
            }

            // Other init
            if (_go) Fire(_go.transform.position);
            _myCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (Stats.weaponType == ProjectileStats.WeaponType.Standard && _myCollider) Physics.IgnoreCollision(_myCollider, OwnerCollider);
        }
        void OnEnable()
        {
            // reset from pooling
            _despawning = false;
            _timer = 0;
            _startPoint = Vector3.zero;
            _endPoint = Vector3.zero;
            _endNormal = Vector3.zero;
            _victim = null;
            _victimGo = null;
            _liveStartColor = TrailStartColor;
            _liveEndColor = TrailEndColor;
            _offsetRng = Random.Range(OffsetRngMin, OffsetRngMax);
        }
        void Start()
        {
            // Awake runs before the Weapon passes in critical data, so we delay here.
            // If some variable which is only initialized in Awake is still null, it hasn't run yet and needs to be called.
            _canContinue = true;
            if (!_go) Awake();

        }
        void Reset()
        {
            TrailStartColor = Color.white;
            TrailEndColor = Color.white;
            TrailAlphaStart = new AnimationCurve();
            TrailUvCurve = new AnimationCurve();
            TrailUvSpeed = 5;

            Stats = new ProjectileStats
            {
                Title = "Pewpew",
                weaponType = ProjectileStats.WeaponType.Standard,
                Speed = 40f,
                Damage = 10,
                MaxDistance = 10f,
                Lifetime = 4f,
                Bouncer = false,
                UsePhysics = true,
                ConstantForce = true,
                CauseAoeDamage = false,
                AoeRadius = 5,
                AoeForce = 50
            };

            ImpactSounds = new List<AudioClip>();
            AttackEffect = null;
            ImpactEffects = new List<GameObject>();
        }
        void OnCollisionEnter(Collision col) // Handles hits for Standard Type.
        {
            if (!Stats.CauseAoeDamage) // then I cause damage to what I collided into.
            {
                _victimGo = col.gameObject;
                if (Owner.gameObject == _victimGo)
                {
                    Physics.IgnoreCollision(_myCollider, col.collider);
                    _rb.velocity = _velocityCache;
                    return;
                }

                if (StaticUtil.LayerMatchTest(Mask, _victimGo))
                {
                    _victim = _victimGo.GetComponent<Subject>();
                    if (_victim != null) ApplyDamageTo(_victim);
                    PopFx(GetCorrectFx(col.collider.gameObject), col.contacts[0].point, GetImpactNormalGoal(col.contacts[0].normal));
                    FinishImpact();
                }
                else
                {
                    Physics.IgnoreCollision(_myCollider, col.collider);
                    _rb.velocity = _velocityCache;
                }
            }
            else if (Stats.CauseAoeDamage && !Stats.Bouncer) ApplyDamageAoe(); // otherwise I cause AoE immediately when I hit something, or I just bounce and nothing happens here.
        }

        void Update()
        {
            if (Vector3.Distance(transform.position, Owner.transform.position) > Stats.MaxDistance)
            {
                FinishImpact();
            }
        }
        
        // Mover
        void FixedUpdate()
        {
            if (!_fireAsStandard || _doneApplyingForces) return;
            if (Stats.UsePhysics && !_doneApplyingForces) MoveByPhysics();
            else if (!_doneApplyingForces) MoveByTranslate();
        }

        public void Fire(Vector3 fromPos)
        {
            _startPoint = fromPos;
            DoMuzzleFlash();

            switch (Stats.weaponType)
            {
                case ProjectileStats.WeaponType.Standard:
                    FireStandard();
                    break;
                case ProjectileStats.WeaponType.Raycast:
                    FireRaycast();
                    break;
                case ProjectileStats.WeaponType.PiercingRaycast:
                    FireRaycastPierce();
                    break;
            }
        }
        private void FireStandard()
        {
            // all we need here is to tell the projectile to get moving and wait for CollisionEnter or timeout.
            _fireAsStandard = true;
        }
        private void FireRaycast()
        {
            // For raycasted types we have to find the hits manually.

            Vector3 dir = _go.transform.TransformDirection(Vector3.forward);

            RaycastHit hit;
            if (Physics.Raycast(_startPoint, dir, out hit, Stats.MaxDistance, Mask))
            {
                // This is a hit.
                _victimGo = hit.collider.gameObject;
                _victim = _victimGo.GetComponent<Subject>();
                if (_victim != null) ApplyDamageTo(_victim); // only Subject's can be damaged.

                _endNormal = hit.normal;
                _endPoint = hit.point;
                if (LogDebug) Debug.Log(name + " registered a Hit on " + _victimGo.name);
            }
            else
            {
                // This is a miss.
                _endPoint = _startPoint + dir * Stats.MaxDistance;
                Owner.Stats.ShotsMissed++;
                if (LogDebug) Debug.Log(name + " registered a Miss");
            }
            
            DrawRayFx(_startPoint, _endPoint);
            if (hit.collider)
            {
                PopFx(GetCorrectFx(hit.collider.gameObject), hit.point, GetImpactNormalGoal(hit.normal));
            }
            else
            {
                PopFx(0, _endPoint, GetImpactNormalGoal(transform.forward));
            }
            FinishImpact();
        }
        private void FireRaycastPierce()
        {
            Vector3 dir = _go.transform.TransformDirection(Vector3.forward);

            // figure out where the pierce stops
            RaycastHit stopHit;
            float distance = Physics.Raycast(_startPoint, dir, out stopHit, Stats.MaxDistance, StopMask) 
                ? Vector3.Distance(_startPoint, stopHit.point) 
                : Stats.MaxDistance;

            // pierce things, gather array of hits
            RaycastHit[] hits = Physics.SphereCastAll(_startPoint + dir * Stats.BeamRadius, Stats.BeamRadius, dir, distance, Mask);
            if (hits.Length > 0)
            {
                // TODO We see extra hits sometimes because the Spherecast ends at the hit point
                // TODO but collects hits from a sphere with given rad at that point, leading to extra collection, even behind walls that were hit.
                // TODO .. Need to fix. Different type of cast or an offset to compensate for the radius.

                // This is a hit.
                foreach (RaycastHit hit in hits)
                {
                    _victimGo = hit.collider.gameObject;
                    _victim = _victimGo.GetComponent<Subject>();
                    if (_victim != null) ApplyDamageTo(_victim);

                    _endPoint = _startPoint + (dir * distance);
                    _endNormal = GetImpactNormalGoal(hit.normal);

                    PopFx(GetCorrectFx(hit.collider.gameObject), hit.point, hit.normal);

                    if (LogDebug) Debug.Log(name + " registered a Hit on " + _victimGo.name);
                }
            }
            else
            {
                // This is a miss.
                _endPoint = _startPoint + (dir * distance);
                _endNormal = Owner.transform.forward;
                Owner.Stats.ShotsMissed++;
                if (LogDebug) Debug.Log(name + " registered a Miss");
            }

            DrawRayFx(_startPoint, _endPoint);
            FinishImpact();
        }

        private void ApplyDamageTo(Subject victim)
        {
            victim.DoDamage(Stats.Damage, Stats.DamageType, Owner);
            Owner.Stats.DamageDealt += Stats.Damage;
        }
        private void ApplyDamageAoe()
        {
            Ray ray = new Ray(transform.position, Vector3.up);
            RaycastHit[] hits = Physics.SphereCastAll(ray, Stats.AoeRadius, 1, Mask);
            foreach (RaycastHit thisHit in hits)
            {
                _victimGo = thisHit.collider.gameObject;
                _victim = _victimGo.GetComponent<Subject>();

                if (Stats.AoeForce > 0)
                {
                    Rigidbody rb = null; /*= _victim?_victim.MyRigidbody:_victimGo.GetComponent<Rigidbody>();*/
                    if (rb != null) rb.AddExplosionForce(Stats.AoeForce, transform.position, Stats.AoeRadius);
                }

                if (_victim != null)
                {
                    _victim.DoDamage(Stats.Damage, Stats.DamageType, Owner);
                    Owner.Stats.DamageDealt += Stats.Damage;
                }

                _endPoint = thisHit.point;
                GetImpactNormalGoal(thisHit.normal);
                PopFx(GetCorrectFx(thisHit.collider.gameObject), thisHit.point, Vector3.up);

                FinishImpact();
            }

            if (Stats.AoeEffect != null) StaticUtil.Spawn(Stats.AoeEffect, transform.position, Quaternion.identity);
        }

        private void DoMuzzleFlash()
        {
            StaticUtil.Spawn(AttackEffect, transform.position, transform.rotation);
        }
        private void DrawRayFx(Vector3 start, Vector3 end)
        {
            if (!LineRenderer) return;

            LineRenderer.SetPosition(0, start);
            LineRenderer.SetPosition(1, end);
        }

        private Vector3 GetImpactNormalGoal(Vector3 hitNormal)
        {
            Vector3 result = new Vector3();
            switch (ImpactStyle)
            {
                case ImpactType.InLineWithShot: 
                    result = -transform.forward;
                    break;
                case ImpactType.HitPointNormal: 
                    result = (Stats.weaponType == ProjectileStats.WeaponType.Standard) 
                    ? hitNormal 
                    : _endNormal;
                    break;
                case ImpactType.ReflectOffHit: 
                    result = (Stats.weaponType == ProjectileStats.WeaponType.Standard) 
                    ? Vector3.Reflect(transform.forward, hitNormal) 
                    : Vector3.Reflect(transform.forward, _endNormal);
                    break;
            }
            return result;
        }

        private int GetCorrectFx(GameObject victim)
        {
            if (ImpactEffects.Count <= 1 || victim == null) return 0;
            for (int i = 0; i < ImpactTagNames.Count; i++)
            {
                if (!ImpactTagNames[i].Equals("Miss") && victim.CompareTag(ImpactTagNames[i])) return i;
            }
            return 0;
        }
        private void PopFx(int index, Vector3 position, Vector3 normal)
        {
            if (ImpactSounds[index] != null) StaticUtil.PlaySoundAtPoint(ImpactSounds[index], _endPoint);
            else if (LogDebug) Debug.LogWarning(gameObject.name + " cannot spawn Impact sound because it is null. This may just be a projectile that hit nothing and can be ignored. Disable LogDebug to supress this message.");

            if (ImpactEffects[index] != null) StaticUtil.Spawn(ImpactEffects[index], position, Quaternion.LookRotation(normal));
            else if (LogDebug) Debug.LogWarning(gameObject.name + " cannot spawn Impact effect because it is null. This may just be a projectile that hit nothing and can be ignored. Disable LogDebug to supress this message.");
        }
        private void FinishImpact()
        {
            if (DetachOnDestroy != null) DetachOnDestroy.transform.SetParent(null);

            // Only for Standard Type, relying on Lifetimer to despawn Raycast Type.
            if (Stats.weaponType == ProjectileStats.WeaponType.Standard) DeSpawn();
            else StartCoroutine(FadeLineTrail());
        }

        public IEnumerator FadeLineTrail()
        {
            while (_timer < Stats.Lifetime)
            {
                // Update the Alpha
                _timer += Time.deltaTime;
                _liveStartColor.a = TrailAlphaStart.Evaluate(_timer / Stats.Lifetime);
                _liveEndColor.a = TrailAlphaEnd.Evaluate(_timer / Stats.Lifetime);

                // Apply updated LineRenderer alphas
                LineRenderer.SetColors(_liveStartColor, _liveEndColor);

                // Evaluate the curve with the given parameters, then update the material
                float a = TrailUvCurve.Evaluate(_timer / Stats.Lifetime) * (TrailUvSpeed / _timer);
                _trailMat.mainTextureOffset = new Vector2(a + (_offsetRng * 100), 0);
                LineRenderer.material.SetTextureScale("_MainTex", new Vector2(Vector3.Distance(_startPoint, _endPoint) / TrailTiling.x * 2, TrailTiling.y));﻿
                yield return null;
            }
        }
        public void Spawn()
        {
            // Pooling TBD
            gameObject.SetActive(true);
        }
        public void DeSpawn()
        {
            // using DeSpawn() to apply aoe dmg?... not sure if that is okay...
            if (Stats.CauseAoeDamage && Stats.Bouncer && !_despawning)
            {
                _despawning = true;
                ApplyDamageAoe();
            }
            StaticUtil.DeSpawn(gameObject);
        }

        //Mover
        void MoveByPhysics() // relies on physics for everything.
        {
            if (!Stats.ConstantForce)
            {
                _rb.AddForce(transform.forward * Stats.Speed * 2);
                _velocityCache = _rb.velocity;
                _doneApplyingForces = true;
            }
            else
            {
                _rb.velocity = transform.forward * Stats.Speed;
                _velocityCache = _rb.velocity;
                _doneApplyingForces = true;
            }
        }
        void MoveByTranslate() // accomodates for bullet through paper issue manually.
        {
            _lastPos = gameObject.transform.position;
            transform.Translate(Vector3.forward * Stats.Speed * 0.03f);
            _thisPos = gameObject.transform.position;
            _heading = (_thisPos - _lastPos);

            if (_firstShot)
            {
                _distance = _heading.magnitude; // only need distance once, assuming projectile speed is constant.
                _firstShot = false;
            }

            RaycastHit hit;
            Physics.Raycast(_lastPos, _heading, out hit, _distance, Mask);

            if (hit.collider != null)
            {
                gameObject.transform.position = hit.point;
            }
        }
    }
}