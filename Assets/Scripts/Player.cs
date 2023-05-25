using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using TMPro;

namespace Com.Ajinkya.FpsGame
{
    public class Player : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region Variables
        public float speed;
        public float sprintModifier;
        public float jumpForce;
        public float jetForce;
        public float jetWait;
        public float jetRecovery;
        public int max_health;
        public float max_fuel;
        public Camera noramalCam;
        public Camera weaponCam;
        public GameObject cameraParent;
        public GameObject mesh;
        public Transform weaponParent;
        public Transform groundDetector;
        public LayerMask ground;

        public ProfileData playerProfile;
        public TextMeshPro playerUsername;

        //public ParticleSystem jetParticles;

        private Transform UI_healthBar;
        private Transform UI_fuelbar;
        private Text UI_ammo;
        private Text UI_username;

        public Rigidbody rig;

        private Vector3 targetWeaponBobPosition;
        private Vector3 weaponParentOrigin;

        private float movementCounter;
        private float idleCounter;

        private float baseFOV;
        private float sprintFOVModifier = 1.5f;

        private int current_health;
        private float current_fuel;
        private float current_recovery;


        private Manager manager;
        private Weapon weapon;

        private bool isAiming;
        private bool canJet;

        private float aimAngle;

        #endregion

        #region Photon Callbacks

        void IPunObservable.OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
        {
            if(p_stream.IsWriting)
            {
                p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
            }
            else
            {
                aimAngle = (int)p_stream.ReceiveNext() / 100f;
            }
        }

        #endregion

        #region MonpBehavoir Callbacks
        private void Start()
        {
            manager = GameObject.Find("Manager").GetComponent<Manager>();
            weapon = GetComponent<Weapon>();
            current_health = max_health;
            current_fuel = max_fuel;

            cameraParent.SetActive(photonView.IsMine);

            if (!photonView.IsMine)
            {
                gameObject.layer = 11;
                ChangeLayerRecursively(mesh.transform, 11);
            }

            baseFOV = noramalCam.fieldOfView;
            
            weaponParentOrigin = weaponParent.localPosition;

            if (photonView.IsMine)
            {
                UI_healthBar = GameObject.Find("HUD/Health/Bar").transform;
                UI_fuelbar = GameObject.Find("HUD/Fuel/Bar").transform;
                UI_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                UI_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();
                
                RefreshHealthBar();
                UI_username.text = Launcher.myProfile.username;

                photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);
            }
        }

        private void ChangeLayerRecursively(Transform p_trans, int p_layer)
        {
            p_trans.gameObject.layer = p_layer;
            foreach (Transform t in p_trans) ChangeLayerRecursively(t, p_layer);
        }

        private void Update()
        {
            if (!photonView.IsMine)
            {
                RefreshMultiplayerState();
                return;
            }

            //Axes
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool pause = Input.GetKeyDown(KeyCode.Escape);

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;

            //Pause
            if(pause)
            {
                GameObject.Find("Pause").GetComponent<Pause>().ToogglePause();
            }

            if(Pause.paused)
            {
                t_hmove = 0f;
                t_vmove = 0f;
                sprint = false;
                jump = false;
                pause = false;
                isGrounded = false;
                isJumping = false;
                isSprinting = false;
            }

            //Jumping
            if (isJumping)
            {
                rig.AddForce(Vector3.up * jumpForce);
                current_recovery = 0f;
            }

            if (Input.GetKeyDown(KeyCode.U)) TakeDamage(100, -1);

            //HeadBob
            if (!isGrounded)
            {
                HeadBob(idleCounter, 0.025f, 0.025f);
                idleCounter += 0;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if (t_hmove == 0 && t_vmove == 0)
            {
                HeadBob(idleCounter, 0.025f, 0.025f);
                idleCounter += Time.deltaTime;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if(!isSprinting)
            {
                HeadBob(movementCounter, 0.035f, 0.035f);
                movementCounter += Time.deltaTime * 3f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else
            {
                HeadBob(movementCounter, 0.15f, 0.075f);
                movementCounter += Time.deltaTime * 7f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            }

            //UI Refreshes
            RefreshHealthBar();
            weapon.RefreshAmmo(UI_ammo);
        }
        void FixedUpdate()
        {
            if (!photonView.IsMine) return;

            //Axes
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool aim = Input.GetMouseButton(1);
            bool jet = Input.GetKey(KeyCode.Space);

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
            isAiming = aim && !isSprinting;

            //Pause
            if (Pause.paused)
            {
                t_hmove = 0f;
                t_vmove = 0f;
                sprint = false;
                jump = false;
                isGrounded = false;
                isJumping = false;
                isSprinting = false;
                isAiming = false;
            }

            //Movements
            Vector3 t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();

            float t_adjustedSpeed = speed;
            if (isSprinting) t_adjustedSpeed *= sprintModifier;

            Vector3 t_targetVelocity = transform.TransformDirection(t_direction) * t_adjustedSpeed * Time.deltaTime;
            t_targetVelocity.y = rig.velocity.y;
            rig.velocity = t_targetVelocity;

            //Jetting
            if (jump && !isGrounded)
                canJet = true;
            if (isGrounded)
                canJet = false;

            if(canJet && jet && current_fuel > 0)
            {
                rig.AddForce(Vector3.up * jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
                current_fuel = Mathf.Max(0, current_fuel - Time.fixedDeltaTime);
                //jetParticles.Play();
            }

            if(isGrounded)
            {
                if (current_recovery < jetWait)
                    current_recovery = Mathf.Min(jetWait, current_recovery + Time.fixedDeltaTime);
                else
                    current_fuel = Mathf.Min(max_fuel, current_fuel + Time.fixedDeltaTime * jetRecovery);
            }

            UI_fuelbar.localScale = new Vector3(current_fuel / max_fuel, 1, 1);

            //Aiming
            isAiming = weapon.Aim(isAiming);

            //Camera Stuff
            if (isSprinting)
            {
                noramalCam.fieldOfView = Mathf.Lerp(noramalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
            }
            else if(isAiming)
            {
                noramalCam.fieldOfView = Mathf.Lerp(noramalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 8f);
            }
            else
            {
                noramalCam.fieldOfView = Mathf.Lerp(noramalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
            }
        }
        #endregion

        #region Private Methods

        void RefreshMultiplayerState()
        {
            float cacheEulY = weaponParent.localEulerAngles.y;

            Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
            weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

            Vector3 finalRotation = weaponParent.localEulerAngles;
            finalRotation.y = cacheEulY;

            weaponParent.localEulerAngles = finalRotation;
        }

        void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
        {
            float t_aim_adjust = 1f;
            if (isAiming) t_aim_adjust = 0.1f;
            targetWeaponBobPosition = weaponParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
        }

        void RefreshHealthBar()
        {
            float t_health_ratio = (float)current_health / (float)max_health;
            UI_healthBar.localScale = Vector3.Lerp(UI_healthBar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
        }
        
        [PunRPC]
        private void SyncProfile(string p_username, int level, int xp)
        {
            playerProfile = new ProfileData(p_username, level, xp);
            playerUsername.text = playerProfile.username;
        }

        #endregion

        #region Public Methods

        public void TakeDamage(int p_damage, int p_actor)
        {
            if (photonView.IsMine)
            {
                current_health -= p_damage;
                RefreshHealthBar();

                if(current_health <= 0)
                {
                    manager.Spawn();
                    manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                    if (p_actor >= 0)
                        manager.ChangeStat_S(p_actor, 0, 1);

                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }

        #endregion
    }
}
