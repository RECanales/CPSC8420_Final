﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject hand, scene_obj, target;
    public float rotate_speed = 1; // how fast the joints rotate
    public float move_speed = 1; // how quick the hand moves
    public float max_stretch = 0;
    public int max_dist = 0;
    List<Transform> joints = new List<Transform>();
    List<GameObject> fingertips = new List<GameObject>();
    List<Vector3> thumb_axes = new List<Vector3>();
    List<int> center_indices = new List<int>();
    int thumb_idx = -1;
    bool stop_rotate = false;
    float degrees = 0;
    float[] finger_states = new float[5] { 0, 0, 0, 0, 0 }; // add an index for opening thumb action
    float[] initial_finger_states = new float[5] { 0, 0, 0, 0, 0 };
    int[] position_state = new int[2] { 0, 0 };   // left /right, forward / backward
    int[] initial_position_state = new int[2] { 0, 0 };
    int grip_state = 1;
    int initial_grip_state = 1;
    int[] finger_indices = new int[5] { 0, 3, 6, 9, 12 };
    GameObject centerOfHand;
    bool terminal = false;

    Vector3 original_position, initial_pos; // original = center, initial = root
    float initial_dist = 0;
    List<Quaternion> original_rotation = new List<Quaternion>();
    Vector3 original_obj_position;
    Quaternion original_obj_rotation;
    public bool ready = false;
    public float max_joint_rotation = 70;
    public bool positioned_over_target = false;

    // Start is called before the first frame update
    void Start()
    {
        if (hand)
        {
            TraverseHierarchy(hand.transform);
            //for (int i = 0; i < joints.Count; ++i)
                //joints[i].GetComponent<JointObj>().UpdateTransform();

            Vector3 thumb1_axis = Vector3.Normalize(joints[thumb_idx].up + joints[thumb_idx].right);
            Vector3 thumb2_axis = Vector3.Cross(joints[thumb_idx + 1].position - joints[thumb_idx].position, joints[thumb_idx + 1].forward);
            Vector3 thumb3_axis = thumb2_axis;
            thumb_axes.Add(thumb1_axis);
            thumb_axes.Add(thumb2_axis);
            thumb_axes.Add(thumb3_axis);
            centerOfHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerOfHand.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            centerOfHand.transform.position = original_position;
            centerOfHand.name = "Goal";

            // setting intial distance
            foreach (Transform t in joints)
            {
                if (t.name.Contains("3"))
                {
                    float bone_scale = t.GetChild(0).lossyScale.y;
                    GameObject new_fingertip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    new_fingertip.transform.localScale = 0.1f * Vector3.one;
                    new_fingertip.transform.position = t.GetChild(0).position + (0.5f * bone_scale) * t.GetChild(0).up;
                    new_fingertip.transform.parent = t;
                    new_fingertip.name = "fingertip";
                    new_fingertip.GetComponent<Renderer>().material = t.GetComponent<Renderer>().material;
                    fingertips.Add(new_fingertip);
                }
            }

            //initial_dist = GetAvgDistFromBall();
            CacheState();

            /*
            foreach (GameObject t in fingertips)
            {
                float radius = scene_obj.transform.localScale.x * scene_obj.GetComponent<SphereCollider>().radius;
                Vector3 surface_pos = scene_obj.transform.position + radius * (t.transform.position - scene_obj.transform.position).normalized;
                initial_dist += Vector3.Magnitude(t.transform.position - surface_pos);
            }*/
            //initial_dist = GetAvgDistFromBall();
            original_obj_position = scene_obj.transform.position;
            original_obj_rotation = scene_obj.transform.rotation;
        }
        ResetState();
        ready = true;
    }

    // Update is called once per frame
    void Update()
    {
        
        InputListener();
    }

    void InputListener()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            MoveFinger(finger_indices[0], "thumb_open");
            //print(finger_states[0]);
        }
        //else MoveFinger(0, "open");

        if (Input.GetKeyDown(KeyCode.W))
            MoveFinger(finger_indices[0], "close");
        //else MoveFinger(1, "open");

        if (Input.GetKeyDown(KeyCode.E))
            MoveFinger(finger_indices[2], "close");
        //else MoveFinger(2, "open");

        if (Input.GetKeyDown(KeyCode.R))
            MoveFinger(finger_indices[3], "close");
        //else MoveFinger(3, "open");

        if (Input.GetKeyDown(KeyCode.T))
            MoveFinger(finger_indices[4], "close");
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

        if(Input.GetKeyDown(KeyCode.O))
        {
            AdjustGrip(1);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            AdjustGrip(-1);
        }
        //print(IsTerminal());
        //print(GetAvgDistFromBall());

        //for (int i = 0; i < joints.Count; ++i)
        //Traverse(joints[i].gameObject.GetComponent<JointObj>());

    }

    public void CacheState()
    {
        // store original rotations for resetting the hand 
        if (original_rotation.Count == 0)
        {
            for (int i = 0; i < joints.Count; ++i)
                original_rotation.Add(joints[i].rotation);
        }

        else
        {
            for (int i = 0; i < joints.Count; ++i)
                original_rotation[i] = joints[i].rotation;
        }

        // store initial position
        initial_pos = hand.transform.position;
        original_position = GetCenterOfHand();

        // store state arrays
        for(int i = 0; i < finger_states.Length; ++i)
            initial_finger_states[i] = finger_states[i];
        for(int i = 0; i < position_state.Length; ++i)
            initial_position_state[i] = 0;

        initial_grip_state = 1;

        // store ball position & rotation
        original_obj_position = scene_obj.transform.position;
        original_obj_rotation = scene_obj.transform.rotation;
    }

    public bool isObjectOnTarget()
    {
        Vector3 target_position, current_position;

        // ignore height (y)
        target_position = new Vector3(target.transform.position.x, hand.transform.position.y, target.transform.position.z);
        current_position = scene_obj.transform.position;
        if (Vector3.Magnitude(target_position - current_position) > 0.1f)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void MoveOverTarget(float speed)
    {
        // translate the hand to be over the target
        Vector3 target_position, current_position;

        // ignore height (y)
        Vector3 hand_center = GetCenterOfHand();
        Vector3 offset = hand_center - hand.transform.position;
        target_position = new Vector3(target.transform.position.x, hand.transform.position.y, target.transform.position.z) - offset;
        current_position = hand.transform.position;

        // if distance is greater than some amount, keep moving
        if (Vector3.Magnitude(target_position - current_position) > speed)
            hand.transform.position = current_position + speed * Vector3.Normalize(target_position - current_position);
        else
            positioned_over_target = true;
        centerOfHand.transform.position = GetCenterOfHand();
    }

    public void MoveFinger(int index, string action) // finger index, close/open
    {
        bool rotate = true;
        // (sign < 0 && finger_states[index] <= 0) condition not necessary when close it only action
        if (action == "close" && finger_states[index] >= max_joint_rotation || (action == "open" && finger_states[index] <= 0))
        {
            //ResetState();
            rotate = false;
        }

        
        if (rotate)
        {
            float sign = action == "open" ? -1 : 1;
            //finger_states[index] = sign > 0 ? finger_states[index] + rotate_speed : finger_states[index] - rotate_speed;

            if(action == "open")
                finger_states[index] -= rotate_speed;
            if (action == "close")
                finger_states[index] += rotate_speed;

            for (int i = finger_indices[index]; i < finger_indices[index] + 3; ++i)
                joints[i].Rotate(new Vector3(sign*rotate_speed, 0, 0));
        }
    }

    public void AdjustGrip(int action)
    {
        if ((grip_state == 0  && action < 0)|| (grip_state == 3 && action > 0))
            return;

        // rotate knuckles
        float sign = (float)action;

        joints[finger_indices[0]].Rotate(new Vector3(0, 0, -sign * max_stretch/3 * 2.5f)); // thumb
        joints[finger_indices[1]].Rotate(new Vector3(0, 0, -sign * max_stretch/3));
        joints[finger_indices[3]].Rotate(new Vector3(0, 0, sign * max_stretch/3));
        joints[finger_indices[4]].Rotate(new Vector3(0, 0, sign * max_stretch/3 * 2)); // pinky

        grip_state += action;
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

        centerOfHand.transform.position = GetCenterOfHand();
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

                joints.Add(child);
            }

            TraverseHierarchy(child);
        }
    }

    public int GetState()
    {
        // first, position (columns of Q table)
        int x = (int)Mathf.Min(position_state[0] + 19, 39);
        int z = (int)Mathf.Min(position_state[1] + 19, 39);
        if (x < 0)
            x = 0;
        if (z < 0)
            z = 0;
        int pos_idx = 40 * x + z; // serve as X index (size X*Z)

        // now rotation (rows)
        int h_idx = (int)Mathf.Min((max_joint_rotation - 1)/ rotate_speed, finger_states[0] / rotate_speed); // 60 degrees for rotate_speed = 1
        if (h_idx < 0)
            h_idx = 0;

        // open or closed (hand spread)
        //h_idx = h_idx + grip_state; // number rotations X 4 columns (grip types)
        int state_idx = 1600 * h_idx + pos_idx; // considering all the fingers move independently.
        Debug.Assert(state_idx < 1600 * (int)(max_joint_rotation / rotate_speed));
        return state_idx;
    }

    public bool IsTerminalOpen()
    {
        //return false;
        //if (grip_state < 0 || grip_state > 3)
        //return true;

        for (int i = 0; i < finger_states.Length; ++i)
        {
            // all the way open
            if (finger_states[i] <= 0)
            {
                return true;
            }
        }

        for (int i = 0; i < position_state.Length; ++i)
        {
            // max movement in one direction
            if (Mathf.Abs(position_state[i]) > 19)
                return true;
        }

        // ball has hit a wall
        if (scene_obj.GetComponent<CollisionDetector>().ungripped /*|| scene_obj.GetComponent<CollisionDetector>().reached_goal*/)
            return true;

        //if(Vector3.Magnitude(scene_obj.transform.position - GetCenterOfHand()) <= 0.45f) // goal
        //return true;
        return false;
    }



    public bool IsTerminal()
    {
        //return false;
        //if (grip_state < 0 || grip_state > 3)
            //return true;

        for (int i = 0; i < finger_states.Length; ++i)
        {
            // all the way closed, all the way open
            if (finger_states[i] >= max_joint_rotation /*|| finger_states[i] < 0*/)
                return true;
        }
        
        for(int i = 0; i < position_state.Length; ++i)
        {
            // max movement in one direction
            if (Mathf.Abs(position_state[i]) > 19)
                return true;
        }
        
        // ball has hit a wall
        if (scene_obj.GetComponent<CollisionDetector>().ungripped /*|| scene_obj.GetComponent<CollisionDetector>().reached_goal*/)
            return true;

        //if(Vector3.Magnitude(scene_obj.transform.position - GetCenterOfHand()) <= 0.45f) // goal
            //return true;
        return false;
    }

    public void ResetState()
    {
        scene_obj.GetComponent<Rigidbody>().isKinematic = true;
        hand.transform.position = initial_pos;
        centerOfHand.transform.position = GetCenterOfHand();
        for (int i = 0; i < joints.Count; ++i)
            joints[i].rotation = original_rotation[i];

        for(int i = 0; i < finger_states.Length; ++i)
            finger_states[i] = initial_finger_states[i];
        for(int i = 0; i < position_state.Length; ++i)
            position_state[i] = initial_position_state[i];

        grip_state = 1;
        scene_obj.transform.position = original_obj_position;
        scene_obj.transform.rotation = original_obj_rotation;
        scene_obj.GetComponent<CollisionDetector>().ResetState();
        scene_obj.GetComponent<Rigidbody>().isKinematic = false;
    }

    public Vector3 GetCenterOfHand()
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < center_indices.Count; ++i)
            center += joints[center_indices[i]].position;
        center /= (float)center_indices.Count;
        centerOfHand.transform.position = center;
        return center;
    }


    public float TargetBallDist()
    {
        float dist = Vector3.Magnitude(scene_obj.transform.position - target.transform.position);
        return dist;
    } 


    public float GetAvgDistFromBall()
    {
        float dist = Vector3.Magnitude(scene_obj.transform.position - GetCenterOfHand());
        foreach (GameObject t in fingertips)
        {
            float radius = scene_obj.transform.lossyScale.x * scene_obj.GetComponent<SphereCollider>().radius;
            Vector3 surface_pos = scene_obj.transform.position + radius * (t.transform.position - scene_obj.transform.position).normalized;
            dist += Vector3.Magnitude(t.transform.position - surface_pos);
        }

        return dist;

        //return 1/(1+dist);
        /*
        if (dist > initial_dist)
        {
            initial_dist = dist;
            return -1;
        }
        if (dist <= initial_dist)
        {
            initial_dist = dist;
            // positive reward for fingertips being close to surface and ball moving up
            //float reward = 1 + 1.5f * (scene_obj.transform.position.y - original_ball_position.y) + 1.5f/(1+Vector3.Magnitude(scene_obj.transform.position-GetCenterOfHand()));
            //float reward = 1 + 1.5f/(1+Vector3.Magnitude(scene_obj.transform.position-GetCenterOfHand()));
            return 1;
        }
        
        return 0;
        */
    }
}
