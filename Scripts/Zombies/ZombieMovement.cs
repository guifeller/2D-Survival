using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pathfinding;


/* Idea for zombie's AI :
 * Si le joueur n'est pas accessible, alors le zombie lance un rayon de lui vers le joueur et attaque le premier objet touche. Le path vers le joueur est recalculé toutes les
 * secondes (a ajuster) et si le path est libre alors va attaquer le joueur, sinon continue d'attaquer l'objet. Une fois l'objet detruit recalcule path et si joueur 
 * toujours inaccessible, repeat.
 */

public class ZombieMovement : MonoBehaviour {

    enum ZombieState {
        idle,
        detected,
        attacking
    };

    public Vector3 target;
    public Transform player;
    public PlayerControler playerData;
    public float hearing;
    public float horizontalCapsule, fov;
    public float speed;
    public float distanceLimite;
    public GameObject testobject;

    private Seeker seeker;
    private int currentWaypoint;
    private int rotateWaypoint = 1;
    private Path path;
    private Path coroutinePath;
    private ZombieState status;
    private bool canSearchAgain = true;
    private Vector3 lookTarget;

    private void Start() {
        status = ZombieState.idle;
        target = player.position;
        seeker = GetComponent<Seeker>();
        seeker.StartPath(transform.position, target);
        seeker.pathCallback += OnPathComplete;
        StartCoroutine("CalculatePaths");
        //StartCoroutine("RotateTowardTarget");
    }

    private void Update() {
        
        Debug.Log("target = " + target);
        Debug.Log("Player.position = " + player.position);
        Debug.Log(StaticNodeLibrary.FindClosestWalkablePosition(player));
        MoveTowardTarget();
    }

    private void FixedUpdate() {
    }

    public void OnPathComplete(Path p) {
        canSearchAgain = true;
        if (!p.error) {
            path = p;
            currentWaypoint = 0;            
        }
        else {
            Debug.Log("ERROR " + p.error);
        }
    }

    public IEnumerator CalculatePaths() {

        while(true) {
            Debug.Log("CalculatePaths while(true)");
            if (canSearchAgain == true) {
                Debug.Log("CalculatePaths while(true) canSearchAgain == true");
                coroutinePath = ABPath.Construct(transform.position, target); //Seule facon non dépréciée de créer un path.
                coroutinePath.nnConstraint = NNConstraint.None; //ne se limite pas aux noeuds walkable
                coroutinePath.nnConstraint.constrainWalkability = true;
                coroutinePath.nnConstraint.walkable = true;
                AstarPath.StartPath(coroutinePath); // Must start the calculation of the path
                yield return (coroutinePath.WaitForPath());
                if (coroutinePath.error && status == ZombieState.attacking) {
                    Debug.Log("path.error");
                    CheckSurrounding();
                    canSearchAgain = true;
                }
                else if (coroutinePath.error &&  (status == ZombieState.idle || status == ZombieState.detected)) {
                    AttackClosestObject();
                    Debug.Log("AttackClosestObject true");
                    canSearchAgain = true;
                }
                else {
                    target = player.position;
                    Debug.Log(status);
                    Debug.Log(path);
                    seeker.StartPath(transform.position, target, OnPathComplete);
                    canSearchAgain = false;
                }
                
                yield return new WaitForSeconds(0.6f); //Il faudra voir pour adapter la duree en fonction de l'intelligence des zombies et des performances.
            }
            else {
                yield return 0;
            }
        }
    }

    public IEnumerator RotateTowardTarget() {
        while(true) {
            
            if (path != null && (status == ZombieState.detected || status == ZombieState.idle)) {
                Debug.Log("path non nul");
                //lookTarget = path.vectorPath[rotateWaypoint];
                //lookTarget = new Vector3(0, 0, (path.vectorPath[rotateWaypoint] - transform.position).z);
                lookTarget = transform.position - path.vectorPath[1];
                Debug.Log("coordonnees rotation " + path.vectorPath[rotateWaypoint]);
            }
            if(status == ZombieState.attacking) {
                //lookTarget = new Vector3(0, 0, (target - transform.position).z);
                lookTarget = transform.position - target;
            }
            /*if(Vector3.Distance(lookTarget, transform.position) < distanceLimite) {
                Debug.Log("TOOCLOSE");
                yield return 0;
            }*/
            Debug.Log("Distance rotation: " + Vector3.Distance(lookTarget, transform.position));
            
            Debug.Log("LookTarget: " + lookTarget);
            transform.rotation = Quaternion.LookRotation(lookTarget, Vector3.forward);
            transform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z);
            yield return new WaitForSeconds(0.1f);
        }
    }

    protected void AttackClosestObject() {

        //If Player cannot be reached, then the zombie searches for the closest object and goes to attack it by making the object the target of the seeker.
        int layerMask = 1 << 9;
        layerMask = ~layerMask;
        Vector2 directionPlayer = (player.position - transform.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionPlayer, Vector3.Distance(transform.position, player.transform.position), layerMask);
        Debug.DrawRay(transform.position, directionPlayer);
        Debug.Log("Tag " + hit.transform.tag);
        if (hit.transform.tag == "Base") {
            //Includes check the player isn't actually reachable in a straight line, covers for problem with A* and possible movement of the player
            target = StaticNodeLibrary.FindClosestWalkablePosition(hit.transform);
            Debug.Log(StaticNodeLibrary.FindClosestWalkablePosition(hit.transform));
            Debug.Log(hit.transform.tag);
            seeker.StartPath(transform.position, target, OnPathComplete);
        }
        else if (hit.transform.tag == "Player") {
            target = player.position;
            status = ZombieState.detected;
            seeker.StartPath(transform.position, target, OnPathComplete);
        }
    }

    protected void CheckSurrounding() {
        /* 
         * The zombie cannot constantly recalculate the presence of the target while attacking because that would block it and waste ressources. 
         * As such while the zombie is attacking an object that is not the player it must constantly be looking for the player but in a way that will not mess with its
         * current path and that is reasonable gameplay-wise. As such the zombie will constantly be checking its surrounding, represented in-game by an overlapcircle (hearing)
         * and an overlapcapsule(FOV).
         * If the player is heard by the zombie then the zombie will check for a possible path. If path inferior to an x number of waypoint then the zombie will move toward the player.
         * The area where the zombie can hear will be affected by a modifier depending on the player's stealth (weight, crouching or not, etc.)
         * If the player is seen by the zombie and there is a path, the zombie will rush toward the player no matter the distance. Otherwhise nothing happens.
         * Either way if there is a path the zombie will scream. The scream will be heard by all the zombies in an area corresponding to y * hearing area. 
         * All the zombies affected will then recalculate a way to the player and walk toward the player if possible.
         */

        float modifier = playerData.stealth;
        Collider2D hitColliderHearing = Physics2D.OverlapCircle(transform.position, hearing * modifier, LayerMask.NameToLayer("Player"));
        Collider2D hitColliderFov = Physics2D.OverlapCapsule(transform.position, new Vector2(horizontalCapsule, fov), CapsuleDirection2D.Vertical, LayerMask.NameToLayer("Player"));
        if (hitColliderFov || hitColliderHearing) {
            status = ZombieState.detected;
            target = player.position;
        }

    }

    protected void MoveTowardTarget() {
        if(path!= null) {
            Vector3 dir = path.vectorPath[1] - transform.position;
            transform.Translate(dir.normalized * speed * Time.deltaTime);
        }
    }


    /*Pour attaque, si DistanceZombiePlayer est inferieure a portee de l'arme, alors rotation est position player - position zombie etc. comme pour la souris dans le cadre du joueur
     * Sinon, le zombie pourchasse le joueur et dans ce cas il regarde droit devant lui.
     */

}

/*
 * Armes dans un autre script, faire appel à ce script depuis celui ci qui est le script maître des zombies
 */