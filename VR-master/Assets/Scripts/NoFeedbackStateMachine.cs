﻿using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class NoFeedbackStateMachine
{

    private TrialState currentTrialState;
    private NoFeedbackView ui;
    private TaskController taskController;
    private Time sessionStartTime;
    private bool sessionStarted;
    public bool isLocked;
    private bool firstUpdate;
    public BCILogger logger;
    private Stopwatch stopWatch;

    public static bool IS_DEBUG_MODE = true;

    public static int START_OF_RIGHT_TASK = 1;
    public static int START_OF_FORWARD_TASK = 2;
    public static int START_OF_LEFT_TASK = 3;
    public static int START_OF_REST_TASK = 4;
    public static int START_OF_TRIAL = 5;
    public static int END_OF_TRIAL = 6;
    public static int START_OF_RUN = 7;
    public static int END_OF_RUN = 8; // Coincides with next START_OF_RUN (not written)
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON = 9;
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF = 10;

    private OculusMovementDetector oculusMovementDetector;
    private bool isOculusExcessivelyMoving;

    private bool isTrialDone;

    // Each session consists of PrepCross --> Icon --> BlankScreen
    public enum TrialState
    {
        PrepCross,
        Icon,
        BlankScreen
    };

    public NoFeedbackStateMachine(NoFeedbackView view, string participantName, int trialNumber, Transform eyeCamera)
    {
        ui = view;
        taskController = new TaskController();
        string date = System.DateTime.Now.ToString("MM-dd-yyyy");
        logger = new BCILogger(participantName, "Calibration", trialNumber, false);
        firstUpdate = true;
        oculusMovementDetector = new OculusMovementDetector(eyeCamera);
        isOculusExcessivelyMoving = false;
    }

    public bool isSessionStarted()
    {
        return sessionStarted;
    }

    public void update()
    {
        if (isTrialDone) return;
        // Update state machine and update UI 
        updateState();

        if (currentTrialState == TrialState.PrepCross &&
            taskController.taskCount == Constants.NUMBER_OF_CLASSES * Constants.TRIALS_PER_CLASS_PER_SESSION)
        {
            // Session is over
            logger.logMarkers(END_OF_TRIAL, stopWatch.ElapsedMilliseconds.ToString());

            stopWatch.Reset();
            isTrialDone = true;
            ui.sessionOver();
        }
        else
        {
            // Lock state machine so frame updates do nothing in between
            launchCoroutine();
            updateUI();
            isLocked = true;
            if (firstUpdate) firstUpdate = false;
        }
    }

    private void updateUI()
    {
        UnityEngine.Debug.Log(logger);
        // Only related to showing/removing task cues
        if (currentTrialState == TrialState.BlankScreen)
        {
            ui.arrow.SetActive(false);
            ui.relax.SetActive(false);
            ui.cross.SetActive(false);
            logger.logMarkers(START_OF_REST_TASK, stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log("Blank Screen");
        }
        else if (currentTrialState == TrialState.Icon)
        {
            TaskController.TrialTask task = taskController.generateTaskRandomly();
            ui.cross.SetActive(false);
            ui.setTaskIcon(task);
            logger.logMarkers(((int)task) + 1, stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log(task.ToString());
        }
        else
        {
            ui.arrow.SetActive(false);
            ui.relax.SetActive(false);
            ui.cross.SetActive(true);
            logger.logMarkers(START_OF_RUN, stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log("Cross");
        }
    }

    public void startSession()
    {
        sessionStarted = true;
        stopWatch = new Stopwatch();
        stopWatch.Start();
        logger.logMarkers(START_OF_TRIAL, "0");
        oculusMovementDetector.startDetector();
    }

    private void launchCoroutine()
    {
        float secondsIdle = 0f;
        if (currentTrialState == TrialState.BlankScreen) secondsIdle = firstUpdate ? Constants.INITIAL_DELAY_SECONDS : Constants.BLANK_SCREEN_SECONDS;
        if (currentTrialState == TrialState.Icon) secondsIdle = Constants.IMAGERY_SECONDS;
        if (currentTrialState == TrialState.PrepCross) secondsIdle = Constants.PREP_CROSS_SECONDS;
        IEnumerator coroutine = StayIdle(secondsIdle);
        ui.startCoroutine(coroutine);
    }

    private void updateState()
    {
        if (firstUpdate)
        {
            currentTrialState = TrialState.BlankScreen;
        }
        else
        {
            int newStateIndex = ((int)currentTrialState + 1) % 3;
            currentTrialState = (TrialState)newStateIndex;
        }
    }

    public void updateExternalBuffers()
    {
        readOculusExcessivelyMoving();
        //readArtifactBuffer();
    }

    IEnumerator StayIdle(float secondsIdle)
    {
        yield return new WaitForSeconds(secondsIdle);
        isLocked = false;
    }

    private void readOculusExcessivelyMoving()
    {
        bool newValue = oculusMovementDetector.updateDetector();
        if (newValue != isOculusExcessivelyMoving)
        {
            isOculusExcessivelyMoving = newValue;
            logger.logMarkers(isOculusExcessivelyMoving ? PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON : PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF
              , stopWatch.ElapsedMilliseconds.ToString());
        }
    }

    public bool isExcessiveBodyMovement()
    {
        return isOculusExcessivelyMoving;
    }
}
 