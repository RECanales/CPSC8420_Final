using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject hand, scene_obj;
    public float rotate_speed = 1;
    List<Transform> joints = new List<Transform>();
    List<Vector3> thumb_axes = new List<Vector3>();
    List<int> center_indices = new List<int>();
    int thumb_idx = -1;
    bool stop_rotate = false;
    float degrees = 0;
    float[] finger_states = new float[5] { 0, 0, 0, 0, 0 };
    int[] finger_indices = new int[5] { 0, 3, 6, 9, 12 };

    // Start is called before the first frame update
    void Start()
    {
        if (hand)
        {
            TraverseHierarchy(hand.transform);
            Vector3 thumb1_axis = Vector3.Normalize(joints[thumb_idx].up + joints[thumb_idx].right);
            Vector3 thumb2_axis = Vector3.Cross(joints[thumb_idx + 1].position-joints[thumb_idx].position, joints[thumb_idx+1].forward);
            Vector3 thumb3_axis = thumb2_axis;
            thumb_axes.Add(thumb1_axis);
            thumb_axes.Add(thumb2_axis);
            thumb_axes.Add(thumb3_axis);
        }
    }

    // Update is called once per frame
    void Update()
    {
        InputListener();

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
        if (Input.GetKey(KeyCode.Q))
            MoveFinger(0, "close");
        else MoveFinger(0, "open");

        if (Input.GetKey(KeyCode.W))
            MoveFinger(1, "close");
        else MoveFinger(1, "open");

        if (Input.GetKey(KeyCode.E))
            MoveFinger(2, "close");
        else MoveFinger(2, "open");

        if (Input.GetKey(KeyCode.R))
            MoveFinger(3, "close");
        else MoveFinger(3, "open");

        if (Input.GetKey(KeyCode.T))
            MoveFinger(4, "close");
        else MoveFinger(4, "open");
    }

    void MoveFinger(int index, string action)
    {
        bool rotate = true;
        float sign = action == "close" ? 1 : -1;
        
        if (sign > 0 && finger_states[index] >= 60)
            rotate = false;

        if (sign < 0 && finger_states[index] <= 0)
            rotate = false;

        if (rotate)
        {
            finger_states[index] = sign > 0 ? finger_states[index] + rotate_speed : finger_states[index] - rotate_speed;
            for (int i = finger_indices[index]; i < finger_indices[index] + 3; ++i)
            {
                joints[i].Rotate(sign * rotate_speed, 0, 0);
                //Rigidbody rb = joints[i].gameObject.GetComponent<Rigidbody>();
                //Quaternion deltaRot = Quaternion.Euler(finger_states[index], 0, 0);
                //rb.MoveRotation(rb.rotation * deltaRot);
            }
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

                if (child.name.Contains("1"))
                    center_indices.Add(joints.Count);

                joints.Add(child);
            }

            TraverseHierarchy(child);
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
}
