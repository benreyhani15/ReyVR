using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Diagnostics;

public class MovementStateMachine {
    public static int BCI_OFF = -2;
    public static int RIGHT = 1;
    public static int FORWARD = 2;
    public static int LEFT = 3;
    public static int REST = 4;
    public static int END_OF_MAZE = 5;

    public bool isBCIOn;
    private PlayerMovementView view;
    private System.Random randomNumGen;

    // read from EEG computer
    private int eegClassificationDecision;

    private static bool GET_KEYBOARD_SIM = true;

    private bool isOculusExcessivelyMoving;
    private OculusMovementDetector oculusMovementDetector;
    public Networker networker;

    public Stopwatch artStopWatch;

    public static float ART_FEEDBACK_CHANGE_INTERVAL = 2000f;

    public MovementStateMachine(PlayerMovementView pmw, Transform cameraTransform) {
        view = pmw;
        eegClassificationDecision = REST;
        isBCIOn = true;

        oculusMovementDetector = new OculusMovementDetector(cameraTransform);
        oculusMovementDetector.startDetector();

        networker = new Networker();
        networker.startListening();

        artStopWatch = new Stopwatch();
        randomNumGen = new System.Random();
    }

    private void readEEGBuffer(){
        int newClassificationResult = eegClassificationDecision;
        //Simulating for now keyboard press
        if (GET_KEYBOARD_SIM)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) newClassificationResult = FORWARD;
            if (Input.GetKeyDown(KeyCode.DownArrow)) newClassificationResult = REST;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) newClassificationResult = LEFT;
            if (Input.GetKeyDown(KeyCode.RightArrow)) newClassificationResult = RIGHT;
        }        

        if (eegClassificationDecision != newClassificationResult){
            // TODO: Log change in classification decision?
            UnityEngine.Debug.Log("Change in classification to: " + newClassificationResult);
            eegClassificationDecision = newClassificationResult;
        }
    }

    public void updateStateMachine() {
        if (GET_KEYBOARD_SIM)
        {
            readEEGBuffer();
        }  else  {
            eegClassificationDecision = networker.getData();
        }
    }

    public void updateArtFBStateMachine(bool forceUpdate) {
        if (!isBCIOn) return;
        if (view.taskCompleted) {
            view.logLabels(eegClassificationDecision);
            view.logger.queueMarkers(Math.Abs(eegClassificationDecision));
            artStopWatch.Reset();
        } else if (forceUpdate || artStopWatch.ElapsedMilliseconds >= ART_FEEDBACK_CHANGE_INTERVAL) {
            int value = randomNumGen.Next(1, 101);
            int oldDecision = eegClassificationDecision;
            eegClassificationDecision = value <= view.PERCENT_ART_CORRECT ? view.currentMovementLabel : -view.currentMovementLabel;
            if (!forceUpdate)
            {
                view.logLabels(oldDecision);
                view.logger.queueMarkers(Math.Abs(oldDecision));
            }
            artStopWatch.Reset();
            artStopWatch.Start();
        }
    }

    public void resetAndStartOculusDetector() {
        oculusMovementDetector.resetAndStopDetector();
        oculusMovementDetector.startDetector();
    }
    public void updateOculusExcessivelyMoving()
    {
        bool newValue = oculusMovementDetector.updateDetector();
        if (newValue != isOculusExcessivelyMoving) {
            isOculusExcessivelyMoving = newValue;
            if (isOculusExcessivelyMoving) view.logMarkers(PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON);
            //view.logMarkers(isOculusExcessivelyMoving ? PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON : PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF);
        }

        //if (isOculusExcessivelyMoving) view.temporarilyTurnBCIOff(PlayerMovementView.BCI_OFF_TIME_AFTER_OCULUS_MOVEMENT);
    }

    public int getClassificationDecision() {
        return isBCIOn ? eegClassificationDecision : BCI_OFF;
    }

    public bool isExcessiveBodyMovement() {
        // TODO: Add EOG AND MMG to decision?
        return isOculusExcessivelyMoving;
    }
}
