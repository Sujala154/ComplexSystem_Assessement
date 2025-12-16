using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ATTENTION: The class name MUST match the file name (Ambulance1.cs)
public class WorkshopAmbulance1 : MonoBehaviour
{
    [Header("References")]
    public GameObject startNode; // waypoint GameObject
    public GameObject goalNode;  // hospital waypoint
    public GameObject patient;   // in-scene patient object (or parcel)

    [Header("Movement")]
    public float baseSpeed = 8f;        // Base speed for 0 items
    [Range(0, 10)]
    public int numberOfItems = 3;       // Items to carry 
    public float waypointThreshold = 0.5f;
    public float turnSpeed = 5f;
    public float stopDurationAtGoal = 3f;

    [Header("Collaboration")]
    public float yieldSpeedThreshold = 0.5f; // Agent must be X speed units slower to yield
    // CORRECTED: The list must hold the same type as the class name
    private List<WorkshopAmbulance1> nearbyAgents = new List<WorkshopAmbulance1>(); 
    
    // Private variables for pathfinding and tracking
    private float currentMovementSpeed;
    private AStarManager aStarManager = new AStarManager();
    private List<Connection> currentPathConnections;
    private float runStartTime;
    private float runDistance;

    void Start()
    {
        // STEP 1: Calculate the agent's speed based on the load
        CalculateMovementSpeed();
        
        BuildGraphFromVisWaypoints();
        StartCoroutine(RunAmbulanceRoutine());
    }

    // NEW METHOD: Calculates and sets the agent's actual speed based on load
    private void CalculateMovementSpeed()
    {
        // Speed drops 10% per item, max 90% (10 items)
        float reductionFactor = 1.0f - (numberOfItems * 0.1f);

        // Clamp the factor to ensure it doesn't go below 0.1 (10% speed, 90% drop)
        reductionFactor = Mathf.Clamp(reductionFactor, 0.1f, 1.0f);

        currentMovementSpeed = baseSpeed * reductionFactor;
        Debug.Log($"{gameObject.name} loaded {numberOfItems} items. Speed: {currentMovementSpeed:F2}");
    }

    void BuildGraphFromVisWaypoints()
    {
        GameObject[] GameObjectsWithWaypointTag = GameObject.FindGameObjectsWithTag("Waypoint");
        List<GameObject> Waypoints = new List<GameObject>();

        foreach (GameObject waypoint in GameObjectsWithWaypointTag)
        {
            // Assuming VisGraphWaypointManager and its contents (Connection, AStarManager) exist in your project
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
            // Updated Debug message to use the current class name
            Debug.LogError("Start or Goal not assigned to WorkshopAmbulance1.");
            yield break;
        }

        // compute path start->goal
        currentPathConnections = aStarManager.PathfindAStar(startNode, goalNode);
        if (currentPathConnections == null || currentPathConnections.Count == 0)
        {
            Debug.LogError("No path found to goal.");
            yield break;
        }

        List<GameObject> pathNodes = ConnectionsToNodeList(currentPathConnections, startNode);
        transform.position = startNode.transform.position;
        runStartTime = Time.time;
        runDistance = 0f;

        // Simulate pickup/loading
        if (patient != null)
        {
            yield return new WaitForSeconds(stopDurationAtGoal / 2f); 
            patient.transform.SetParent(transform);
            patient.transform.localPosition = new Vector3(0, 0.5f, -1f); 
            patient.transform.localRotation = Quaternion.identity;
        }

        // follow path to goal
        yield return StartCoroutine(FollowNodes(pathNodes));

        Debug.Log("Reached goal!");

        // Simulate drop off
        if (patient != null)
        {
            patient.transform.SetParent(null); 
            Vector3 dropPosition = transform.position + transform.right * -2.1f;
            dropPosition.y = 0.8f; 
            patient.transform.position = dropPosition;
        }

        yield return new WaitForSeconds(stopDurationAtGoal); 

        // return path
        var returnConnections = aStarManager.PathfindAStar(goalNode, startNode);
        if (returnConnections == null || returnConnections.Count == 0)
        {
            Debug.LogError("No path found to return.");
            yield break;
        }

        List<GameObject> returnNodes = ConnectionsToNodeList(returnConnections, goalNode);
        yield return StartCoroutine(FollowNodes(returnNodes));

        // PERFORMANCE MEASUREMENT OUTPUT (Required)
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

    // Core movement coroutine, now includes collaboration logic
    private IEnumerator FollowNodes(List<GameObject> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 targetPos = nodes[i].transform.position;
            while (Vector3.Distance(transform.position, targetPos) > waypointThreshold)
            {
                // STEP 4: Check collaboration before moving
                if (ShouldYield())
                {
                    // Outward Behaviour: Stop movement (yield/wait)
                    yield return null; 
                    continue; // Skip the movement and check again next frame
                }
                
                // If not yielding, proceed with movement:
                Vector3 prevPos = transform.position;
                Vector3 direction = (targetPos - transform.position).normalized;
                
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }

                // Use the load-affected currentMovementSpeed
                transform.position = Vector3.MoveTowards(transform.position, targetPos, currentMovementSpeed * Time.deltaTime);
                
                // PERFORMANCE MEASUREMENT: Distance tracking
                runDistance += Vector3.Distance(transform.position, prevPos);
                
                yield return null;
            }
        }
    }

    // NEW METHOD: Determines if this agent should yield to another
    private bool ShouldYield()
    {
        // Remove null references in case an agent was destroyed
        nearbyAgents.RemoveAll(agent => agent == null);

        if (nearbyAgents.Count == 0) return false;

        // Right-of-Way Logic: Check if any nearby agent is significantly faster
        // CORRECTED: iterating over WorkshopAmbulance1 type
        foreach (WorkshopAmbulance1 otherAgent in nearbyAgents)
        {
            // Use a threshold to prevent jittering (e.g., must be 0.5f faster)
            if (otherAgent.currentMovementSpeed > (this.currentMovementSpeed + yieldSpeedThreshold))
            {
                // Output information about collaboration (Required)
                Debug.Log($"COLLAB: {gameObject.name} (Slow: {currentMovementSpeed:F2}) yields at node to {otherAgent.name} (Fast: {otherAgent.currentMovementSpeed:F2}).");
                return true; // Yes, we must yield
            }
        }
        return false; // No reason to yield, keep moving
    }
    
    // STEP 3: Unity Trigger functions for proximity detection
    void OnTriggerEnter(Collider other)
    {
        // CORRECTED: Getting the WorkshopAmbulance1 component
        WorkshopAmbulance1 otherAgent = other.GetComponent<WorkshopAmbulance1>(); 

        // Check if it's a valid agent, not ourselves, and not already tracked
        if (otherAgent != null && otherAgent != this && !nearbyAgents.Contains(otherAgent))
        {
            nearbyAgents.Add(otherAgent);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // CORRECTED: Getting the WorkshopAmbulance1 component
        WorkshopAmbulance1 otherAgent = other.GetComponent<WorkshopAmbulance1>();
        if (otherAgent != null)
        {
            nearbyAgents.Remove(otherAgent);
        }
    }
}