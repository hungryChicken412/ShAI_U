using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {
	[Header("ShAI_U Gun")]

	[Header("Main")]
	[SerializeField] private Animator anim;
	[SerializeField] private ShAI_U aiThatHasThisGun;
	[SerializeField] private int bullets = 100;
	[SerializeField] private int magCap = 30;
	[SerializeField] private int bulletsInMag;
	[SerializeField] private float fireRate = 1f;
	[SerializeField] private Transform gunBarrel;
	[SerializeField] private GameObject flash;
	[SerializeField] private GameObject impactEffect;
	public bool reloading;
	float shootTime, nextTimeToShoot;
	// Use this for initialization
	void Start () {
		bulletsInMag = magCap;
		bullets -= magCap;
	}
	
	// Update is called once per frame
	void Update () {
		shootTime += Time.deltaTime;


	}

	public void Shoot(Vector3 shootDirection){
		if (shootTime >= nextTimeToShoot && !reloading) {
			if (bulletsInMag > 1) {
				nextTimeToShoot = shootTime + fireRate;
				GameObject flashObj = (GameObject)Instantiate (flash, gunBarrel.position, gunBarrel.rotation);
				Destroy (flashObj, 1f); // Destroy the flash Object after 5 Seconds;
				RaycastHit hit;
				if (Physics.Raycast (gunBarrel.position, shootDirection, out hit, Mathf.Infinity)) {
					GameObject impactObj = (GameObject)Instantiate (impactEffect, hit.point, Quaternion.LookRotation (hit.normal));
					Destroy (impactObj, 5f); // Destroy the Impact Object after 5 Seconds;
					if (hit.transform.GetComponent<ShAI_U>()){
						hit.transform.GetComponent<ShAI_U> ().health -= 10f;
					}
				}
				bulletsInMag--;
			} else {
				Reload ();
			}
		}

	}

	public void Reload(){
		
		if (aiThatHasThisGun.inCover) {
			anim.SetBool ("crouch", true);
			anim.SetLayerWeight (1, 1);
		}
		if (bullets > 0) {
			nextTimeToShoot = shootTime + 3f;
			if (bullets > magCap){
				bulletsInMag = magCap;
				bullets -= magCap;
			}
			else {
				bulletsInMag = magCap;
				bullets = 0;
			}
			reloading = true;
			anim.SetBool ("Reload", reloading);
			anim.SetLayerWeight (2, 1);
		} else {
			if (aiThatHasThisGun != null) {// null means the player has this gun;
				aiThatHasThisGun.RunForYourLife();
			}
		}

	}



}