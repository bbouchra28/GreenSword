using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneIA : MonoBehaviour {

    public struct Message
    {
        public Vector3 position;
        public string tag;
    }

    DroneMovementScript controller;
    Wifi wifi;
    LidarRotative lidarRotative;
    Stack <Vector3> path;
    List<Message> targets;
    // Use this for initialization
    void Start () {
        controller = GetComponent<DroneMovementScript>();
        wifi = GetComponent<Wifi>();
        lidarRotative = GetComponent<LidarRotative>();
        /*path = squareSpiral((int)this.transform.position.x, (int)this.transform.position.z);
        foreach(Vector3 v in path)
        {
            Debug.Log(v);
        }*/
        targets = TripleToMessage(lidarRotative.GetData());
    }

    // Update is called once per frame
    void FixedUpdate ()
    {
        /*if (path.Count > 0)
        {
            Vector3 position = path.Pop();
            if (this.transform.position != position)
            {
                Debug.Log("Position = " + position);
                controller.GoToPoint(position);
            }
        }*/
        controller.GoToPoint(new Vector3(50, 5, 50));
        List<byte[]> messageToSend = new List<byte[]>();
        byte[] message;
        foreach (Message m in targets)
        {
            message = DataToByte(m.position, m.tag);
            messageToSend.Add(message);
        }

        List<GameObject> robots = wifi.getRobotAround();
        foreach (GameObject g in robots)
        {
            Debug.Log("I am " + this.tag + " rover, I detect " + g.tag + " rover");
            foreach (byte[] m in messageToSend)
            {
                wifi.Send(g, m);
                Debug.Log("I will send him a target pos " + ByteToData(m).position);
            }
        }

        byte[] messageReceived;
        if (wifi.isWaiting())
        {
            messageReceived = wifi.getMessage();
            Message m = ByteToData(messageReceived);
            if (m.tag == this.tag)
            {
                Debug.Log("I am " + this.tag + ", Another robot send me this message " + m.position);
            }
        }
	}

    public Stack<Vector3> squareSpiral(int X, int Y)
    {
        Stack<Vector3> myStack = new Stack<Vector3>();
        // Generate an Ulam spiral centered at (0, 0).
        int x = 0;
        int y = 0;

        int end = Mathf.Max(X, Y) * Mathf.Max(X, Y);
        for (int i = 0; i < end; ++i)
        {
            // Translate coordinates and mask them out.
            int xp = x + X / 2;
            int yp = y + Y / 2;
            if (xp >= 0 && xp < X && yp >= 0 && yp < Y)

            // No need to track (dx, dy) as the other examples do:
            if (Mathf.Abs(x) <= Mathf.Abs(y) && (x != y || x >= 0))
                x += ((y >= 0) ? 1 : -1);
            else
                y += ((x >= 0) ? -1 : 1);
            myStack.Push(new Vector3(x, 5, y));
        }
        return myStack;
    }

    // This function return the list of targets in lidar range
    public List<Triple> FindTargets(List<Triple> targets)
    {
        if (targets.Count == 0)
            return null;

        List<Triple> typeTargets = new List<Triple>();

        foreach (Triple t in targets)
        {
            if ((t.Tag == "Metal") || (t.Tag == "Glass"))
            {
                typeTargets.Add(t);
            }
        }

        if (typeTargets.Count == 0)
            return null;

        Debug.Log("Number of targets detected " + typeTargets.Count);

        return typeTargets;
    }

    // This function convert Triple list to position list
    public List<Message> TripleToMessage(List<Triple> list)
    {
        if (list == null)
            return null;
        List<Message> messageList = new List<Message>();
        Message m = new Message();
        foreach (Triple t in list)
        {
            m.position = GetPosition(t);
            m.tag = t.Tag;
            messageList.Add(m);
        }
        return messageList;
    }

    // This function return a target position (x,y,z)
    Vector3 GetPosition(Triple target)
    {
        Quaternion rotation = Quaternion.AngleAxis(target.Angle, Vector3.up);
        Vector3 addDistanceToDirection = rotation * transform.forward * target.Value;
        Vector3 position = transform.position + addDistanceToDirection;
        return position;
    }

    // This function return the angle between the rover's orientation and a target
    float GetAngle(Vector3 target)
    {
        Vector3 targetDirection = target - transform.position;
        Debug.Log("Target direction " + targetDirection);
        targetDirection = targetDirection.normalized;
        float angle = Vector3.Angle(transform.forward, targetDirection);
        Debug.Log("Angle value = " + angle);

        return angle;
    }

    /* ******************************* Data conversion functions ******************************* */

    // This function convert a data to bytes array
    private byte[] DataToByte(Vector3 position, string tag)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(tag);
        byte[] buffer = new byte[bytes.Length + 3];
        System.Array.ConstrainedCopy(bytes, 0, buffer, 3, bytes.Length);
        buffer[0] = (byte)position.x;
        buffer[1] = (byte)position.y;
        buffer[2] = (byte)position.z;
        return buffer;
    }

    // This function convert bytes array to data
    private Message ByteToData(byte[] buffer)
    {
        Message m = new Message();
        m.position = new Vector3(buffer[0], buffer[1], buffer[2]);
        byte[] tagBytes = new byte[buffer.Length - 3];
        System.Array.ConstrainedCopy(buffer, 3, tagBytes, 0, buffer.Length - 3);
        m.tag = System.Text.Encoding.ASCII.GetString(tagBytes);
        return m;
    }

    /* ***************************************************************************************** */

}
