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
    WhichPolicy CurrentPolicy = WhichPolicy.Grasping;
    
    Controller handControl;
    Vector3 original_obj_position;
    Quaternion original_obj_rotation;

    int NUM_STATES = 100; // discretized hand position (2d 10x10 = 100 position grid for now) and finger rotation (0 - 60, @ rotate speed)
    public int NUM_ACTIONS = 10; // move hand in some direction, rotate fingers in
    public float gamma = 0.95f; // discount factor
    public float eps = 1.0f;    // epsilon-greedy parameter
    public float alpha = 0.1f;  // learning rate
    public int max_iterations = 1000;
    public int max_episode_steps = 100;
    public int iteration_number = 0;
    public int episode_loop = 0;

    List<List<float>> Q = new List<List<float>>();  // Q values
    Dictionary<int, int> pi = new Dictionary<int, int>(); // policy
    Dictionary<int, float> logReward = new Dictionary<int, float>(); // logger
    Dictionary<int, float> logEpisode = new Dictionary<int, float>(); // logger

    bool training_complete = false;
    bool start_training = false;
    bool stop_animation = false;
    bool follow_policy = false;
    bool train_release = false;

    float total_iterations = 0;
    float total_reward = 0;
    float initial_dist;
    float DelayTime = 1f; // delay between when animation stops and starts again
    public float AnimationTime = 4f; // playback for 4 seconds
    float waitTime = 1f;
    bool waitComplete = false;
    bool startPlayback = false;

    void Start()
    {
        handControl = controller.GetComponent<Controller>();
        //NUM_STATES = handControl.max_height * handControl.num_grips * (int)(handControl.max_joint_rotation / handControl.rotate_speed);
        NUM_STATES = handControl.max_height * handControl.num_grips * handControl.max_horizontal_travel *
            handControl.max_horizontal_travel * (int)(handControl.max_joint_rotation / handControl.rotate_speed);
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
        Time.fixedDeltaTime /= 10; // faster physics
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

    IEnumerator Delay()
    {
        waitComplete = false;
        float CachedTime = waitTime;
        while (waitTime > 0)
        {
            waitTime -= Time.deltaTime;
            yield return null;
        }

        waitTime = CachedTime;
        waitComplete = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (handControl.ready && !training_complete) // wait until hand is done following grasping policy and moved over target
        {
            if (PolicyType == WhichPolicy.Releasing && !follow_policy)
            {
                handControl.CacheState();
                handControl.ResetState();
                follow_policy = true;
                Dictionary<int, int> GraspingPolicy = LoadPolicy("Grasping_Policy.csv");
                StartCoroutine("FollowPolicy", GraspingPolicy);
                return;
            }

            if (!start_training && (PolicyType == WhichPolicy.Grasping || train_release)) // makes sure hand is intialized before training
            {
                handControl.CacheState();
                handControl.ResetState();
                if (PolicyType == WhichPolicy.Grasping)
                    initial_dist = Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand());
                else
                    initial_dist = handControl.TargetObjDist(scene_obj);

                // store object position & rotation
                original_obj_position = scene_obj.transform.position;
                original_obj_rotation = scene_obj.transform.rotation;

                start_training = true;
                print("State cached. Training...");
                StartCoroutine("PhysicsQLearn");
            }

            else if (start_training)
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
                        string policyName = PolicyType == WhichPolicy.Grasping ? "Grasping_Policy.csv" : "Release_Policy.csv";
                        WriteCSV(policyName, pi); // write policy
                        string episodeName = PolicyType == WhichPolicy.Grasping ? "Grasp_EpisodeLog.csv" : "Release_EpisodeLog.csv";
                        WriteCSV(episodeName, logEpisode);
                        string rewardName = PolicyType == WhichPolicy.Grasping ? "Grasp_RewardLog.csv" : "Release_RewardLog.csv";
                        WriteCSV(rewardName, logReward);
                        print("Learned Policy has been written to " + policyName);
                        print("Episode and reward values have been logged.");

                        training_complete = true;
                        //StartCoroutine
                    }
                }
            }
        }

        if (training_complete && !stop_animation)
        {
            ResetState();
            StartCoroutine("FollowPolicy", pi); // follow learned policy, loop
        }
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

                int cachedState = handControl.GetState();
                Step(action);
                int next_state = handControl.GetState();

                string _policy = PolicyType == WhichPolicy.Grasping ? "grasp" : "release";
                bool terminal = handControl.IsTerminal(_policy);

                // make sure physics updates before getting reward
                if(PolicyType == WhichPolicy.Grasping)
                    yield return new WaitForFixedUpdate();

                if(PolicyType == WhichPolicy.Releasing && action == 7)
                {
                    StartCoroutine("Delay");
                    yield return new WaitUntil(() => waitComplete || scene_obj.GetComponent<CollisionDetector>().terminalCollision);
                }

                //if (terminal)
                    //break;
                //float r = Reward();
                float r = 0;
                if (next_state == cachedState)
                    r = -1;
                else
                    r = Reward();

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

                if (terminal || episode_loop > max_episode_steps)
                    break;
            }

            iteration_number = i;
            logEpisode.Add(iteration_number, episode_loop);
            logReward.Add(iteration_number, total_reward/(float)total_iterations);
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
        float _reward = 0;
        if (PolicyType == WhichPolicy.Grasping)
        {
            if (scene_obj.transform.position.y <= original_obj_position.y /*|| Vector3.Magnitude(scene_obj.transform.position - handControl.GetCenterOfHand()) >= initial_dist*/)
                _reward = -1;
            else
                _reward = (scene_obj.transform.position.y / original_obj_position.y);
        }

        else
        {
            float dist = handControl.TargetObjDist(scene_obj);
            if (dist >= initial_dist)
                _reward = -1;
            else
                _reward = initial_dist / ((dist*dist + 1));
        }

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

    Dictionary<int,int> LoadPolicy(string fileName)
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

    IEnumerator FollowPolicy(Dictionary<int, int> policy)
    {
        print("Following policy...");
        stop_animation = true;
        float cached_AnimationTime = AnimationTime;
        while (AnimationTime > 0)
        {
            int hand_state = handControl.GetState();
            int action = policy[hand_state];
            Step(action);
            yield return null;
            AnimationTime -= Time.deltaTime;
        }

        AnimationTime = cached_AnimationTime;
        stop_animation = false;
        if (!training_complete)
            StartCoroutine("MoveHand");
    }

    IEnumerator MoveHand()
    {
        print("Moving hand...");
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
        train_release = true;
        print("Done. Ready to train.");
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
