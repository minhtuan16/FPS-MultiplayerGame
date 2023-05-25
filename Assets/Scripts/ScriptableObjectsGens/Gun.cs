using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Ajinkya.FpsGame
{
    [CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string gunName;
        public int damage;
        public int ammo;
        public int burst;
        public int pellets;
        public int clipsize;
        public float firerate;
        public float bloom;
        public float recoil;
        public float kickBack;
        public float aimSpeed;
        public float reload;
        [Range(0, 1)] public float mainFOV;
        [Range(0, 1)] public float weaponFOV;
        public AudioClip gunshotSound;
        public float pitchRandomization;
        public float shotVolume;
        public GameObject prefab;
        public bool recovery;
        public int stash;
        public int clip;

        public void initialize()
        {
            stash = ammo;
            clip = clipsize;
        }

        public bool FireBullet()
        {
            if (clip > 0)
            {
                clip -= 1;
                return true;
            }
            else return false;
        }

        public void Reload()
        {
            stash += clip;
            clip = Mathf.Min(clipsize, stash);
            stash -= clip;
        }
        
        public int GetStash()
        {
            return stash;
        }
        public int GetClip()
        {
            return clip;
        }
    }
}