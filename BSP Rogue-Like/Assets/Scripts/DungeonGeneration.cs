using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGeneration : MonoBehaviour
{
    private int roomsCreated = 0;
    private int itemsExhausted = 0;
    public int maxItems = 1;

    public List<GameObject> rooms = new List<GameObject>();
    public List<GameObject> corridors = new List<GameObject>();

    struct deletionMemory
    {
        public deletionMemory(Room toDelete, Room parent)
        {
            roomToDelete = toDelete;
            parentRoom = parent;
        }
        
        public Room roomToDelete;
        public Room parentRoom;
    }
    private List<deletionMemory> deletionList = new List<deletionMemory>();
    
    [SerializeField][Range(0.0f, 1.0f)]
    private float corridorRoomChance = 0.5f;

    [SerializeField][Range(0.0f, 10.0f)]
    private float collisionRange = 2.0f;

    [SerializeField]
    private LayerMask collisionLayer;
    
    private Room rootNode;
    private bool gotRoot = false;

    private enum DebugLevel
    {
        All,
        
    }
    public Room CreateRoom(Transform LinkPoint, Room spawner)
    {
        // get rootNode
        if (!gotRoot)
        {
            rootNode = spawner;
            gotRoot = true;
        }

        Debug.Log("####### Room " + roomsCreated.ToString() + " ##########");
        if (!(roomsCreated >= maxItems-1))
        {
            // select what room to spawn
            GameObject roomToSpawn;
            if (spawner.roomType == Room.RoomType.CORRIDOR)
            {
                float rand = Random.Range(0.0f, 1.0f);
                
                if(rand < corridorRoomChance)
                    roomToSpawn = rooms[Random.Range(0, rooms.Count)];
                else
                    roomToSpawn = corridors[Random.Range(0, corridors.Count)];
                
            }
            else//(spawner.roomType == Room.RoomType.ROOM)
            {
                roomToSpawn = corridors[Random.Range(0, corridors.Count)];
            }
            
            // spawn the room
            GameObject spawnedRoom = Instantiate(roomToSpawn);
            Room spawnedRoomComponent = spawnedRoom.GetComponent<Room>();
            spawnedRoomComponent.Init();
            spawnedRoomComponent.connectedRooms.Add(spawner);
            
            // get a random connection point from it
            Transform connectionPointToLink = spawnedRoomComponent.GetRandomConnection();
            
            // align new room with link point
            // we need to rotate the object so that the new link point aligns with the old link point
            Vector3 eulerRot = LinkPoint.rotation.eulerAngles - connectionPointToLink.rotation.eulerAngles;
            eulerRot.y -= 180;
            
            spawnedRoom.GetComponent<Transform>().Rotate(eulerRot, Space.World);
            
            // this offset is a but funny but we'll see
            Debug.Log("Position before moving: " + connectionPointToLink.position.ToString());
            
            // calculate vector from the connection point on the new room to the connection point on the old room
            Vector3 connectionOffset = LinkPoint.position - connectionPointToLink.position;
            
            // move the room by the vector
            spawnedRoom.GetComponent<Transform>().position += connectionOffset;
            
            Debug.Log("Position after moving: " + connectionPointToLink.position.ToString());

            // check the room fits where we want it to
            RaycastHit[] hitInfo;
            hitInfo = Physics.SphereCastAll(spawnedRoom.transform.position, collisionRange, Vector3.forward,
                collisionRange, collisionLayer);

            // Remove the room if it collides with something
            // the room is removed here instead of being marked for deletion because
            // we don't want this room to spawn any others and create floating rooms
            // we check for a length of more than 1
            if (hitInfo.Length > 1)
            {
                Debug.Log("Room collided with another room. Destroying room...");
                spawnedRoomComponent.RemoveSelf();
            }
            else
            {
                roomsCreated++;

                return spawnedRoomComponent;
            }
        }
        else
        {
            Debug.Log("No more available rooms, sorry bro");
        }

        return null;
    }

    public void ExhaustRoom(Room room)
    {
        itemsExhausted++;
        Debug.Log("Exhausted " + itemsExhausted.ToString() + " items out of " + maxItems.ToString());

        if (itemsExhausted >= maxItems)
        {
            Debug.Log("[PROCESS] Finding dead ends");
            FindDeadEnds(rootNode);
            
            Debug.Log("[PROCESS] Trimming dead ends");
            TrimDeadEnds();
        }
    }


    // WARNING: RECURSIVE
    private void FindDeadEnds(Room currNode)
    {
        // BASE CASE
        // am I a dead end?
        bool needToRecurse = true;
        if (currNode.roomType == Room.RoomType.CORRIDOR)
        {
            Debug.Log("Corridor found");
            // check each connected room. if there is a connection missing, we know this is a dead end.
            int nullCount = 0;
            foreach (Room node in currNode.connectedRooms)
            {
                if (node == null)
                    nullCount++;
            }
            
            Debug.Log("Number of nulls in corridor: " + nullCount);

            // if nullcount is 1, we have a dead end. If it is over 1, somehow this corridor is not connected
            // to anything and it should probably be removed anyway
            if (nullCount >= 1)
            {
                Debug.Log("Dead end found");

                // recurse back until we find a room
                BackTrackToRoom(currNode);
                
                // end this recursion
                needToRecurse = false;
            }
        }
        
        // RECURSION
        if (needToRecurse)
        {
            foreach (Room node in currNode.connectedRooms)
            {
                Debug.Log("Starting recursion...");
                // make sure node is not null, as there is every possibility it could be
                if (node != null)
                {
                    if (!node.visitedByTrimmer)
                    {
                        Debug.Log("Going deeper...");
                        node.visitedByTrimmer = true;
                        FindDeadEnds(node);
                    }
                }
            }
        }
    }

    private void BackTrackToRoom(Room currNode)
    {
        Debug.Log("Back tracking to room...");
        Room prevNode = currNode.connectedRooms[0];
        while (currNode.roomType == Room.RoomType.CORRIDOR)
        {
            // mark it for deletion
            MarkForDeletion(new deletionMemory(currNode, prevNode));

            Debug.Log("[DELETION] Corridor marked for deletion");
            currNode = prevNode;
            
            // 1st connected room will be the one closer to the start of the 
            prevNode = currNode.connectedRooms[0];
        }
        
        Debug.Log("[SUCCESS] Room found. Back track ended");
    }

    private void TrimDeadEnds()
    {
        foreach (deletionMemory DM in deletionList)
        {
            Debug.Log("Deleting item...");
            // remove link to the room/corridor before it
            if(DM.parentRoom != null)
                DM.parentRoom.connectedRooms.Remove(DM.roomToDelete);
            
            // remove this corridor
            DM.roomToDelete.RemoveSelf();
        }
    }

    private void MarkForDeletion(deletionMemory roomToDelete)
    {
        deletionList.Add(roomToDelete);
    }
}