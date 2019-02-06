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

    public BCILogger(String pathFromApplicationDirectory, String sessionType, int trialNumber, bool isForFeedbackSession) {
        useOneFile = true;
        // Participants name, create file based on date
        string date = System.DateTime.Now.ToString("MM-dd-yyyy");
        markersFile = "Logs/" + pathFromApplicationDirectory + "/markers_" + date + "_" + sessionType + "_" +trialNumber.ToString() + ".txt";
        feedbackSession = isForFeedbackSession;
        if (isForFeedbackSession) {
            classificationResultFile = "Logs/" + pathFromApplicationDirectory + "/classification_results_" + date + "_" + sessionType + "_" + trialNumber.ToString() + ".txt";
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