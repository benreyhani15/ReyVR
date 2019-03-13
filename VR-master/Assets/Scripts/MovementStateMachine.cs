﻿using System.Collections;
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

    private static bool GET_KEYBOARD_SIM = false;

    private bool isOculusExcessivelyMoving;
    private OculusMovementDetector oculusMovementDetector;
    public Networker networker;

    private Stopwatch artStopWatch;

    public static float ART_FEEDBACK_CHANGE_INTERVAL = 500f;

    public MovementStateMachine(PlayerMovementView pmw, Transform cameraTransform) {
        view = pmw;
        UnityEngine.Debug.Log("Made MSM");
        eegClassificationDecision = REST;
        isBCIOn = true;
        oculusMovementDetector = new OculusMovementDetector(cameraTransform);
        oculusMovementDetector.startDetector();
        isOculusExcessivelyMoving = false;
        networker = new Networker();
        networker.startListening();
        artStopWatch = new Stopwatch();
        artStopWatch.Start();
        randomNumGen = new System.Random();
    }

    private void readEEGBuffer(){
        UnityEngine.Debug.Log("Updating MovementStateMachine");
        // TODO: Read from buffer written by EEG computer connected PP/USB
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
        } else if (view.IS_ART_FEEDBACK) {
            int value = randomNumGen.Next(1, 101);
            if (artStopWatch.ElapsedMilliseconds >= ART_FEEDBACK_CHANGE_INTERVAL) {
                eegClassificationDecision = value <= view.PERCENT_ART_CORRECT ? view.currentMovementLabel : REST;
                artStopWatch.Reset();
                artStopWatch.Start();
            }
        } else  {
            eegClassificationDecision = networker.getData();
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
            view.logMarkers(isOculusExcessivelyMoving ? PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON : PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF);
        }

        if (isOculusExcessivelyMoving) view.temporarilyTurnBCIOff(PlayerMovementView.BCI_OFF_TIME_AFTER_OCULUS_MOVEMENT);
    }

    public int getClassificationDecision() {
        return isBCIOn ? eegClassificationDecision : BCI_OFF;
    }

    public bool isExcessiveBodyMovement() {
        // TODO: Add EOG AND MMG to decision?
        return isOculusExcessivelyMoving;
    }
}
