using UnityEngine;
using System.Collections;
using UnityEditor;

namespace hypercube
{
	public class calibratorMenuOptions : MonoBehaviour 
	{
		#if HYPERCUBE_DEV
		[MenuItem("Hypercube/Save Settings", false, 51)]
		public static void saveCubeSettings()
		{
		//hypercube.castMesh c = GameObject.FindObjectOfType<hypercube.castMesh>();
		//if (c)
		//c.saveConfigSettings();
		//else
		//Debug.LogWarning("No castMesh was found, and therefore no saving could occur.");
		}


#endif

        [MenuItem("Hypercube/TEST DATA", false, 51)]
        public static void testData()
        {

            //float[] output = autoCalibrator.getPeaksFromData(7, 60, new int[0]); //600ms
            float[] output = autoCalibrator.getPeaksFromData(1, 60, 10, .13f, new int[0]); //100ms
            string s = "";
            foreach (float d in output)
            {
                s += d + "   ";
            }
            Debug.Log(s);
        }
    }
}
