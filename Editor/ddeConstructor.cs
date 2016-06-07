﻿using System.Collections.Generic;
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
		
		//Рисуем редактор:
		EditorGUILayout.Separator();
		
		if (main.Diafilm.Count > 1) FoldAll();
		
		for (int i = 0; i < main.Diafilm.Count; i++)
		{
			GUILayout.BeginHorizontal();
			
			//Формируем и оформляем заголовок фолдаута:
			string   title = string.Format ("{0}: {1}", i+1, main.Diafilm[i].Title);
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
				
				GUILayout.BeginHorizontal();
				GUILayout.Label("Группа");
				main.Diafilm[i].Group = GUILayout.TextField(main.Diafilm[i].Group, GUILayout.Width(Screen.width-100));
				GUILayout.EndHorizontal();
			
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
													 typeof(Sprite), false) as Sprite;                               
						GUILayout.EndHorizontal();
					}
				}
				
				//Кнопка "добавить изобрадение"
				GUILayout.BeginHorizontal();
				if (GUILayout.Button(add.texture, buttonStyle, GUILayout.Width(20)))
				{
					main.Diafilm[i].Pictures.Add(new Sprite());
				}
				GUILayout.Label ("Добавить изображение");
				GUILayout.EndHorizontal();
				
				EditorGUILayout.Separator();
				
				GUILayout.BeginHorizontal();
				GUILayout.Label("Звук слайдшоу", GUILayout.Width(100));
				main.Diafilm[i].RegularSound = EditorGUILayout.ObjectField (main.Diafilm[i].RegularSound,
				                                                            typeof(AudioClip), false) as AudioClip;
				GUILayout.EndHorizontal();
				
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
		
		if (GUI.changed) EditorUtility.SetDirty (main);
	}
	
	private void FoldAll()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button ("Свернуть все", GUILayout.Width(Screen.width/2-10)))
		{
			for (int itr1 = 0; itr1 < showSlide.Count; itr1++)
				showSlide[itr1] = false;
		}
		if (GUILayout.Button ("Развернуть все", GUILayout.Width(Screen.width/2-10)))
		{
			for (int itr1 = 0; itr1 < showSlide.Count; itr1++)
				showSlide[itr1] = true;
		}
		EditorGUILayout.EndHorizontal();
	}
}