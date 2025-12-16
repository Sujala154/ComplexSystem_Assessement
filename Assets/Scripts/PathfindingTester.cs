using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingTester : MonoBehaviour
{
    // The A* manager.
    private AStarManager AStarManager = new AStarManager();

    // List of possible waypoints.
    private List<GameObject> Waypoints = new List<GameObject>();

    // List of waypoint map connections. Represents a path.
    private List<Connection> AStarPath = new List<Connection>();

    // The start and end nodes.
    [SerializeField] private GameObject start;
    [SerializeField] private GameObject end;

    // Debug line offset.
    Vector3 OffSet = new Vector3(0, 0.3f, 0);

    // A list of all waypoint nodes set to goal in the environment.
    private List<GameObject> WaypointGoals = new List<GameObject>();

    // Movement variables.
    private float currentSpeed = 8;
    private int currentTargetArrayIndex = 0;
    private Vector3 currentTargetPos;
    private bool agentMove = true;

    // Route Timer.
    private float timer = 0;

    // Distance Calculator.
    private float totalDistance = 0;
    private Vector3 lastPosition = new Vector3();

    // Start is called before the first frame update
    void Start()
    {
        if (start == null || end == null)
        {
            Debug.Log("No start or end waypoints.");
            return;
        }

        VisGraphWaypointManager tmpWpM = start.GetComponent<VisGraphWaypointManager>();
        if (tmpWpM == null)
        {
            Debug.Log("Start is not a waypoint.");
            return;
        }

        tmpWpM = end.GetComponent<VisGraphWaypointManager>();
        if (tmpWpM == null)
        {
            Debug.Log("End is not a waypoint.");
            return;
        }

        // Find all the waypoints in the level.
        GameObject[] GameObjectsWithWaypointTag = GameObject.FindGameObjectsWithTag("Waypoint");

        foreach (GameObject waypoint in GameObjectsWithWaypointTag)
        {
            VisGraphWaypointManager tmpWaypointMan = waypoint.GetComponent<VisGraphWaypointManager>();
            if (tmpWaypointMan)
            {
                Waypoints.Add(waypoint);
            }
        }

        // Go through the waypoints and create connections.
        foreach (GameObject waypoint in Waypoints)
        {
            VisGraphWaypointManager tmpWaypointMan = waypoint.GetComponent<VisGraphWaypointManager>();

            if (tmpWaypointMan.WaypointType == VisGraphWaypointManager.waypointPropsList.Goal)
            {
                WaypointGoals.Add(waypoint);
            }

            // Loop through a waypoint's connections.
            foreach (VisGraphConnection aVisGraphConnection in tmpWaypointMan.Connections)
            {
                if (aVisGraphConnection.ToNode != null)
                {
                    Connection aConnection = new Connection();
                    aConnection.FromNode = waypoint;
                    aConnection.ToNode = aVisGraphConnection.ToNode;
                    AStarManager.AddConnection(aConnection);
                }
                else
                {
                    Debug.Log("Warning, " + waypoint.name +
                              " has a missing to node for a connection!");
                }
            }
        }

        // Run A*...
        // AStarPath stores all the connections in the path/route to the goal/end node.
        AStarPath = AStarManager.PathfindAStar(start, end);

        if (AStarPath.Count == 0)
        {
            Debug.Log("Warning, A* did not return a path between the start and end node.");
        }

        lastPosition = transform.position;
    }

    // Draws debug objects in the editor and during editor play (if option set).
    void OnDrawGizmos()
    {
        // Draw path.
        if (AStarPath == null)
            return;

        foreach (Connection aConnection in AStarPath)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                (aConnection.FromNode.transform.position + OffSet),
                (aConnection.ToNode.transform.position + OffSet)
            );
        }
    }

    // Update is called once per frame
void Update()
{
    if (agentMove)
    {
        // Timer and distance.
        Vector3 tmpDir = lastPosition - transform.position;
        float tmpDistance = tmpDir.magnitude;
        totalDistance += tmpDistance;
        // Debug.Log("distance: " + totalDistance);
        lastPosition = transform.position;
        timer += Time.deltaTime;

        // Set the current target.
        currentTargetPos = AStarPath[currentTargetArrayIndex].ToNode.transform.position;

        // Clear y to avoid up/down movement. Assumes flat surface.
        currentTargetPos.y = transform.position.y;

        // Get a vector to the target position.
        Vector3 direction = currentTargetPos - transform.position;

        // Calculate the length of the relative position vector
        float distance = direction.magnitude;

        // Face in the right direction.
        direction.y = 0;
        if (direction.magnitude > 0)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = rotation;
        }

        // Calculate the normalized direction to the target from the game object.
        Vector3 normDirection = direction / distance;

        // Move the game object.
        transform.position += normDirection * currentSpeed * Time.deltaTime;

        // Check if close to current target.
        if (distance < 1f)
        {
            // Close to target, so move to the next target in the list (if there is one).
            currentTargetArrayIndex++;

            if (currentTargetArrayIndex == AStarPath.Count)
            {
                // The A* agent has reached the goal location.

                // Output timer and distance information.
                Debug.Log("Time: " + timer);
                Debug.Log("Distance: " + totalDistance);

                // Reset distance and timer for the next trip.
                totalDistance = 0;
                timer = 0;

                // Check if the current target is the start node. If yes, then stop.
                if (AStarPath[currentTargetArrayIndex - 1].ToNode.Equals(start))
                {
                    agentMove = false;
                    Debug.Log("Agent Stopped.");
                    return;
                }

                // Not back at start, so plan path back to the start.
                AStarPath = AStarManager.PathfindAStar(end, start);
                currentTargetArrayIndex = 0;
            }
        }
    }
    else
    {
        // This code runs if agentMove is false.
    }
}

}
