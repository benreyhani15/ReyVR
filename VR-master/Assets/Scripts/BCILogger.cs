using System;
using System.IO;

public class BCILogger
{
    private String markersFile;
    private String timeFile;
    private bool useOneFile;
    private String upFolderName;
    private String classificationResultFile;
    private bool feedbackSession;

    public BCILogger(String markersPath, String timePath)
    {
        markersFile = markersPath;
        timeFile = timePath;
        useOneFile = false;
        UnityEngine.Debug.Log(markersFile);
    }

    public BCILogger(String pathFromApplicationDirectory, int trialNumber, bool isForFeedbackSession) {
        useOneFile = true;
        // Participants name, create file based on date
        upFolderName = pathFromApplicationDirectory;
        string date = System.DateTime.Now.ToString("MM-dd-yyyy");
        markersFile = "Logs/" + upFolderName + "/markers:" + date+"-"+trialNumber.ToString();
        feedbackSession = isForFeedbackSession;
        if (isForFeedbackSession) {
            classificationResultFile = "Logs/" + upFolderName + "/classification_results:" + date + "-" + trialNumber.ToString();
        }
    }

    public void logMarkers(int marker, String timeFromStartMS) {
        appendToTextFile(marker.ToString(), timeFromStartMS);
        sendMarkers(marker);
    }

    public void logClassificationResults(int trueLabel, int predictedLabel, String timeMS) {
        using (StreamWriter sw = new StreamWriter(classificationResultFile, true))
        {
            sw.WriteLine(trueLabel + "," + predictedLabel + "," + timeMS);
        }
    }

    public void appendToTextFile(String marker, String timeMS)
    {

        if (useOneFile) {
            using (StreamWriter sw = new StreamWriter(markersFile, true))
            {
                sw.WriteLine(marker + "," + timeMS);
            }

        } else {
            using (StreamWriter sw = new StreamWriter(markersFile, true))
            {
                sw.WriteLine(marker);
            }

            using (StreamWriter sw = new StreamWriter(timeFile, true))
            {
                sw.WriteLine(timeMS);
            }
        }

    }

    private Boolean sendMarkers(int marker) {
        // TODO: Connect to whatever medium to send markers to EEG computer
        return true;
    }
}