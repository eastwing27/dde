using UnityEngine;
using System.Collections;

public class ddeMenuItem : MonoBehaviour {

	[HideInInspector]
	public int index = -1;

	public void OnClick()
	{
		GameObject diafilm = ddeMain.diafilms[index].transform.gameObject;
		ddeMain.LoadDiafilm(diafilm);
	}
}
