using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoverIA : MonoBehaviour {

    public struct Message
    {
        public Vector3 position;
        public string tag;
    }

    RoverMovementScript controller;
    Wifi wifi;
    LidarRotative lidarRotative;
    List<Triple> data, targetList, obstaclesList, objectList;
    Triple target;
    List<Vector3> targetPosList;

    float KR = 0.02f; // repulsive potential gain 0.02;
    float KA = 1.5f; // attractive potential gain 1.5
    float PMAX = 0.3f;

    float robotPosX, robotPosY, targetPosX, targetPosY;
    // Use this for initialization
    void Start() {
        controller = GetComponent<RoverMovementScript>();
        wifi = GetComponent<Wifi>();
        lidarRotative = GetComponent<LidarRotative>();
        data = lidarRotative.GetData();
        targetList = FindTargets(data);
        targetPosList = TripleToPos(targetList);
        obstaclesList = FindObstacles(data);
        objectList = FindObjects(data);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*if (targetList != null)
        {
            target = FindTarget(targetList);
            Vector3 targetPos = GetPosition(target);
            Debug.Log("I am " + this.tag + " rover, I detect this target " + targetPos);
            if (obstaclesList != null)
            {
                Vector2[] obst = GetObstacleVect(obstaclesList);
                robotPosX = this.transform.position.x;
                robotPosY = this.transform.position.z;
                targetPosX = targetPos.x;
                targetPosY = targetPos.z;
                Vector2 p = potentialField(robotPosX, robotPosY, targetPosX, targetPosY, obst);
                GoTo(p);
            }
            else
            {
                GoTo(GetPosition(target));
            }
        }
        else
        {
            controller.ChangeMotorSpeed(1.0f, -1.0f); 
        }
        data = lidarRotative.GetData();
        /*targetList.Union(FindTargets(data));
        obstaclesList.Union(FindObstacles(data));*/
        Vector3 pos;
        List<byte[]> messageToSend = new List<byte[]>();
        byte[] message;
        foreach (Triple t in objectList)
        {
            pos = GetPosition(t);
            message = DataToByte(pos, t.Tag);
            messageToSend.Add(message);
        }

        List<GameObject> robots = wifi.getRobotAround();
        foreach(GameObject g in robots)
        {
            Debug.Log("I am " + this.tag + " rover, I detect " + g.tag + " rover");
            foreach(byte[] m in messageToSend)
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
                Debug.Log("I am " + this.tag + " rover, Another robot send me this message " + m);
                targetPosList.Add(m.position);
            }
        }
        objectList = FindObjects(data);
    }

 /* ******************************* Collecting targets ******************************* */

    // This function deactivate a target when the rover reached it
    private void OnCollisionEnter(Collision collision)
    {
        if ((collision.collider.CompareTag("Metal") && this.tag == "RobotMetal") ||
            (collision.collider.CompareTag("Glass") && this.tag == "RobotGlass"))
        {
            collision.collider.gameObject.SetActive(false);
        }
    }

 /* ********************************************************************************** */


 /* ******************************* Detecting targets ******************************* */
    // This function return the closest target in lidar range
    public Triple FindTarget(List<Triple> targets)
    {
        if (targets.Count == 0)
            return null;

        List<Triple> typeTargets = new List<Triple>();

        foreach (Triple t in targets)
        {
            if ((t.Tag == "Metal" && this.tag == "RobotMetal") ||
                (t.Tag == "Glass" && this.tag == "RobotGlass"))
            {
                typeTargets.Add(t);
            }
        }

        if (typeTargets.Count == 0)
            return null;

        Triple closest = typeTargets[0];

        float minDistance = Mathf.Infinity;

        foreach (Triple t in typeTargets)
        {
            if (t.Value < minDistance)
            {
                closest = t;
                minDistance = t.Value;
            }
        }

        Debug.Log("Number of targets detected " + typeTargets.Count);

        return closest;
    }

    // This function return the list of targets in lidar range
    public List<Triple> FindTargets(List<Triple> targets)
    {
        if (targets.Count == 0)
            return null;

        List<Triple> typeTargets = new List<Triple>();

        foreach (Triple t in targets)
        {
            if ((t.Tag == "Metal" && this.tag == "RobotMetal") ||
                (t.Tag == "Glass" && this.tag == "RobotGlass"))
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
    public List<Vector3> TripleToPos(List<Triple> list)
    {
        if (list == null)
            return null;
        List<Vector3> posList = new List<Vector3>();
        foreach(Triple t in list)
        {
            posList.Add(GetPosition(t));
        }
        return posList;
    }

 /* ********************************************************************************** */

 /* ******************************* Detecting obstacles ******************************* */
    // This function return the nearest obstacle in lidar range
    public Triple FindObstacle()
    {
        List<Triple> objects = lidarRotative.GetData();
        if (objects.Count == 0)
            return null;
        List<Triple> obstacles = new List<Triple>();

        foreach (Triple t in objects)
        {
            if ((t.Tag != "Metal" && this.tag == "RobotMetal") ||
                (t.Tag != "Glass" && this.tag == "RobotGlass"))
            {
                if (Mathf.Abs(t.Angle) < 45 && t.Value < 5)
                {
                    Debug.Log("Obstacle = " + t.Value);
                    obstacles.Add(t);
                }
            }
        }

        if (obstacles.Count == 0)
            return null;
        Triple closestObs = obstacles[0];
        float minDistance = obstacles[0].Value;

        foreach (Triple o in obstacles)
        {
            if (o.Value < minDistance)
            {
                closestObs = o;
                minDistance = o.Value;
            }
        }

        Debug.Log("Number of obstacles detected " + obstacles.Count);

        return closestObs;
    }

    // This function return the list of obstacles in lidar range
    public List<Triple> FindObstacles(List<Triple> objects)
    {
        if (objects.Count == 0)
            return null;
        List<Triple> obstacles = new List<Triple>();

        foreach (Triple t in objects)
        {
            if ((t.Tag != "Metal" && this.tag == "RobotMetal") ||
                (t.Tag != "Glass" && this.tag == "RobotGlass"))
            {
                if (Mathf.Abs(t.Angle) < 45 && t.Value < 5)
                {
                    Debug.Log("Obstacle = " + t.Value);
                    obstacles.Add(t);
                }
            }
        }

        if (obstacles.Count == 0)
            return null;
        else
            return obstacles;
    }

    // This function convert obstacles List to obstacles Vector to be used for Potential Field
    private Vector2[] GetObstacleVect(List<Triple> obstacles)
    {
        if (obstacles == null)
        {
            return null;
        }

        int length = obstacles.Count;
        Vector2[] obst = new Vector2[length];
        for (int i = 0; i < length; i++)
        {
            obst[i] = GetPosition(obstacles[i]);
        }
        return obst;
    }

 /* ********************************************************************************** */

 /* ******************************* Detecting objects ******************************* */
    // This function return the list of other type objects in lidar range
    public List<Triple> FindObjects(List<Triple> objects)
    {
        if (objects.Count == 0)
            return null;
        List<Triple> obj = new List<Triple>();

        foreach (Triple t in objects)
        {
            if ((t.Tag == "Metal" && this.tag == "RobotGlass") ||
                (t.Tag == "Glass" && this.tag == "RobotMetal"))
            {
                    obj.Add(t);
            }
        }

        if (obj.Count == 0)
            return null;
        else
            return obj;
    }

    /* ********************************************************************************** */

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

    // This function allow the rover to move towards the target
    public void GoTo(Vector3 target)
    {
        float angle = GetAngle(target);
        Debug.Log("Angle = : " + angle);
        if (Mathf.Abs(angle) <= 5.0f)
        {
            controller.ChangeMotorSpeed(1.0f, 1.0f);
        }
    }

    // This function allow rovers to avoid obstacles based on angle.
    public void AvoidObstacle(float angle)
    {
        if (Mathf.Abs(angle) < 35.0f)
        {
            controller.ChangeMotorSpeed(1.0f, -1.0f);
        }
        else
        {
            controller.ChangeMotorSpeed(1.0f, 1.0f);
        }
    }

 /* ***************************** Potential Field functions ******************************** */

    // Attractive potential field gradient function
    private Vector2 AttPotGrad(float robotPosX, float robotPosY, float targetPosX, 
                               float targetPosY)
    {
        Vector2 apg = new Vector2(KA * (robotPosX - targetPosX), KA * (robotPosY - targetPosY));
        Debug.Log("Attractive Potential Field Gradient = " + apg);
        return apg;
    }

    // Repulsive potential field gradient function
    private Vector2 RepPotGrad(float robotPosX, float robotPosY, Vector2 obstacle)
    {
        float maxDistance = Mathf.Sqrt(Mathf.Pow(obstacle.x - robotPosX, 2) + Mathf.Pow(obstacle.y - robotPosY, 2));
        if (maxDistance < PMAX)
        {
            Vector2 rpg = new Vector2(KR * (obstacle.x - robotPosX), KR * (obstacle.y - robotPosY)) / Mathf.Pow(maxDistance, 4);
            Debug.Log("Repulsive Potential Field Gradient = " + rpg);
            return rpg;
        }
        return new Vector2 (0.0f, 0.0f);
    }

    // Calculate potential field 
    private Vector2 PotentialField(float robotPosX, float robotPosY, float targetPosX, 
                                   float targetPosY, Vector2[] obstaclesPos)
    {
        Vector2 potential = AttPotGrad(robotPosX, robotPosY, targetPosX, targetPosY);
        if (obstaclesPos != null)
        {
            for (int i = 0; i < obstaclesPos.Length; i++)
            {
                potential += RepPotGrad(robotPosX, robotPosY, obstaclesPos[i]);
            }
        }
        Debug.Log("potentialField = " + potential);
        return -potential;
    }

 /* ***************************************************************************************** */

 /* ******************************* Data conversion functions ******************************* */

    // This function convert a data to bytes array
    private byte[] DataToByte(Vector3 position, string tag)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(tag);
        byte[] buffer = new byte[bytes.Length + 3];
        System.Array.ConstrainedCopy (bytes, 0, buffer, 3, bytes.Length);
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
