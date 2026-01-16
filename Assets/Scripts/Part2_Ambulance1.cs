using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkshopAmbulance1 : MonoBehaviour
{
    [Header("References")]
    public GameObject startNode;
    public GameObject goalNode;

    [Header("Movement")]
    public float baseSpeed = 8f;
    [Range(0, 10)]
    public int numberOfItems = 3;
    public float waypointThreshold = 0.5f;
    public float turnSpeed = 5f;
    public float stopDurationAtGoal = 3f;

    [Header("Global Speeds")]
    public float maxReturnSpeed = 12f; // SAME for all ambulances

    [Header("Status for UI")]
    public float currentSpeed;
    public int itemsCarried;
    public float totalDistance;
    public string deliveryStatus;

    [Header("Collision Avoidance (Minimal)")]
    public float safeDistance = 3f;
    public float slowDownFactor = 0.35f;
    public float sideMoveSpeed = 2f;

    [Header("Patients")]
    public List<Transform> patientSlots = new List<Transform>();
    public List<GameObject> patientsToDeliver = new List<GameObject>();

    private List<WorkshopAmbulance1> nearbyAgents = new List<WorkshopAmbulance1>();

    private float currentMovementSpeed;
    private float targetSpeed;
    private float nominalMovementSpeed;

    private AStarManager aStarManager = new AStarManager();
    private List<Connection> currentPathConnections;

    private float runStartTime;
    private float runDistance;

    void Start()
    {
        deliveryStatus = "In Progress";
        totalDistance = 0f;

        CalculateMovementSpeed();
        BuildGraphFromVisWaypoints();
        StartCoroutine(RunAmbulanceRoutine());
    }

    private void CalculateMovementSpeed()
    {
        float reductionFactor = 1.0f - (numberOfItems * 0.1f);
        reductionFactor = Mathf.Clamp(reductionFactor, 0.1f, 1.0f);

        nominalMovementSpeed = baseSpeed * reductionFactor;
        currentMovementSpeed = nominalMovementSpeed;
        targetSpeed = nominalMovementSpeed;
    }

    void BuildGraphFromVisWaypoints()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Waypoint");

        foreach (GameObject wp in objs)
        {
            VisGraphWaypointManager mgr = wp.GetComponent<VisGraphWaypointManager>();
            if (mgr == null) continue;

            foreach (VisGraphConnection vconn in mgr.Connections)
            {
                if (vconn.ToNode != null)
                {
                    Connection conn = new Connection
                    {
                        FromNode = wp,
                        ToNode = vconn.ToNode
                    };
                    aStarManager.AddConnection(conn);
                }
            }
        }
    }

    IEnumerator RunAmbulanceRoutine()
    {
        AttachAllPatients();

        currentPathConnections = aStarManager.PathfindAStar(startNode, goalNode);
        List<GameObject> pathNodes = ConnectionsToNodeList(currentPathConnections, startNode);

        transform.position = startNode.transform.position;
        runStartTime = Time.time;
        runDistance = 0f;

        yield return StartCoroutine(FollowNodes(pathNodes));

        // Reached hospital
        currentSpeed = 0f;
        DropAllPatients();
        deliveryStatus = "Delivered";

        yield return new WaitForSeconds(stopDurationAtGoal);

        // RETURN JOURNEY (ALL SAME SPEED)
        nominalMovementSpeed = maxReturnSpeed;
        currentMovementSpeed = nominalMovementSpeed;
        targetSpeed = nominalMovementSpeed;

        var returnConnections = aStarManager.PathfindAStar(goalNode, startNode);
        List<GameObject> returnNodes = ConnectionsToNodeList(returnConnections, goalNode);

        yield return StartCoroutine(FollowNodes(returnNodes));

        transform.position = startNode.transform.position;
        currentSpeed = 0f;
    }

    private void AttachAllPatients()
    {
        deliveryStatus = "In Progress";

        int count = Mathf.Min(patientsToDeliver.Count, patientSlots.Count);

        for (int i = 0; i < count; i++)
        {
            GameObject p = patientsToDeliver[i];
            Transform slot = patientSlots[i];

            if (p == null || slot == null) continue;

            p.transform.SetParent(slot);
            p.transform.localPosition = Vector3.zero;
            p.transform.localRotation = Quaternion.identity;

            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void DropAllPatients()
    {
        foreach (GameObject p in patientsToDeliver)
        {
            if (p == null) continue;

            p.transform.SetParent(null);

            Vector3 dropPos =
                transform.position
                + transform.right * Random.Range(1f, 2f)
                - transform.forward * Random.Range(0.5f, 1.5f);

            p.transform.position = dropPos;
            p.transform.rotation = Quaternion.identity;

            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        itemsCarried = 0;
    }

    private List<GameObject> ConnectionsToNodeList(List<Connection> conns, GameObject start)
    {
        List<GameObject> nodes = new List<GameObject> { start };
        foreach (Connection c in conns)
            nodes.Add(c.ToNode);
        return nodes;
    }

    private IEnumerator FollowNodes(List<GameObject> nodes)
    {
        foreach (GameObject node in nodes)
        {
            Vector3 targetPos = node.transform.position;

            while (Vector3.Distance(transform.position, targetPos) > waypointThreshold)
            {
                HandleCollisionAvoidance();

                currentMovementSpeed = Mathf.Lerp(
                    currentMovementSpeed,
                    targetSpeed,
                    Time.deltaTime * 3f
                );

                Vector3 prevPos = transform.position;

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    currentMovementSpeed * Time.deltaTime
                );

                float moved = Vector3.Distance(transform.position, prevPos);
                totalDistance += moved;
                runDistance += moved;

                currentSpeed = moved > 0.001f ? currentMovementSpeed : 0f;

                itemsCarried = 0;
                foreach (Transform slot in patientSlots)
                {
                    if (slot.childCount > 0)
                        itemsCarried++;
                }

                Vector3 direction = (targetPos - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        turnSpeed * Time.deltaTime
                    );
                }

                yield return null;
            }
        }
    }

    private void HandleCollisionAvoidance()
    {
        nearbyAgents.RemoveAll(a => a == null);

        bool tooClose = false;
        Vector3 pushDir = Vector3.zero;

        foreach (WorkshopAmbulance1 other in nearbyAgents)
        {
            float dist = Vector3.Distance(transform.position, other.transform.position);

            if (dist < safeDistance)
            {
                tooClose = true;
                pushDir += (transform.position - other.transform.position).normalized;
            }
        }

        if (tooClose)
        {
            targetSpeed = nominalMovementSpeed * slowDownFactor;
            transform.position += pushDir.normalized * sideMoveSpeed * Time.deltaTime;
        }
        else
        {
            targetSpeed = nominalMovementSpeed;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        WorkshopAmbulance1 agent = other.GetComponent<WorkshopAmbulance1>();
        if (agent != null && agent != this && !nearbyAgents.Contains(agent))
            nearbyAgents.Add(agent);
    }

    void OnTriggerExit(Collider other)
    {
        WorkshopAmbulance1 agent = other.GetComponent<WorkshopAmbulance1>();
        if (agent != null)
            nearbyAgents.Remove(agent);
    }
}




