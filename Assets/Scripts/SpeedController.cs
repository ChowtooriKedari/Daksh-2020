using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SpeedController : MonoBehaviour
{

    public float normalSpeed = 15.0f;
    public float dropSpeed = 20.0f;
    public float climbUpSpeed = 10.0f;

    public float speedChangeAcceleration = 5.0f;

    public AudioClip fastAudioClip;
    public AudioClip normalAudioClip;
    public AudioClip slowAudioClip;

    private DoubleAudioSource doubleAudioSource;
    private const int NORMAL_SPEED_MODE = 1;
    private const int DROP_SPEED_MODE = 2;
    private const int CLIMB_UP_SPEED_MODE = 3;

    private int speedMode = NORMAL_SPEED_MODE;
    private int previousSpeedMode = NORMAL_SPEED_MODE;

    private bool changeSpeed = false;

    private SplineFollower splineFollower;

    public TextMesh s;

    // Use this for initialization
    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        doubleAudioSource = GetComponent<DoubleAudioSource>();
        if (doubleAudioSource != null && normalAudioClip != null)
        {
            doubleAudioSource.CrossFade(normalAudioClip, 1.0f, 2.0f);

        }

        

        splineFollower.speed = normalSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (changeSpeed)
        {
            if (previousSpeedMode == NORMAL_SPEED_MODE && speedMode == CLIMB_UP_SPEED_MODE)
            {
                splineFollower.speed -= (speedChangeAcceleration * Time.deltaTime);

                if (splineFollower.speed <= climbUpSpeed)
                {
                    print("Reached climb up speed");

                    changeSpeed = false;
                }
            }
            else if (previousSpeedMode == CLIMB_UP_SPEED_MODE && speedMode == NORMAL_SPEED_MODE)
            {
                splineFollower.speed += (speedChangeAcceleration * Time.deltaTime);

                if (splineFollower.speed >= normalSpeed)
                {
                    print("Reached normal speed");
                    changeSpeed = false;
                }
            }
            else if (previousSpeedMode == NORMAL_SPEED_MODE && speedMode == DROP_SPEED_MODE)
            {
                splineFollower.speed += (speedChangeAcceleration * Time.deltaTime);

                if (splineFollower.speed >= dropSpeed)
                {
                    print("Reached drop speed");
                    changeSpeed = false;
                }
            }
            else if (previousSpeedMode == DROP_SPEED_MODE && speedMode == NORMAL_SPEED_MODE)
            {
                splineFollower.speed -= (speedChangeAcceleration * Time.deltaTime);

                if (splineFollower.speed <= normalSpeed)
                {
                    print("Reached normal speed");
                    changeSpeed = false;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        print("OnTriggerEnter " + other.tag);
        previousSpeedMode = speedMode;

        if (other.CompareTag("Normal1"))
        {
            speedMode = NORMAL_SPEED_MODE;
            changeSpeed = true;
            print("Change to normal speed");
            SceneManager.LoadScene("SOLARSYSTEM");
            if (doubleAudioSource != null && normalAudioClip != null)
            {
                doubleAudioSource.CrossFade(normalAudioClip, 1.0f, 2.0f);
            }

        }
        else if (other.CompareTag("Drop"))
        {
            speedMode = DROP_SPEED_MODE;
            splineFollower.speed = 50.0f;
          changeSpeed = true;
            print("Change to drop speed");

            if (doubleAudioSource != null && fastAudioClip != null)
            {
                doubleAudioSource.CrossFade(fastAudioClip, 1.0f, 2.0f);
            }

        }
        else if (other.CompareTag("Slow"))
        {
            speedMode = DROP_SPEED_MODE;
            splineFollower.speed = 20.0f;
            changeSpeed = true;
 

        }
        else if (other.CompareTag("Medium"))
        {
            speedMode = DROP_SPEED_MODE;
            splineFollower.speed = 30.0f;
            changeSpeed = true;


        }
        else if (other.CompareTag("Fast"))
        {
            speedMode = DROP_SPEED_MODE;
            splineFollower.speed = 50.0f;
            changeSpeed = true;


        }

        else if (other.CompareTag("Final"))
        {
            speedMode = DROP_SPEED_MODE;
            splineFollower.speed = 15.0f;
            changeSpeed = true;
            if (doubleAudioSource != null && fastAudioClip != null)
            {
                doubleAudioSource.CrossFade(fastAudioClip, 1.0f, 2.0f);
            }


        }
        else if (other.CompareTag("End"))
        {
 //           speedMode = DROP_SPEED_MODE;
   //         splineFollower.speed = 20.0f;
     //       changeSpeed = true;
            SceneManager.LoadScene("END");
        }
        else if (other.CompareTag("Moon"))
        {
            SceneManager.LoadScene("Earth");
            if (doubleAudioSource != null && fastAudioClip != null)
            {
                doubleAudioSource.CrossFade(fastAudioClip, 1.0f, 2.0f);
            }

        }
        else if (other.CompareTag("Climb up"))
        {
            speedMode = CLIMB_UP_SPEED_MODE;
            changeSpeed = true;
            print("Change to climb up speed");
            //s.text = "Collided With Climb up";
            //print(s.text);
            // PrintWarning("Not enough money!");
            if (doubleAudioSource != null && slowAudioClip != null)
            {
                doubleAudioSource.CrossFade(slowAudioClip, 1.0f, 2.0f);

            }

        }

    }
    private void OnTriggerExit(Collider other)
    {
        print("OnTriggerExit");
    }
}