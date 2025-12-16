using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarManager : MonoBehaviour
{
    private AStar AStar = new AStar();
    private Graph aGraph = new Graph();
    private Heuristic aHeuristic = new Heuristic();

    public AStarManager() { }

    public void AddConnection(Connection connection) { aGraph.AddConnection(connection); }
    public List<Connection> PathfindAStar(GameObject start, GameObject end) { return AStar.PathfindAStar(aGraph, start, end, aHeuristic); }
}

