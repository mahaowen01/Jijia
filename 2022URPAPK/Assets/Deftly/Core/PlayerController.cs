// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;

namespace Deftly
{
    [AddComponentMenu("Deftly/Player Controller")]
    [RequireComponent(typeof(Subject))]
    public class PlayerController : MonoBehaviour
    {
        // soon
        public enum ControlVersion { Stock, InControl, ReWired }
        public readonly ControlVersion Version = ControlVersion.InControl;
        public float AimTension;
        public float MoveTension;
        //

        public enum ControlFeel { Stiff, Loose }
        public ControlFeel AimControlFeel = ControlFeel.Loose;
        public ControlFeel MoveControlFeel = ControlFeel.Loose;

        public string ChangeWeapon;
        public string Horizontal;
        public string Vertical;
        public string Fire1;
        public string Fire2;
        public string Reload;
        public string Interact;
        public string DropWeapon;
        public string Sprint = "Sprint";

        public LayerMask Mask;
        public bool LogDebug;
        public float WalkSpeed;
        public float SprintSpeed;
        public bool UseRootMotion;
        public float AnimatorDampening;

        public bool InputPermission; // Critical for letting other scripts turn off controls while doing certain things.

        protected Animator ThisAnimator;
        protected Camera MainCam;
        protected GameObject ThisGo;
        protected Subject ThisSubject;
        protected Rigidbody ThisRb;
        protected Weapon CurrentWeapon;
        protected string _h;
        protected string _v;
        protected Vector3 _aimInput;
        protected Vector3 _aimCache;

        // InControl Support
        //
        public int DeviceNumber; // not recommended to use this to track players, the number is not likely to remain consistent.
        public bool UseInControl;
        public bool ManagedDevice; // the Device is managed by another script. Like a PlayerManager, tracking Players and Device connections. If false, you must assign a DeviceID.

        public virtual void Reset()
        {
            InputPermission = false;
            AimTension = 20;
            MoveTension = 8;
            WalkSpeed = 1f;
            SprintSpeed = 2f;
            ChangeWeapon = "Mouse ScrollWheel";
            Horizontal = "Horizontal";
            Vertical = "Vertical";
            Fire1 = "Fire1";
            Fire2 = "Fire2";
            Reload = "Reload";
            Interact = "Interact";
            DropWeapon = "DropWeapon";
            Mask = -1;
        }
        public virtual void Start()
        {
            InputPermission = true;

            _aimCache = Vector3.forward;

            ThisGo = gameObject;
            ThisSubject = GetComponent<Subject>();
            ThisRb = GetComponent<Rigidbody>();
            MainCam = Camera.main;

            if (!MainCam) Debug.LogError("Main Camera not found! You must tag your primary camera as Main Camera.");
            if (LogDebug) Debug.Log("Rigidbody: " + ThisRb);
            if (LogDebug) Debug.Log("Main Camera: " + MainCam);
            if (LogDebug) Debug.Log("Subject: " + ThisSubject);

            _h = ThisSubject.ControlStats.AnimatorHorizontal;
            _v = ThisSubject.ControlStats.AnimatorVertical;

            if (ThisSubject.Stats.UseMecanim)
            {
                ThisAnimator = ThisSubject.GetAnimator();
                if (LogDebug) Debug.Log("PlayerController: Grabbed Animator from Subject: " + ThisAnimator);
                if (ThisAnimator == null) Debug.LogWarning("PlayerController: No Animator found! Check the reference to the Animator Host Obj and confirm that it has an Animator component.");
            }
        }
        public virtual void FixedUpdate()
        {
            if (ThisSubject.IsDead | !InputPermission) return;

            Aim();
            Move();
        }

        public virtual void Aim()
        {
            Ray ray;

            _aimInput = GetAimAxis;
            if (_aimInput != Vector3.zero) _aimCache = _aimInput;
            else _aimInput = _aimCache;

            // Find the Aim Point depending on input type
            // Basically we're using rays and screen coordinates to get consistent input results here.
            // We get the screen size so the ray will always be a consistent distance from the player. Closer rays mean sloppier aiming.
            // We use the player's screen position to relatively get the aim direction. Without it, the ray revolves around the center of screen. 
            // (that would be bad, and would feel very inconsistent)
            if (UseInControl)
            {
                float halfWidth = Screen.width / 2f;
                float halfHeight = Screen.height / 2f;
                Vector3 playerPositionOnScreen = MainCam.WorldToScreenPoint(transform.position);
                _aimInput = new Vector3(
                    playerPositionOnScreen.x + (_aimInput.x * halfWidth),
                    playerPositionOnScreen.y + (_aimInput.z * halfHeight),
                    0);
                ray = MainCam.ScreenPointToRay(_aimInput);
            }
            else ray = MainCam.ScreenPointToRay(_aimInput);

            // Raycast into the scene
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, Mask))
            {
                Vector3 dir = hit.point - ThisGo.transform.position; dir.y = 0f;
                Quaternion fin = Quaternion.LookRotation(dir);

                switch (AimControlFeel)
                {
                    // Stiff can have a maximum turn rate.
                    case ControlFeel.Stiff:
                        float angle = Quaternion.Angle(ThisGo.transform.rotation, fin);
                        float derp = angle / ThisSubject.ControlStats.TurnSpeed / AimTension;
                        float progress = Mathf.Min(1f, Time.deltaTime / derp);

                        ThisGo.transform.rotation = Quaternion.Slerp(transform.rotation, fin, progress);
                        break;

                    // Loose is a standard smooth blend. No max turn rate can be established.
                    case ControlFeel.Loose:
                        ThisGo.transform.rotation = Quaternion.Slerp(ThisGo.transform.rotation, fin, Time.deltaTime * ThisSubject.ControlStats.TurnSpeed);
                        break;
                }
            }

            if (LogDebug)
            {
                Debug.DrawRay(ray.origin, ray.direction * 20, Color.magenta);
                Debug.DrawLine(transform.position, hit.point, Color.red);
                Debug.DrawLine(MainCam.transform.position, hit.point, Color.green);
            }
        }
        public virtual void Move()
        {

            // COMMENT:
            // Yeah it's a little weird and obscure, but it works great and is super inexpensive to process so I don't care.

            // Get the raw input.
            Vector3 input = GetMovementAxis * (GetSprint > 0 && CurrentWeapon ? SprintSpeed : WalkSpeed); ;

            // Store original camera Quaternion.
            Quaternion s = MainCam.transform.rotation;

            // Get the Camera Euler rotation, then flatten the X rotation.
            // The X rotation screws up the TransformDirection() below, so we set it to 0.
            Vector3 t = MainCam.transform.rotation.eulerAngles;
            t.x = 0;

            // Apply the fixed rotation, now TransformDirection works correctly, then apply/revert to the original stored rotation.
            MainCam.transform.rotation = Quaternion.Euler(t);
            Vector3 movement = MainCam.transform.TransformDirection(input);
            MainCam.transform.rotation = s;

            // Get data for Mecanim which will be the translated inputs (movement) but relative to the body's (Subject's) facing direction.
            Vector3 relative = ThisSubject.transform.InverseTransformDirection(movement);

            // And now the movement and mecanim translation should be bang-on accurate no matter where the camera is.




            // Apply the movement to the Subject
            if (!UseRootMotion) ThisRb.MovePosition(transform.position + movement * ThisSubject.ControlStats.MoveSpeed * .01f);

            // Apply the relative to Mecanim
            if (ThisSubject.Stats.UseMecanim)
            {
                if (!ThisAnimator)
                {
                    Debug.LogWarning("No Animator Component was found so the PlayerController cannot animate the Subject. Have you assigned the Animator Host Obj?");
                }
                else
                {
                    if (AnimatorDampening > 0.01)
                    {
                        ThisAnimator.SetFloat(_h, relative.x, AnimatorDampening, Time.deltaTime);
                        ThisAnimator.SetFloat(_v, relative.z, AnimatorDampening, Time.deltaTime);
                    }
                    else
                    {
                        ThisAnimator.SetFloat(_h, relative.x);
                        ThisAnimator.SetFloat(_v, relative.z);
                    }
                }
            }
        }

        public virtual Vector3 GetMovementAxis
        {
            get
            {
                return new Vector3(
                        Input.GetAxis(Horizontal),
                        0,
                        Input.GetAxis(Vertical));
            }
        }
        public virtual Vector3 GetAimAxis
        {
            get
            {
                return Input.mousePosition;
            }
        }

        public virtual float GetInputFire1 { get { return Input.GetAxis(Fire1); } }
        public virtual float GetInputFire2 { get { return Input.GetAxis(Fire2); } }

        public virtual bool GetInputInteract { get { return Input.GetButton(Interact); } }
        public virtual bool GetInputDropWeapon { get { return Input.GetButton(DropWeapon); } }
        public virtual bool GetInputReload { get { return Input.GetButton(Reload); } }
        public virtual float InputChangeWeapon { get { return Input.GetAxisRaw(ChangeWeapon); } }
        public virtual float GetSprint { get { return Input.GetAxisRaw(Sprint); } }

        public virtual void OnGUI()
        {

        }

        public virtual void ResetMecanimParameters()
        {
            if (ThisAnimator)
            {
                ThisAnimator.SetFloat(_h, 0f);
                ThisAnimator.SetFloat(_v, 0f);
            }
        }
        public virtual void GetLatestWeapon()
        {
            CurrentWeapon = ThisSubject.GetCurrentWeaponComponent();
        }
    }
}