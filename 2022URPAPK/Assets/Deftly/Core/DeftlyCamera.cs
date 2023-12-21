// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Deftly
{
    // NOTE: This script initializes the static variable for ingame preferences at runtime.
    // If replacing this script, you must initialize the runtime preferences.




    [AddComponentMenu("Deftly/Deftly Camera")]
    public class DeftlyCamera : MonoBehaviour
    {
        public enum MoveStyle {Loose, Stiff}
        public MoveStyle FollowingStyle;
        public MoveStyle RotationStyle = MoveStyle.Stiff;

        public enum TrackingStyle {PositionalAverage, AimingAverage}
        public TrackingStyle Tracking;

        public float TrackDistance;
        public float TrackSpeed;
        public bool InEditorTracking;

        public List<GameObject> Targets = new List<GameObject>();
        public Vector3 Offset;
        public int FrustumLayer = 0;

        private Vector3 _averagePos;
        public Camera Cam; 

        //*** Frustum Confines Mesh System                                                          
        //                                                                                          
        // How it works:                                                                            
        // I create 4 Rays that cast down the corners of the frustum to intersect with the         
        // height of the Camera's Target Position. Then I identify those 4 points as vertexes      
        // and also define 4 more vertex points slightly above for a total of 8. Then I use        
        // some array maps to tell the new meshes how to build their triangles in those       
        // vertex positions. The sequence determines the normal direction of the poly's.
        //                                                                                         
        // The Planes had to be vertical so the player doesn't get compressed downward when
        // the constraint presses against it. That is why we don't use a slightly simpler method.
        //
        // TODO Players can push the borders and pull players on the opposite side through colliders.
        //
        public bool UseConfinement;
        public bool ShowDebugGizmos = true;
        public bool ConfineIsSetup;

        public bool UseFrustumBumper;
        public Vector2 FrustumBumperSize = new Vector2(5, 10);

        private Vector3[] _corners = new Vector3[4]; // live value for corners, for applying a bumper spacing.
        private static readonly Vector3[] CornersMax = // the maximum extent of the corners. starting point for Corners variable.
        {
            new Vector3(0, 0, 0), // 0 bottom left
            new Vector3(0, 1, 0), // 1 top left
            new Vector3(1, 1, 0), // 2 top right
            new Vector3(1, 0, 0)  // 3 bottom right
        };

        private Ray _topLeft; // Rays which are fired from corners of the screen/frustum.
        private Ray _topRight;
        private Ray _bottomRight;
        private Ray _bottomLeft;

        private readonly Vector3 _confineBorderHeight = new Vector3(0, 5, 0); // how tall the barrier will be.
        private Vector3[] _allRealtimeVertPositions = new Vector3[8]; // list of all necessary vertex positions.
        
        private readonly int[] _vertMapForLeftPoly =   { 0, 4, 5, 1 }; // maps of which verts each mesh will use (from the list of positions).
        private readonly int[] _vertMapForRightPoly =  { 2, 6, 7, 3 }; // assigning verts anti-clockwise results in inverted normals.
        private readonly int[] _vertMapForTopPoly =    { 1, 5, 6, 2 };
        private readonly int[] _vertMapForBottomPoly = { 3, 7, 4, 0 };

        private readonly int[] _triangles =  { 0, 1, 2, 2, 3, 0 }; // all triangles use the same vertex order and reference from the relevant map above. Sets of 3.

        private GameObject[] _confines = new GameObject[4];             // array containing all GameObject's with the components on them.
        private MeshFilter[] _confineMeshFilters = new MeshFilter[4];   // array with the MeshFilter component refs.
        private Mesh[] _confineMeshes = new Mesh[4];                    // array with the Mesh component refs.
        private MeshCollider[] _confineColliders = new MeshCollider[4]; // array with the collider component refs.

        // Misc
        //
        public static string PreferencesFileName = "Preferences";
        private GameObject _listenerAudio;
        private readonly Vector3 _listenerOffset = new Vector3(0,10,0);

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Cam) Cam = GetComponent<Camera>();
            if (!Application.isPlaying && InEditorTracking) FixedUpdate();
            if (UseConfinement && ShowDebugGizmos)
            {
                UpdateRays();
                Debug.DrawLine(GetGoal(_topLeft), GetGoal(_topRight), Color.cyan);
                Debug.DrawLine(GetGoal(_topRight), GetGoal(_bottomRight), Color.cyan);
                Debug.DrawLine(GetGoal(_bottomRight), GetGoal(_bottomLeft), Color.cyan);
                Debug.DrawLine(GetGoal(_bottomLeft), GetGoal(_topLeft), Color.cyan);
            }
        }
#endif
        void Reset()
        {
            UseFrustumBumper = false;
            FrustumBumperSize = new Vector2(5,10);
            FollowingStyle = MoveStyle.Loose;
            RotationStyle = MoveStyle.Stiff;
            Tracking = TrackingStyle.AimingAverage;
            TrackDistance = 2f;
            TrackSpeed = 5f;
            Offset = new Vector3(0.5f, 10.0f, -0.5f);
            Targets = new List<GameObject>();
            ShowDebugGizmos = true;
        }
        void Awake()
        {
            StaticUtil.Init();
            Cam = GetComponent<Camera>();
            if (UseConfinement) SetupFrustumConfines();
            _listenerAudio = new GameObject {name = "Audio Listener"};
            _listenerAudio.AddComponent<AudioListener>();
        }
        public void FixedUpdate()
        {
            if (Targets.Count > 0) FollowTargets();
            if (UseConfinement && Application.isPlaying) UpdateFrustumConfines();
        }

        // Frustum system
        public void SetupFrustumConfines()
        {
            // *** Update Corners Data accomodating Bumpers *** //
            UpdateCorners();

            // *** 1. Setup GameObject's for Confine Containers *** //
            GameObject container = new GameObject();
            container.name = "Frustum Constraints Container";
            _confines = new[]
            {
                new GameObject("Frustum Confines - Left"),      // 0
                new GameObject("Frustum Confines - Right"),     // 1
                new GameObject("Frustum Confines - Top"),       // 2
                new GameObject("Frustum Confines - Bottom"),    // 3
            };

            // *** 2. Setup Mesh Variables *** //
            _confineMeshes = new[]
            {
                new Mesh(), // 0 Left
                new Mesh(), // 1 Right
                new Mesh(), // 2 Top
                new Mesh(), // 3 Bottom
            };

            _confineMeshes[0].name = "Left";
            _confineMeshes[1].name = "Right";
            _confineMeshes[2].name = "Top";
            _confineMeshes[3].name = "Bottom";

            // *** 3. Setup Loop for Mesh Filters, Mesh assignment and Colliders *** //
            for (int i = 0; i < _confines.Length; i++)
            {
                _confines[i].transform.SetParent(container.transform);              // throw stuff into a container to reduce scene clutter
                _confineMeshFilters[i] = _confines[i].AddComponent<MeshFilter>();   // add mesh filter to the wall GameObject
                _confineMeshFilters[i].mesh = _confineMeshes[i];                    // apply the mesh to the mesh filter
                _confineColliders[i] = _confines[i].AddComponent<MeshCollider>();   // add a mesh collider to the wall GameObject
                _confines[i].layer = FrustumLayer;                                  // apply the layer to the wall GameObject

                //_confines[i].AddComponent<MeshRenderer>();
            }

            ConfineIsSetup = true; // flag that everything is setup for realtime.
        }
        public void UpdateFrustumConfines()
        {
            if (!ConfineIsSetup) SetupFrustumConfines();
            
            // *** Update Corner Rays for accurate Goals //
            UpdateRays();

            // *** Update Positions of all Vertices //
            _allRealtimeVertPositions[0] = GetGoal(_bottomLeft);
            _allRealtimeVertPositions[1] = GetGoal(_topLeft);
            _allRealtimeVertPositions[2] = GetGoal(_topRight);
            _allRealtimeVertPositions[3] = GetGoal(_bottomRight);
            _allRealtimeVertPositions[4] = _allRealtimeVertPositions[0] + _confineBorderHeight;
            _allRealtimeVertPositions[5] = _allRealtimeVertPositions[1] + _confineBorderHeight;
            _allRealtimeVertPositions[6] = _allRealtimeVertPositions[2] + _confineBorderHeight;
            _allRealtimeVertPositions[7] = _allRealtimeVertPositions[3] + _confineBorderHeight;

            // *** Define verts and tris for meshes //
            Vector3[] vertices = new Vector3[4];

            for (int i = 0; i < vertices.Length; i++) vertices[i] = _allRealtimeVertPositions[_vertMapForLeftPoly[i]];
            _confineMeshes[0].vertices = vertices;
            _confineMeshes[0].triangles = _triangles;

            for (int i = 0; i < vertices.Length; i++) vertices[i] = _allRealtimeVertPositions[_vertMapForRightPoly[i]];
            _confineMeshes[1].vertices = vertices;
            _confineMeshes[1].triangles = _triangles;

            for (int i = 0; i < vertices.Length; i++) vertices[i] = _allRealtimeVertPositions[_vertMapForTopPoly[i]];
            _confineMeshes[2].vertices = vertices;
            _confineMeshes[2].triangles = _triangles;

            for (int i = 0; i < vertices.Length; i++) vertices[i] = _allRealtimeVertPositions[_vertMapForBottomPoly[i]];
            _confineMeshes[3].vertices = vertices;
            _confineMeshes[3].triangles = _triangles;

            // *** Update Collider meshes //
            for (int i = 0; i < _confines.Length; i++) _confineColliders[i].sharedMesh = _confineMeshes[i];
        }
        public void UpdateCorners()
        {
            // *** Apply Bumper stuff by moving the ray's screen origin //
            CornersMax.CopyTo(_corners, 0);
            // Corners = CornersMax;

            if (UseFrustumBumper)
            {
                _corners[0].x += FrustumBumperSize.x / 100; // bottom left
                _corners[0].y += FrustumBumperSize.y / 100;

                _corners[1].x += FrustumBumperSize.x / 100; // top left
                _corners[1].y -= FrustumBumperSize.y / 100;

                _corners[2].x -= FrustumBumperSize.x / 100; // top right
                _corners[2].y -= FrustumBumperSize.y / 100;

                _corners[3].x -= FrustumBumperSize.x / 100; // bottom right
                _corners[3].y += FrustumBumperSize.y / 100;
            }
        }
        public void UpdateRays()
        {
            if (!Application.isPlaying && UseFrustumBumper) UpdateCorners();

            _bottomLeft = Cam.ViewportPointToRay(_corners[0]);
            _topLeft = Cam.ViewportPointToRay(_corners[1]);
            _topRight = Cam.ViewportPointToRay(_corners[2]);
            _bottomRight = Cam.ViewportPointToRay(_corners[3]);
        }
        private Vector3 GetGoal(Ray ray) { return ray.origin + (((ray.origin.y - _averagePos.y) / -ray.direction.y) * ray.direction); }
        //

        public void CalibrateRotation()
        {
            if (Targets.Count == 0) transform.LookAt(transform.position - Offset);
            else FixedUpdate();
        }
        
        void FollowTargets()
        {
            _averagePos = GetAveragePos();

            if (!Application.isPlaying)
            {
                transform.position = _averagePos + Offset;
                transform.LookAt(_averagePos);
                return;
            }

            if (FollowingStyle == MoveStyle.Loose)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    _averagePos + Offset,
                    TrackSpeed*Time.deltaTime);
            }
            else transform.position = _averagePos + Offset;
            if (RotationStyle == MoveStyle.Loose) transform.LookAt(_averagePos);

            // Audio positioning
            if (Application.isPlaying)
            {
                _listenerAudio.transform.position = _averagePos + _listenerOffset;
                _listenerAudio.transform.rotation = transform.rotation;
            }
        }
        Vector3 GetAveragePos()
        {
            _averagePos = Vector3.zero;

            if (!Application.isPlaying) return transform.position - Offset;

            // Clean Target List, remove all null's
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets.Count > 0 && (Targets[i] == null || !Targets[i]))
                {
                    Targets.RemoveAt(i);
                    i--;
                }
            }
            
            // If there aren't any valid targets, return default spot.
            if (Targets.Count == 0) return transform.position - Offset;

            // Otherwise..

            // Work over Positions list since there are valid targets
            Vector3[] positions = new Vector3[Targets.Count];
            for (int i = 0; i < Targets.Count; i++)
            {
                positions[i] = Tracking == TrackingStyle.PositionalAverage
                    ? Targets[i].transform.position
                    : Targets[i].transform.position + (Targets[i].transform.forward*TrackDistance);
                _averagePos += positions[i];
            }

            // Return position
            return _averagePos / positions.Length;
        }

        void OnDisable() { }
        void OnDestroy() { }
    }
}