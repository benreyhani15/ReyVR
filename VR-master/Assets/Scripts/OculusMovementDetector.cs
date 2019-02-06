using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;

public class OculusMovementDetector
{
    private Transform cameraAnchor;
    private bool isOculusExcessivelyMoving;
    private static float detectionWindowTime = 50f; // looks at change in values within 'x' ms
    private static float deltaThreshold = 1f;
    private Stopwatch stopWatch;
    private Vector3 rotationValues;

    public OculusMovementDetector(Transform eyeCameraAnchor) {
        cameraAnchor = eyeCameraAnchor;
        isOculusExcessivelyMoving = false;
        stopWatch = new Stopwatch();
    }

    public void startDetector()
    {
        stopWatch.Start();
        rotationValues = getRotationValues();
    }

    public void resetAndStopDetector() {
        stopWatch.Reset();
    }

    public bool updateDetector() {
        if (stopWatch.ElapsedMilliseconds >= detectionWindowTime) {
            Vector3 newRotationValues = getRotationValues();
            isOculusExcessivelyMoving = Math.Abs(newRotationValues.x - rotationValues.x) >= deltaThreshold ||
                    Math.Abs(newRotationValues.y - rotationValues.y) >= deltaThreshold ||
                        Math.Abs(newRotationValues.z - rotationValues.z) >= deltaThreshold;
            rotationValues = newRotationValues;
            stopWatch.Reset();
            stopWatch.Start();
        }
        return isOculusExcessivelyMoving;
    }

    private Vector3 getRotationValues() {
        float x = (float) Math.Round((cameraAnchor.rotation.x * 100), 2);
        float y = (float)Math.Round((cameraAnchor.rotation.y * 100), 2);
        float z = (float)Math.Round((cameraAnchor.rotation.z * 100), 2);
        return new Vector3(x, y, z);
    }
}
