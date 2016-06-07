using UnityEngine;
using System.Collections;

public class ddeDemo : MonoBehaviour {


	public void GoToFull ()
	{
		Application.OpenURL("market://details?id=com.danko.ddeFloraFauna");
	}

	public void CloseWindow ()
	{
		ddeMain.ShowMenu();
		GameObject.DestroyImmediate(GameObject.Find ("demoGroup"));
	}
}
