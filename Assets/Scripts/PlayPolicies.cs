using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlayPolicies: MonoBehaviour 
{
    public GameObject controller;
    Controller handControl;
    public GameObject scene_obj;
    bool playback = false;
    bool loop = false;
    Dictionary<int, int> grasp_policy = new Dictionary<int, int>(); // grasping policy
    Dictionary<int, int> release_policy = new Dictionary<int, int>(); // release policy
    public float AnimationTime = 4f; // playback for 4 seconds
    float delayTime = 0.5f;
    Vector3 original_obj_position;
    Quaternion original_obj_rotation;

    // Start is called before the first frame update
    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        grasp_policy = LoadPolicy("Grasping_Policy.csv");
        release_policy = LoadPolicy("Release_Policy.csv");
    }

    private void Update()
    {
        if(handControl.ready && !playback)
        {
            CacheState();
            playback = true;
            loop = true;
        }

        if(loop)
        {
            StartCoroutine("Playback");
            print("start");
        }
    }

    void Step(int action)
    {
        switch (action)
        {
            case 0:
                // lift hand
                handControl.MoveHand("up");
                break;
            case 1:
                // lower hand
                handControl.MoveHand("down");
                break;
            case 2:
                // move hand forward
                handControl.MoveHand("forward");
                break;
            case 3:
                // hand backwards
                handControl.MoveHand("backward");
                break;
            case 4:
                // hand to the right
                handControl.MoveHand("right");
                break;
            case 5:
                // hand to the left
                handControl.MoveHand("left");
                break;
            case 6:
                // rotate fingers (close)
                handControl.MoveFinger(0, "close");
                handControl.MoveFinger(1, "close");
                handControl.MoveFinger(2, "close");
                handControl.MoveFinger(3, "close");
                handControl.MoveFinger(4, "close");
                break;
            case 7:
                // rotate fingers (open)
                handControl.MoveFinger(0, "open");
                handControl.MoveFinger(1, "open");
                handControl.MoveFinger(2, "open");
                handControl.MoveFinger(3, "open");
                handControl.MoveFinger(4, "open");
                break;
            case 8:
                // adjust grip (open)
                handControl.AdjustGrip(-1);
                break;
            case 9:
                // adjust grip (close)
                handControl.AdjustGrip(1);
                break;
        }
    }

    void CacheState()
    {
        handControl.CacheState();

        // store object position & rotation
        original_obj_position = scene_obj.transform.position;
        original_obj_rotation = scene_obj.transform.rotation;
    }

    void ResetState()
    {
        scene_obj.GetComponent<Rigidbody>().isKinematic = true;

        // reset hand state
        handControl.ResetState();

        scene_obj.transform.position = original_obj_position;
        scene_obj.transform.rotation = original_obj_rotation;
        scene_obj.GetComponent<CollisionDetector>().ResetState();
        scene_obj.GetComponent<Rigidbody>().isKinematic = false;
    }

    IEnumerator Delay()
    {
        yield return new WaitForSecondsRealtime(delayTime);
        ResetState();
        loop = true;
    }

    IEnumerator Playback()
    {
        loop = false;

        // grasping first
        float cached_AnimationTime = AnimationTime;
        while(AnimationTime > 0 && !handControl.IsTerminal("grasp", scene_obj))
        {
            int hand_state = handControl.GetState();
            int action = grasp_policy[hand_state];
            Step(action);
            yield return null;
            AnimationTime -= Time.deltaTime;
        }

        // move
        Transform cached_parent = scene_obj.transform.parent;
        scene_obj.transform.parent = GameObject.Find("hand_root").transform;
        scene_obj.GetComponent<Rigidbody>().isKinematic = true;

        while (!handControl.positioned_over_target)
        {
            handControl.MoveOverTarget(0.05f);
            yield return null;
        }

        scene_obj.transform.parent = cached_parent;
        scene_obj.GetComponent<Rigidbody>().isKinematic = false;

        // release
        AnimationTime = cached_AnimationTime;
        while (AnimationTime > 0 && !scene_obj.GetComponent<CollisionDetector>().hit_target)
        {
            int hand_state = handControl.GetState();
            int action = release_policy[hand_state];
            Step(action);
            yield return null;
            AnimationTime -= Time.deltaTime;
        }

        AnimationTime = cached_AnimationTime;
        StartCoroutine("Delay");
    }

    Dictionary<int, int> LoadPolicy(string fileName)
    {
        Dictionary<int, int> loaded_policy = new Dictionary<int, int>();
        using (var reader = new StreamReader(fileName))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(',');
                loaded_policy.Add(int.Parse(values[0]), int.Parse(values[1]));
            }
        }

        return loaded_policy;
    }
}
