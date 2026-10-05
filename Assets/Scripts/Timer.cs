using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 10;
    public bool timerIsRunning = false;
    public Text timeText;
    Color readyColor;
    public Color notReadyColor = Color.white;

    public bool initEachEnable = false;
    DateTime originalDateTime;
    Text originalTextComponent;
    Color originalColor;
    public bool emptyisCustom = false;
    public string customEmptyString = "Empty";
    private void OnEnable()
    {
        //test data
        //DateTime future = DateTime.Now.AddMinutes(1);
        //InitializeTimer(future);
        if (initEachEnable && timerIsRunning)
        {
            InitializeTimerByDT(originalDateTime, originalTextComponent, originalColor);
        }

        if (emptyisCustom)
        {
            timeText.text = customEmptyString;
        }
    }

    #region Init by all different ways, making it easy to use
    public void InitializeTimerByDT(DateTime endTime, Text incomingTextComponent, Color _readyColor)
    {
        //store original so we can bring it back up
        originalDateTime = endTime;
        originalTextComponent = incomingTextComponent;
        originalColor = _readyColor;

        readyColor = _readyColor;
        timeText = incomingTextComponent;
        DateTime startTime = DateTime.Now;
        TimeSpan span = endTime.Subtract(startTime);
        timeRemaining = (int)span.TotalSeconds;
        // Starts the timer automatically
        timerIsRunning = true;
    }

    public void InitializeTimerByMinutes(int amt, Text incomingTextComponent)
    {
        timeText = incomingTextComponent;
        DateTime startTime = DateTime.Now;
        DateTime endTime = DateTime.Now.AddMinutes(amt);
        TimeSpan span = endTime.Subtract(startTime);
        timeRemaining = (int)span.TotalSeconds;
        // Starts the timer automatically
        timerIsRunning = true;
    }

    public void InitializeTimerBySeconds(int amt, Text incomingTextComponent)
    {
        timeText = incomingTextComponent;
        DateTime startTime = DateTime.Now;
        DateTime endTime = DateTime.Now.AddSeconds(amt);
        TimeSpan span = endTime.Subtract(startTime);
        timeRemaining = (int)span.TotalSeconds;
        // Starts the timer automatically
        timerIsRunning = true;
    }
    #endregion

    void FixedUpdate()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
                timeText.color = notReadyColor;
            }
            else
            {
                //Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;

                if (emptyisCustom)
                {
                    timeText.text = customEmptyString;
                }
                else
                {
                    timeText.text = "Ready!";
                    timeText.color = readyColor;
                }
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public bool IsReady()
    {
        return !timerIsRunning;//if timers running, it isn't ready
    }
}