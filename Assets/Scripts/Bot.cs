﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bot : MonoBehaviour
{
    public Transform followPos;
    public Transform lookPos;
    public List<GameObject> detectedCheckPoints = new List<GameObject>();

    public LayerMask raycastMaskWalls;

    public Vector3 rayStartPosition = new Vector3(0, 0, 0);
    public float startProtection = 1.0f;
    public float rayDistance = 20.0f;
    public bool drawRays = false;

    public static int[] layers;
    public static float[] input;
    public NeuralNetwork network;

    public float fitness;
    public bool stop = false;

    public static int radiusRay;
    private float timeOldRotateWheels = 0;
    private float oldRotateWheels = 0;

    public static void Awake(int rRay)
    {
        radiusRay = rRay;
        layers = new int[3] { 360/radiusRay, Mathf.RoundToInt(360/radiusRay*15/18), 2 };
        input = new float[layers[0]];
    }

    private void Start()
    {
        transform.parent = null;
    }

    private void FixedUpdate()
    {
        if(!stop)
        {
            for (int i = 0; i < 360/radiusRay; i++)
            {
                Vector3 newVector = Quaternion.AngleAxis(i * radiusRay, new Vector3(0, 1, 0)) * transform.right;
                RaycastHit hit;
                Ray Ray = new Ray(transform.position + rayStartPosition, newVector);

                if(drawRays) Debug.DrawRay(transform.position + rayStartPosition, newVector * rayDistance, Color.green);

                if (Physics.Raycast(Ray, out hit, rayDistance, raycastMaskWalls))
                {
                    if (hit.collider.gameObject != this.gameObject)
                    {
                        input[i] = (rayDistance - hit.distance) / rayDistance;
                    }
                }
                else
                {
                    input[i] = 0;
                }
            }
            if (GetComponent<Rigidbody>().velocity.magnitude < 0.1f) fitness -= 0.01f;

            if (transform.position.y <= 50)
            {
                fitness -= 5.0f;
                stop = true;
            }

            float[] output = network.FeedForward(input);

            GetComponent<RearWheelDrive>().horizontalAxis = output[0];
            GetComponent<RearWheelDrive>().verticalAxis = output[1];

            if(output[0] != oldRotateWheels && Time.time - timeOldRotateWheels <= 0.5f)
            {
                fitness -= 0.01f;

                oldRotateWheels = output[0];
                timeOldRotateWheels = Time.time;
            }
        }
        else
        {
            GetComponent<RearWheelDrive>().horizontalAxis = 0;
            GetComponent<RearWheelDrive>().verticalAxis = 0;
        }

        if(startProtection > 0)
        {
            stop = false;
            fitness = 0;
            startProtection -= Time.deltaTime;
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Default") && !stop) 
        {
            fitness -= 10.0f;
            stop = true;
        }
        if(collision.gameObject.layer == LayerMask.NameToLayer("Win") && !stop) 
        {
            fitness += 10.0f;
            stop = true;
        }
        if(collision.gameObject.layer == LayerMask.NameToLayer("CarsColliders") && collision.gameObject != this.gameObject && !stop)
        {
            fitness -= 10.0f;
            stop = true;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if(collider.gameObject.layer == LayerMask.NameToLayer("Default") && !stop && Manager.training) 
        {
            fitness -= 10.0f;
            stop = true;
        }
        if(collider.gameObject.layer == LayerMask.NameToLayer("CheckPoint") && !stop)
        {
            bool isCollided = false;

            foreach (GameObject checkPoint in detectedCheckPoints)
            {
                if (checkPoint == collider.gameObject) isCollided = true;
            }
            if(!isCollided)
            {
                fitness += 1f;
                if(detectedCheckPoints.Count < 2) detectedCheckPoints.Add(collider.gameObject);
                else
                {
                    detectedCheckPoints[1] = detectedCheckPoints[0];
                    detectedCheckPoints[0] = collider.gameObject;
                }
            }
        }
    }


    public void UpdateFitness()
    {
        network.fitness = fitness;
    }
}
