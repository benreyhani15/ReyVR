using System.Collections;
using UnityEngine;

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
    public bool showArtifactAlert = true;

    // Use this for initialization
    void Start () {
        stateMachine = new NoFeedbackStateMachine(this, participantName, trialNumber, eyeCamera);
        arrow.SetActive(false);
        relax.SetActive(false);
        cross.SetActive(false);
        artifactUI.SetActive(showArtifactAlert);
        if (showArtifactAlert)
            artifactUI.GetComponent<Renderer>().material.color = Color.green;
    }

    // Update is called once per frame
    void Update () {
        if (!stateMachine.isSessionStarted() && Input.GetKeyDown(KeyCode.Return)) {
            stateMachine.startSession();
        }

        if (stateMachine.isSessionStarted()) {
            // Launch specific coroutine
            if (!stateMachine.isLocked) {
                stateMachine.update();
            }
            stateMachine.updateExternalBuffers();
            if (artifactUI != null && showArtifactAlert) {
                artifactUI.GetComponent<Renderer>().material.color = stateMachine.isExcessiveBodyMovement() ? Color.red : Color.green;
            }
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
            Application.Quit();
        }
    }
}