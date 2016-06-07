using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ddeParent : MonoBehaviour {

	private datPseudoToast toast;
	private bool buttonPressed = false;

	private CanvasGroup parents;
	private CanvasGroup menu;

	void Start ()
	{
		toast = FindObjectOfType(typeof(datPseudoToast)) as datPseudoToast;

		menu    = ddeMain.player.transform.GetChild(0).gameObject.GetComponent<CanvasGroup>();
		parents = ddeMain.player.transform.GetChild(3).gameObject.GetComponent<CanvasGroup>();

		StartCoroutine(LoadOptions());
	}


	//Кнопка "Родителям"
	public void OnClick ()
	{
		if (buttonPressed)
		{
			toast.DestroyAll();
			StartCoroutine(ddeMain.increaseAlpha(parents, 0.01f));
			StartCoroutine(ddeMain.decreaseAlpha(menu,    0.01f));
			parents.transform.gameObject.SetActive(true);
			parents.interactable = true;
			menu.interactable = false;
		}
		else
		{
			buttonPressed = true;
			toast.Show("Нажмите кнопку 'Родителям' дважды", 3.5f);
			StartCoroutine(waitForDoubleclick(0.7f));
		}
	}


	//Страница "Pодителям", кнопка "Оценить"
	public void OnGP ()
	{
		Application.OpenURL("market://details?id=com.danko.ddeFloraFauna");
	}


	//Страница "Родителям", кнопка "Назад"
	public void OnBack ()
	{
		StartCoroutine(ddeMain.decreaseAlpha(parents, 0.01f));
		StartCoroutine(ddeMain.increaseAlpha(menu,    0.01f));
		parents.interactable = false;
		menu.interactable = true;
		parents.transform.gameObject.SetActive(false);
	}


	//Страница "Родителям", ссылка на мыло
	public void OnMail()
	{
		Application.OpenURL("mailto:support@danko-games.com");
	}

	private IEnumerator waitForDoubleclick(float timer)
	{
		while (timer > 0)
		{
			yield return new WaitForSeconds(0.1f);
			timer -= 0.1f;
		}
		buttonPressed = false;
	}

	private IEnumerator LoadOptions()
	{
		parents.transform.gameObject.SetActive(true);
		yield return new WaitForSeconds(0.1f);
		parents.transform.gameObject.SetActive(false);
	}
}
