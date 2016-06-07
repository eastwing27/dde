using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ddeOptions : MonoBehaviour {

	private string playSound;

	private Toggle tgSound = null;
	private Toggle tgLiter = null;

	public List<AudioClip> audioListSound = new List<AudioClip>();
	public List<AudioClip> audioListLiter = new List<AudioClip>();


	void Start () 
	{
		tgSound = GameObject.Find("tglSounds").GetComponent<Toggle>();
		tgLiter = GameObject.Find("tglLiteras").GetComponent<Toggle>();

		playSound = PlayerPrefs.GetString("playSound", "yes");

		if (playSound == "yes")
		{
			tgSound.isOn = true;
			tgLiter.isOn = false;
			setSounds (audioListSound);
		}
		else
		{
			tgLiter.isOn = true;
			tgSound.isOn = false;;
			setSounds (audioListLiter);
		}
	}


	public void checkSound ()
	{
		if (tgSound.isOn)
		{
			PlayerPrefs.SetString("playSound", "yes");
			setSounds (audioListSound);
		}
	}


	public void checkLiter ()
	{
		if (tgLiter.isOn)
		{
			PlayerPrefs.SetString("playSound", "no");
			setSounds (audioListLiter);
		}
	}

	
	private void setSounds (List<AudioClip> source)
	{
		ddeDiafilm diaAnimals = GameObject.Find ("dfmAnimal").GetComponent<ddeDiafilm>();
		ddeDiafilm diaVegetab = GameObject.Find   ("dfmFood").GetComponent<ddeDiafilm>();

		for (int i = 0; i < source.Count; i++)
		{
			diaAnimals.Diafilm[i].Labels[0].audio = source[i];
			diaVegetab.Diafilm[i].Labels[0].audio = source[i];
		}
	}


}
