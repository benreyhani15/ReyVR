using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementView : MonoBehaviour {
    private static float MAX_IDLE_TIME = 10000000f;

    public float maxHandTaskTime = 20f;
    public float maxFeetTaskTime = 10f;
    public float maxRestTaskTime = 10f;

    // Amount of time to turn off BCI at beggining of new task
    public float BCIOffTimeBetweenStateChange = 5f;
    public float initialBCIOffTime = 5f;

    private float currentTaskElapsedTime;
    private float currentTaskMaxTime;

    public GameObject forwardBall;
    public GameObject rotationLine;
    public GameObject restSlider;

    public float restBarSpeed = 1f; 
    public float rotationLineSpeed = 20f; // 20 degree/s
    public float ballSpeed = 1f; // 1 m/s

    public float restBarCheckpoint = 5f; // 5m
    public float rotationAngleCheckpoint = 90f; // 90 degrees
    public float ballDistanceCheckpoint = 5f;

    public Vector3 originalBallLocalPosition;
    public Vector3 originalEulerLineRotation;

    UnityEngine.UI.Slider progressSlider;

    MovementStateMachine movementStateMachine;
    private int currentMovementLabel;
    private Transform playerTransform;

    // Set to true when checkpoints are finished and user has to stay idle for a few seconds, in this mode a progress bar appears and it should be filled by correctly being in NC state
    private bool inIdleState;

	// Use this for initialization
	void Start () {
        inIdleState = false;
        originalBallLocalPosition = forwardBall.transform.localPosition;
        Debug.Log("Original local pos: " + originalBallLocalPosition.ToString());
        originalEulerLineRotation = rotationLine.transform.localEulerAngles;
        Debug.Log("Original euler line rotation: " + originalEulerLineRotation.ToString());
        currentTaskMaxTime = MAX_IDLE_TIME;
        progressSlider = restSlider.GetComponentInChildren<UnityEngine.UI.Slider>();
        playerTransform = GetComponent<Transform>();
        currentMovementLabel = MovementStateMachine.REST;
        movementStateMachine = new MovementStateMachine();
        temporarilyTurnBCIOff(initialBCIOffTime);
	}
	
	// Update is called once per frame
	void Update () {
        updateCurrentMovementLabel();
        movementStateMachine.updateStateMachine();
        updateUI(Time.deltaTime);
	}

    private void movePlayerToNextPosition() {
        
    }

    private void updateUI(float deltaTime){
        
        Debug.Log("current task elapsed time : " + currentTaskElapsedTime + "\n Current task max time: " + currentTaskMaxTime);
        if (currentMovementLabel == MovementStateMachine.END_OF_MAZE) {
            Debug.Log("Reached the end of current maze");
            // End game or move on to next maze
        }
        else if (inIdleState) {
            Debug.Log("in idle state");
            updateRestUI(deltaTime);
        } else {
            Debug.Log("updating movement UI");
            updateMovementUI(deltaTime);
        }
    }

    private void updateRestUI(float deltaTime){
        if (!restSlider.activeInHierarchy) {
            restSlider.SetActive(true);
            currentTaskMaxTime = maxRestTaskTime;
            currentTaskElapsedTime = 0;
            temporarilyTurnBCIOff(BCIOffTimeBetweenStateChange);
        } else {
            if (isSliderFull(progressSlider) || (currentTaskElapsedTime > currentTaskMaxTime))
            {
                // Slider is complete or ran out of time; TODO: show animation saying good job, etc.
                progressSlider.value = 0;
                restSlider.SetActive(false);

                if (rotationLine.activeInHierarchy)
                {
                    //changeUserOrientation();
                    rotationLine.transform.localEulerAngles = originalEulerLineRotation;
                    rotationLine.SetActive(false);
                }

                if (forwardBall.activeInHierarchy)
                {
                    //changeUserPosition();
                    forwardBall.transform.localPosition = originalBallLocalPosition;
                    forwardBall.SetActive(false);
                }
                inIdleState = false;
            }
            else if(movementStateMachine.isBCIOn) 
            {
                if (movementStateMachine.getClassificationDecision() == MovementStateMachine.REST)
                {
                    Debug.Log("Correctly classified as rest");
                    float newVal = progressSlider.value + (deltaTime * restBarSpeed);
                    newVal = Mathf.Min(newVal, progressSlider.maxValue);
                    progressSlider.value = newVal;
                } else {
                    Debug.Log("incorrectly not classified as rest");
                }
                currentTaskElapsedTime += deltaTime;
            }
        }
    }

    private void updateMovementUI(float deltaTime) {
        if (currentMovementLabel == MovementStateMachine.FORWARD) {
            Debug.Log("Current movement label is forward");
            if (!forwardBall.activeInHierarchy) {
                Debug.Log("Activating ball");
                forwardBall.SetActive(true);
                currentTaskMaxTime = maxFeetTaskTime;
                currentTaskElapsedTime = 0f;
                temporarilyTurnBCIOff(BCIOffTimeBetweenStateChange);
            } else {
                // Reached checkpoint or ran out of time
                if (forwardBall.transform.localPosition.z >= (ballDistanceCheckpoint+originalBallLocalPosition.z) || (currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                }
                else if (movementStateMachine.isBCIOn)
                {
                    Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.FORWARD)
                    {
                        Debug.Log("Feet MI classified correctly");
                        moveBallForward(deltaTime);
                    } else {
                        Debug.Log("Misclassified Feet MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                }

            }

        } else if (currentMovementLabel == MovementStateMachine.LEFT) {
            if(!rotationLine.activeInHierarchy) {
                Debug.Log("Activating rotation line for LEFT");
                rotationLine.SetActive(true);
                currentTaskMaxTime = maxHandTaskTime;
                currentTaskElapsedTime = 0f;
                temporarilyTurnBCIOff(BCIOffTimeBetweenStateChange);
            } else {
                // Reached checkpoint or ran out of time
                float curAngle = rotationLine.transform.localEulerAngles.y;
                float convertedAngle = curAngle == 0f ? 360 : curAngle;
                if (convertedAngle <= (360 - rotationAngleCheckpoint) || (currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                }
                else if (movementStateMachine.isBCIOn)
                {
                    Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.LEFT)
                    {
                        Debug.Log("Left-hand MI classified correctly");
                        moveArrowLeft(deltaTime);
                    }
                    else
                    {
                        Debug.Log("Misclassified Left-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                }
            }


        } else if (currentMovementLabel == MovementStateMachine.RIGHT) {
            if (!rotationLine.activeInHierarchy)
            {
                Debug.Log("Activating rotation line for RIGHT");
                rotationLine.SetActive(true);
                currentTaskMaxTime = maxHandTaskTime;
                currentTaskElapsedTime = 0f;
                temporarilyTurnBCIOff(BCIOffTimeBetweenStateChange);
            } else {
                // Reached checkpoint or ran out of time
                if (rotationLine.transform.localEulerAngles.y >= rotationAngleCheckpoint || (currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                }
                else if (movementStateMachine.isBCIOn)
                {
                    Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.RIGHT)
                    {
                        Debug.Log("Right-hand MI classified correctly");
                        moveArrowRight(deltaTime);
                    }
                    else
                    {
                        Debug.Log("Misclassified Right-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                }  
            }
        }
    }

    private void updateCurrentMovementLabel(){
        // TODO: Read Plane labels where the user is
        int newMovementLabel = getCurrentMovementLabel();
        if (newMovementLabel != currentMovementLabel) {
            Debug.Log("Change in labelled action to: " + newMovementLabel);
            // TODO: Log it into text file?
            currentMovementLabel = newMovementLabel;
        }
    }

    private int getCurrentMovementLabel() {
        // TODO: Read the label of the plane that player is standing on
        return MovementStateMachine.FORWARD;
    }

    private bool isSliderFull(UnityEngine.UI.Slider slider) {
        return slider.value >= slider.maxValue;
    }

    private void moveBallForward(float deltaTime)
    {
        float newForwardPos = forwardBall.transform.localPosition.z + (deltaTime * ballSpeed);
        float maxPos = ballDistanceCheckpoint + originalBallLocalPosition.z;
        Debug.Log("new Pos: " + newForwardPos + "Max Pos: " + maxPos);
        newForwardPos = Mathf.Min(newForwardPos, maxPos);
        Debug.Log("Moving ball to z local pos: " + newForwardPos);
        forwardBall.transform.localPosition = new Vector3(originalBallLocalPosition.x, originalBallLocalPosition.y, newForwardPos);
    }

    private void moveArrowLeft(float deltaTime) {
        // deltaTime will be negative for Left
        float curAngle = rotationLine.transform.localEulerAngles.y;
        if (curAngle == 0f) curAngle = 360;
        float newAngle = curAngle - (deltaTime * rotationLineSpeed);
        newAngle = Mathf.Max(newAngle, 360-rotationAngleCheckpoint);
        Debug.Log("Moving line rotation to angle: " + newAngle);
        rotationLine.transform.localEulerAngles = new Vector3(originalEulerLineRotation.x, newAngle, originalEulerLineRotation.z);
    }

    private void moveArrowRight(float deltaTime)
    {
        // deltaTime will be negative for Left
        float newAngle = rotationLine.transform.localEulerAngles.y + (deltaTime * rotationLineSpeed);
        newAngle = Mathf.Min(newAngle, rotationAngleCheckpoint);
        Debug.Log("Moving line rotation to angle: " + newAngle);
        rotationLine.transform.localEulerAngles = new Vector3(originalEulerLineRotation.x, newAngle, originalEulerLineRotation.z);
    }

    IEnumerator TurnBCIOff(float seconds)
    {
        movementStateMachine.isBCIOn = false;
        Debug.Log("Turned BCI Off");
        yield return new WaitForSeconds(seconds);
        movementStateMachine.isBCIOn = true;
        Debug.Log("Turned BCI on");
    }

    public void temporarilyTurnBCIOff(float seconds)
    {
        Debug.Log("Starting coroutine to suspend BCI");
        StartCoroutine(TurnBCIOff(seconds));
    }
}
