using UnityEngine;
using System.Collections;

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

        public RectTransform horizontal;
        public RectTransform vertical;

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
            SerialController serial = gameObject.AddComponent<SerialController>();
            serial.portName = getPortName();
            serial.reconnectionDelay = 500;
            serial.maxUnreadMessages = 100;
            serial.maxFailuresAllowed = 3;
            serial.enabled = true;

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
    }

}