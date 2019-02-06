using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementStateMachine {
    public static int BCI_OFF = -2;
    public static int RIGHT = 0;
    public static int FORWARD = 1;
    public static int LEFT = 2;
    public static int REST = 3;
    public static int END_OF_MAZE = 4;

    public bool isBCIOn;
    private PlayerMovementView view;

    // read from EEG computer
    private int eegClassificationDecision;
    private bool isMMGExcessive;
    private bool isEOGExcessive;

    private bool isOculusExcessivelyMoving;
    private OculusMovementDetector oculusMovementDetector;

    public MovementStateMachine(PlayerMovementView pmw, Transform cameraTransform) {
        view = pmw;
        UnityEngine.Debug.Log("Made MSM");
        Debug.Log("Start in MovementStateMachine");
        eegClassificationDecision = REST;
        isBCIOn = true;
        oculusMovementDetector = new OculusMovementDetector(cameraTransform);
        oculusMovementDetector.startDetector();
        isOculusExcessivelyMoving = false;

        isEOGExcessive = false;
        isMMGExcessive = false;
    }

    private void readEEGBuffer(){
        Debug.Log("Updating MovementStateMachine");
        // TODO: Read from buffer written by EEG computer connected PP/USB

        int newClassificationResult = eegClassificationDecision;
        //Simulating for now keyboard press
        if (Input.GetKeyDown(KeyCode.UpArrow)) newClassificationResult = FORWARD;
        if (Input.GetKeyDown(KeyCode.DownArrow)) newClassificationResult = REST;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) newClassificationResult = LEFT;
        if (Input.GetKeyDown(KeyCode.RightArrow)) newClassificationResult = RIGHT;

        if (eegClassificationDecision != newClassificationResult){
            // TODO: Log change in classification decision?
            Debug.Log("Change in classification to: " + newClassificationResult);
            eegClassificationDecision = newClassificationResult;
        }
    }

    public void updateStateMachine() {
        readOculusExcessivelyMoving();
        readEEGBuffer();
        readArtifactBuffer();
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

    private void readArtifactBuffer() {
        bool newMMG = getMMGExcessive();
        if (newMMG != isMMGExcessive) {
            isMMGExcessive = newMMG;
            view.logMarkers(isMMGExcessive ? PlayerMovementView.EXCESSIVE_MMG_ON : PlayerMovementView.EXCESSIVE_MMG_OFF);
        }

        bool newEOG = getEOGExcessive();
        if (newEOG != isEOGExcessive) {
            isEOGExcessive = newEOG;
            view.logMarkers(isEOGExcessive ? PlayerMovementView.EXCESSIVE_EOG_ON : PlayerMovementView.EXCESSIVE_EOG_OFF);
        }
    }

    private bool getMMGExcessive()
    {
        return false;
    }

    private bool getEOGExcessive()
    {
        return false;
    }

    public int getClassificationDecision() {
        return isBCIOn ? eegClassificationDecision : BCI_OFF;
    }

    public bool isExcessiveBodyMovement() {
        // TODO: Add EOG AND MMG to decision?
        return isOculusExcessivelyMoving;
    }
}
