using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FollowPolicy: MonoBehaviour 
{
    public GameObject controller;
    Controller handControl;
    public GameObject scene_obj;
    bool stop_animation = false;
    bool done = false;
    Dictionary<int, int> policy = new Dictionary<int, int>(); // policy

    // Start is called before the first frame update
    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        using (var reader = new StreamReader("Grasping_Policy.csv"))
        {
          while (!reader.EndOfStream)
          {
                var line = reader.ReadLine();
                if (line == null) continue;
                var values = line.Split(',');
                policy.Add(int.Parse(values[0]), int.Parse(values[1]));
          }
        }
    }

    void Step(int action)
    {
        switch (action)
        {
            case 0:
                handControl.MoveHand("right");
                break;
            case 1:
                handControl.MoveHand("left");
                break;
            case 2:
                handControl.MoveHand("forward");
                break;
            case 3:
                handControl.MoveHand("backward");
                break;
            case 4:
                // close
                handControl.MoveFinger(0, "close");
                handControl.MoveFinger(1, "close");
                handControl.MoveFinger(2, "close");
                handControl.MoveFinger(3, "close");
                handControl.MoveFinger(4, "close");
                break;
            case 5:
                // adjust grip open
                handControl.AdjustGrip(1);
                break;
            case 6:
                handControl.AdjustGrip(-1);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!done)
        {
            print("DONE");
            handControl.ResetState();
            print(handControl.GetState());
            scene_obj.GetComponent<Rigidbody>().isKinematic = false;
        }
        done = true;
        if (!stop_animation)
        {
            int hand_state = handControl.GetState();
            int action = policy[hand_state];
            //print($"State {hand_state} Action {action}");
            //stored_states.Add(hand_state);
            Step(action);
        }

        if (handControl.IsTerminal() && !stop_animation)
        {
            stop_animation = true;
            //StartCoroutine("timer");
            //stop_animation = false; // loop animation
        }
    }

}
