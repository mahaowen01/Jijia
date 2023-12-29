using UnityEngine;
using System.Collections;

namespace FORGE3D
{
    public class F3DPlayerTurretController : MonoBehaviour
    {
        RaycastHit hitInfo; // Raycast structure
        public F3DTurret turret;
        bool isFiring; // Is turret currently in firing state
        public F3DFXController fxController;
        public GameObject AttackWeapon;
        public GameObject car;
        private float carmove = 20f;
        [System.Obsolete]
  
        void Update()
        {
            CheckForTurn();
            CheckForFire();
            if (Input.GetKeyDown(KeyCode.F))
            {
                car.GetComponent<Animator>().SetTrigger("Take 001");
                AttackWeapon.GetComponent<Animator>().SetTrigger("AttackWeapon");
                carmove = 5.0f;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                car.GetComponent<Animator>().SetTrigger("Stand");
                AttackWeapon.GetComponent<Animator>().SetTrigger("AttackWeaponEnd");
                carmove = 20.0f;
            }


            MovePostion(carmove);
        }

        void CheckForFire()
        {
            // Fire turret
            if (!isFiring && Input.GetKeyDown(KeyCode.Mouse0))
            {
                isFiring = true;
                fxController.Fire();
            }

            // Stop firing
            if (isFiring && Input.GetKeyUp(KeyCode.Mouse0))
            {
                isFiring = false;
                fxController.Stop();
            }
        }

        void CheckForTurn()
        {
            // Construct a ray pointing from screen mouse position into world space
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Raycast
            if (Physics.Raycast(cameraRay, out hitInfo, 500f))
            {
                turret.SetNewTarget(hitInfo.point);
            }
        }

        void MovePostion(float Speed )
        {
            var SpeedMove = Speed;
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * SpeedMove * Time.deltaTime;
            turret.transform.position += movement;
            //if (!isFiring &&)
        }


    }
}