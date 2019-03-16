using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public class PlayerMovementView : MonoBehaviour {

    // TODO: CHANGE THESE CONSTANTS!!!!
    private static float MAX_COMPLETION_TIME = 10000000000000000000; // 5 mins, max time user can attempt to free-roam
    private static float METRES_PER_TELEPORT = 5f;

    public static float MAX_HAND_TASK_TIME = 5f;
    public static float MAX_FEET_TASK_TIME = 5f;
    public static float MAX_REST_TASK_TIME = 5f;

    // Amount of time to turn off BCI at beggining of new task
    public static float BCI_OFF_TIME_BETWEEN_STATE_CHANGE = 2f;
    public static float BCI_OFF_TIME_AFTER_OCULUS_MOVEMENT = 0f;
    public static float INITIAL_BCI_OFF_TIME = 3f;

    private float currentTaskElapsedTime;
    private float currentTaskMaxTime;
    private float totalElapsedTime;

    public GameObject rotationLine;
    public GameObject restSlider;

    public static float REST_BAR_SPEED = 1f; 
    public static float ROTATION_LINE_SPEED = 9f; // 20 degree/s
    public static float STRENGTH_SPEED = 0.5f;

    public static float REST_BAR_CHECKPOINT = 5f; // 
    public static float ROTATION_ANGLE_CHECKPOINT = 90f; // 90 degrees
    public static float STRENGTH_DISTANCE_CHECKPOINT = 5f;

    public static int START_OF_RIGHT_TASK = 1;
    public static int START_OF_FORWARD_TASK = 2;
    public static int START_OF_LEFT_TASK = 3;
    public static int START_OF_REST_TASK = 4;
    public static int START_OF_TRIAL = 5;
    public static int END_OF_TRIAL = 6;
    public static int BCI_PAUSED = 7;
    public static int BCI_UNPAUSED = 8;
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_ON = 9;
    public static int EXCESSIVE_OCULUS_MOVEMENT_TURNED_OFF = 10;
    public static int END_OF_TASK_SUCCESS = 11;
    public static int END_OF_TASK_UNSUCCESS = 12;
    //public static int MOVED_FORWARD = 13;
    //public static int ORIENTATION_CHANGED = 14;
    public static int MAZE_COMPLETED = 15;

    public Vector3 originalEulerLineRotation;
    public float originalVRTeleporterStrength;

    public bool taskCompleted;
    public bool IS_TRAINING_SESSION = false;

    public bool IS_ART_FEEDBACK = false;
    public int PERCENT_ART_CORRECT = 80;

    UnityEngine.UI.Slider progressSlider;

    MovementStateMachine movementStateMachine;
    public int currentMovementLabel;
    public Transform playerTransform;

    public VRTeleporter vrTeleporter;

    private static bool SUPPRESS_UI_LOGS = true;

    // Set to true when checkpoints are finished and user has to stay idle for a few seconds, in this mode a progress bar appears and it should be filled by correctly being in NC state
    private bool inIdleState;
    // There's a bug that needs to be fixed when set to true
    private bool hasIdleState = false;

    public BCILogger logger;
    public string participantName = "Ben";
    public int trialNumber = 1;
    public float classificationResultLogInterval = 250; // how often in ms, the classification decision vs. label output should be written to log

    private Stopwatch totalStopWatch;
    private Stopwatch classificationStopWatch;
    public Transform eyeCameraTransform;

    public UnityEngine.GameObject alertUI;
    public UnityEngine.GameObject stopUI;

    private static bool TEST_OCULUS_MOVEMENT = false;
    
    private static Vector3[] MAZE_STARTING_SPOTS = { new Vector3(169.22f, 0f, 133f) };

	// Use this for initialization
	void Start () {
        originalEulerLineRotation = rotationLine.transform.localEulerAngles;
        originalVRTeleporterStrength = vrTeleporter.strength;

        currentTaskMaxTime = MAX_COMPLETION_TIME;

        progressSlider = restSlider.GetComponentInChildren<UnityEngine.UI.Slider>();
        currentMovementLabel = MovementStateMachine.REST;
        string logFilePrefix = IS_ART_FEEDBACK ? "Feedback" : "Roam";
        logger = new BCILogger(participantName, logFilePrefix, trialNumber, true);
        logger.logMarkers(START_OF_TRIAL, "0");
        movementStateMachine = new MovementStateMachine(this, eyeCameraTransform);
        totalStopWatch = new Stopwatch();
        totalStopWatch.Start();
        classificationStopWatch = new Stopwatch();
        playerTransform.position = MAZE_STARTING_SPOTS[trialNumber % MAZE_STARTING_SPOTS.Length];

        if (TEST_OCULUS_MOVEMENT)
        {
            alertUI.SetActive(true);
            stopUI.SetActive(false);
        }
        else {
            stopUI.SetActive(false);
            alertUI.SetActive(false);
        }
        
        temporarilyTurnBCIOff(INITIAL_BCI_OFF_TIME);
    }

    private void OnApplicationQuit()
    {
        if (this.isActiveAndEnabled) {
            movementStateMachine.networker.stopListening();
        }
    }

    // Update is called once per frame
    void Update () {
        updateUI(Time.deltaTime);
        if (IS_ART_FEEDBACK)
        {
            movementStateMachine.updateArtFBStateMachine(false);
        }
        else {
            movementStateMachine.updateStateMachine();
        }
        logger.sendMarkers(totalStopWatch.ElapsedMilliseconds.ToString());
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

    private void updateAlertUI() {
        movementStateMachine.updateOculusExcessivelyMoving();
        if (TEST_OCULUS_MOVEMENT) {
            if (alertUI != null) alertUI.GetComponent<Renderer>().material.color = movementStateMachine.isExcessiveBodyMovement() ? Color.red : Color.green;
        } else {
            if (stopUI != null) stopUI.SetActive(movementStateMachine.isExcessiveBodyMovement());
        }
    }

    private void updateUI(float deltaTime){

        //UnityEngine.Debug.Log("current task elapsed time : " + currentTaskElapsedTime + "\n Current task max time: " + currentTaskMaxTime);
        if (currentMovementLabel == MovementStateMachine.END_OF_MAZE || (!IS_ART_FEEDBACK && totalElapsedTime >= MAX_COMPLETION_TIME)) {
            bool mazeCompleteSuccess = currentMovementLabel == MovementStateMachine.END_OF_MAZE;
            if (mazeCompleteSuccess) {
                if (IS_TRAINING_SESSION) logMarkers(END_OF_TRIAL);
                else logMarkers(MAZE_COMPLETED);
                EditorApplication.isPlaying = false;
            }
            else
            {
                logMarkers(END_OF_TRIAL);
            }
            temporarilyTurnBCIOff(MAX_COMPLETION_TIME);
            vrTeleporter.ToggleDisplay(false);
            //movementStateMachine.networker.stopListening();
            //Application.Quit();
        }

        else if (inIdleState) {
            updateRestUI(deltaTime);
        } else {
            updateMovementUI(deltaTime);
        }
        updateAlertUI();
        totalElapsedTime += deltaTime;
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
        }
        else
        {
            bool ranOutOfTime = currentTaskElapsedTime > currentTaskMaxTime;
            bool successfullyFinished = isSliderFull(progressSlider);
            if (successfullyFinished || ranOutOfTime)
            {
                // Slider is complete or ran out of time; TODO: show animation saying good job, etc.
                if (isSliderFull(progressSlider))
                {
                    logMarkers(END_OF_TASK_SUCCESS);
                }
                else
                {
                    logMarkers(END_OF_TASK_UNSUCCESS);
                }

                progressSlider.value = 0;
                restSlider.SetActive(false);
                updateNextStateAndView();
                inIdleState = false;
                taskCompleted = false;
            }
            else if (movementStateMachine.isBCIOn)
            {
                if (movementStateMachine.getClassificationDecision() == MovementStateMachine.REST)
                {
                    float newVal = progressSlider.value + (deltaTime * REST_BAR_SPEED);
                    newVal = Mathf.Min(newVal, progressSlider.maxValue);
                    progressSlider.value = newVal;
                }
                if (!IS_ART_FEEDBACK) logClassificationResult(MovementStateMachine.REST, movementStateMachine.getClassificationDecision());
                currentTaskElapsedTime += deltaTime;
            }
        }
    }

    private void updateNextStateAndView() {
        if (currentMovementLabel == MovementStateMachine.RIGHT || currentMovementLabel == MovementStateMachine.LEFT)
        {
            if (IS_TRAINING_SESSION || (!IS_TRAINING_SESSION && taskCompleted))
            {
                changeUserOrientation();
                currentMovementLabel = MovementStateMachine.FORWARD;
            }
            resetLineArrow();
        }
        
        else if (currentMovementLabel == MovementStateMachine.FORWARD)
        {
            // using VRteleporter
            if (IS_TRAINING_SESSION || (!IS_TRAINING_SESSION && taskCompleted))
            {
                changeUserPosition();
              //  if (!hasIdleState && !IS_ART_FEEDBACK) currentMovementLabel = MovementStateMachine.REST;
            }
            resetVRTeleporter();
        }
        movementStateMachine.resetAndStartOculusDetector();
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
            if (!vrTeleporter.gameObject.activeInHierarchy) {
                if (!hasIdleState) taskCompleted = false;
                initVRTeleporter();
                resetLineArrow();
                logMarkers(START_OF_FORWARD_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                if ((vrTeleporter.strength >= (STRENGTH_DISTANCE_CHECKPOINT+originalVRTeleporterStrength)) || (IS_TRAINING_SESSION && currentTaskElapsedTime >= currentTaskMaxTime)){
                    taskCompleted = vrTeleporter.strength >= STRENGTH_DISTANCE_CHECKPOINT + originalVRTeleporterStrength;
                    if (taskCompleted) {
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    vrTeleporter.ToggleDisplay(false);
                    classificationStopWatch.Reset();
                    if (hasIdleState) inIdleState = true;
                    else updateNextStateAndView();
                }
                else if (movementStateMachine.isBCIOn)
                {
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.FORWARD)
                    {
                        UnityEngine.Debug.Log("Feet MI classified correctly: " + totalStopWatch.ElapsedMilliseconds.ToString());
                        moveForward(deltaTime);
                    } else {
                        UnityEngine.Debug.Log("Misclassified Feet MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    if (!IS_ART_FEEDBACK) logClassificationResult(MovementStateMachine.FORWARD, movementStateMachine.getClassificationDecision());
                }
            }
        } else if (currentMovementLabel == MovementStateMachine.LEFT) {
            if(!rotationLine.activeInHierarchy) {
                if (!hasIdleState) taskCompleted = false;
                initLineArrow();
                resetVRTeleporter();
                logMarkers(START_OF_LEFT_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                float curAngle = rotationLine.transform.localEulerAngles.y;
                float convertedAngle = curAngle == 0f ? 360 : curAngle;
                if (convertedAngle <= (360 - ROTATION_ANGLE_CHECKPOINT) || (IS_TRAINING_SESSION && currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    taskCompleted = convertedAngle <= (360 - ROTATION_ANGLE_CHECKPOINT);
                    if (taskCompleted) {
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    rotationLine.SetActive(false);
                    classificationStopWatch.Reset();
                    if (hasIdleState) inIdleState = true;
                    else updateNextStateAndView();
                    
                }
                else if (movementStateMachine.isBCIOn)
                {
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.LEFT)
                    {
                        UnityEngine.Debug.Log("left classified correctly: " + totalStopWatch.ElapsedMilliseconds.ToString());
                        moveArrowLeft(deltaTime);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Misclassified Left-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    if (!IS_ART_FEEDBACK) logClassificationResult(MovementStateMachine.LEFT, movementStateMachine.getClassificationDecision());
                }
            }


        } else if (currentMovementLabel == MovementStateMachine.RIGHT) {
            if (!rotationLine.activeInHierarchy)
            {
                if (!hasIdleState) taskCompleted = false;
                initLineArrow();
                resetVRTeleporter();
                logMarkers(START_OF_RIGHT_TASK);
                temporarilyTurnBCIOff(BCI_OFF_TIME_BETWEEN_STATE_CHANGE);
            } else {
                // Reached checkpoint or ran out of time
                if (rotationLine.transform.localEulerAngles.y >= ROTATION_ANGLE_CHECKPOINT || (IS_TRAINING_SESSION && currentTaskElapsedTime >= currentTaskMaxTime))
                {
                    taskCompleted = rotationLine.transform.localEulerAngles.y >= ROTATION_ANGLE_CHECKPOINT;
                    if (taskCompleted){
                        logMarkers(END_OF_TASK_SUCCESS);
                    } else {
                        logMarkers(END_OF_TASK_UNSUCCESS);
                    }
                    rotationLine.SetActive(false);
                    classificationStopWatch.Reset();
                    if (hasIdleState) inIdleState = true;
                    else updateNextStateAndView();
                   
                }
                else if (movementStateMachine.isBCIOn)
                {
                    if (movementStateMachine.getClassificationDecision() == MovementStateMachine.RIGHT)
                    {
                        UnityEngine.Debug.Log("right MI classified correctly: " + totalStopWatch.ElapsedMilliseconds.ToString());
                        moveArrowRight(deltaTime);
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Misclassified Right-hand MI");
                    }
                    currentTaskElapsedTime += deltaTime;
                    if (!IS_ART_FEEDBACK) logClassificationResult(MovementStateMachine.RIGHT, movementStateMachine.getClassificationDecision());
                }  
            }
        }
    }

    private void changeUserPosition() {
        playerTransform.Translate(Vector3.forward * METRES_PER_TELEPORT);
        //logMarkers(MOVED_FORWARD);
    }

    private void changeUserOrientation() {
        if (currentMovementLabel == MovementStateMachine.RIGHT) {
            playerTransform.Rotate(0, ROTATION_ANGLE_CHECKPOINT, 0);
        } else if (currentMovementLabel == MovementStateMachine.LEFT) {
            playerTransform.Rotate(0, 360 - ROTATION_ANGLE_CHECKPOINT, 0);
        }
        //logMarkers(ORIENTATION_CHANGED);
    }

    private bool isSliderFull(UnityEngine.UI.Slider slider) {
        return slider.value >= slider.maxValue;
    }

    private void moveForward(float deltaTime)
    {      
        float newStrength = vrTeleporter.strength + (deltaTime * STRENGTH_SPEED);
        float maxStrength = STRENGTH_DISTANCE_CHECKPOINT + originalVRTeleporterStrength;
        float newForwardStrength = Mathf.Min(newStrength, maxStrength);
        vrTeleporter.strength = newForwardStrength;
    
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
        logMarkers(BCI_PAUSED);
        if (!IS_ART_FEEDBACK) classificationStopWatch.Reset();
        yield return new WaitForSeconds(seconds);
        movementStateMachine.isBCIOn = true;
        if (IS_ART_FEEDBACK) movementStateMachine.updateArtFBStateMachine(true);
        else classificationStopWatch.Start();
        logMarkers(BCI_UNPAUSED);
    }

    public void temporarilyTurnBCIOff(float seconds)
    {
        if (movementStateMachine.isBCIOn) {
            StartCoroutine(TurnBCIOff(seconds));
        } 
    }

    public void logMarkers(int marker) {
        logger.logMarkers(marker, totalStopWatch.ElapsedMilliseconds.ToString());
        if (!IS_ART_FEEDBACK) logger.queueMarkers(marker);
    }

    public void logClassificationResult(int trueLabel, int predictedLabel) {
        if (!movementStateMachine.isBCIOn) return; 
        if (classificationStopWatch.IsRunning && classificationStopWatch.ElapsedMilliseconds >= classificationResultLogInterval)
        {
            logger.logClassificationResults(trueLabel, predictedLabel, totalStopWatch.ElapsedMilliseconds.ToString());
            classificationStopWatch.Reset();
            classificationStopWatch.Start();
            if (!SUPPRESS_UI_LOGS) UnityEngine.Debug.Log("Logging Classification Result while stopwatch running @ time: " + totalStopWatch.ElapsedMilliseconds.ToString());
        }
    }

    public void logLabels(int label) {
        logger.logLabels(label, totalStopWatch.ElapsedMilliseconds.ToString());
    }
}