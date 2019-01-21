using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementStateMachine : MonoBehaviour {
    public static int BCI_OFF = -2;
    public static int EXCESSIVE_MOVEMENT = -1;
    public static int REST = 0;
    public static int FORWARD = 1;
    public static int RIGHT = 2;
    public static int LEFT = 3;
    public static int END_OF_MAZE = 4;

    public bool isBCIOn;
    private int eegClassificationDecision;
    private bool isOculusExcessivelyMoving;

	// Use this for initialization
	void Start () {
        Debug.Log("Start in MovementStateMachine");
        eegClassificationDecision = REST;
        isOculusExcessivelyMoving = false;
	}

    private void updateEEGClassification(){
        Debug.Log("Updating MovementStateMachine");
        // TODO: Read from buffer written by EEG computer connected PP/USB

        int newClassificationResult = eegClassificationDecision;
        //Simulating for now keyboard press
        if (Input.GetKeyDown(KeyCode.UpArrow)) newClassificationResult = FORWARD;
        if (Input.GetKeyDown(KeyCode.DownArrow)) newClassificationResult = REST;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) newClassificationResult = LEFT;
        if (Input.GetKeyDown(KeyCode.RightArrow)) newClassificationResult = RIGHT;
        if (Input.GetKeyDown(KeyCode.Space)) newClassificationResult = EXCESSIVE_MOVEMENT;

        if (eegClassificationDecision != newClassificationResult){
            // TODO: Log change in classification decision?
            Debug.Log("Change in classification to: " + newClassificationResult);
            eegClassificationDecision = newClassificationResult;
        }
    }


    private bool getOculusExcessivelyMoving() {
        return false;
    }

    public void updateStateMachine() {
        isOculusExcessivelyMoving = getOculusExcessivelyMoving();
        updateEEGClassification();
    }

    public int getClassificationDecision() {
        return isBCIOn ? eegClassificationDecision : BCI_OFF;
    }
}
