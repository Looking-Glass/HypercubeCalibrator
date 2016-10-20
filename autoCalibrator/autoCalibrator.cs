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
                    if (checkPeak(0f, data[i], data[last], minPeakAmplitude))
                        peakData.Add(i);
                }
                else if (last >= data.Length) //near end of analysis
                {
                    if (checkPeak(data[first], data[i], 0f, minPeakAmplitude))
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
                    noisyPeaks[currentPeak].Add(d);
                else
                {
                    noisyPeaks.Add(new List<int>());
                    currentPeak++;
                }
            }

            //lets, ignore any peaks with very few data points (which are probably mostly noise)
            int thirdFreq = Mathf.RoundToInt((float)minPeakFreq / 3f);
            List<List<int>> peaks = new List<List<int>>();
            for (int p = 0; p < noisyPeaks.Count; p++)
            {
                if (noisyPeaks[p].Count >= thirdFreq)
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
            ulong total = 0;
            ulong div = 0;
            foreach (int d in dataPoints)
            {
                total += (ulong)(d * allData[d]);
                div += (ulong)allData[d];
            }
            return (float)(total/div);
        }

    }
}