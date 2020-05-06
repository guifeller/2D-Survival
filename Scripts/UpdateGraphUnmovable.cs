using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class UpdateGraphUnmovable : Pathfinding.GraphModifier {

    /** Collider to get bounds information from */
    Collider coll;

    /** 2D Collider to get bounds information from */
    Collider2D coll2D;

    /** Cached transform component */
    Transform tr;

    /** The minimum change in world units along one of the axis of the bounding box of the collider to trigger a graph update */
    public float updateError = 1;


    /** Bounds of the collider the last time the graphs were updated */
    Bounds prevBounds;

    /** Rotation of the collider the last time the graphs were updated */
    Quaternion prevRotation;

    /** True if the collider was enabled last time the graphs were updated */
    bool prevEnabled;

    Bounds bounds {
        get {
            if (coll != null) {
                return coll.bounds;
            }
            else {
                var b = coll2D.bounds;
                // Make sure the bounding box stretches close to infinitely along the Z axis (which is the axis perpendicular to the 2D plane).
                // We don't want any change along the Z axis to make a difference.
                b.extents += new Vector3(0, 0, 10000);
                return b;
            }
        }
    }

    bool colliderEnabled {
        get {
            return coll != null ? coll.enabled : coll2D.enabled;
        }
    }

    protected override void Awake() {
        base.Awake();
        coll = GetComponent<Collider>();
        coll2D = GetComponent<Collider2D>();
        tr = transform;
        if (coll == null && coll2D == null) {
            throw new System.Exception("A collider or 2D collider must be attached to the GameObject(" + gameObject.name + ") for the DynamicGridObstacle to work");
        }

        prevBounds = bounds;
        prevRotation = tr.rotation;
        // Make sure we update the graph as soon as we find that the collider is enabled
        prevEnabled = false;
    }

    public override void OnPostScan() {
        // In case the object was in the scene from the start and the graphs
        // were scanned then we ignore the first update since it is unnecessary.
        prevEnabled = colliderEnabled;
    }

    void Start() {
        if (coll == null && coll2D == null) {
            Debug.LogError("Removed collider from DynamicGridObstacle", this);
            enabled = false;
            return;
        }

        if (AstarPath.active == null || AstarPath.active.isScanning || !Application.isPlaying) {
            return;
        }

        if (colliderEnabled) {
            DoUpdateGraphs();
        }
        else {
            // Collider has just been disabled
            if (prevEnabled) {
                DoUpdateGraphs();
            }
        }
    }

    /** Revert graphs when disabled.
		* When the DynamicObstacle is disabled or destroyed, a last graph update should be done to revert nodes to their original state
		*/
    protected override void OnDisable() {
        base.OnDisable();
        if (AstarPath.active != null && Application.isPlaying) {
            var guo = new GraphUpdateObject(prevBounds);
            AstarPath.active.UpdateGraphs(guo);
            prevEnabled = false;
        }
    }

    /** Update the graphs around this object.
		* \note The graphs will not be updated immediately since the pathfinding threads need to be paused first.
		* If you want to guarantee that the graphs have been updated then call AstarPath.active.FlushGraphUpdates()
		* after the call to this method.
		*/
    public void DoUpdateGraphs() {
        if (coll == null && coll2D == null) return;

        if (!colliderEnabled) {
            // If the collider is not enabled, then col.bounds will empty
            // so just update prevBounds
            AstarPath.active.UpdateGraphs(prevBounds);
        }
        else {
            Bounds newBounds = bounds;

            Bounds merged = newBounds;
            merged.Encapsulate(prevBounds);

            // Check what seems to be fastest, to update the union of prevBounds and newBounds in a single request
            // or to update them separately, the smallest volume is usually the fastest
            if (BoundsVolume(merged) < BoundsVolume(newBounds) + BoundsVolume(prevBounds)) {
                // Send an update request to update the nodes inside the 'merged' volume
                AstarPath.active.UpdateGraphs(merged);
            }
            else {
                // Send two update request to update the nodes inside the 'prevBounds' and 'newBounds' volumes
                AstarPath.active.UpdateGraphs(prevBounds);
                AstarPath.active.UpdateGraphs(newBounds);
            }

            prevBounds = newBounds;
        }

        prevEnabled = colliderEnabled;
        prevRotation = tr.rotation;

        // Set this here as well since the DoUpdateGraphs method can be called from other scripts
    }

    /** Volume of a Bounds object. X*Y*Z */
    static float BoundsVolume(Bounds b) {
        return System.Math.Abs(b.size.x * b.size.y * b.size.z);
    }
}


