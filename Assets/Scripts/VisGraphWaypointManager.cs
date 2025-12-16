using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VisGraphWaypointManager : MonoBehaviour
{
    [SerializeField] private enum waypointTextColour { Blue, Cyan, Yellow };
#pragma warning disable
    [SerializeField] private waypointTextColour WaypointTextColour = waypointTextColour.Blue;
#pragma warning restore

    [SerializeField] public List<VisGraphConnection> connections = new List<VisGraphConnection>();
    public List<VisGraphConnection> Connections { get { return connections; } }

    public enum waypointPropsList { Standard, Start, Goal };
#pragma warning disable
    [SerializeField] private waypointPropsList waypointType = waypointPropsList.Standard;
#pragma warning restore
    public waypointPropsList WaypointType { get { return waypointType; } }

    private const bool displayType = false;
    private bool ObjectSelected = false;
    private const bool displayText = true;
    private string infoText = "";
    private Color infoTextColor;

    void OnDrawGizmos()
    {
        infoText = "";
        if (displayType)
        {
            #pragma warning disable
            infoText = "Type: " + WaypointType.ToString() + " / ";
            #pragma warning restore
        }
        infoText += gameObject.name + "\n Connections: " + Connections.Count;

        switch (WaypointTextColour)
        {
            case waypointTextColour.Blue: infoTextColor = Color.blue; break;
            case waypointTextColour.Cyan: infoTextColor = Color.cyan; break;
            case waypointTextColour.Yellow: infoTextColor = Color.yellow; break;
        }

        DrawWaypointAndConnections(ObjectSelected);

        if (displayText)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = infoTextColor;
            Handles.Label(transform.position + Vector3.up * 1, infoText, style);
        }
        ObjectSelected = false;
    }

    void OnDrawGizmosSelected() { ObjectSelected = true; }

    private void DrawWaypointAndConnections(bool ObjectSelected)
    {
        Color WaypointColor = Color.yellow;
        Color ArrowHeadColor = Color.blue;
        if (ObjectSelected)
        {
            WaypointColor = Color.red;
            ArrowHeadColor = Color.magenta;
        }

        Gizmos.color = WaypointColor;
        Gizmos.DrawSphere(transform.position, 0.2f);

        for (int i = 0; i < Connections.Count; i++)
        {
            if (Connections[i].ToNode != null)
            {
                if (Connections[i].ToNode.Equals(gameObject))
                {
                    infoText = "WARNING - Connection to SELF at element: " + i;
                    infoTextColor = Color.red;
                }

                Vector3 direction = Connections[i].ToNode.transform.position - transform.position;
                DrawConnection(i, transform.position, direction, ArrowHeadColor);

                if (ObjectSelected)
                {
                    Gizmos.color = ArrowHeadColor;
                    float dist = direction.magnitude;
                    float pos = dist * 0.1f;
                    Gizmos.DrawSphere(transform.position + (direction.normalized * pos), 0.3f);
                    pos = dist * 0.2f;
                    Gizmos.DrawSphere(transform.position + (direction.normalized * pos), 0.3f);
                    pos = dist * 0.3f;
                    Gizmos.DrawSphere(transform.position + (direction.normalized * pos), 0.3f);
                }
            }
            else
            {
                infoText = "WARNING - Connection is missing at element: " + i;
                infoTextColor = Color.red;
            }
        }
    }

    public void DrawConnection(float ConnectionsIndex, Vector3 pos, Vector3 direction,
        Color ArrowHeadColor, float arrowHeadLength = 0.5f, float arrowHeadAngle = 40.0f)
    {
        Debug.DrawRay(pos, direction, Color.blue);

        Vector3 right = Quaternion.LookRotation(direction) *
            Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);

        Vector3 left = Quaternion.LookRotation(direction) *
            Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);

        Debug.DrawRay(pos + direction.normalized +
            (direction.normalized * (0.1f * ConnectionsIndex)),
            right * arrowHeadLength, ArrowHeadColor);
        Debug.DrawRay(pos + direction.normalized +
            (direction.normalized * (0.1f * ConnectionsIndex)),
            left * arrowHeadLength, ArrowHeadColor);
    }
}

