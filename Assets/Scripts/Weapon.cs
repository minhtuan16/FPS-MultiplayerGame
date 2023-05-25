using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;

namespace Com.Ajinkya.FpsGame
{
    public class Weapon : MonoBehaviourPunCallbacks
    {
        #region Variables
        public List<Gun> loadout;
        [HideInInspector] public Gun currentGunData;

        public Transform weaponParent;
        public GameObject bulletholePrefab;
        public LayerMask canBeShot;
        public bool isAiming = false;
        public AudioClip hitmarkerSound;
        public AudioSource sfx;

        private float currentCooldown;
        private int currentIndex;
        private GameObject currentWeapon;

        private Image hitmarkerImage;
        private float hitmarkerWait;

        private bool isReloading;

        private Color CLEARWHITE = new Color(1, 1, 1, 0);
        #endregion

        #region MonoBehavoir Callbacks
        void Start()
        {
            foreach (Gun a in loadout) a.initialize();
            hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
            hitmarkerImage.color = CLEARWHITE;
            Equip(0);
        }

        void Update()
        {  
            if(Pause.paused && photonView.IsMine)
            {
                return;
            }

            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
            {
                photonView.RPC("Equip", RpcTarget.All, 0);
            }

            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2))
            {
                photonView.RPC("Equip", RpcTarget.All, 1);
            }

            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha3))
            {
                photonView.RPC("Equip", RpcTarget.All, 2);
            }

            if (currentWeapon != null)
            {
                if (photonView.IsMine)
                {
                    if (loadout[currentIndex].burst != 1)
                    {
                        if (Input.GetMouseButtonDown(0) && currentCooldown <= 0)
                        {
                            if (loadout[currentIndex].FireBullet())
                            {
                                photonView.RPC("Shoot", RpcTarget.All);
                            }
                            else if (!isReloading && loadout[currentIndex].stash > 0) 
                                StartCoroutine(Reload(loadout[currentIndex].reload));
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButton(0) && currentCooldown <= 0)
                        {
                            if (loadout[currentIndex].FireBullet())
                            {
                                photonView.RPC("Shoot", RpcTarget.All);
                            }
                            else if(!isReloading && loadout[currentIndex].stash > 0)
                                StartCoroutine(Reload(loadout[currentIndex].reload));
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.R) && loadout[currentIndex].clip != loadout[currentIndex].clipsize && !isReloading && loadout[currentIndex].stash > 0) 
                        StartCoroutine(Reload(loadout[currentIndex].reload));

                    //Cooldown
                    if (currentCooldown > 0)
                    {
                        currentCooldown -= Time.deltaTime;
                    }
                }

                //Weapon position elasticity
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
            }

            if(photonView.IsMine)
            {
                if(hitmarkerWait > 0)
                {
                    hitmarkerWait -= Time.deltaTime;
                }
                else if(hitmarkerImage.color.a > 0)
                {
                    hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, CLEARWHITE, Time.deltaTime * 2f);
                }
            }
        }
        #endregion

        #region Private Methods

        IEnumerator Reload(float p_wait)
        {
            isReloading = true;

            if (currentWeapon.GetComponent<Animator>())
            {
                currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
            }
            else
            {
                currentWeapon.SetActive(false);
            }

            yield return new WaitForSeconds(p_wait);

            loadout[currentIndex].Reload();
            currentWeapon.SetActive(true);
            isReloading = false;
        }

        [PunRPC]
        void Equip(int p_ind)
        {
            if (currentWeapon != null)
            {
                if(isReloading) StopCoroutine("Reload");
                Destroy(currentWeapon);
            }

            currentIndex = p_ind;

            GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;
            t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

            if (photonView.IsMine) ChangeLayersRecursively(t_newWeapon, 10);
            else ChangeLayersRecursively(t_newWeapon, 0);

            t_newWeapon.GetComponent<Animator>().Play("Equip", 0, 0);
            
            currentWeapon = t_newWeapon;
            currentGunData = loadout[p_ind];
        }

        [PunRPC]
        void pickupWeapon(string name)
        {
            Gun newWeapon = GunLibrary.FindGun(name);

            if(loadout.Count >= 2)
            {
                loadout[currentIndex] = newWeapon;
                Equip(currentIndex);
            }
            else
            {
                loadout.Add(newWeapon);
                Equip(loadout.Count - 1);
            }
        }
        private void ChangeLayersRecursively(GameObject p_target, int p_layer)
        {
            p_target.layer = p_layer;
            foreach (Transform a in p_target.transform) ChangeLayersRecursively(a.gameObject, p_layer);
        }

        public bool Aim(bool p_isAisming)
        {
            if (!currentWeapon) return false;
            if (isReloading) p_isAisming = false;

            isAiming = p_isAisming;
            Transform t_anchor = currentWeapon.transform.GetChild(0);
            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

            if (p_isAisming)
            {
                //Aim
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }
            else
            {
                //Hip
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
            }

            return p_isAisming;
        }

        [PunRPC]
        void Shoot()
        {
            if (isReloading) return;

            Transform t_spawn = transform.Find("Cameras/Normal Camera");

            for(int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
            {
                //bloom
                Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
                t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
                t_bloom -= t_spawn.position;
                t_bloom.Normalize();

                //Raycast
                RaycastHit t_hit = new RaycastHit();
                if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
                {
                    GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                    t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                    Destroy(t_newHole, 5f);

                    if (photonView.IsMine)
                    {
                        //Shooting other player on network
                        if (t_hit.collider.gameObject.layer == 11)
                        {
                            //Give damage
                            t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                            //Display hitmarker
                            hitmarkerImage.color = Color.white;
                            sfx.PlayOneShot(hitmarkerSound);
                            hitmarkerWait = 1f;
                        }

                    }
                }
            }

            //sound
            sfx.Stop();
            sfx.clip = currentGunData.gunshotSound;
            sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
            sfx.volume = currentGunData.shotVolume;
            sfx.Play();

            //Gun fx
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
            currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickBack;
            if(currentGunData.recovery) currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0);

            //Cooldown
            currentCooldown = loadout[currentIndex].firerate;
        }

        [PunRPC]
        private void TakeDamage(int p_damage, int p_actor)
        {
            GetComponent<Player>().TakeDamage(p_damage, p_actor);
        }

        #endregion

        #region Public Methods

        public void RefreshAmmo(Text p_text)
        {
            int t_clip = loadout[currentIndex].GetClip();
            int t_stash = loadout[currentIndex].GetStash();

            p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");
        }

        #endregion
    }
}