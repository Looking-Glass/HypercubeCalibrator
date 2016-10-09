using UnityEngine;
using System.Collections;

namespace hypercube
{
	public class vertexCalibrator : calibrator {

		[Tooltip("What are the dimensions of the LED sensor array")]
		public int xArticulation = 3;
		public int yArticulation = 3;

		SerialController serial;
		public float threshold = .7f;

		public Material displayMat;
		Texture2D displayTex;
		public int textureRes = 1024;
		public int successPoint = 4;

		Vector4 lastSuccessfulStepValues;
		Vector4 currentStepValues;
		bool widthHeight;
		bool firstSecond; //which half are we cutting off.

		public castMesh canvas;

		void Start()
		{
			assignNewDisplayTexture ();

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
			string[] allSerialPorts = hypercube.input.getPortNames ();
			foreach (string n in allSerialPorts) 
			{
				if (n.Contains ("usbmodem")) //TEMP! turn this into the real hardware name, not default arduino mega name!
					return n;
			}
			return "NOT FOUND!";
		}
			


		public override Material[] getMaterials ()
		{
			Material[] mats = new Material[canvas.getSliceCount()];
			for(int i = 0; i < mats.Length; i ++)
			{
				mats [i] = displayMat;
			}
			return mats;
		}

		void Update()
		{

			//do we see a lit up screen? 
			string[] data = serial.ReadSerialMessage().Split(' ');

			bool result = false;
			//if (c > threshold)
			//	result = true;

			takeHalvingTestStep (result);
		}


		void takeHalvingTestStep(bool lastStepSuccess)
		{
			if (lastStepSuccess) 
			{
				lastSuccessfulStepValues = currentStepValues;
				widthHeight = !widthHeight;
				if (widthHeight) 
				{ //chop width wise
					currentStepValues.z /= 2f;
					if (firstSecond)
						currentStepValues.x += lastSuccessfulStepValues.z / 2f;
				} 
				else
				{
					currentStepValues.w /= 2f;
					if (firstSecond)
						currentStepValues.y += lastSuccessfulStepValues.w / 2f;
				}

				fill (currentStepValues);
				return;
			}

			//it failed last time.
			firstSecond = !firstSecond;
			if (widthHeight) 
			{ //chop width wise
				if (firstSecond)
					currentStepValues.x += lastSuccessfulStepValues.z / 2f;
				else
					currentStepValues.x -= lastSuccessfulStepValues.z / 2f;

				if (currentStepValues.y < 0 || currentStepValues.y + currentStepValues.z > textureRes) 
				{
					firstSecond = !firstSecond;
					currentStepValues = lastSuccessfulStepValues;
					return;
				}
			} 
			else
			{
				if (firstSecond)
					currentStepValues.y += lastSuccessfulStepValues.w / 2f;
				else 
					currentStepValues.y -= lastSuccessfulStepValues.w / 2f;
				
				if (currentStepValues.y < 0 || currentStepValues.y + currentStepValues.w > textureRes) 
				{
					firstSecond = !firstSecond;
					currentStepValues = lastSuccessfulStepValues;
					return;
				}
			}

			if (currentStepValues.x < 0f)
				Debug.LogWarning (currentStepValues.x + " - X is less than 0f!");
			if (currentStepValues.y < 0f)
				Debug.LogWarning (currentStepValues.y + " - Y is less than 0f!");

			fill (currentStepValues);
		}

		void startNewHalvingTest()
		{
			lastSuccessfulStepValues = currentStepValues = new Vector4 (0f, 0f, textureRes, textureRes);
		}


		void fill(Vector4 v)
		{
			fill ((int)v.x, (int)v.y, (int)v.z, (int)v.w);
		}
		void fill(int _x, int _y, int _w, int _h)
		{
			if (_w < successPoint || _h < successPoint) //we found our point.
			{
				//SUCCESS!
				startNewHalvingTest ();
				return;
			}

			currentStepValues = new Vector4 (_x, _y, _w, _h);
			if (displayTex.width != textureRes)
				assignNewDisplayTexture ();
				
			for (int x = 0; x < textureRes; x++) 
			{
				for (int y = 0; y < textureRes; y++) 
				{
					if (x >= _x && x < _x + _w && y >= _y && y < _y + _h)
						displayTex.SetPixel (x, y, Color.white);
					else
						displayTex.SetPixel (x, y, Color.black);
				}
			}
			displayTex.Apply ();
		}

		void assignNewDisplayTexture()
		{
			displayTex = new Texture2D (textureRes, textureRes);
			displayMat.SetTexture ("_MainTex", displayTex);
		}
			
	}
}
