using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class StaticNodeLibrary : MonoBehaviour {

	public static bool CanPlaceObject(Transform obj) {
        GraphNode node = AstarPath.active.GetNearest(obj.position).node;

        if(node.Walkable) {
            return true;
        }
        else {
            return false;
        }
    }

    public static Vector3 FindClosestWalkablePosition(Transform obj) {
        var constraint = NNConstraint.None;
        constraint.walkable = true;
        constraint.constrainWalkability = true;
        var pos = AstarPath.active.GetNearest(obj.position, constraint);
        return pos.position;
    }

    public static Transform CreateClosestGameObject(Transform obj) {
        var constraint = NNConstraint.None;
        constraint.constrainWalkability = true;
        constraint.walkable = true;
        var pos = AstarPath.active.GetNearest(obj.position, constraint);
        GameObject go = new GameObject();
        go.transform.position = pos.position;
        return go.transform;
    }
}
