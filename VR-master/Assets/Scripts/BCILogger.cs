using System;
using System.IO;

public class BCILogger {
    private String markersFile;
    private String timeFile;

    public BCILogger(String markersPath, String timePath) {
        markersFile = markersPath;
        timeFile = timePath;
    }

    public void appendToTextFile(String marker, String timeMS) {
        using (StreamWriter sw = new StreamWriter(markersFile, true)) {
            sw.WriteLine(marker);
        }

        using (StreamWriter sw = new StreamWriter(timeFile, true))
        {
            sw.WriteLine(timeMS);
        }
    }
}