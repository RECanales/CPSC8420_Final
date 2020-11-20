using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class QLearning : MonoBehaviour
{
    public GameObject controller;
    public GameObject rewardDisplay;
    public GameObject scene_obj; // object being interacted with
    public enum WhichPolicy { Grasping, Releasing }; // pick which policy is being learned
    public WhichPolicy PolicyType = WhichPolicy.Grasping;
    
    Controller handControl;
    Vector3 original_obj_position;
    Quaternion original_obj_rotation;

    int NUM_STATES = 100; // discretized hand position (2d 10x10 = 100 position grid for now) and finger rotation (0 - 60, @ rotate speed)
    public int NUM_ACTIONS = 4; // move hand in some direction, rotate fingers in
    public float gamma = 0.95f; // discount factor
    public float eps = 1.0f;    // epsilon-greedy parameter
    public float alpha = 0.1f;  // learning rate
    public int max_iterations = 1000;
    public int iteration_number = 0;
    public int episode_loop = 0;

    List<List<float>> Q = new List<List<float>>();  // Q values
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy
    Dictionary<int, float> logReward = new Dictionary<int, float>(); // logger
    Dictionary<int, float> logEpisode = new Dictionary<int, float>(); // logger

    bool training_complete = false;
    bool start_training = false;
    bool stop_animation = false;

    float total_iterations = 0;
    float total_reward = 0;
    float initial_dist;
    float DelayTime = 1f; // delay between when animation stops and starts again
    float AnimationTime = 4f; // playback for 4 seconds

    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        NUM_STATES = handControl.max_height * handControl.num_grips * (int)(handControl.max_joint_rotation / handControl.rotate_speed);
        print("Number of states = " + NUM_STATES.ToString());

        // init all Q values to 0
        for(int i = 0; i < NUM_STATES; ++i)
        {
            List<float> action_list = new List<float>();
            for (int j = 0; j < NUM_ACTIONS; ++j)
                action_list.Add(0);
            Q.Add(action_list);
            pi.Add(i, 0);
        }

        scene_obj.AddComponent<CollisionDetector>();
        Time.fixedDeltaTime /= 5; // faster physics

        // store object position & rotation
        original_obj_position = scene_obj.transform.position;
        original_obj_rotation = scene_obj.transform.rotation;
    }

    void Step(int action)
    {
        switch (action)
        {
            case 0:
                // lift / lower hand
                string move_direction = PolicyType == WhichPolicy.Grasping ? "up" : "down";
                handControl.MoveHand(move_direction);
                break;
            case 1:
                // rotate fingers
                string rotate_direction = PolicyType == WhichPolicy.Grasping ? "close" : "open";
                handControl.MoveFinger(0, rotate_direction);
                handControl.MoveFinger(1, rotate_direction);
                handControl.MoveFinger(2, rotate_direction);
                handControl.MoveFinger(3, rotate_direction);
                handControl.MoveFinger(4, rotate_direction);
                break;
            case 2:
                // adjust grip
                handControl.AdjustGrip(-1);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!start_training && handControl.ready) // makes sure hand is intialized before training
        {
            handControl.CacheState();
            handControl.ResetState();
            initial_dist = Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand());
            start_training = true;
            StartCoroutine("PhysicsQLearn");
        }

        else
        {
            if (iteration_number < max_iterations)
            {
                // compute average reward & display on screen
                float avg_reward = total_reward / (float)total_iterations;
                rewardDisplay.GetComponent<Text>().text = "Average reward: " + avg_reward.ToString("F3");
            }

            else
            {
                if (!training_complete)
                {
                    print("Training complete.");
                    ResetState();
                    StopCoroutine("PhysicsQLearn");

                    // RC: I made a function to clean this up
                    WriteCSV("Grasping_Policy.csv", pi); // write policy
                    WriteCSV("EpisodeLog.csv", logEpisode);
                    WriteCSV("RewardLog.csv", logReward);
                    print("Learned Policy has been written to Grasping_Policy.csv file.");
                    print("Episode and reward values have been logged.");

                    training_complete = true;
                }

                if (!stop_animation)
                {
                    int hand_state = handControl.GetState();
                    int action = pi[hand_state];
                    Step(action);
                    AnimationTime -= Time.deltaTime;
                }

                if (AnimationTime <= 0)
                {
                    stop_animation = true;
                    AnimationTime = 4;
                    print("Playback complete. Looping...");
                    StartCoroutine("Delay");
                }
            }
        }
    }

    IEnumerator Delay()
    {
        yield return new WaitForSeconds(DelayTime);
        ResetState();
        print("State reset to: " + handControl.GetState().ToString());
        stop_animation = false;
    }

    IEnumerator PhysicsQLearn()
    {
        for (int i = 0; i < max_iterations; ++i)
        {
            ResetState();
            int s = handControl.GetState();
            episode_loop = 0;
            while (true)
            {
                
                Debug.Assert(s < Q.Count);
                int action = Random.Range(0, NUM_ACTIONS); // random action
                float random_val = Random.value;

                if (random_val > eps)
                    action = GetMaxIndex(Q[s]);

                Debug.Assert(action < Q[0].Count);

                Step(action);
                int next_state = handControl.GetState();

                string _policy = PolicyType == WhichPolicy.Grasping ? "grasp" : "release";
                bool terminal = handControl.IsTerminal(_policy);

                // make sure physics updates before getting reward
                yield return new WaitForFixedUpdate();

                float r = Reward();
                float target = r;
                if (!terminal)
                {
                    int next_action = Random.Range(0, NUM_ACTIONS);
                    if (random_val > eps)
                        next_action = GetMaxIndex(Q[s]);

                    Debug.Assert(next_state < Q.Count);
                    target = r + gamma * Q[next_state][next_action];
                }

                Q[s][action] = (1 - alpha) * Q[s][action] + alpha * target;

                Debug.Assert(s < pi.Keys.Count);
                pi[s] = GetMaxIndex(Q[s]);
                s = next_state;

                episode_loop++;
                total_reward += r;
                total_iterations++;

                if (terminal || episode_loop > 1000) // prevent infinite loop
                    break;
            }

            iteration_number = i;
            logEpisode.Add(iteration_number, episode_loop);
            logReward.Add(iteration_number, total_reward);
            eps = Mathf.Max(0.1f, (1 - (float)i / (float)(max_iterations)));
        }

        iteration_number = max_iterations;
        print("Total iterations: " + total_iterations.ToString());
    }

    int GetMaxIndex(List<float> l)
    {
        int max_idx = 0;
        float max_val = l[0];
        for(int i = 1; i < l.Count; ++i)
        {
            if (l[i] > max_val)
            {
                max_idx = i;
                max_val = l[i];
            }
        }
        Debug.Assert(max_idx < NUM_ACTIONS);
        return max_idx;
    }

    float Reward()
    {
        // distance of ball from center of hand
        float dist = Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand());
        //float fingertip_dist = handControl.GetAvgDistFromBall();
        //float dist = handControl.GetAvgDistFromBall();
        if (scene_obj.transform.position.y <= original_obj_position.y)
            return -1;

        float _reward = (scene_obj.transform.position.y / original_obj_position.y);
        return _reward;
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

    void WriteCSV<T>(string fileName, Dictionary<int, T> data)
    {
        using (var writer = new StreamWriter(fileName))
        {
            foreach (var pair in data)
            {
                writer.WriteLine("{0},{1},", pair.Key, pair.Value);
            }
        }
    }
}
