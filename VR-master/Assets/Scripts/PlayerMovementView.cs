using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class PlayerMovementView : MonoBehaviour {
    private static float MAX_COMPLETION_TIME = 10000000000000000000; // 5 mins, max time user can attempt to free-roam
    private static float METRES_PER_TELEPORT = 5f;

    public static float MAX_HAND_TASK_TIME = 20f;
    public static float MAX_FEET_TASK_TIME = 20f;
    public static float MAX_REST_TASK_TIME = 20f;

    // Amount of time to turn off BCI at beggining of new task
    public static float BCI_OFF_TIME_BETWEEN_STATE_CHANGE = 1f;
    public static float BCI_OFF_TIME_AFTER_OCULUS_MOVEMENT = 1f;
    public static float INITIAL_BCI_OFF_TIME = 2f;

    private float currentTaskElapsedTime;
    private float currentTaskMaxTime;
    private float totalElapsedTime;

    public GameObject forwardBall;
    public GameObject rotationLine;
    public GameObject restSlider;

    public static float REST_BAR_SPEED = 1f; 
    public static float ROTATION_LINE_SPEED = 20f; // 20 degree/s
    public static float BALL_SPEED = 1f; // 1 m/s
    public static float STRENGTH_SPEED = 1f;

    public static float REST_BAR_CHECKPOINT = 5f; // 
    public static float ROTATION_ANGLE_CHECKPOINT = 90f; // 90 degrees
    public static float BALL_DISTANCE_CHECKPOINT = 5f; // 5m 
    public static float STRENGTH_DISTANCE_CHECKPOINT = 5f;

    public static int START_OF_RIGHT_TASK = 0;
    public static int START_OF_FORWARD_TASK = 1;
    public static int START_OF_LEFT_TASK = 2;
    public static int START_OF_REST_TASK = 3;
    public static int START_OF_TRIAL = 4;
    public static int END_OF_TRIAL = 5;
    public static int BCI_PAUSED = 6;
    public static int BCI_UNPAUSED = 7;
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON = 8;
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF = 9;
    public static int END_OF_TASK_SUCCESS = 10;
    public static int END_OF_TASK_UNSUCCESS = 11;
    public static int MOVED_FORWARD = 12;
    public static int ORIENTATION_CHANGED = 13;
    public static int MAZE_COMPLETED = 14;
    public static int EXCESSIVE_EOG_ON = 15;
    public static int EXCESSIVE_EOG_OFF = 16;
    public static int EXCESSIVE_MMG_ON = 17;
    public static int EXCESSIVE_MMG_OFF = 18;

    public Vector3 originalBallLocalPosition;
    public Vector3 originalEulerLineRotation;
    public float originalVRTeleporterStrength;

    private bool taskCompleted;
    public bool IS_TRAINING_SESSION = false;

    UnityEngine.UI.Slider progressSlider;

    MovementStateMachine movementStateMachine;
    private int currentMovementLabel;
    public Transform playerTransform;

    public VRTeleporter vrTeleporter;
    private static bool USE_BALL = false;

    // Set to true when checkpoints are finished and user has to stay idle for a few seconds, in this mode a progress bar appears and it should be filled by correctly being in NC state
    private bool inIdleState;

    private BCILogger logger;
    public string participantName = "Ben";
    public int trialNumber = 1;
    public float classificationResultLogInterval = 250; // how often in ms, the classification decision vs. label output should be written to log

    private Stopwatch totalStopWatch;
    private Stopwatch classificationStopWatch;
    public Transform eyeCameraTransform;

    public UnityEngine.GameObject alertUI;
    public bool showAlertUI = true; 

    private static Vector3[] MAZE_STARTING_SPOTS = { new Vector3(169.22f, 0f, 133f) };

	// Use this for initialization
	void Start () {
        inIdleState = false;
        taskCompleted = false;
        UnityEngine.Debug.Log("Training session: " + IS_TRAINING_SESSION);
        originalBallLocalPosition = forwardBall.transform.localPosition;
        originalEulerLineRotation = rotationLine.transform.localEulerAngles;
        originalVRTeleporterStrength = vrTeleporter.strength;

        currentTaskMaxTime = MAX_COMPLETION_TIME;
        totalElapsedTime = 0;

        progressSlider = restSlider.GetComponentInChildren<UnityEngine.UI.Slider>();
        currentMovementLabel = MovementStateMachine.REST;
        string logFilePrefix = IS_TRAINING_SESSION ? "Train" : "Test";
        logger = new BCILogger(participantName, logFilePrefix, trialNumber, true);
        logger.logMarkers(START_OF_TRIAL, "0");
        movementStateMachine = new MovementStateMachine(this, eyeCameraTransform);
        totalStopWatch = new Stopwatch();
        totalStopWatch.Start();
        classificationStopWatch = new Stopwatch();
        UnityEngine.Debug.Log("Finished START");
        playerTransform.position = MAZE_STARTING_SPOTS[trialNumber % MAZE_STARTING_SPOTS.Length];
        alertUI.SetActive(showAlertUI);
        temporarilyTurnBCIOff(INITIAL_BCI_OFF_TIME);
    }

    // Update is called once per frame
    void Update () {
        if (movementStateMachine == null) UnityEngine.Debug.Log("MSM is null");
        movementStateMachine.updateStateMachine();
        updateUI(Time.deltaTime);
	}

    private void resetVRTeleporter() {
        if (vrTeleporter!= null) {
            vrTeleporter.ToggleDisplay(false);
            vrTeleporter.gameObject.SetActive(false);
            vrTeleporter.strength = originalVRTeleporterStrength;  
        }
    }

    private void initVRTeleporter() {
        vrTeleporter.gameObject.SetActive(true);
        vrTeleporter.ToggleDisplay(true);
        currentTaskMaxTime = IS_TRAINING_SESSION ? MAX_FEET_TASK_TIME : MAX_COMPLETION_TIME;
        currentTaskElapsedTime = 0f;
    }

    private void resetLineArrow() {
        rotationLine.transform.localEulerAngles = originalEulerLineRotation;
        rotationLine.SetActive(false);
    }

    private void initLineArrow() {
        rotationLine.SetActive(true);
        currentTaskMaxTime = IS_TRAINING_SESSION ? MAX_HAND_TASK_TIME : MAX_COMPLETION_TIME;
        currentTaskElapsedTime = 0f;
    }

    private void updateAlertUI()
    {
        if (alertUI != null)
        {
            alertUI.GetComponent<Renderer>().material.color = movementStateMachine.isExcessiveBodyMovement()? Color.red : Color.green;
        }
    }

    private void updateUI(float deltaTime){

        //UnityEngine.Debug.Log("current task elapsed time : " + currentTaskElapsedTime + "\n Current task max time: " + currentTaskMaxTime);
        if (currentMovementLabel == MovementStateMachine.END_OF_MAZE || totalElapsedTime >= MAX_COMPLETION_TIME) {
            bool mazeCompleteSuccess = currentMovementLabel == MovementStateMachine.END_OF_MAZE;
            if (mazeCompleteSuccess) {
                UnityEngine.Debug.Log("Maze successfully completed");
                if (IS_TRAINING_SESSION) logMarkers(END_OF_TRIAL);
                else logMarkers(MAZE_COMPLETED);
            } else {
                UnityEngine.Debug.Log("Maze unsuccessfully completed");
                logMarkers(END_OF_TRIAL);
            }
            temporarilyTurnBCIOff(MAX_COMPLETION_TIME);
            vrTeleporter.ToggleDisplay(false);
            Application.Quit();
        }

        else if (inIdleState) {
            UnityEngine.Debug.Log("in idle state");
            updateRestUI(deltaTime);
        } else {
            UnityEngine.Debug.Log("updating movement UI");
            updateMovementUI(deltaTime);
        }
        totalElapsedTime += deltaTime;
        updateAlertUI();
    }

    private void updateRestUI(float deltaTime){
        if (!restSlider.activeInHierarchy) {
            restSlider.SetActive(true);

            //TODO: decide whether to do statement below:
            currentTaskMaxTime = IS_TRAINING_SESSION ? MAX_REST_TASK_TIME : MAX_COMPLETION_TIME;
            //currentTaskMaxTime = MAX_REST_TASK_TIME;
            currentTaskElapsedTime = 0;
            logMarkers(START_OF_REST_TASK);
            temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
        } else {
            bool ranOutOfTime = currentTaskElapsedTime > currentTaskMaxTime;
            bool successfullyFinished = isSliderFull(progressSlider);
            if (successfullyFinished || ranOutOfTime)
            {
                // Slider is complete or ran out of time; TODO: show animation saying good job, etc.
                if (isSliderFull(progressSlider)) {
                    logMarkers(END_OF_TASK_SUCCESS);
                } else {
                    logMarkers(END_OF_TASK_UNSUCCESS);
                }

                progressSlider.value = 0;
                restSlider.SetActive(false);

                if (currentMovementLabel == MovementStateMachine.RIGHT || currentMovementLabel == MovementStateMachine.LEFT)
                {
                    if (IS_TRAINING_SESSION || (!IS_TRAINING_SESSION && taskCompleted)) {
                        changeUserOrientation();
                        currentMovementLabel = MovementStateMachine.FORWARD;
                    } 
                    resetLineArrow();
                } else if (USE_BALL && currentMovementLabel == MovementStateMachine.FORWARD) {
                    if (IS_TRAINING_SESSION || (!IS_TRAINING_SESSION && taskCompleted))
                    {
                        changeUserPosition();
                    }
                    forwardBall.transform.localPosition = originalBallLocalPosition;
                    forwardBall.SetActive(false);
                } else if(!USE_BALL && currentMovementLabel == MovementStateMachine.FORWARD) {
                    // using VRteleporter
                    if (IS_TRAINING_SESSION || (!IS_TRAINING_SESSION && taskCompleted)) {
                        changeUserPosition();
                    }
                    resetVRTeleporter();
                  }

                inIdleState = false;
                taskCompleted = false;
                classificationStopWatch.Reset();
            }
            else if(movementStateMachine.isBCIOn) 
            {
                if (movementStateMachine.getClassificationDecision() == MovementStateMachine.REST)
                {
                    UnityEngine.Debug.Log("Correctly classified as rest");
                    float newVal = progressSlider.value + (deltaTime * REST_BAR_SPEED);
                    newVal = Mathf.Min(newVal, progressSlider.maxValue);
                    progressSlider.value = newVal;
                } else {
                    UnityEngine.Debug.Log("incorrectly not classified as rest");
                }
                logClassificationResult(MovementStateMachine.REST, movementStateMachine.getClassificationDecision());
                currentTaskElapsedTime += deltaTime;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;
        // LOG STUFF
        if (tag.Equals("Forward")) {
            currentMovementLabel = MovementStateMachine.FORWARD;
            other.gameObject.transform.parent.GetChild(0).gameObject.SetActive(false);
        } else if (tag.Equals("Right")){
            currentMovementLabel = MovementStateMachine.RIGHT;
            other.gameObject.transform.parent.GetChild(0).gameObject.SetActive(false);
        } else if (tag.Equals("Left")) {
            currentMovementLabel = MovementStateMachine.LEFT;
            other.gameObject.transform.parent.GetChild(0).gameObject.SetActive(false);
        } else if (tag.Equals("End")) {
            currentMovementLabel = MovementStateMachine.END_OF_MAZE;
            other.gameObject.transform.parent.GetChild(0).gameObject.SetActive(false);
        }
    }

    private void updateMovementUI(float deltaTime) {
        if (currentMovementLabel == MovementStateMachine.FORWARD) {
            UnityEngine.Debug.Log("Current movement label is forward");
            if (!forwardBall.activeInHierarchy && USE_BALL) {
                UnityEngine.Debug.Log("Activating ball");
                forwardBall.SetActive(true);
                resetLineArrow();
                currentTaskMaxTime = IS_TRAINING_SESSION ? MAX_FEET_TASK_TIME : MAX_COMPLETION_TIME;
                currentTaskElapsedTime = 0f;
                logMarkers(START_OF_FORWARD_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else if (!vrTeleporter.gameObject.activeInHierarchy && !USE_BALL){
                UnityEngine.Debug.Log("Activating teleporter");
                initVRTeleporter();
                resetLineArrow();
                logMarkers(START_OF_FORWARD_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                if (USE_BALL && ((forwardBall.transform.localPosition.z >= BALL_DISTANCE_CHECKPOINT + originalBallLocalPosition.z) || currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    UnityEngine.Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                    if (taskCompleted)
                    {
                        logMarkers(END_OF_TASK_SUCCESS);
                    }
                    else
                    {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    forwardBall.SetActive(false);
                    classificationStopWatch.Reset();
                } else if (!USE_BALL && ((vrTeleporter.strength >= (STRENGTH_DISTANCE_CHECKPOINT+originalVRTeleporterStrength)) || (currentTaskElapsedTime >= currentTaskMaxTime))){
                    UnityEngine.Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                    taskCompleted = vrTeleporter.strength >= STRENGTH_DISTANCE_CHECKPOINT + originalVRTeleporterStrength;
                    if (taskCompleted) {
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    vrTeleporter.ToggleDisplay(false);
                    classificationStopWatch.Reset();
                }
                else if (movementStateMachine.isBCIOn)
                {
                    UnityEngine.Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.FORWARD)
                    {
                        UnityEngine.Debug.Log("Feet MI classified correctly");
                        moveForward(deltaTime);
                    } else {
                        UnityEngine.Debug.Log("Misclassified Feet MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    logClassificationResult(MovementStateMachine.FORWARD, movementStateMachine.getClassificationDecision());
                }

            }

        } else if (currentMovementLabel == MovementStateMachine.LEFT) {
            if(!rotationLine.activeInHierarchy) {
                UnityEngine.Debug.Log("Activating rotation line for LEFT");
                initLineArrow();
                resetVRTeleporter();
                logMarkers(START_OF_LEFT_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                float curAngle = rotationLine.transform.localEulerAngles.y;
                float convertedAngle = curAngle == 0f ? 360 : curAngle;
                if (convertedAngle <= (360 - ROTATION_ANGLE_CHECKPOINT) || (currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    UnityEngine.Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                    taskCompleted = convertedAngle <= (360 - ROTATION_ANGLE_CHECKPOINT);
                    if (taskCompleted) {
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    rotationLine.SetActive(false);
                    classificationStopWatch.Reset();
                }
                else if (movementStateMachine.isBCIOn)
                {
                    UnityEngine.Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.LEFT)
                    {
                        UnityEngine.Debug.Log("Left-hand MI classified correctly");
                        moveArrowLeft(deltaTime);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Misclassified Left-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    logClassificationResult(MovementStateMachine.LEFT, movementStateMachine.getClassificationDecision());
                }
            }


        } else if (currentMovementLabel == MovementStateMachine.RIGHT) {
            if (!rotationLine.activeInHierarchy)
            {
                UnityEngine.Debug.Log("Activating rotation line for RIGHT");
                initLineArrow();
                resetVRTeleporter();
                logMarkers(START_OF_RIGHT_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                if (rotationLine.transform.localEulerAngles.y >= ROTATION_ANGLE_CHECKPOINT || (currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    UnityEngine.Debug.Log("Reached Checkpoint or ran out of time");
                    inIdleState = true;
                    taskCompleted = rotationLine.transform.localEulerAngles.y >= ROTATION_ANGLE_CHECKPOINT;
                    if (taskCompleted){
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    rotationLine.SetActive(false);
                    classificationStopWatch.Reset();
                }
                else if (movementStateMachine.isBCIOn)
                {
                    UnityEngine.Debug.Log("Haven't reached checkpoint, will move ball if classification congurent");
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.RIGHT)
                    {
                        UnityEngine.Debug.Log("Right-hand MI classified correctly");
                        moveArrowRight(deltaTime);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Misclassified Right-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    logClassificationResult(MovementStateMachine.RIGHT, movementStateMachine.getClassificationDecision());
                }  
            }
        }
    }

    private void changeUserPosition() {
        playerTransform.Translate(Vector3.forward * METRES_PER_TELEPORT);
        logMarkers(MOVED_FORWARD);
    }

    private void changeUserOrientation() {
        if (currentMovementLabel == MovementStateMachine.RIGHT) {
            playerTransform.Rotate(0, ROTATION_ANGLE_CHECKPOINT, 0);
        } else if (currentMovementLabel == MovementStateMachine.LEFT) {
            playerTransform.Rotate(0, 360 - ROTATION_ANGLE_CHECKPOINT, 0);
        }
        logMarkers(ORIENTATION_CHANGED);
    }

    private int getCurrentMovementLabel() {
        // TODO: Read the label of the plane that player is standing on: SIMULATED FOR NOW
        if (Input.GetKeyDown(KeyCode.W)) return MovementStateMachine.FORWARD;
        else if (Input.GetKeyDown(KeyCode.A)) return MovementStateMachine.LEFT;
        else if (Input.GetKeyDown(KeyCode.D)) return MovementStateMachine.RIGHT;
        else return currentMovementLabel;
    }

    private bool isSliderFull(UnityEngine.UI.Slider slider) {
        return slider.value >= slider.maxValue;
    }

    private void moveForward(float deltaTime)
    {
        if (USE_BALL) {
            //TODO: this is wrong, use Translate
            float newForwardPos = forwardBall.transform.localPosition.z + (deltaTime * BALL_SPEED);
            float maxPos = BALL_DISTANCE_CHECKPOINT + originalBallLocalPosition.z;
            UnityEngine.Debug.Log("new Pos: " + newForwardPos + "Max Pos: " + maxPos);
            newForwardPos = Mathf.Min(newForwardPos, maxPos);
            UnityEngine.Debug.Log("Moving ball to z local pos: " + newForwardPos);
            forwardBall.transform.localPosition = new Vector3(originalBallLocalPosition.x, originalBallLocalPosition.y, newForwardPos);
        } else {
            float newStrength = vrTeleporter.strength + (deltaTime * STRENGTH_SPEED);
            float maxStrength = STRENGTH_DISTANCE_CHECKPOINT + originalVRTeleporterStrength;
            float newForwardStrength = Mathf.Min(newStrength, maxStrength);
            vrTeleporter.strength = newForwardStrength;
        }
    }

    private void moveArrowLeft(float deltaTime) {
        // deltaTime will be negative for Left
        float curAngle = rotationLine.transform.localEulerAngles.y;
        if (curAngle == 0f) curAngle = 360;
        float newAngle = curAngle - (deltaTime * ROTATION_LINE_SPEED);
        newAngle = Mathf.Max(newAngle, 360-ROTATION_ANGLE_CHECKPOINT);
        //UnityEngine.Debug.Log("Moving line rotation to angle: " + newAngle);
        rotationLine.transform.localEulerAngles = new Vector3(originalEulerLineRotation.x, newAngle, originalEulerLineRotation.z);
    }

    private void moveArrowRight(float deltaTime)
    {
        // deltaTime will be negative for Left
        float newAngle = rotationLine.transform.localEulerAngles.y + (deltaTime * ROTATION_LINE_SPEED);
        newAngle = Mathf.Min(newAngle, ROTATION_ANGLE_CHECKPOINT);
        //UnityEngine.Debug.Log("Moving line rotation to angle: " + newAngle);
        rotationLine.transform.localEulerAngles = new Vector3(originalEulerLineRotation.x, newAngle, originalEulerLineRotation.z);
    }

    IEnumerator TurnBCIOff(float seconds)
    {
        movementStateMachine.isBCIOn = false;
        UnityEngine.Debug.Log("Turned BCI Off");
        logMarkers(BCI_PAUSED);
        yield return new WaitForSeconds(seconds);
        movementStateMachine.isBCIOn = true;
        UnityEngine.Debug.Log("Turned BCI on");
        logMarkers(BCI_UNPAUSED);
    }

    public void temporarilyTurnBCIOff(float seconds)
    {
        UnityEngine.Debug.Log("Starting coroutine to suspend BCI");
        if (movementStateMachine.isBCIOn) StartCoroutine(TurnBCIOff(seconds));
    }

    public void logMarkers(int marker){
        logger.logMarkers(marker, totalStopWatch.ElapsedMilliseconds.ToString());
    }

    public void logClassificationResult(int trueLabel, int predictedLabel) {
        if (classificationStopWatch.IsRunning) {
            if (classificationStopWatch.ElapsedMilliseconds >= classificationResultLogInterval) {
                logger.logClassificationResults(trueLabel, predictedLabel, totalStopWatch.ElapsedMilliseconds.ToString());
                classificationStopWatch.Reset();
                classificationStopWatch.Start();
                UnityEngine.Debug.Log("Logging Classification Result while stopwatch running @ time: " + totalStopWatch.ElapsedMilliseconds.ToString());
            }
        } else {
            logger.logClassificationResults(trueLabel, predictedLabel, totalStopWatch.ElapsedMilliseconds.ToString());
            classificationStopWatch.Start();
            UnityEngine.Debug.Log("Logging Classification Result while stopwatch stopped @ time: " + totalStopWatch.ElapsedMilliseconds.ToString());
        }
    }
}