using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class MovementStateMachine {
    public static int BCI_OFF = -2;
    public static int RIGHT = 1;
    public static int FORWARD = 2;
    public static int LEFT = 3;
    public static int REST = 4;
    public static int END_OF_MAZE = 5;

    public bool isBCIOn;
    private PlayerMovementView view;

    // read from EEG computer
    private int eegClassificationDecision;

    private bool isOculusExcessivelyMoving;
    private OculusMovementDetector oculusMovementDetector;
    public Networker networker;

    public MovementStateMachine(PlayerMovementView pmw, Transform cameraTransform) {
        view = pmw;
        UnityEngine.Debug.Log("Made MSM");
        Debug.Log("Start in MovementStateMachine");
        eegClassificationDecision = REST;
        isBCIOn = true;
        oculusMovementDetector = new OculusMovementDetector(cameraTransform);
        oculusMovementDetector.startDetector();
        isOculusExcessivelyMoving = false;
        networker = new Networker();
        networker.startListening();
    }

    private void readEEGBuffer(){
        Debug.Log("Updating MovementStateMachine");
        // TODO: Read from buffer written by EEG computer connected PP/USB
        bool GET_KEYBOARD_SIM = false;
        int newClassificationResult = eegClassificationDecision;
        //Simulating for now keyboard press
        if (GET_KEYBOARD_SIM)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) newClassificationResult = FORWARD;
            if (Input.GetKeyDown(KeyCode.DownArrow)) newClassificationResult = REST;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) newClassificationResult = LEFT;
            if (Input.GetKeyDown(KeyCode.RightArrow)) newClassificationResult = RIGHT;
        }
        else {
            string path = "C:\\Users\\reyhanib\\Documents\\VR\\ReyVR\\VR-master\\Assets\\eeg_buffer.txt";
            // Get from text file that stores value received from network python script
            StreamReader inp_stm = new StreamReader(path);

            while (!inp_stm.EndOfStream)
            {
                string inp_ln = inp_stm.ReadLine();
                Debug.Log("Classification value read from textfile: " + inp_ln);
                eegClassificationDecision = Convert.ToInt32(inp_ln);
            }

            inp_stm.Close();
        }
        

        if (eegClassificationDecision != newClassificationResult){
            // TODO: Log change in classification decision?
            Debug.Log("Change in classification to: " + newClassificationResult);
            eegClassificationDecision = newClassificationResult;
        }
    }

    public void updateStateMachine() {
        readOculusExcessivelyMoving();
        eegClassificationDecision = networker.getData();
        //readEEGBuffer();
    }

    private void readOculusExcessivelyMoving()
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
