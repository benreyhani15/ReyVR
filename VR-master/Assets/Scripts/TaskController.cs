using System.Collections;
using UnityEngine;

public class TaskController {
    private int[] trialCounts;
    public int taskCount;

    public enum TrialTask {
// Relax,
        Right,
        Forward,
        Left
    };

    public TaskController() {
        taskCount = 0;
        trialCounts = new int [] {Constants.TRIALS_PER_CLASS_PER_SESSION, Constants.TRIALS_PER_CLASS_PER_SESSION, 
            Constants.TRIALS_PER_CLASS_PER_SESSION};
    }

    public TrialTask generateTaskRandomly() {
        int taskIndex = chooseRandomly(getTaskProbs());
        trialCounts[taskIndex]--;
        taskCount++;
        return (TrialTask)taskIndex;
    }

    private int chooseRandomly(float[] probs) {
        
        float total = 0; 
        foreach (float elem in probs) {
            total += elem;
        }

        float randomPoint = Random.value * total;

        for (int i = 0; i < probs.Length; i++) {
            if (randomPoint < probs[i]) {
                return i;
            } else {
                randomPoint -= probs[i];
            }
        }
        return probs.Length - 1;
    }

    private float [] getTaskProbs() {
        float [] probs = new float[Constants.NUMBER_OF_CLASSES];
        int trialsRemaining = (Constants.NUMBER_OF_CLASSES * Constants.TRIALS_PER_CLASS_PER_SESSION) - taskCount;
        for (int i = 0; i < probs.Length; i++) {
            probs[i] = (float)trialCounts[i] / (float) trialsRemaining;
        }
        return probs;
    }
}