using UnityEngine;
using System.Collections;


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
    }

}