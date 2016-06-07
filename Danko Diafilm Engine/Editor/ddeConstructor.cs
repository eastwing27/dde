using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ddeDiafilm))]
public class DDE_Constructor : Editor
{
	protected static List<bool> showSlide = new List<bool>();
	
	private Sprite delete = Resources.Load<Sprite> ("delete");
	private Sprite add	  = Resources.Load<Sprite> ("new");	
	private Sprite up	  = Resources.Load<Sprite> ("up");	
	private Sprite down	  = Resources.Load<Sprite> ("down");		

	public override void OnInspectorGUI ()
	{
		ddeDiafilm main = (ddeDiafilm)target;
		
		//Синхронизируем размеры списков:
		if (showSlide.Count < main.Diafilm.Count)
		{
			for (int cnt1 = showSlide.Count; cnt1 < main.Diafilm.Count; cnt1++)
				showSlide.Add (false);
		}

		EditorGUILayout.BeginVertical();

		//GUILayout.Label("Заголовок", GUILayout.Width(Screen.width-400));
		main.title = EditorGUILayout.TextField("Заголовок", main.title, GUILayout.Width(Screen.width-50));

		//GUILayout.Label("Слайд для меню", GUILayout.Width(Screen.width-400));
		main.previewID = EditorGUILayout.IntField("Слайд для меню", main.previewID, GUILayout.Width(Screen.width-50));

		//Рисуем редактор:
		EditorGUILayout.Separator();
		
		if (main.Diafilm.Count > 1) FoldAll();
		
		for (int i = 0; i < main.Diafilm.Count; i++)
		{
			GUILayout.BeginHorizontal();
			
			//Формируем и оформляем заголовок фолдаута:
			string   title = string.Format ("{0:00}: {1}", i, main.Diafilm[i].Title);
			GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
			foldoutStyle.fontStyle = FontStyle.Bold;
			
			//Работаем с маленькими кнопками:
			GUIStyle buttonStyle = new GUIStyle();
			buttonStyle.stretchHeight = false;
			buttonStyle.stretchWidth  = false;
			
			//Рисуем заголовок слайда (фолдаут, кнопки перемещения, кнопка удаления):
			showSlide[i] = EditorGUILayout.Foldout(showSlide[i], title, foldoutStyle);
			if (GUILayout.Button(up.texture, buttonStyle, GUILayout.Width(20))) //переместить слайд вверх
			{
				if (i-1 >= 0)
				{
					bool 		     tmpBool  = showSlide[i-1];
					ddeDiafilm.Slide tmpSlide = main.Diafilm[i-1];
					
					main.Diafilm[i-1] = main.Diafilm[i];
					main.Diafilm[i]   = tmpSlide;
					showSlide[i-1]    = showSlide[i];
					showSlide[i]      = tmpBool;
				}
			}
			if (GUILayout.Button(down.texture, buttonStyle, GUILayout.Width(20)))//Переместить слайд вниз
			{
				if (i+1 <= main.Diafilm.Count-1)
				{
					bool 		  tmpBool  = showSlide[i+1];
					ddeDiafilm.Slide tmpSlide = main.Diafilm[i+1];
					main.Diafilm[i+1] = main.Diafilm[i];
					main.Diafilm[i] = tmpSlide;
					showSlide[i+1] = showSlide[i];
					showSlide[i] = tmpBool;
				}
			}
			if (GUILayout.Button(delete.texture, buttonStyle, GUILayout.Width(20)))
			{
				main.Diafilm.Remove(main.Diafilm[i]);
				EditorGUIUtility.ExitGUI();
			}
			
			GUILayout.EndHorizontal();
			
			//Отображение слайда (если его секция развёрнута)
			if (showSlide[i])
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Название");
				main.Diafilm[i].Title = GUILayout.TextField(main.Diafilm[i].Title, GUILayout.Width(Screen.width-100));
				GUILayout.EndHorizontal();
				
				/*GUILayout.BeginHorizontal();
				//GUILayout.Label("Группа");
				main.Diafilm[i].Group = GUILayout.TextField(main.Diafilm[i].Group, GUILayout.Width(Screen.width-100));
				GUILayout.EndHorizontal();*/
			
				EditorGUILayout.Separator();
				
				int spritesCount = main.Diafilm[i].Pictures.Count;
				GUILayout.Label("Изображения:");
				if (spritesCount > 0)
				{
					for (int c = 0; c < spritesCount; c++)
					{	
						//Sprite sprite = main.Diafilm[i].Pictures[c];					
						
						GUILayout.BeginHorizontal();
						//Кнопка "удалить изображение":
						if (GUILayout.Button(delete.texture, buttonStyle, GUILayout.Width(20)))
						{
							main.Diafilm[i].Pictures.Remove(main.Diafilm[i].Pictures[c]);
							EditorGUIUtility.ExitGUI();
						}
						GUILayout.Label(string.Format("{0}: ", c+1));
						main.Diafilm[i].Pictures[c] = EditorGUILayout.ObjectField (main.Diafilm[i].Pictures[c], 
													 typeof(Texture2D), false) as Texture2D;                               
						GUILayout.EndHorizontal();
					}
				}
				
				//Кнопка "добавить изображение"
				GUILayout.BeginHorizontal();
				if (GUILayout.Button(add.texture, buttonStyle, GUILayout.Width(20)))
				{
					main.Diafilm[i].Pictures.Add(new Texture2D(1900, 1080));
				}
				GUILayout.Label ("Добавить изображение");
				GUILayout.EndHorizontal();
				
				EditorGUILayout.Separator();


				//Рисуем лэйбелы
				GUILayout.Label ("Титры и аудиоклипы");

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button ("Добавить", GUILayout.Width(Screen.width/2-50)))
				{
					main.Diafilm[i].Labels.Add (new ddeDiafilm.clLabels());
					
					int curTitle = main.Diafilm[i].Labels.Count - 1;
					
					switch (curTitle)
					{
						case 0:
						main.Diafilm[i].Labels[curTitle].placeAt = new Vector2(0.10f, 0.90f);
						main.Diafilm[i].Labels[curTitle].fontSize = 120;
						break;
						
						case 1:
						main.Diafilm[i].Labels[curTitle].text = main.Diafilm[i].Title;
						main.Diafilm[i].Labels[curTitle].placeAt = new Vector2(0.48f, 0.13f);
						break;
					}
					
				}

				if (GUILayout.Button ("Удалить", GUILayout.Width(Screen.width/2-50)))
				{
					int lblCount = main.Diafilm[i].Labels.Count - 1;
					if (lblCount >= 0) main.Diafilm[i].Labels.RemoveAt(lblCount);
				}

				EditorGUILayout.EndHorizontal();

				foreach (ddeDiafilm.clLabels label in main.Diafilm[i].Labels)
				{
					label.text		= EditorGUILayout.TextField   ("Титр",               label.text);
					label.fontSize	= EditorGUILayout.IntField    ("Размер шрифта",      label.fontSize);
					label.placeAt	= EditorGUILayout.Vector2Field("Ппозиция на слайде", label.placeAt);
					label.audio		= EditorGUILayout.ObjectField ("Аудиоклип",          label.audio, typeof(AudioClip), false) as AudioClip;

					EditorGUILayout.Separator();
				}


				//Закончили рисовать лэйбелы

				EditorGUILayout.Separator();
				
				/*GUILayout.BeginHorizontal();
				GUILayout.Label("Звук слайдшоу", GUILayout.Width(100));
				main.Diafilm[i].RegularSound = EditorGUILayout.ObjectField (main.Diafilm[i].RegularSound,
				                                                            typeof(AudioClip), false) as AudioClip;
				GUILayout.EndHorizontal();*/
				
				GUILayout.BeginHorizontal();
				GUILayout.Label("Звук экзамена", GUILayout.Width(100));
				main.Diafilm[i].ExaminationSound = EditorGUILayout.ObjectField (main.Diafilm[i].ExaminationSound,
				                                                            typeof(AudioClip), false) as AudioClip;
				GUILayout.EndHorizontal();
				
				EditorGUILayout.Separator();
				
				GUILayout.Label ("--------------------------------------------------");
				
				EditorGUILayout.Separator();
				EditorGUILayout.Separator();
			} //Конец отображения слайда
		}
		
		if (main.Diafilm.Count > 3) FoldAll();
		
		EditorGUILayout.Separator();
		
		if (GUILayout.Button("Добавить слайд"))
		{
			main.Diafilm.Add(new ddeDiafilm.Slide());
		}

		EditorGUILayout.EndVertical();
		
		if (GUI.changed) EditorUtility.SetDirty (main);
	}
	
	private void FoldAll()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button ("Свернуть все", GUILayout.Width(Screen.width/2-25)))
		{
			for (int itr1 = 0; itr1 < showSlide.Count; itr1++)
				showSlide[itr1] = false;
		}
		if (GUILayout.Button ("Развернуть все", GUILayout.Width(Screen.width/2-25)))
		{
			for (int itr1 = 0; itr1 < showSlide.Count; itr1++)
				showSlide[itr1] = true;
		}
		EditorGUILayout.EndHorizontal();
	}
}