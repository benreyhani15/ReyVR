using System.Collections;
using UnityEngine;
using System.Diagnostics;

public class NoFeedbackView : MonoBehaviour {
    public GameObject arrow;
    public GameObject relax;
    public GameObject quad;
    public GameObject cross;

    public string participantName = "Ben";
    public int trialNumber = 1;

    public Transform eyeCamera;

    private NoFeedbackStateMachine stateMachine;

    public UnityEngine.GameObject artifactUI;
    public UnityEngine.GameObject stopUI;

    private static bool TEST_OCULUS_MOVEMENT = false;

    // Use this for initialization
    void Start () {
        stateMachine = new NoFeedbackStateMachine(this, participantName, trialNumber, eyeCamera);
        arrow.SetActive(false);
        relax.SetActive(false);
        cross.SetActive(false);

        if (TEST_OCULUS_MOVEMENT) {
            artifactUI.SetActive(true);
            stopUI.SetActive(false);
            artifactUI.GetComponent<Renderer>().material.color = Color.green;
        } else {
            artifactUI.SetActive(false);
            stopUI.SetActive(false);
        }
    }

    private void OnApplicationQuit()
    {
        if (this.isActiveAndEnabled) {
            UnityEngine.Debug.LogWarning("On Application quit");
        }
    }

    // Update is called once per frame
    void Update () {
        Stopwatch totalStopWatch = new Stopwatch();
        totalStopWatch.Start();
        if (Time.deltaTime > 30) UnityEngine.Debug.LogWarning("Update with time DELTA: " + Time.deltaTime);
        if (!stateMachine.isSessionStarted() && Input.GetKeyDown(KeyCode.Return)) {
            stateMachine.startSession();
        }

        if (stateMachine.isSessionStarted()) {
            // Launch specific coroutine
            if (!stateMachine.isLocked) {
                stateMachine.update();
            }

            stateMachine.updateExternalBuffers();
        }

        updateStopUI();
        stateMachine.sendMarkers();
        if (totalStopWatch.ElapsedMilliseconds > 30) UnityEngine.Debug.LogWarning("Update took @: " + (totalStopWatch.ElapsedMilliseconds));
    }

    private void updateStopUI() {
        if (TEST_OCULUS_MOVEMENT) {
            if (artifactUI != null) artifactUI.GetComponent<Renderer>().material.color = stateMachine.isExcessiveBodyMovement() ? Color.red : Color.green;
        } else {
            if (stopUI != null) stopUI.SetActive(stateMachine.isExcessiveBodyMovement());
        }
    }

    public void setStandActive(bool active) {
        quad.SetActive(active);
    }

    public void setStandVisibility(float alpha) {
        //TODO:FIX THIS
        Renderer rend = quad.GetComponent<Renderer>();
        Color c = rend.material.color;
        c.a = alpha;
        rend.material.SetColor("_Color", c);
    }

    public void startCoroutine(IEnumerator e) {
        StartCoroutine(e);
    }

    public void setTaskIcon(TaskController.TrialTask task) {
        if (task.Equals(TaskController.TrialTask.Forward) || task.Equals(TaskController.TrialTask.Right) ||
            task.Equals(TaskController.TrialTask.Left)) {
            arrow.SetActive(true);
            relax.SetActive(false);
            float zAngle = 0;

            SpriteRenderer spr = arrow.GetComponent<SpriteRenderer>();
            Sprite arrowIcon = Resources.Load<Sprite>("ArrowRight");
            if (task == TaskController.TrialTask.Left) arrowIcon = Resources.Load<Sprite>("ArrowLeft");
            if (task == TaskController.TrialTask.Forward) arrowIcon = Resources.Load<Sprite>("ArrowUp");
            spr.sprite = arrowIcon;
        } /*else {
            arrow.SetActive(false);
            relax.SetActive(true);
        }*/
    }

    public void sessionOver() {
        // Do something indicating session over
        if (Application.isEditor) {
        // UnityEditor.EditorApplication.isPlaying = false;
        } else {
            //Application.Quit();
        }
    }
}