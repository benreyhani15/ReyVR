using System.Collections;
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

    //public static int IDLE_MARKER = 0;
    public static int RIGHT_MARKER = 0;
    public static int FORWARD_MARKER = 1;
    public static int LEFT_MARKER = 2;
    public static int BLANK_SCREEN_MARKER = -1;
    public static int CROSS_MARKER = -2;
    public static int END_MARKER = -3;

    // Each session consists of PrepCross --> Icon --> BlankScreen
    public enum TrialState
    {
        PrepCross,
        Icon,
        BlankScreen
    };

    public NoFeedbackStateMachine(NoFeedbackView view)
    {
        ui = view;
        taskController = new TaskController();
        string date = System.DateTime.Now.ToString("MM-dd-yyyy");
        logger = new BCILogger("Logs/"+date+"/ben_1_test_markers.txt"
            , "Logs/"+date+"/ben_1_test_time.txt");
        firstUpdate = true;
    }

    public bool isSessionStarted() {
        return sessionStarted;
    }

    public void update()
    {
        // Update state machine and update UI 
        updateState();

        if (currentTrialState == TrialState.PrepCross &&
            taskController.taskCount == Constants.NUMBER_OF_CLASSES * Constants.TRIALS_PER_CLASS_PER_SESSION)
        {
            // Session is over
            logger.appendToTextFile(END_MARKER.ToString(), stopWatch.ElapsedMilliseconds.ToString());
            stopWatch.Stop();
            ui.sessionOver();
        }
        else {
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
        if (currentTrialState == TrialState.BlankScreen) {
            ui.arrow.SetActive(false);
            ui.relax.SetActive(false);
            ui.cross.SetActive(false);
            logger.appendToTextFile(BLANK_SCREEN_MARKER.ToString(), firstUpdate ? "0" : stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log("Blank Screen");
        } else if (currentTrialState == TrialState.Icon) {
            TaskController.TrialTask task = taskController.generateTaskRandomly();
            ui.cross.SetActive(false);
            ui.setTaskIcon(task);
            logger.appendToTextFile(((int)task).ToString(), stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log(task.ToString());
        }
        else {
            ui.arrow.SetActive(false);
            ui.relax.SetActive(false);
            ui.cross.SetActive(true);
            logger.appendToTextFile(CROSS_MARKER.ToString(), stopWatch.ElapsedMilliseconds.ToString());
            UnityEngine.Debug.Log("Cross");
        }
    }

    public void startSession() {
        sessionStarted = true;
        stopWatch = new Stopwatch();
        stopWatch.Start();
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
        if (firstUpdate) {
            currentTrialState = TrialState.BlankScreen;
        } else {
            int newStateIndex = ((int)currentTrialState + 1) % 3;
            currentTrialState = (TrialState)newStateIndex;
        }
    }

    IEnumerator StayIdle(float secondsIdle)
    {
        yield return new WaitForSeconds(secondsIdle);
        isLocked = false;
    }
}