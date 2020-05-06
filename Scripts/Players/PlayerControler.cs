using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControler : MonoBehaviour {

    public float speed;
    public float stealth; //Stealth between 0 and 2, 0 being the  stealthiesy and 2 the least stealthy.

    private Rigidbody2D rb2d;

    private void Start() {
        rb2d = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate() {

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Quaternion rot = Quaternion.LookRotation(transform.position - mousePosition,
            Vector3.forward);
        transform.rotation = rot;
        transform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z);
        rb2d.angularVelocity = 0;

        if(Input.GetKey(KeyCode.W)) {
            rb2d.AddForce(gameObject.transform.up * speed * Time.fixedDeltaTime);
        }

        else {
            rb2d.velocity = Vector3.zero;
            rb2d.Sleep();
        }
    }
}
