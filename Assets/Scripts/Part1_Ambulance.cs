using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkshopAmbulance : MonoBehaviour
{
    [Header("References")]
    public GameObject startNode; // waypoint GameObject with VisGraphWaypointManager
    public GameObject goalNode;  // hospital waypoint
    public GameObject patient;   // in-scene patient object to deliver

    [Header("Movement")]
    public float speed = 8f;
    public float waypointThreshold = 0.5f;
    public float turnSpeed = 5f;
    public float stopDurationAtGoal = 3f;

    private AStarManager aStarManager = new AStarManager();
    private List<Connection> currentPathConnections;
    private float runStartTime;
    private float runDistance;

    void Start()
    {
        BuildGraphFromVisWaypoints();
        StartCoroutine(RunAmbulanceRoutine());
    }

    void BuildGraphFromVisWaypoints()
    {
        GameObject[] GameObjectsWithWaypointTag = GameObject.FindGameObjectsWithTag("Waypoint");
        List<GameObject> Waypoints = new List<GameObject>();

        foreach (GameObject waypoint in GameObjectsWithWaypointTag)
        {
            VisGraphWaypointManager tmp = waypoint.GetComponent<VisGraphWaypointManager>();
            if (tmp != null) Waypoints.Add(waypoint);
        }

        // Build graph connections in AStarManager
        foreach (GameObject wp in Waypoints)
        {
            VisGraphWaypointManager tmp = wp.GetComponent<VisGraphWaypointManager>();
            if (tmp == null) continue;
            foreach (VisGraphConnection vconn in tmp.Connections)
            {
                if (vconn.ToNode != null)
                {
                    Connection conn = new Connection();
                    conn.FromNode = wp;
                    conn.ToNode = vconn.ToNode;
                    aStarManager.AddConnection(conn);
                }
            }
        }
    }

    IEnumerator RunAmbulanceRoutine()
    {
        if (startNode == null || goalNode == null)
        {
            Debug.LogError("Start or Goal not assigned to WorkshopAmbulance.");
            yield break;
        }

        // compute path start->goal
        currentPathConnections = aStarManager.PathfindAStar(startNode, goalNode);
        if (currentPathConnections == null || currentPathConnections.Count == 0)
        {
            Debug.LogError("No path found to goal.");
            yield break;
        }

        // convert to node list
        List<GameObject> pathNodes = ConnectionsToNodeList(currentPathConnections, startNode);

        // place ambulance at start
        transform.position = startNode.transform.position;
        runStartTime = Time.time;
        runDistance = 0f;

        // =========================
        // PICK UP PATIENT (half stop duration)
        if (patient != null)
        {
            yield return new WaitForSeconds(stopDurationAtGoal / 2f); // simulate pickup
            patient.transform.SetParent(transform); // attach to ambulance
            patient.transform.localPosition = new Vector3(0, 0.5f, -1f); // adjust position
            patient.transform.localRotation = Quaternion.identity;
        }
        // =========================

        // follow path
        yield return StartCoroutine(FollowNodes(pathNodes));

        // reached hospital
        Debug.Log("Reached hospital!");
        

        // =========================
        // DROP PATIENT at goal node
        if (patient != null)
        {
            patient.transform.SetParent(null); // detach from ambulance
            Vector3 dropPosition = transform.position + transform.right * -2.1f;
            dropPosition.y = 0.8f; // ensure on ground
            patient.transform.position = dropPosition;
        }
        // =========================

        yield return new WaitForSeconds(stopDurationAtGoal); // ambulance already stops

        // return path
        var returnConnections = aStarManager.PathfindAStar(goalNode, startNode);
        if (returnConnections == null || returnConnections.Count == 0)
        {
            Debug.LogError("No path found to return.");
            yield break;
        }

        List<GameObject> returnNodes = ConnectionsToNodeList(returnConnections, goalNode);
        yield return StartCoroutine(FollowNodes(returnNodes));

        float totalTime = Time.time - runStartTime;
        Debug.Log($"Run complete! Time: {totalTime:F2}s, Distance: {runDistance:F2} units.");
        Debug.Log("Agent Stopped.");
    }

    private List<GameObject> ConnectionsToNodeList(List<Connection> conns, GameObject start)
    {
        List<GameObject> nodes = new List<GameObject>();
        nodes.Add(start);
        foreach (Connection c in conns)
        {
            nodes.Add(c.ToNode);
        }
        return nodes;
    }

    private IEnumerator FollowNodes(List<GameObject> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 targetPos = nodes[i].transform.position;
            while (Vector3.Distance(transform.position, targetPos) > waypointThreshold)
            {
                Vector3 prevPos = transform.position;
                Vector3 direction = (targetPos - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                runDistance += Vector3.Distance(transform.position, prevPos);
                yield return null;
            }
        }
    }
}
