using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject hand, scene_obj;
    public float rotate_speed = 1; // how fast the joints rotate
    public float move_speed = 1; // how quick the hand moves
    public int max_dist = 0;
    List<Transform> joints = new List<Transform>();
    List<Vector3> thumb_axes = new List<Vector3>();
    List<int> center_indices = new List<int>();
    int thumb_idx = -1;
    bool stop_rotate = false;
    float degrees = 0;
    float[] finger_states = new float[5] { 0, 0, 0, 0, 0 };
    int[] position_state = new int[2] { 0, 0 };   // left /right, forward / backward
    int[] finger_indices = new int[5] { 0, 3, 6, 9, 12 };
    GameObject centerOfHand;
    bool terminal = false;

    Vector3 original_position, initial_pos; // original = center, initial = root
    public GameObject ball;
    float initial_dist = 0;
    List<Quaternion> original_rotation = new List<Quaternion>();

    // these are for manual transformations
    List<Matrix4x4> matrixStack = new List<Matrix4x4>();

    // Start is called before the first frame update
    void Start()
    {
        if (hand)
        {
            TraverseHierarchy(hand.transform);
            for(int i = 0; i < joints.Count; ++i)
            {
                if (joints[i].name.Contains("2") || joints[i].name.Contains("3"))
                {
                    joints[i].parent = null;
                    //JointObj parent = joints[i - 1].GetComponent<JointObj>();
                    //joints[i].GetComponent<JointObj>().UpdateSkeleton(parent.GetMatrix());
                }
            }
            for (int i = 0; i < joints.Count; ++i)
                joints[i].GetComponent<JointObj>().UpdateTransform();

            Vector3 thumb1_axis = Vector3.Normalize(joints[thumb_idx].up + joints[thumb_idx].right);
            Vector3 thumb2_axis = Vector3.Cross(joints[thumb_idx + 1].position-joints[thumb_idx].position, joints[thumb_idx+1].forward);
            Vector3 thumb3_axis = thumb2_axis;
            thumb_axes.Add(thumb1_axis);
            thumb_axes.Add(thumb2_axis);
            thumb_axes.Add(thumb3_axis);
            centerOfHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            original_position = GetCenterOfHand();
            initial_pos = hand.transform.position;
            initial_dist = GetAvgDistFromBall();
        }
    }

    // Update is called once per frame
    void Update()
    {
        InputListener();
        centerOfHand.transform.position = GetCenterOfHand();
        /*
        if (joints.Count > 0)
        {
            foreach(Transform j in joints)
            {
                if (!stop_rotate)
                {
                    //if (j.rotation.eulerAngles.x < 90)
                    j.Rotate(rotate_speed, 0, 0);
                    
                }

                else
                    j.Rotate(-rotate_speed, 0, 0);
            }

            if (!stop_rotate)
                degrees += rotate_speed;
            else
                degrees -= rotate_speed;
            if (degrees > 60)
                stop_rotate = true;
            else if (degrees <= 0)
                stop_rotate = false;

            scene_obj.transform.position = GetCenterOfHand();
        }*/
    }

    void InputListener()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            MoveFinger(0, "open");
            print(finger_states[0]);
        }
        //else MoveFinger(0, "open");

        if (Input.GetKeyDown(KeyCode.W))
            MoveFinger(1, "close");
        //else MoveFinger(1, "open");

        if (Input.GetKeyDown(KeyCode.E))
            MoveFinger(2, "close");
        //else MoveFinger(2, "open");

        if (Input.GetKeyDown(KeyCode.R))
            MoveFinger(3, "close");
        //else MoveFinger(3, "open");

        if (Input.GetKeyDown(KeyCode.T))
            MoveFinger(4, "close");
        //else MoveFinger(4, "open");

        if (Input.GetKey(KeyCode.UpArrow))
            MoveHand("left");

        if (Input.GetKey(KeyCode.DownArrow))
            MoveHand("right");

        if (Input.GetKey(KeyCode.RightArrow))
            MoveHand("forward");

        if (Input.GetKey(KeyCode.LeftArrow))
            MoveHand("backward");

        if (Input.GetKeyDown(KeyCode.Space))
            ResetState();
        //print(IsTerminal());
        //print(GetAvgDistFromBall());

        //for (int i = 0; i < center_indices.Count; ++i)
         Traverse(joints[0].gameObject.GetComponent<JointObj>());
        
    }

    public void MoveFinger(int index, string action) // finger index, close/open
    {
        bool rotate = true;
        float sign = action == "close" ? 1 : -1;
        
        //if (sign > 0 && finger_states[index] >= 60)
            //rotate = false;

        //if (sign < 0 && finger_states[index] <= 0)
            //rotate = false;

        if (rotate)
        {
            finger_states[index] = sign > 0 ? finger_states[index] + rotate_speed : finger_states[index] - rotate_speed;
            for (int i = finger_indices[index]; i < finger_indices[index] + 3; ++i)
            {
                //joints[i].Rotate(sign * rotate_speed, 0, 0);
                joints[i].GetComponent<JointObj>().RotateJoint(new Vector3(sign * rotate_speed, 0, 0));
                //Rigidbody rb = joints[i].gameObject.GetComponent<Rigidbody>();
                //Quaternion deltaRot = Quaternion.Euler(finger_states[index], 0, 0);
                //rb.MoveRotation(rb.rotation * deltaRot);
            }
        }
    }

    public void MoveHand(string direction)
    {
        if (direction == "up")
            hand.transform.position += new Vector3(0, move_speed, 0);
        else if (direction == "down")
            hand.transform.position += new Vector3(0, -move_speed, 0);
        else if (direction == "right")
        {
            hand.transform.position += new Vector3(move_speed, 0, 0);
            position_state[0] += 1;
        }
        else if (direction == "left")
        {
            hand.transform.position += new Vector3(-move_speed, 0, 0);
            position_state[0] -= 1;
        }
        else if (direction == "forward")
        {
            hand.transform.position += new Vector3(0, 0, move_speed);
            position_state[1] += 1;
        }
        else if (direction == "backward")
        {
            hand.transform.position += new Vector3(0, 0, -move_speed);
            position_state[1] -= 1;
        }
    }

    // start from parent object, find all children
    void TraverseHierarchy(Transform currObj)
    {
        foreach (Transform child in currObj)
        {
            if (child.name.Contains("1") || child.name.Contains("2") || child.name.Contains("3"))
            {
                if (thumb_idx == -1 && child.name.Contains("thumb"))
                    thumb_idx = joints.Count;

                // metacarpal joints
                if (child.name.Contains("1"))
                    center_indices.Add(joints.Count);

                // add the JointObj script to gameobjects
                child.gameObject.AddComponent<JointObj>();
                if (child.parent.name.Contains("1") || child.parent.name.Contains("2"))
                {
                    if (!child.parent.GetComponent<JointObj>())
                        child.parent.gameObject.AddComponent<JointObj>();
                    child.parent.GetComponent<JointObj>().AddChild(child.GetComponent<JointObj>());
                    child.parent.GetComponent<JointObj>().SetBoneLength(Vector3.Magnitude(child.position - child.parent.position));
                    child.GetComponent<JointObj>().SetMatrix(Matrix4x4.TRS(child.position, child.rotation, child.localScale));
                    if (child.parent.name.Contains("1"))
                        print(child.position);
                    //child.GetComponent<JointObj>().UpdateSkeleton(child.parent.GetComponent<JointObj>().GetMatrix());
                }

                joints.Add(child);
                // store original rotations for resetting the hand 
                original_rotation.Add(child.rotation);
            }

            TraverseHierarchy(child);
        }
    }

    void Traverse(JointObj startNode)
    {
        // Push identity matrix to stack
        // Stack not really needed for this HW
        Matrix4x4 M = Matrix4x4.identity;
        matrixStack.Add(M);

        // linked list traversal method
        JointObj joint = startNode;
        Matrix4x4 translation = joint.GetTranslation();
        Matrix4x4 rotation = joint.GetRotation();

        // Push transformations to stack
        Matrix4x4 pushedMatrix = translation * joint.GetMatrix() * rotation * joint.GetMatrix().inverse;    // always rotate selection locally
        matrixStack.Add(pushedMatrix);

        while (joint != null)
        {
            // apply transforms to current node
            Matrix4x4 mat = matrixStack[matrixStack.Count - 1]; // location

            // compute transformation matrix
            Matrix4x4 model = joint.GetMatrix() * joint.GetArticulation().inverse;
            Matrix4x4 transformation = mat * model;
            joint.SetMatrix(transformation * joint.GetArticulation());

            // move to child node
            joint = joint.GetChild(0);
        }

        matrixStack.Clear();

        //for (int i = 0; i < joints.Count; ++i)  // Updates the position & rotation of the gameobject  i.e. render
        //{
        //joints[i].GetComponent<JointObj>().UpdateTransform();
        //}
        startNode.UpdateTransform();
    }

    public int GetState()
    {
        int x = position_state[0] + 10;
        int z = position_state[1] + 10;
        int pos_idx = x + z * 20; // serve as X index
        int h_idx = (int)(finger_states[0] / rotate_speed); // mod rotation speed

        return pos_idx + h_idx * 400;
    }

    public bool IsTerminal()
    {
        /*
        for (int i = 0; i < 5; ++i)
        {
            if (finger_states[i] >= 60)
                return true;
            if (finger_states[i] <= 0)
                return true;
        }
        */
        for(int i = 0; i < 2; ++i)
        {
            if (Mathf.Abs(position_state[i]) >= 10) // max movement in one direction
                return true;
        }

        return false;
    }

    public void ResetState()
    {
        hand.transform.position = initial_pos;
        for (int i = 0; i < 5; ++i)
        {
            finger_states[i] = 0;
        }

        for(int i = 0; i < joints.Count; ++i)
        {
            joints[i].rotation = original_rotation[i];
        }
    }

    public Vector3 GetCenterOfHand()
    {
        Vector3 center = hand.transform.position;
        for (int i = 0; i < center_indices.Count; ++i)
            center += joints[center_indices[i]].position;
        center /= 5;
        return center;
    }

    public float GetAvgDistFromBall()
    {
        float dist = 0;
        foreach(Transform t in joints)
        {
            if (t.name.Contains("3"))
                dist += Vector3.Magnitude(t.position - scene_obj.transform.position);
        }

        //return 1/(1+dist);
        if (dist > initial_dist)
        {
            initial_dist = dist;
            return -1;
        }
        if (dist <= initial_dist)
        {
            initial_dist = dist;
            return 1;
        }
        
        return 0;
    }
}
