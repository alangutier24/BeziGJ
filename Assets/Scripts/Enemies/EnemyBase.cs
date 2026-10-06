using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{

    protected NavMeshAgent agent;

    [SerializeField]
    private Transform[] waypoints; 

    [SerializeField]
    private float waypointPause = 2.5f;

    [SerializeField]
    protected float moveSpeed = 2f;

    [SerializeField]
    protected float viewRange = 7f;
    
    [SerializeField]
    protected float halfFovAngle = 45f;

    [SerializeField]
    private float eyeHeight = 1f;

    [SerializeField]
    private float targetHeight = 0.5f;

    [SerializeField]
    private LayerMask obstacleMask;

    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    protected Transform playerTransform;

    [SerializeField]
    protected NavMeshPath navMesh;

    public bool CanSeeTarget = false;

    public bool PlayerInRange = false;

    protected Vector3 lastKnownPosition;

    private int wayPointIndex = 0;

    public Transform currentTarget;

    protected float waitTimer = 0f;

    private const float ArriveMargin = 0.1f;

    public Vector3 lookDir;

    public bool Shooter = false;

    protected enum EnemyState
    {
        Patrol,
        Pursue,
        Search,
        Catch,
        Attack,
        Shoot
    }

    [SerializeField]
    protected EnemyState state = EnemyState.Patrol;

    public Quaternion RightOffset = Quaternion.Euler(0f, -90f, 0f);

    bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log($"Awake ran on {name}, GetComponent returned: {agent}", this);
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        navMesh = new NavMeshPath();
    }

    protected virtual void Start()
    {
        agent.speed = moveSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.updateRotation = false;

        state = EnemyState.Patrol;

        if (HasWaypoints)
        {
            currentTarget = waypoints[wayPointIndex];
            setNextWaypointPosition();
        }
    }

    static readonly Quaternion FaceRight = Quaternion.identity;          // modelo mira a +X
    static readonly Quaternion FaceLeft  = new Quaternion(0f, 1f, 0f, 0f); // (x, y, z, w)

    [SerializeField] private float facingDeadZone = 0.1f; // evita parpadeo al moverse casi solo en Z

    protected virtual bool CanSeePlayer()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        PlayerInRange = toPlayer.sqrMagnitude <= viewRange * viewRange;
        if (!PlayerInRange) return false;

        // El cono solo cuenta para detectarlo; ya visto, solo lo rompen distancia u obstáculo
        if (!CanSeeTarget &&
            Vector3.Dot(lookDir, toPlayer.normalized) < Mathf.Cos(halfFovAngle * Mathf.Deg2Rad))
            return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = playerTransform.position + Vector3.up * targetHeight;
        Vector3 dir = target - origin;
        float distance = dir.magnitude;
        dir /= distance;

        Debug.DrawRay(origin, dir * distance, Color.red, 0.1f);
        return Physics.Raycast(origin, dir, out RaycastHit hit, distance, playerLayer | obstacleMask)
            && IsInMask(hit.collider.gameObject.layer, playerLayer);
    }

    protected virtual void Update()
    {
        CanSeeTarget = CanSeePlayer();
        if (CanSeeTarget) lastKnownPosition = playerTransform.position;

        EnemyState previous = state;
        state = DecideState();
        if (state != previous) OnStateChanged(previous, state);

        TickState();

        if(!Shooter)
           FaceHorizontal(CanSeeTarget ? playerTransform.position.x - transform.position.x : agent.velocity.x);
    }

    protected void FaceHorizontal(float dirX)
    {
        if (Mathf.Abs(dirX) < facingDeadZone)  return; // mantiene el último lado
        transform.rotation = dirX < 0f ? FaceLeft : FaceRight;
        lookDir = dirX < 0f ? new Vector3(-1f, 0f, 0f) : new Vector3(1f, 0f, 0f);
    }
    /*
    void FaceForward(float dirX)
    {
        if (Mathf.Abs(dirX) < facingDeadZone) return; // mantiene el último lado
        transform.rotation = dirX < 0f ? FaceBack : FaceForward;
    }
    */
    protected abstract EnemyState DecideState();

    protected abstract void TickState();

    protected virtual void OnStateChanged(EnemyState oldState, EnemyState newState)
    {
        waitTimer = 0f;
        if (newState == EnemyState.Patrol && currentTarget != null)
        {
            Move(currentTarget.position);
        }
    }

    // ---------- Perception ----------

    protected bool PlayerFound()
    {
        CanSeeTarget = false;
        PlayerInRange = false;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= viewRange * viewRange)
        {
            //playerTransform in radius of range
            PlayerInRange = true;
            CanSeeTarget = CanSeePlayer();
        }

        if (CanSeeTarget)
        {
            lastKnownPosition = playerTransform.position;
        }

        return CanSeeTarget;
    }

    protected bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    // ---------- Movement ----------

    protected virtual void Move(Vector3 destination)
    {
        agent.SetDestination(destination);
        if(Shooter)
            lookDir = currentTarget.position - transform.position;
    }

    protected bool HasArrived()
    {
        return !agent.pathPending &&
               (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + ArriveMargin);
    }

    protected void Patrol()
    {
        if (!HasWaypoints)
        {
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                setNextWaypointPosition();
            }
            return;
        }

        if (HasArrived())
        {
            if (waypointPause > 0f)
            {
                waitTimer = waypointPause;
            }
            else
            {
                setNextWaypointPosition();
            }
            return;
        }

        UnstickAgent();
    }

    void setNextWaypointPosition()
    {
        wayPointIndex += 1;
        wayPointIndex %= waypoints.Length;

        currentTarget = waypoints[wayPointIndex];

        Move(currentTarget.position);
    }

    void UnstickAgent()
    {
        if (agent.pathPending) return;

        switch (agent.pathStatus)
        {
            case NavMeshPathStatus.PathComplete:
            break;
            case NavMeshPathStatus.PathPartial:
                Debug.Log("Path Partial!");
                setNextWaypointPosition();
            break; 
            case NavMeshPathStatus.PathInvalid:
                setNextWaypointPosition();
                Debug.Log("Path Invalid!");
            break;
        }
    }

    // ---------- Facing ----------

    protected void FaceMovement()
    {
        Vector3 v = agent.velocity;
        v.y = 0f;
        if (v.sqrMagnitude < 0.01f) return; 
        
        float h = v.x;
        float depth = v.z;

        if (Mathf.Abs(h) > Mathf.Abs(depth))
        {
            //sr.sprite = side;
            //sr.flipX = h < 0f;
        }
        
        //transform.rotation = Quaternion.LookRotation(v) * RightOffset ;
    }

    protected void FaceTarget(Vector3 targetPos, float turnSpeed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir) * RightOffset ;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    // ---------- Gizmos ----------

#if UNITY_EDITOR
    protected void DrawGizmoCircle(Vector3 center, float radius, int segments = 48)
    {
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = step * i;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    protected virtual Color GizmoColor()
    {
        if (CanSeeTarget) return Color.red;
        if (PlayerInRange) return Color.purple;
        return Color.green;
    }

    protected virtual void OnDrawGizmos()
    {
        Color stateColor = GizmoColor();

        Gizmos.color = CanSeeTarget ? Color.red : Color.yellow;
        Vector3 left  = Quaternion.Euler(0, -halfFovAngle, 0) * lookDir;
        Vector3 right = Quaternion.Euler(0,   halfFovAngle, 0) * lookDir;
        Gizmos.DrawRay(transform.position, left  * viewRange);
        Gizmos.DrawRay(transform.position, right * viewRange);

        if (CanSeeTarget)
        {
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }

        UnityEditor.Handles.color = stateColor;
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up,
        left, halfFovAngle * 2f, viewRange);

        Gizmos.color = stateColor;
        DrawGizmoCircle(transform.position, viewRange);
    }
#endif

}
