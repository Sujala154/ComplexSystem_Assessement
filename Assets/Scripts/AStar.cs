using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    public AStar() { }

    public List<Connection> PathfindAStar(Graph aGraph, GameObject start, GameObject end, Heuristic myHeuristic)
    {
        NodeRecord StartRecord = new NodeRecord();
        StartRecord.Node = start;
        StartRecord.Connection = null;
        StartRecord.CostSoFar = 0;
        StartRecord.EstimatedTotalCost = myHeuristic.Estimate(start, end);

        PathfindingList OpenList = new PathfindingList();
        PathfindingList ClosedList = new PathfindingList();

        OpenList.AddNodeRecord(StartRecord);

        NodeRecord CurrentRecord = null;
        List<Connection> Connections;

        while (OpenList.GetSize() > 0)
        {
            CurrentRecord = OpenList.GetSmallestElement();

            if (CurrentRecord.Node.Equals(end)) break;

            Connections = aGraph.GetConnections(CurrentRecord.Node);

            foreach (Connection aConnection in Connections)
            {
                GameObject EndNode = aConnection.ToNode;
                float EndNodeCost = CurrentRecord.CostSoFar + aConnection.Cost;
                NodeRecord EndNodeRecord;
                float EndNodeHeuristic;

                if (ClosedList.Contains(EndNode))
                {
                    EndNodeRecord = ClosedList.Find(EndNode);
                    if (EndNodeRecord.CostSoFar <= EndNodeCost) continue;
                    ClosedList.RemoveNodeRecord(EndNodeRecord);
                    EndNodeHeuristic = EndNodeRecord.EstimatedTotalCost - EndNodeRecord.CostSoFar;
                }
                else if (OpenList.Contains(EndNode))
                {
                    EndNodeRecord = OpenList.Find(EndNode);
                    if (EndNodeRecord.CostSoFar <= EndNodeCost) continue;
                    EndNodeHeuristic = EndNodeRecord.EstimatedTotalCost - EndNodeRecord.CostSoFar;
                }
                else
                {
                    EndNodeRecord = new NodeRecord();
                    EndNodeRecord.Node = EndNode;
                    EndNodeHeuristic = myHeuristic.Estimate(EndNode, end);
                }

                EndNodeRecord.CostSoFar = EndNodeCost;
                EndNodeRecord.Connection = aConnection;
                EndNodeRecord.EstimatedTotalCost = EndNodeCost + EndNodeHeuristic;

                if (!OpenList.Contains(EndNode)) OpenList.AddNodeRecord(EndNodeRecord);
            }

            OpenList.RemoveNodeRecord(CurrentRecord);
            ClosedList.AddNodeRecord(CurrentRecord);
        }

        List<Connection> tempList = new List<Connection>();
        if (CurrentRecord == null || !CurrentRecord.Node.Equals(end)) return tempList;
        else
        {
            while (!CurrentRecord.Node.Equals(start))
            {
                tempList.Add(CurrentRecord.Connection);
                CurrentRecord = ClosedList.Find(CurrentRecord.Connection.FromNode);
            }

            List<Connection> tempList2 = new List<Connection>();
            for (int i = (tempList.Count - 1); i >= 0; i--) tempList2.Add(tempList[i]);
            return tempList2;
        }
    }
}
