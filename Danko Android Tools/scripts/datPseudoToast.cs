using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class datPseudoToast : MonoBehaviour {

	public Canvas parentCanvas;

	private IEnumerator routine = null;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	//Демонстрация "тоста"
	public void Show (string message, Vector2 anchorMin, Vector2 anchorMax, float ShowTimer)
	{
		//Грузим тост из ресурсов
		Image objToast = Resources.Load<Image> ("pseudoToast");
		Image toast	   = Instantiate (objToast) as Image;

		RectTransform rt = toast.GetComponent<RectTransform>();
		CanvasGroup   cg = toast.GetComponent<CanvasGroup>();

		//Располагаем и задаём размеры тоста
		toast.transform.SetParent(parentCanvas.transform);
		toast.transform.localScale = Vector3.one;

		rt.sizeDelta		= Vector2.zero;
		rt.anchoredPosition	= Vector2.zero;

		rt.anchorMin = anchorMin;
		rt.anchorMax = anchorMax;

		//Присваиваем текст
		toast.transform.GetChild(0).GetComponent<Text>().text = message;

		routine = PopoupAndDie(cg, ShowTimer, 0.0125f);
		StartCoroutine(routine);
	}
	
	public void Show (string message, float ShowTimer)
	{
		Show (message, new Vector2(0.00f, 0.05f), new Vector2(1.00f, 0.12f), ShowTimer);
	}
	
	public void Show (string message)
	{
		Show (message, new Vector2(0.00f, 0.05f), new Vector2(1.00f, 0.12f), 3.0f);
	}


	//Грубо убираем все тосты
	public void DestroyAll ()
	{
		if (routine != null)
			StopCoroutine(routine);

		GameObject[] allToasts = GameObject.FindGameObjectsWithTag("datPseudoToast");

		foreach (GameObject toast in allToasts)
			GameObject.DestroyImmediate(toast);
	}


	//Плавное проявление указанного канвасгрупа
	private IEnumerator fadeIn(CanvasGroup target, float delay)
	{
		target.alpha = 0; //Чисто на всякий случай
		
		while (target.alpha < 1)
		{
			yield return new WaitForSeconds(delay); 
			target.alpha += 0.05f;
		}	
	}


	//Плавное исчезание указанного канвасгрупа
	private IEnumerator fadeOut(CanvasGroup target, float delay)
	{
		target.alpha = 1; //Чисто на всякий случай
		
		while (target.alpha > 0)
		{	
			yield return new WaitForSeconds(delay); 
			target.alpha -= 0.05f;

		}	
	}



	//Исчезание и проявление
	private IEnumerator PopoupAndDie(CanvasGroup target, float timer, float delay)
	{
		float fadeTimer = 40 * delay;
		StartCoroutine(fadeIn (target, delay));
		yield return new WaitForSeconds(timer - fadeTimer*2);
		StartCoroutine(fadeOut(target, delay));
		Destroy(target.transform.gameObject, fadeTimer);
	}
}