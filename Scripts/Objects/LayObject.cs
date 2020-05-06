using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayObject : MonoBehaviour {

    public GameObject gObj;

	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.P)) {
            Instantiate(gObj);
        }
    }
}
