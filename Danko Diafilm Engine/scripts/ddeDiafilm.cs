using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]

public class ddeDiafilm : MonoBehaviour {

	//Заголовок диафильма, который отображается в меню
	public string title = "Новый диафильм";

	//Номер слайда, который будет представлять диафильм в меню
	public int 	  previewID = 1; 

	//Титры, которые появляются на слайде после его демонстрации
	[System.Serializable]
	public class clLabels 
	{
		//Содержание лэйбла
		public string    text     = "";
		//Размер шрифта
		public int       fontSize = 72;
		//Положение на экране в процентах от соответствующей стороны
		public Vector2   placeAt  = new Vector2 (10, 10);
		//Звук, проигрываемый при появлении текста
		public AudioClip audio    = null;

	}
	
	//Это слайды...
	[Serializable]
	public class Slide
	{
		public string 			Title			 = "Новый слайд";
		public string 			Group			 = "";
		public List<Texture2D>	Pictures 		 = new List<Texture2D>();
		public List<clLabels>	Labels			 = new List<clLabels>();
		public AudioClip		RegularSound	 = null;
		public AudioClip		ExaminationSound = null;
	}
	
	//...из которых состоит диафильм:
	public List<Slide> Diafilm = new List<Slide>();
}
