using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VisGraphConnection
{
    [SerializeField]
    private GameObject toNode;
    public GameObject ToNode { get { return toNode; } }
}
