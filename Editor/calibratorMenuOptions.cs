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
	}
}
