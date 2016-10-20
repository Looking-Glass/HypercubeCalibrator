using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//this autocalibrator is a state machine
//since presumably we can have many different kinds of calibration types and layouts of slices, detecting them all with one piece
// of code is only sane if we have a different state that handles the different types of calibration.

//generic features are few, but include methods for detecting peaks in the light sensor data, and for displaying a gradient on screen


namespace hypercube
{

    public class autoCalibrator : MonoBehaviour
    {

        [Tooltip("How many sensors in the LED sensor array")]
        public int xArticulation = 3;
        public int yArticulation = 3;

        [Tooltip("These will hide and show based on what is being seen.")]
        public GameObject[] indicators;

        SerialController serial;
        public float threshold = .7f;

        public GameObject gradientLine;
        public GameObject gradientMesh;

        public castMesh canvas;

        autoCalibratorModule currentModule;

        //modules
        sampleLines_inline inline;

        // Use this for initialization
        void Awake()
        {
            inline = new sampleLines_inline();
        }

        void Start()
        {
            setModule(inline);  //TEMP!!!  this should be set from a gui option

            //set up comm to our light sensors in the hardware
            //SerialController serial = gameObject.AddComponent<SerialController>();
            //serial.portName = getPortName();
            //serial.reconnectionDelay = 500;
            //serial.maxUnreadMessages = 100;
            //serial.maxFailuresAllowed = 3;
            //serial.enabled = true;

            //dataFileDict vars = canvas.GetComponent<dataFileDict> ();

        }


        static string getPortName()
        {
            string[] allSerialPorts = hypercube.input.getPortNames();
            foreach (string n in allSerialPorts)
            {
                if (n.Contains("usbmodem")) //TEMP! turn this into the real hardware name, not default arduino mega name!
                    return n;
            }
            return "NOT FOUND!";
        }

        public void setModule(autoCalibratorModule m)
        {
            if (currentModule != null)
                currentModule.end(this);

            currentModule = m;

            if (currentModule != null)
                currentModule.start(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (currentModule != null)
                currentModule.update(this);

            if (!serial)
                return;

            //do we see a lit up screen? We only care about the most recent message.
            string data = serial.ReadSerialMessage();
            string lastData = null;
            if (data == null) // not data this frame.
                return;

            lastData = data;
            while (data != null)
            {              
                data = serial.ReadSerialMessage();
                if (data != null)
                    lastData = data;
            }

            string[] arr = lastData.Split(' ');

            for(int i = 0; i < arr.Length; i ++)
            {
                float v = dataFileDict.stringToFloat(arr[i], 0f);
                if (v > threshold)
                    indicators[i].SetActive(true);
                else
                    indicators[i].SetActive(false);
            }
        }

        public void setLine(float posX, float posY, float w, float h, bool horizontal, bool onOff = true)
        {
            float screenX = 0f;
            float screenY = 0f;
            canvas.getScreenDims(ref screenX, ref screenY);
            float pixelX = 1f/screenX;
            float pixelY = 1f/screenY;

            float aspectRatio = canvas.getScreenAspectRatio();
            gradientLine.transform.localScale = new Vector3( 2f * pixelX * w * aspectRatio, 2f * pixelY * h, 1f);

            if (horizontal)
                gradientMesh.transform.localRotation = Quaternion.identity;
            else
                gradientMesh.transform.localRotation = Quaternion.Euler(0f,0f,90f);

            gradientLine.transform.localPosition = new Vector3((2f * posX * pixelX * aspectRatio) - ( pixelX * screenX *  aspectRatio), 1f-(posY * pixelY * 2f), 0f);

        }

        public Vector3 screenCoordToLocalCoord(float x, float y)
        {
            float screenX = 0f;
            float screenY = 0f;
            canvas.getScreenDims(ref screenX, ref screenY);
            float pixelX = 1f / screenX;
            float pixelY = 1f / screenY;

            float aspectRatio = canvas.getScreenAspectRatio();
            return new Vector3((2f * x * pixelX * aspectRatio) - (pixelX * screenX * aspectRatio), 1f - (y * pixelY * 2f), 0f);
        }

        /// <summary>
        /// This method will extract 'peaks' from noisy data, returning the positions in the array where it thinks the peaks occurred.
        /// Note that the returned values are floats: It uses averages to figure out where it thinks the true peak is which may lie 'in between' the given data points.
        /// </summary>
        /// <param name="minPeakAmplitude">The minimum 'height' a peak should be.</param>
        /// <param name="minPeakFreq">The minimum 'width' of expected peaks. This will keep it from returning noise as peaks.</param>
        /// <param name="data">The data to obtain the peaks from.</param>
        /// <returns></returns>
        public static float[] getPeaksFromData(int minPeakAmplitude, int minPeakFreq, int[] data)
        {
            data = new int[]
            {
                75, 76, 75, 74, 76, 74, 77, 77, 79, 79, 80, 80, 81, 81, 80, 80, 80, 82, 82, 81, 83, 83, 84, 84, 82,
                83, 85, 82, 83, 84, 86, 85, 85, 83, 84, 84, 81, 78, 77, 78, 78, 77, 78, 76, 75, 75, 79, 79, 76, 74,
                73, 75, 75, 77, 76, 74, 74, 74, 74, 76, 74, 76, 76, 75, 74, 76, 76, 73, 73, 74, 77, 78, 79, 79, 77,
                74, 71, 74, 74, 77, 75, 75, 79, 80, 78, 75, 79, 80, 77, 77, 75, 79, 79, 79, 79, 79, 76, 77, 76, 80,
                76, 77, 75, 78, 78, 77, 77, 78, 78, 78, 77, 75, 77, 77, 76, 75, 76, 76, 78, 77, 79, 78, 76, 79, 80,
                79, 81, 81, 83, 82, 84, 84, 85, 87, 88, 88, 88, 89, 88, 89, 87, 87, 86, 87, 86, 86, 85, 82, 83, 82,
                82, 81, 79, 80, 80, 78, 77, 76, 76, 74, 72, 74, 73, 71, 74, 74, 74, 75, 74, 74, 74, 76, 77, 78, 79,
                78, 79, 79, 75, 75, 76, 75, 75, 77, 77, 77, 76, 77, 79, 78, 80, 82, 82, 81, 82, 83, 83, 82, 84, 84,
                84, 83, 83, 83, 85, 83, 83, 85, 85, 84, 85, 81, 80, 82, 82, 81, 83, 85, 83, 84, 85, 86, 86, 88, 88,
                89, 90, 89, 88, 90, 91, 91, 91, 92, 93, 94, 94, 93, 93, 93, 94, 97, 97, 99, 100, 101, 101, 102, 102,
                101, 101, 99, 98, 97, 99, 98, 95, 95, 94, 93, 92, 90, 89, 88, 89, 88, 87, 89, 89, 88, 89, 88, 84, 86,
                83, 84, 84, 85, 86, 81, 79, 76, 79, 81, 85, 85, 85, 85, 85, 85, 85, 86, 86, 86, 86, 87, 87, 86, 84,
                83, 84, 85, 86, 85, 85, 86, 86, 84, 84, 85, 85, 83, 82, 86, 88, 86, 84, 83, 89, 89, 89, 89, 89, 88,
                86, 86, 88, 90, 88, 89, 89, 91, 90, 89, 91, 89, 87, 88, 88, 89, 87, 90, 94, 93, 95, 96, 96, 97, 99,
                101, 100, 102, 102, 103, 106, 107, 107, 104, 104, 102, 99, 99, 97, 93, 93, 91, 88, 89, 88, 88, 86, 85,
                86, 86, 86, 87, 87, 88, 88, 89, 88, 87, 86, 88, 85, 84, 85, 84, 86, 85, 82, 81, 82, 81, 82, 81, 82,
                81, 83, 83, 85, 88, 86, 87, 87, 88, 87, 88, 83, 85, 87, 84, 87, 86, 84, 84, 88, 89, 88, 87, 88, 86,
                88, 91, 92, 92, 91, 85, 87, 89, 90, 90, 89, 89, 88, 91, 91, 91, 91, 91, 90, 92, 91, 87, 90, 91, 93,
                92, 93, 95, 95, 96, 96, 96, 98, 99, 98, 99, 102, 103, 105, 106, 107, 109, 110, 111, 112, 110, 109,
                110, 108, 107, 105, 102, 102, 99, 98, 96, 95, 95, 94, 92, 91, 91, 91, 91, 90, 91, 91, 90, 90, 89, 89,
                88, 90, 88, 89, 90, 90, 90, 90, 89, 88, 88, 88, 90, 89, 88, 88, 88, 89, 88, 89, 89, 89, 88, 90, 90,
                90, 88, 90, 90, 90, 90, 89, 90, 88, 89, 90, 90, 90, 90, 90, 90, 90, 88, 88, 90, 88, 88, 89, 89, 88,
                90, 90, 88, 88, 90, 88, 88, 90, 89, 90, 90, 90, 90, 89, 91, 91, 90, 90, 91, 91, 94, 94, 95, 95, 97,
                99, 98, 99, 102, 104, 104, 106, 105, 106, 107, 108, 103, 105, 100, 100, 99, 99, 97, 96, 95, 94, 93,
                92, 91, 90, 89, 88, 88, 87, 87, 86, 87, 85, 84, 85, 87, 86, 84, 84, 85, 86, 82, 84, 82, 83, 83, 84,
                83, 84, 86, 85, 85, 85, 85, 85, 86, 83, 83, 84, 83, 85, 85, 88, 87, 81, 81, 83, 86, 85, 81, 78, 79,
                80, 82, 85, 83, 82, 86, 85, 86, 82, 83, 84, 84, 84, 84, 84, 82, 80, 84, 82, 83, 81, 81, 82, 83, 83,
                83, 85, 86, 87, 90, 87, 89, 88, 95, 96, 97, 97, 101, 103, 103, 106, 108, 111, 114, 114, 117, 118, 115,
                115, 113, 111, 109, 106, 102, 98, 96, 95, 93, 93, 93, 90, 88, 88, 87, 85, 85, 86, 85, 85, 84, 83, 83,
                83, 83, 84, 85, 85, 85, 85, 85, 83, 84, 85, 84, 81, 81, 83, 80, 81, 82, 80, 82, 82, 82, 81, 79, 80,
                81, 85, 84, 81, 81, 82, 82, 82, 82, 82, 84, 83, 81, 82, 84, 82, 83, 83, 83, 83, 80, 83, 85, 82, 81,
                81, 81, 79, 79, 77, 79, 77, 80, 80, 79, 79, 83, 84, 82, 83, 81, 84, 83, 84, 85, 84, 87, 90, 88, 88,
                91, 94, 95, 96, 98, 99, 102, 100, 104, 102, 105, 105, 102, 98, 97, 97, 94, 91, 87, 85, 82, 82, 81, 82,
                81, 80, 79, 79, 78, 78, 77, 76, 74, 77, 79, 76, 74, 72, 73, 75, 75, 75, 74, 73, 74, 73, 77, 79, 76,
                76, 79, 79, 80, 80, 81, 81, 81, 81, 81, 80, 80, 80, 80, 80, 81, 81, 81, 81, 81, 81, 80, 81, 81, 80,
                80, 81, 81, 82, 80, 80, 80, 78, 77, 80, 77, 81, 81, 79, 79, 78, 78, 77, 79, 77, 79, 81, 79, 78, 76,
                77, 76, 80, 80, 79, 76, 76, 77, 82, 81, 83, 86, 92, 89, 95, 96, 101, 104, 108, 111, 113, 116, 117,
                116, 115, 112, 108, 100, 97, 96, 92, 87, 85, 83, 80, 77, 75, 76, 73, 72, 73, 73, 74, 73, 72, 75, 75,
                73, 74, 74, 75, 73, 73, 73, 72, 72, 73, 74, 74, 69, 70, 71, 71, 68, 68, 70, 71, 68, 73, 73, 73, 73,
                73, 73, 71, 72, 73, 73, 72, 73, 73, 74, 71, 71, 71, 71, 71, 73, 70, 69, 72, 74, 72, 72, 73, 72, 73,
                73, 71, 73, 73, 71, 71, 73, 71, 74, 73, 73, 73, 73, 72, 69, 68, 69, 70, 72, 72, 71, 72, 74, 74, 76,
                80, 83, 85, 88, 92, 94, 96, 100, 106, 107, 110, 108, 107, 105, 101, 97, 94, 93, 88, 85, 84, 81, 78,
                76, 73, 72, 71, 70, 70, 69, 68, 68, 66, 66, 66, 67, 67, 67, 65, 63, 64, 64, 62, 62, 64, 64, 65, 65,
                64, 65, 65, 65, 65, 65, 64, 65, 64, 65, 63, 64, 64, 64, 64, 64, 62, 61, 63, 62, 59, 59, 57, 58
            };


            //first, we will filter out what parts of the data count as a part of a peak.
            int halfFreq = Mathf.RoundToInt((float)minPeakFreq/2f);
            int first = 0;
            int last = 0;
            List<int> peakData = new List<int>(); //store here any values that meet basic peak criteria
            for (int i = 0; i < data.Length; i++)
            {
                first = i - halfFreq;
                last = i + halfFreq;

                if (first < 0 && last >= data.Length)
                    Debug.LogError("Data set is too small to analyze using a minPeakFreq of " + minPeakFreq);

                if (first < 0) //near start of analysis
                {
                    if (checkPeak(0, data[i], data[last], minPeakAmplitude))
                        peakData.Add(i);
                }
                else if (last >= data.Length) //near end of analysis
                {
                    if (checkPeak(data[first], data[i], 0, minPeakAmplitude))
                        peakData.Add(i);
                }
                else //normal path
                {
                    if (checkPeak(data[first], data[i], data[last], minPeakAmplitude))
                        peakData.Add(i);
                }
            }

            if (peakData.Count == 0) //sanity check
            {
                Debug.LogWarning("Could not find any peaks in the given data. Is the sensor broken? Printing raw data to log...");
                Debug.Log(data);
                return new float[0]; //could not find peaks
            }
                

            //now we know what parts of the data are part of peaks. Let's arrange them into groups (peaks).
            //maxAllowedMissingDataPoints will be used to separate out the peaks from each other.
            List<List<int>> noisyPeaks = new List<List<int>>();    
            int maxAllowedMissingDataPoints =  Mathf.RoundToInt((float)minPeakFreq / 10f);
            if (maxAllowedMissingDataPoints < 3)
                maxAllowedMissingDataPoints = 3;
            int currentPeak = 0;
            noisyPeaks.Add(new List<int>());
            for (int d = 0; d < peakData.Count -1; d++)
            {
                if (peakData[d + 1] - peakData[d] < maxAllowedMissingDataPoints)
                    noisyPeaks[currentPeak].Add(peakData[d]);
                else
                {
                    noisyPeaks.Add(new List<int>());
                    currentPeak++;
                }
            }

            //lets, ignore any peaks with very few data points (which are probably noise)
            List<List<int>> peaks = new List<List<int>>();
            for (int p = 0; p < noisyPeaks.Count; p++)
            {
                if (noisyPeaks[p].Count >= 3)
                    peaks.Add(noisyPeaks[p]);
            }


            //now lets do a biased average of the data contained in each peak, and return that as our final values.
            List<float> output = new List<float>();
            foreach (List<int> p in peaks)
            {
                output.Add(findPeakInDataPoints(p, data, .8f));
            }

            return output.ToArray();
        }

        static bool checkPeak(int first, int middle, int last, int threshold)
        {
            if (middle - first > threshold && middle - last > threshold)
                return true;
            return false;
        }

        // use a weighted average to find the peak
        static float findPeakInDataPoints(List<int> dataPoints, int[] allData, float bias)
        {
            double total = 0;
            double div = 0;
            foreach (int d in dataPoints)
            {
                total += d * allData[d];
                div += allData[d];
            }
            return (float)(total/div);
        }

    }
}