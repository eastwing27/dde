using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]

public class ddeDiafilm : MonoBehaviour {

	//Это слайды...
	[Serializable]
	public class Slide
	{
		public string 		Title			 = "Новый слайд";
		public string 		Group			 = "";
		public List<Sprite> Pictures 		 = new List<Sprite>();
		public AudioClip	RegularSound	 = null;
		public AudioClip	ExaminationSound = null;
		//public int 			ShowedSprite 	 = -1; 
	}
	
	//...из которых состоит диафильм:
	public List<Slide> Diafilm = new List<Slide>();
}
