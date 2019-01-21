using System;
using System.IO;

public class BCILogger
{
    private String markersFile;
    private String timeFile;

    public BCILogger(String markersPath, String timePath)
    {
        markersFile = markersPath;
        timeFile = timePath;
        UnityEngine.Debug.Log(markersFile);
    }

    public void appendToTextFile(String marker, String timeMS)
    {
        using (StreamWriter sw = new StreamWriter(markersFile, true))
        {
            sw.WriteLine(marker);
        }

        using (StreamWriter sw = new StreamWriter(timeFile, true))
        {
            sw.WriteLine(timeMS);
        }
    }

    private Boolean sendMarkers(int marker) {
        // TODO: Connect to whatever medium to send markers to EEG computer
        return true;
    }
}