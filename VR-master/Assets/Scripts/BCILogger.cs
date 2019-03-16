using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public class BCILogger
{
    private String markersFile;
    private String timeFile;
    private bool useOneFile;
    private String upFolderName;
    private String classificationResultFile;
    private bool feedbackSession;

    // For parallel port writing
    protected const short LPT1 = 888;
    protected const short LPT2 = 632;

    private Queue<int> markerQueue;

    static class InpOut32_Declarations
    {
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        public static extern void Output(int address, int value);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        public static extern int Input(int address);
        /*
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUlong")]
        public static extern void DlPortWritePortUlong_x64(int PortAddress, uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUlong")]
        public static extern uint DlPortReadPortUlong_x64(int PortAddress);
        
         * /*
        [System.Runtime.InteropServices.DllImport("inpoutx64")]
        public static extern short ReadParallel64(short PortAddress);
        [System.Runtime.InteropServices.DllImport("inpoutx64")]
        public static extern void WriteParallel64(short PortAddress, short value);
       
        [System.Runtime.InteropServices.DllImport("inpout32")]
        public static extern short ReadParallel32(short PortAddress);
        [System.Runtime.InteropServices.DllImport("inpout32")]
        public static extern void WriteParallel32(short PortAddress, short value);*/
    }

    public BCILogger(String pathFromApplicationDirectory, String sessionType, int trialNumber, bool isForFeedbackSession) {
        useOneFile = true;
        // Participants name, create file based on date
        string date = System.DateTime.Now.ToString("MM-dd-yyyy");
        markersFile = "Logs/" + pathFromApplicationDirectory + "/markers_" + date + "_" + sessionType + "_" +trialNumber.ToString() + ".txt";
        feedbackSession = isForFeedbackSession;
        if (isForFeedbackSession) {
            classificationResultFile = "Logs/" + pathFromApplicationDirectory + "/classification_results_" + date + "_" + sessionType + "_" + trialNumber.ToString() + ".txt";
        }
        markerQueue = new Queue<int>();
        checkValidParallelPort();
    }

    public void logMarkers(int marker, String timeFromStartMS) {
        UnityEngine.Debug.LogWarning("Logging markers: " + marker + "@: " + timeFromStartMS);
        appendToTextFile(marker.ToString(), timeFromStartMS);
    }

    public void queueMarkers(int marker) {
        markerQueue.Enqueue(marker);
        markerQueue.Enqueue(0);
    }

    public void logClassificationResults(int trueLabel, int predictedLabel, String timeMS) {
        using (StreamWriter sw = new StreamWriter(classificationResultFile, true))
        {
            sw.WriteLine(trueLabel + "," + predictedLabel + "," + timeMS);
        }
    }

    public void logLabels(int trueLabel, String timeMS)
    {
        using (StreamWriter sw = new StreamWriter(classificationResultFile, true))
        {
            sw.WriteLine(trueLabel +  "," + timeMS);
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

    public void sendMarkers(string timeMS) {
        if (markerQueue.Count > 0)
        {
            int marker = markerQueue.Dequeue();
            WriteParallel(marker);
            UnityEngine.Debug.LogWarning("Marker sent: " + marker + "@: " + timeMS);
        }
    }
      
    private int ReadParallel()
    {
        int marker = -1;
        try
        {
            marker = InpOut32_Declarations.Input(LPT1);
        }
        catch (Exception e) {
            UnityEngine.Debug.Log(e.ToString());
        }
        return marker;
    }

    private bool WriteParallel(int marker)
    {
        bool success = true;
        try
        {
            InpOut32_Declarations.Output(LPT1, marker);
            //UnityEngine.Debug.Log("Wrote marker "+ marker + " to " + LPT1);
        }
        catch (Exception e) {
            UnityEngine.Debug.Log(e.ToString());
            success = false;
        }
        return success;
    }
    
    private void checkValidParallelPort() {
        try
        {
            WriteParallel(10);
            UnityEngine.Debug.Log(ReadParallel());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex.ToString());
        }
    }
}