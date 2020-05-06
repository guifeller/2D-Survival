using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mold_Position : MonoBehaviour {

    public GameObject obj;
    public static Mold_Position instance = null;
    private float xPosition, yPosition;
    private float floorX, floorY, ceilingX, ceilingY;

    void Awake() {
        //Check if instance already exists
        if (instance == null)

            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
    }

    void Start() {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        xPosition = mousePosition.x;
        yPosition = mousePosition.y;
        floorX = Mathf.Floor(xPosition);
        floorY = Mathf.Floor(yPosition);
        transform.position = new Vector3(floorX + 0.5f, floorY + 0.5f, 0);
    }

    // Update is called once per frame
    void Update () {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        xPosition = mousePosition.x;
        yPosition = mousePosition.y;
        floorX = Mathf.Floor(xPosition);
        floorY = Mathf.Floor(yPosition);
        transform.position = new Vector3(floorX + 0.5f, floorY + 0.5f, 0);
        if (Input.GetMouseButtonDown(2)) {
            Instantiate(obj, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}
