using System.Collections;
using System.Collections.Generic;

public class Node
{
    Node()
    {
        connectedRooms = new List<Node>();
    }

    public List<Node> connectedRooms;
}