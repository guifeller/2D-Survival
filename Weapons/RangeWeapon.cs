using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeWeapon : MonoBehaviour {

    public float range, damage;
    public Ammunition ammo;

    private Transform pPar, zPar;

    void Awake() {
        if(transform.parent.tag == "Player") {
            ammo = 
        }
    }

}
