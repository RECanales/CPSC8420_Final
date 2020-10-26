using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject hand;
    public float rotate_speed = 1;
    List<Transform> joints = new List<Transform>();
    List<Vector3> thumb_axes = new List<Vector3>();
    int thumb_idx = -1;
    bool stop_rotate = false;
    float degrees = 0;

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
                /*
                else
                {
                    if (j.name.Contains("1"))
                        j.Rotate(thumb_axes[0], 0.2f, Space.World);
                    else if (j.name.Contains("2"))
                        j.Rotate(thumb_axes[1], 0.2f);
                    else
                        j.Rotate(thumb_axes[2], 0.2f);
                }*/
            }

            if (!stop_rotate)
                degrees += rotate_speed;
            else
                degrees -= rotate_speed;
            if (degrees > 60)
                stop_rotate = true;
            else if (degrees <= 0)
                stop_rotate = false;
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

                joints.Add(child);
            }

            TraverseHierarchy(child);
        }
    }
}
