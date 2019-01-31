using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementStateMachine : MonoBehaviour {
    public static int BCI_OFF = -2;
    public static int REST = 0;
    public static int FORWARD = 1;
    public static int RIGHT = 2;
    public static int LEFT = 3;
    public static int END_OF_MAZE = 4;

    public bool isBCIOn;
    private PlayerMovementView view;

    private bool isOculusExcessivelyMoving;

    // read from EEG computer
    private int eegClassificationDecision;
    private bool isMMGExcessive;
    private bool isEOGExcessive;

    public MovementStateMachine(PlayerMovementView pmw) {
        view = pmw;
    }

	// Use this for initialization
	void Start () {
        Debug.Log("Start in MovementStateMachine");
        eegClassificationDecision = REST;
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

    private bool getMMGExcessive() {
        return false;
    }

    private bool getEOGExcessive() {
        return false; 
    }

    private bool getOculusExcessive() {
        return false;
    }

    public void updateStateMachine() {
        readOculusExcessivelyMoving();
        readEEGBuffer();
        readArtifactBuffer();
    }

    private void readOculusExcessivelyMoving()
    {
        bool newValue = getOculusExcessive();
        if (newValue != isOculusExcessivelyMoving) {
            isOculusExcessivelyMoving = newValue;
            view.logMarkers(isOculusExcessivelyMoving ? PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON : PlayerMovementView.EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF);
        }
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

    public int getClassificationDecision() {
        return isBCIOn ? eegClassificationDecision : BCI_OFF;
    }
}
