using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class UpdateGraphMovable : MonoBehaviour {

	// Use this for initialization
	void Start () {
        UpdateGraph();
    }
	
	// Update is called once per frame
	void Update () {
    }

    void UpdateGraph() {
        Bounds bounds = GetComponent<Collider2D>().bounds;
        var guo = new GraphUpdateObject(bounds);

        // Set some settings
        guo.updatePhysics = false;
        AstarPath.active.UpdateGraphs(guo);
    }

}
