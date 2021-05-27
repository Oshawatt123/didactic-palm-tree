using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Room : MonoBehaviour
{
    public enum RoomType
    {
        ROOM,
        CORRIDOR,
    }
    
    public List<Transform> connectionPoints = new List<Transform>();
    private List<int> connectionPointConnected = new List<int>();
    public RoomType roomType;
    private DungeonGeneration generator;
    
    private bool madeRooms = false;
    private bool initialized = false;

    public List<Room> connectedRooms;

    public float collisionRange = 5f;

    [HideInInspector]
    public bool visitedByTrimmer = false;

    public void Init()
    {
        if (!initialized)
        {
            // init connected values
            for (int i = 0; i < connectionPoints.Count; i++)
            {
                connectionPointConnected.Add(0);
            }

            initialized = true;
        }
    }
    
    void Start()
    {
        generator = GameObject.FindGameObjectWithTag("Generator").GetComponent<DungeonGeneration>();
        Init();
    }
    
    void Update()
    {
        if (!madeRooms)
        {
            Debug.Log("@@@@@@@@@@@@ Making " + connectionPoints.Count.ToString() + " connections @@@@@@@@@@@@@");
            for (int i = 0; i < connectionPoints.Count; i++)
            {
                Debug.Log("Connection " + i.ToString());
                // if the connection isnt already taken
                if(connectionPointConnected[i] == 0)
                    connectedRooms.Add(generator.CreateRoom(connectionPoints[i], this));

            }

            generator.ExhaustRoom(this);
            madeRooms = true;
        }
    }

    public Transform GetRandomConnection()
    {
        int index = Random.Range(0, connectionPoints.Count - 1);
        connectionPointConnected[index] = 1;
        return connectionPoints[index];
    }

    public void RemoveSelf()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionRange);
    }
}
