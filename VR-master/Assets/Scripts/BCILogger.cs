using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        appendToTextFile(marker.ToString(), timeFromStartMS);
        markerQueue.Enqueue(marker);
        markerQueue.Enqueue(0);
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

    public void sendMarkers() {
        if (markerQueue.Count > 0)
        {
            bool sent = WriteParallel(markerQueue.Dequeue());
        }
    }

    private int ReadParallel()
    {
        return InpOut32_Declarations.Input(LPT2);
    }

    private bool WriteParallel(int marker)
    {
        InpOut32_Declarations.Output(LPT2, 0);
        return true;
    }
    
    private void checkValidParallelPort() {
        try
        {
            WriteParallel(10);
            UnityEngine.Debug.Log(ReadParallel());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("cant access Parallel port");
        }
    }
}