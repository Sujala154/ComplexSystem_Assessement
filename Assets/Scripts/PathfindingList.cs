using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingList
{
    private List<NodeRecord> NodeRecordList = new List<NodeRecord>();

    public PathfindingList() { }

    public void AddNodeRecord(NodeRecord NodeRecord) { NodeRecordList.Add(NodeRecord); }
    public void RemoveNodeRecord(NodeRecord NodeRecord) { NodeRecordList.Remove(NodeRecord); }
    public int GetSize() { return NodeRecordList.Count; }

    public NodeRecord GetSmallestElement()
    {
        NodeRecord TmpNodeRecord = new NodeRecord();
        TmpNodeRecord.EstimatedTotalCost = float.MaxValue;
        foreach (NodeRecord NodeRecord in NodeRecordList)
        {
            if (NodeRecord.EstimatedTotalCost < TmpNodeRecord.EstimatedTotalCost)
            {
                TmpNodeRecord = NodeRecord;
            }
        }
        return TmpNodeRecord;
    }

    public bool Contains(UnityEngine.GameObject Node)
    {
        foreach (NodeRecord NodeRecord in NodeRecordList)
        {
            if (NodeRecord.Node.Equals(Node)) return true;
        }
        return false;
    }

    public NodeRecord Find(UnityEngine.GameObject Node)
    {
        foreach (NodeRecord NodeRecord in NodeRecordList)
        {
            if (NodeRecord.Node.Equals(Node)) return NodeRecord;
        }
        return null;
    }
}

