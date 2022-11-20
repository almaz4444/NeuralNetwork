﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Cinemachine;

public class Manager : MonoBehaviour
{
    public const string BASE_BOT_NN = "BASE_BOT_NN";
        
    public bool training;
    public float timeFrame = 20;
    public int populationSize;
    public GameObject[] prefabs;
    public Transform[] spawnPoints;
    public CinemachineVirtualCamera vCam;
    public Toggle traningToggle;

    public int[] layers = new int[3] { 5, 3, 2 };

    [Range(0.0001f, 1f)] public float MutationChance = 0.01f;

    [Range(0f, 1f)] public float MutationStrength = 0.5f;

    [Range(0.1f, 50f)] public float Gamespeed = 1f;

    public List<NeuralNetwork> networks = new List<NeuralNetwork>(8);
    public List<Bot> bots = new List<Bot>(8);
    private int followingBot = 0;

    private void Start()
    {
        InitNetworks();
        if (training) InvokeRepeating("CreateBots", 0.1f, timeFrame);
        else CreateBots();
    }

    private void Update()
    {
        training = traningToggle.isOn;
        if(training == false && bots != null)
        {
            for (int i = 0; i < bots.Count; i++)
            {
                if(bots[i].stop)
                {
                    GameObject.Destroy(bots[i].gameObject);

                    Bot bot = (Instantiate(prefabs[i], spawnPoints[i])).GetComponent<Bot>();
                    bot.network = networks[i];
                    bots[i] = bot;
                    
                    vCam.Follow = bots[followingBot].transform;
                    vCam.LookAt = bots[followingBot].transform;
                }
            }
        }
    }

    public void InitNetworks()
    {
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers);
            // net.Load("Assets/Save.txt");
            net.Load(BASE_BOT_NN);
            networks[i] = net;
        }
    }

    public void CreateBots()
    {
        Time.timeScale = Gamespeed;
        
        if (bots[0] != null)
        {
            for (int i = 0; i < bots.Count; i++)
            {
                GameObject.Destroy(bots[i].gameObject);
                bots[i] = null;
            }

            SortNetworks();
        }
        
        for (int i = 0; i < populationSize; i++)
        {
            Bot bot = (Instantiate(prefabs[i], spawnPoints[i])).GetComponent<Bot>();
            bot.network = networks[i];
            bots[i] = bot;
        }

        vCam.Follow = bots[followingBot].transform;
        vCam.LookAt = bots[followingBot].transform;
    }

    public void SortNetworks()
    {
        for (int i = 0; i < populationSize; i++)
        {
            bots[i].UpdateFitness();
        }
        networks.Sort();
        networks[populationSize - 1].Save("Assets/Save.txt");
        for (int i = 0; i < populationSize / 2; i++)
        {
            networks[i] = networks[i + populationSize / 2].copy(new NeuralNetwork(layers));
            networks[i].Mutate((int)(1/MutationChance), MutationStrength);
        }
    }

    public void FollowBot(int where)
    {
        if (followingBot < populationSize - 1 && where > 0) followingBot += where;
        else followingBot = 0;
        
        vCam.Follow = bots[followingBot].transform;
        vCam.LookAt = bots[followingBot].transform;
    }
}
