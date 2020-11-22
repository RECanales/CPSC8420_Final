using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject hand, target;
    public bool debug = false;
    public float rotate_speed = 1; // how fast the joints rotate
    public float move_speed = 1; // how quick the hand moves
    public int max_height = 20;
    public int num_grips = 5;
    public float max_stretch = 0;
    List<Transform> joints = new List<Transform>();
    List<GameObject> fingertips = new List<GameObject>();
    List<int> center_indices = new List<int>();
    float[] finger_states = new float[5] { 0, 0, 0, 0, 0 }; // add an index for opening thumb action
    float[] initial_finger_states = new float[5] { 0, 0, 0, 0, 0 };
    int[] position_state = new int[3] { 0, 0, 0 };   // left /right, forward / backward, up
    int[] initial_position_state = new int[3] { 0, 0, 0 };
    int grip_state = 0; // was 1 when close was enabled
    int initial_grip_state = 0;
    int[] finger_indices = new int[5] { 0, 3, 6, 9, 12 };
    //GameObject centerOfHand;
    bool terminal = false;

    Vector3 original_position, initial_pos; // original = center, initial = root
    float initial_dist = 0;
    List<Quaternion> original_rotation = new List<Quaternion>();
    Vector3 original_obj_position;
    Quaternion original_obj_rotation;
    public bool ready = false;
    public float max_joint_rotation = 70;
    public bool positioned_over_target = false;
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy

    // Start is called before the first frame update
    void Start()
    {
        if (hand)
        {
            TraverseHierarchy(hand.transform);
            //centerOfHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //centerOfHand.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            //centerOfHand.transform.position = original_position;
            //centerOfHand.name = "Goal";
            //centerOfHand.GetComponent<BoxCollider>().enabled = false;

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
        }

        CacheState();
        ready = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(debug)
            InputListener();
    }

    void InputListener()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            MoveFinger(0, "close");
            MoveFinger(1, "close");
            MoveFinger(2, "close");
            MoveFinger(3, "close");
            MoveFinger(4, "close");
        }
        //else MoveFinger(0, "open");
        //else MoveFinger(4, "open");

        if (Input.GetKeyDown(KeyCode.UpArrow))
            MoveHand("up");

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

        initial_grip_state = grip_state;
    }

    public bool ObjectOnTarget(GameObject scene_obj)
    {
        Vector3 target_position, current_position;

        // ignore height (y)
        target_position = new Vector3(target.transform.position.x, hand.transform.position.y, target.transform.position.z);
        current_position = scene_obj.transform.position;
        if (Vector3.Magnitude(target_position - current_position) > 0.01f)
            return false;
        return true;
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
        if (Vector3.Magnitude(target_position - current_position) > 0.1f)
            hand.transform.position = current_position + speed * Vector3.Normalize(target_position - current_position);
        else
            positioned_over_target = true;
        //centerOfHand.transform.position = GetCenterOfHand();
    }

    public void MoveFinger(int index, string action) // finger index, close/open
    {
        bool rotate = true;
        // (sign < 0 && finger_states[index] <= 0) condition not necessary when close it only action
        if (action == "close" && finger_states[index] >= max_joint_rotation || (action == "open" && finger_states[index] <= 0))
            rotate = false;
        
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
        if ((grip_state == 0  && action > 0)|| (grip_state == num_grips - 1 && action < 0))
            return;

        // rotate knuckles
        float sign = (float)action;

        joints[finger_indices[0]].Rotate(new Vector3(0, 0, -sign * max_stretch / (float)num_grips * 2.5f)); // thumb
        joints[finger_indices[1]].Rotate(new Vector3(0, 0, -sign * max_stretch / (float)num_grips));
        joints[finger_indices[3]].Rotate(new Vector3(0, 0, sign * max_stretch / (float)num_grips));
        joints[finger_indices[4]].Rotate(new Vector3(0, 0, sign * max_stretch / (float)num_grips * 2)); // pinky

        grip_state += -action;
    }

    public void MoveHand(string direction)
    {
        if (Mathf.Abs(position_state[0]) > 4)
            return;
        if (Mathf.Abs(position_state[1]) > 4)
            return;
        //if (position_state[2] > max_height - 1 || position_state[2] <= 0)
            //return;

        if (position_state[2] < max_height - 1 && direction == "up")
        {
            hand.transform.position += new Vector3(0, move_speed, 0);
            position_state[2] += 1;
        }
        else if (position_state[2] > 0 && direction == "down")
        {
            hand.transform.position += new Vector3(0, -move_speed, 0);
            position_state[1] -= 1;
        }
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

        //centerOfHand.transform.position = GetCenterOfHand();
    }

    // start from parent object, find all children
    void TraverseHierarchy(Transform currObj)
    {
        foreach (Transform child in currObj)
        {
            if (child.name.Contains("1") || child.name.Contains("2") || child.name.Contains("3"))
            {
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
     
        int yMax = max_height;
        int xMax = 10;
        // first, position (columns of Q table)
        int x = (int)Mathf.Min(position_state[0] + (xMax / 2) - 1, xMax-1);
        int z = (int)Mathf.Min(position_state[1] + (xMax / 2) - 1, xMax-1);
        int y = (int)Mathf.Min(position_state[2], yMax - 1);
        
        if (x < 0)
            x = 0;
        if (z < 0)
            z = 0;
        if (y < 0)
            y = 0;

        //int pos_idx = 40 * x + z; // serve as X index (size X*Z)

        //int pos_idx = (z * xMax * yMax) + (y * xMax) + x; // columns

        // now rotation (rows)
        int h_idx = (int)Mathf.Min(max_joint_rotation / rotate_speed - 1, finger_states[0] / rotate_speed); // 60 degrees for rotate_speed = 1
        if (h_idx < 0)
            h_idx = 0;

        //(width * height * z) + (width * y) + x
        int state_idx = (yMax * (int)(max_joint_rotation / rotate_speed) * grip_state) + (yMax * h_idx) + y;
        //int state_idx = h_idx * (int)(max_joint_rotation / rotate_speed) + y; // only move up

        //state_idx = x + y * xMax + z * xMax * yMax + h_idx * xMax * xMax * yMax;
        Debug.Assert(state_idx < yMax * num_grips * (int)(max_joint_rotation / rotate_speed));
        return state_idx;
    }


    public bool IsTerminal(string policy)
    {
        /*
        // hand has moved up to max height
        if ((policy == "grasp" && position_state[2] >= max_height - 1) || (policy == "release" && position_state[2] <= 0))
            return true;*/

        // all the way closed or all the way open
        if ((policy == "grasp" && finger_states[0] >= max_joint_rotation - 1) || (policy == "release" && finger_states[0] <= 0))
            return true;

        if (policy == "grasp")
            return GameObject.FindGameObjectWithTag("Ball").GetComponent<CollisionDetector>().ungripped;
        else
            return GameObject.FindGameObjectWithTag("Ball").GetComponent<CollisionDetector>().hit_floor || GameObject.FindGameObjectWithTag("Ball").GetComponent<CollisionDetector>().hit_target;
    }

    public void ResetState()
    {
        hand.transform.position = initial_pos;
        //centerOfHand.transform.position = GetCenterOfHand();
        for (int i = 0; i < joints.Count; ++i)
            joints[i].rotation = original_rotation[i];

        for(int i = 0; i < finger_states.Length; ++i)
            finger_states[i] = initial_finger_states[i];
        for(int i = 0; i < position_state.Length; ++i)
            position_state[i] = initial_position_state[i];

        grip_state = initial_grip_state;
    }

    public Vector3 GetCenterOfHand()
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < center_indices.Count; ++i)
            center += joints[center_indices[i]].position;
        center /= (float)center_indices.Count;
        //centerOfHand.transform.position = center;
        return center;
    }

    public float TargetObjDist(GameObject scene_obj)
    {
        float dist = Vector3.Magnitude(scene_obj.transform.position - target.transform.position);
        return dist;
    }


    /*
    public float GetAvgDistFromBall()
    {
        if (!scene_obj.GetComponent<SphereCollider>())
            return 0;
        float dist = Vector3.Magnitude(scene_obj.transform.position - GetCenterOfHand());
        int c = 0;
        foreach (GameObject t in fingertips)
        {
            float radius = scene_obj.transform.lossyScale.x * scene_obj.GetComponent<SphereCollider>().radius;
            Vector3 surface_pos = scene_obj.transform.position + radius * (t.transform.position - scene_obj.transform.position).normalized;
            //if(c == 0)
                //Debug.DrawLine(scene_obj.transform.position, surface_pos, Color.red, 0.01f, false);
            dist += Vector3.Magnitude(t.transform.position - surface_pos);
            c++;
        }

        return dist;
    }
    */
}
