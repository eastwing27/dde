using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ddeMain : MonoBehaviour {

	private static ddeMain       main;
	private static GameObject    projector;
	private static GameObject    menu;
	private static GameObject    exam;
	private static GameObject 	 demo;
	private static RectTransform rtPrj;

	public  static GameObject    player;
	
	private static bool allowTouches = true;
	
	// направление автоматического движения слайдов (для перехода между ними или отмены
	// перехода). zero - автоматическое движение не задано - в этом случае разрешена
	// обработка касаний пользователя
	private enum   direction : byte {zero, back, left, right};
	private static direction moveTo = direction.zero;
	
	//Фазы игры
	public enum    State : byte {Menu, Diafilm, Examination, Transit};
	public static  State GameState = State.Menu;
	
	private static List<ddeDiafilm.Slide> film   = new List<ddeDiafilm.Slide>();
	private static List<Image>			  frames = new List<Image>();
	private static int					  index  = 0;
	
	public static ddeDiafilm[] diafilms;
	
	private static CanvasGroup cgMenu;
	private static CanvasGroup cgProj;
	private static CanvasGroup cgExam;
	private static CanvasGroup cgDemo;
	
	private static bool menuCreated = false;

	//Источник аудио
	private static AudioSource Audio = new AudioSource();
	
	//Случайная последовательность кадров в экзамене
	private static List<int> examSequence = new List<int>();
	
	//Множители ручного и автоматического движений слайдов:
	const float MOVE_SPEED  = 2500f;
	const float SWIPE_SPEED = 300f;

	//Источник диафильма, который проигрывается в настоящий момент (if any, как говорится)
	private static GameObject loadedDiafilm;

	//Псевдотост :)
	public datPseudoToast toast;
	
	//Проверка на нажатие ескейпа, нужна при попытке выйти из игры или вернуться в предыдущий режим
	private bool escapePressed = false;

	//Список титров текущего кадра
	private static List<CanvasGroup> titles = new List<CanvasGroup>();

	//Рутина для отображения титров (добавлено для корректного прерывания)
	private static IEnumerator titleRoutine = null;

	void Awake () 
	{
		main      = this;
		player    = main.transform.gameObject;
		menu	  = player.transform.GetChild(0).gameObject;
		projector = player.transform.GetChild(1).gameObject;
		exam	  = player.transform.GetChild(2).gameObject;
		demo	  = null;
		rtPrj     = projector.GetComponent<RectTransform>();
		diafilms  = FindObjectsOfType(typeof(ddeDiafilm)) as ddeDiafilm[];

		loadedDiafilm = new GameObject();

		//Скрипт, управляющий всплывающими сообщениями
		toast = player.GetComponent<datPseudoToast>();

		Audio = player.GetComponent<AudioSource>();

	}
	
	
	void Start ()
	{
		cgMenu = menu.GetComponent<CanvasGroup>();
		cgProj = projector.GetComponent<CanvasGroup>();
		cgExam = exam.GetComponent<CanvasGroup>();
		cgDemo = null;
		
		//Рисуем фон
		Image back = player.GetComponent<Image>();
		
		Texture2D imageSpr = Resources.Load<Texture2D> ("background");
		Texture2D backSpr  = Instantiate(imageSpr) as Texture2D;
		
		back.sprite = crop (rtPrj, backSpr);	

		//Криво пытаемся определить, демо-версия это или нет
		if (GameObject.Find("ddeDemo") != null)
		{
			demo = player.transform.GetChild(3).gameObject;
			cgDemo = demo.GetComponent<CanvasGroup>();
		}

		//Первичная демонстрация меню
		ShowMenu ();

		//Govnokod-mode активация настроек

	}


	void Update () 
	{
		//Считываем касания в зависимости от фазы игры
		switch (GameState)
		{
			case State.Menu:
				if (Input.GetKeyDown (KeyCode.Escape)) 
				{
					if (!escapePressed)
					{
						escapePressed = true;
						StartCoroutine(tryToEscape(0.5f));
						toast.Show("Нажмите дважды для выхода из игры", 3.5f);
					}
					else
					{
						Application.Quit();
						Debug.Log ("Ванья, я ушёль!");
					}
				}
				
			break;
				
			case State.Diafilm:
				if (allowTouches)
				{
					if (Input.GetKeyDown (KeyCode.Escape)) 
					{
						if (!escapePressed)
						{
							escapePressed = true;
							StartCoroutine(tryToEscape(0.5f));
							toast.Show("Нажмите дважды, чтобы вернуться в меню", 3.5f);
						}
						else
						{
							toast.DestroyAll();
							UnloadDiafilm();
							ShowMenu();
						}

					}
					else
						moveTo = touchWhenDiafilm ();
				}
				
				if (moveTo == direction.back)
					moveHolderBack();
				else
				if (moveTo != direction.zero)
					moveHolderFurther(moveTo);	
			break;
				
			case State.Examination:
				if (allowTouches)
				{
						if (Input.GetKeyDown (KeyCode.Escape)) 
					{
						UnloadDiafilm();
						ShowMenu();
					}
					else
						touchWhenExamination ();
				}
			break;
			}
	}
	
	
//-------------Функции фазы диафильма---------------------------------------------
	
	//Обработка касаний в фазе диафильма
	private static direction touchWhenDiafilm ()
	{
		int   	  count  = Input.touchCount;
		float     delta  = 0.0f;
		
		//Вывод функции
		direction result = direction.zero;

		//Рект-трансформ текущего кадра
		RectTransform rtFrm  = frames[index].GetComponent<RectTransform>();
		int 		  factor = index + 1;
		
		if (allowTouches) //Если обработка касаний разрешена
		{
			for (int i = 0; i < count; i++)
			{
				Touch touch = Input.GetTouch(i);
				
				switch (touch.phase)
				{
					case TouchPhase.Moved:
						delta = touch.deltaPosition.x;
						
						//Перемещать слайды можно только в пределах трёх: текущий, предыдущий и следующий
						//Это верно и для первого/последнего слайдов, но в их случаях возможен преход
						//в другие фазы игры
						if (rtPrj.anchoredPosition.x > -rtFrm.rect.width * (factor)
						&&  rtPrj.anchoredPosition.x <  rtFrm.rect.width * (factor))
							rtPrj.Translate(delta * Time.deltaTime * SWIPE_SPEED, 0.0f, 0.0f);
						
						//В этой фазе слайд двигается только вручную
						result = direction.zero;
					break;
					
					case TouchPhase.Ended:
						//Вычислить и вернуть направление автоматического движения
						result = automovingDirection();
					break;
					
					default:
						//Никуда не двигаться в фазах начала, удержания и отмены
						result = direction.zero;
					break;
				}
			}
		}
		
		// Запретить обработку касаний, если есть навправление автодвижения
		if (result != direction.zero)
			allowTouches = false;
		
		return result;
	}
	
	
	//Проверяем, куда нужно перемещать слайд
	private static direction automovingDirection ()
	{
		int factor = index + 1;
		float frameWidth = frames[0].GetComponent<RectTransform>().rect.width;
		float rtLeftPos  = rtPrj.anchoredPosition.x + (frameWidth * index);
		float rtRightPos = rtPrj.anchoredPosition.x + (frameWidth * factor);
		
		//Холдер больше, чем на шестую часть ушёл влево
		if (rtLeftPos < -frameWidth/6)
			return direction.left;
		else
		//Холдер больше, чем на шестую (назовём её так) часть ушёл вправо
		if (rtRightPos > frameWidth*1.1666f)
			return direction.right;
		//Холдер не вышел за пределы
		else
			return direction.back;
	}


	//автоматическое возвращение холдера на исходную позицию
	private static void moveHolderBack()
	{
		bool movingIsOver = false;
		RectTransform rtFrm = frames[index].GetComponent<RectTransform>();
		
		float positionZero = -(rtFrm.rect.width * index);
		
		if (rtPrj.anchoredPosition.x < positionZero) // Холдер возвращается слева
		{
			rtPrj.Translate(Vector2.right * Time.deltaTime *  MOVE_SPEED);
			// Если после очережного щага холдер встал на место или немного перелетел:
			if (rtPrj.anchoredPosition.x >= positionZero) 
				movingIsOver = true;
		}
		else
			if (rtPrj.anchoredPosition.x >  positionZero) // Холдер возвращается справа
		{
			rtPrj.Translate(-Vector2.right * Time.deltaTime *  MOVE_SPEED);
			// Если после очережного щага холдер встал на место или немного перелетел:
			if (rtPrj.anchoredPosition.x <=  positionZero) 
				movingIsOver = true;
		}
		else // Такое может быть только если холдер в процессе возврата нечаянно встал на место
			movingIsOver = true;
		
		//Если слайд встал на место, выставляем позицию (слайд может "перелететь"), и сообщаем
		//системе, что можно трогать
		if (movingIsOver)
		{
			rtPrj.anchoredPosition = new Vector2 (positionZero, 0);
			moveTo = direction.zero;
			allowTouches = true;
		}
	}
	
	
	//автоматическое перемещение холдера в заданную сторону
	private static void moveHolderFurther (direction dir)
	{
		bool movingIsOver = false;
		int  factor = index + 1;
		
		//Ширина кадра (и экрана заодно)
		float frameWidth = frames[index].GetComponent<RectTransform>().rect.width;
		
		//Позиции, на которых должен оказаться проектор в результате автоперемещения:
		float leftPos  = -frameWidth * factor;    //Если едет влево
		float rightPos = -frameWidth * (index-1); //Если едет вправо
		
		// Направление движения
		switch (dir)
		{
			case direction.left:

				rtPrj.Translate(Vector2.right * Time.deltaTime *  MOVE_SPEED * -1);
				
				if (rtPrj.anchoredPosition.x <= leftPos)
					movingIsOver = true;
			break;
			
			case direction.right:
				rtPrj.Translate(Vector2.right * Time.deltaTime *  MOVE_SPEED);
				
				if (rtPrj.anchoredPosition.x >= rightPos)
					movingIsOver = true;
			break;
			
			//Я без понятия, при каких условиях функция может получить back или zero, но вдруг
			default:
				//Просто прерываем движение
				movingIsOver = true;
			break;
		}
		
		if (movingIsOver)
		{
			float newPos = 0.0f;
			
			moveTo = direction.zero;
			allowTouches = true;
			
			if (dir == direction.left)
			{
				newPos = leftPos;
				index++;
			}
			else
			if (dir == direction.right)
			{
				newPos = rightPos;
				index--;
			}
			
			rtPrj.anchoredPosition = new Vector2 (newPos, 0.0f);
			
		//Определяем, что делать дальше:
			//Первый кадр уехал вправо, возвращаемся в меню
			if (index < 0) 
			{
				ShowMenu();
				return;
			}
			else
			//Последний кадр уехал влево,переходим к тесту
			if (index >= frames.Count)
				//StartExam();
				if (demo == null)
				{
					ShowMenu();
					return;
				}
				else
					EndDemo();
			//Фаза диафильма не прекращена, озвучиваем текущий кадр
			else 
				playFrame(index);
		}
		
		film = loadedDiafilm.GetComponent<ddeDiafilm>().Diafilm;
		//Пытаемся добавить новый кадр из списка 
		if ((film.Count  > index+1)  //Если в диафильме есть такой кадр
		&& (frames.Count <= index+1)) //И он не был добавлен в ленту ранее
		{
			main.StartCoroutine(addFrameAfterTimer (0.1f, film[index+1]));
		}

	}
	
	
	private static void playFrame (int id)
	{
		Audio.Stop();
		if (titleRoutine != null)
			main.StopCoroutine(titleRoutine);

		//Очищаем устаревший список титров
		foreach (CanvasGroup canvasG in titles)
			if (canvasG != null) DestroyImmediate(canvasG.transform.gameObject);
		titles.Clear();

		//Заполнение списка, создание актуальных титров
		foreach (ddeDiafilm.clLabels label in film[id].Labels)
		{

			Button		  objBtn = Resources.Load<Button>("btnLitera");
			Button		  button = Instantiate (objBtn) as Button;
			CanvasGroup   cg	 = button.GetComponent<CanvasGroup>();
			RectTransform rt	 = button.GetComponent<RectTransform>();

			cg.alpha = 0;
			titles.Add (cg);

			button.transform.SetParent(frames[id].transform);

			button.transform.localScale = Vector3.one;
			rt.sizeDelta		        = Vector2.zero;
			rt.anchoredPosition         = Vector2.zero;

			rt.anchorMin = label.placeAt;
			rt.anchorMax = label.placeAt;

			Text txt = button.transform.GetChild (0).GetComponent<Text>();

			txt.text     = label.text;
			txt.fontSize = label.fontSize;

			//main.StartCoroutine(increaseAlpha(cg, 0.1f));
		}

		titleRoutine = showTitles(film[id].Labels, titles);
		main.StartCoroutine(titleRoutine);
		//Audio.clip = film[id].RegularSound;
		//Audio.Play();
	}
	
	
	//Подготовка и запуск диафильма. Не использовать в Awake!
	public static void LoadDiafilm (GameObject source)
	{
		//int i = 0;

		index = 0;
		
		cgMenu.alpha = 0;
		
		film = source.GetComponent<ddeDiafilm>().Diafilm;

		loadedDiafilm = source;
		
		main.StartCoroutine(addFrame (film[0]));
		if (film[1] != null)
			main.StartCoroutine(addFrame (film[1]));
		
		RebuildFrameLine();
		
		GameState = State.Diafilm;
		
		rtPrj.anchoredPosition = new Vector2 (0.0f, 0.0f);
		
		main.StartCoroutine(increaseAlpha(cgProj, 0.01f));
		
		playFrame(0);
	}


	//Выстраивание кадров по порядку
	public static void RebuildFrameLine()
	{
		float frameX = 0.0f;

		for (int i = 0; i < frames.Count; i++)
		{
			RectTransform rt = frames[i].GetComponent <RectTransform>();
			
			rt.anchoredPosition = new Vector2(frameX, 0);
			frameX += rt.rect.width;
		}
	}
	
	
	//Очистка списка кадров
	private static void UnloadDiafilm ()
	{
		Audio.Stop ();

		for (int i = 0; i < frames.Count; i++)
			GameObject.DestroyImmediate(frames[i].transform.gameObject);
		
		index = 0;
		frames.Clear();
		film = new List<ddeDiafilm.Slide>();
		
		cgProj.alpha = 0;
	}


//-------------Функции главного меню----------------------------------------------
	/*private static void touchWhenMenu ()
	{
		int count = Input.touchCount;

		for (int i = 0; i < count; i++)
		{
			Touch touch = Input.GetTouch(i);
				
			if (touch.phase == TouchPhase.Ended)
			{
			
				LoadDiafilm(GameObject.Find ("Diafilm"));
			}
		}
	}*/

	
	//Демонстрация главного меню игры
	public static void ShowMenu ()
	{
		//Создаём пункты меню, числом не более четырёх (зачёркнуто) двух, если не создавали ранее
		if (menuCreated == false)
		{
			for (int i = 0; i < diafilms.Length; i++)
			{

				Button image = Resources.Load<Button> ("menuItem_");
				Button frame = Instantiate(image) as Button;

				//Выводим название диафильма под пунктом меню:
				UnityEngine.UI.Text title = frame.transform.GetChild(0).GetComponent<Text>();
				title.text = diafilms[i].title;
				//title.rectTransform.anchoredPosition = new Vector2(0, title.rectTransform.rect.y/2-100);
				
				RectTransform frameRT = frame.GetComponent<RectTransform>();
				
				Texture2D picture = diafilms[i].Diafilm[diafilms[i].previewID].Pictures[0];
				
				frame.name = "ddeMenuItem_0" + i.ToString();
				frame.transform.SetParent(GameObject.Find ("ddeMainMenu").transform, false);
				
				frame.transform.localScale = Vector3.one;
				frameRT.sizeDelta		   = Vector2.zero;
				frameRT.anchoredPosition   = new Vector2(0, 0);
				
				switch (i)
				{
					case 0:
						frameRT.anchorMin = new Vector2(0.02f, 0.33f);
						frameRT.anchorMax = new Vector2(0.49f, 0.83f);
					break;
					
					case 1:
						frameRT.anchorMin = new Vector2(0.51f, 0.33f);
						frameRT.anchorMax = new Vector2(0.98f, 0.83f);
					break;
					
					/*case 2:
						frameRT.anchorMin = new Vector2(0.05f, 0.05f);
						frameRT.anchorMax = new Vector2(0.45f, 0.45f);
					break;
					
					case 3:
						frameRT.anchorMin = new Vector2(0.55f, 0.05f);
						frameRT.anchorMax = new Vector2(0.95f, 0.45f);
					break;*/
				}
				
				//Нумеруем пункты меню:
				frame.GetComponent<ddeMenuItem>().index = i;
				
				//Создаём предпросмотр
				frame.transform.GetComponent<Image>().sprite = crop (frameRT, picture);
				
			}
			
			menuCreated = true;
		}
		
		main.StartCoroutine(increaseAlpha(cgMenu, 0.02f));
		
		UnloadDiafilm ();
		
		GameState = State.Menu;
	}
			
//-------------Функции фазы экзамена----------------------------------------------
	
	private static void touchWhenExamination ()
	{
		
	}


	//Запуск фазы теста
	private static void StartExam ()
	{
		List<int>    	tempSequence = new List<int>();
		System.Random	random       = new System.Random();
	
		examSequence.Clear();
	
		//Заполняем временный список номерами кадров подряд
		for (int i = 0; i < film.Count; i++)
		{
			tempSequence.Add (i);
		}
		
		//Тасуем и записываем в список последовательностей
		examSequence = tempSequence.OrderBy (x=>random.Next ()).ToList ();
		
		main.StartCoroutine(increaseAlpha(cgExam, 0.02f));
		
		GameState = State.Examination;
	}
	
	
	//Переход между заданиями
	private static void nextTask ()
	{
		
	}
	
//-------------Общие функции------------------------------------------------------
	
	//Создать спрайт путём обрезания источника пропорционально размерам указанного рет-трансформа
	//(в нашем случае он равен разрешению экрана)
	private static Sprite crop (RectTransform frame, Texture2D source)
	{
		//Размеры источника
		int sourceWidth  = source.width;
		int sourceHeight = source.height;
		
		//Размеры кадра
		float frameWidth  = frame.rect.width;
		float frameHeight = frame.rect.height;
		
		//Высчитываем нужные размеры. 
		float imageWidth  = (frameWidth*sourceHeight)/frameHeight;
		float imageHeight;
		
		//Если шиина исходника больше или равна получившейся ширине, то всё хорошо
		if (sourceWidth >= imageWidth)
			imageHeight = sourceHeight;
		//Иначе - пересчитываем размеры, опираясь на высоту (в противном случае возникнет ашыпка)
		else
		{
			imageWidth  = sourceWidth;
			imageHeight = (frameHeight*sourceWidth)/frameWidth;
		}
		
		//Считаем "рамку" - прямоугольник, который будем вырезать
		float rectX = (sourceWidth  - imageWidth)  / 2;
		float rectY = (sourceHeight - imageHeight) / 2;
		Rect rect = new Rect(rectX, rectY, imageWidth, imageHeight);
		
		//Ура!
		return Sprite.Create (source, rect, new Vector2(0, 0));
	}
	
	
	static IEnumerator addFrame (ddeDiafilm.Slide slide)
	{
		int   count = frames.Count;
		
		//Генерация нового кадра из префаба:
		Image image = Resources.Load<Image> ("slideHolder_");
		Image frame = Instantiate(image) as Image;
		
		RectTransform frameRT = frame.GetComponent<RectTransform>();
		
		//Случайным образом выбирается картинка для демонстрации (в слайде диафильма их может быть несколько)
		int spriteNumber = UnityEngine.Random.Range(0, slide.Pictures.Count/*-1*/);
		
		//Называем кадр, и добавляем его в проектор
		frame.name = string.Format("slideHolder_{0:00}", count);
		frame.transform.SetParent(projector.transform, false);

		//Установки размера и позиции
		frame.transform.localScale = Vector3.one;
		frameRT.sizeDelta		   = Vector2.zero;
		frameRT.anchoredPosition   = Vector2.zero;
		
		//Из исходной картинки вырезается новая пропорционально соотношению сторон экрана
		frame.sprite = crop (frameRT, slide.Pictures[spriteNumber]);

		//Кадр добавляется в список кадров
		frames.Add(frame); 
		
		yield return null;
	}
	
	
	//Отложенное добавление кадра. Необходимо для сохранения плавного движения слайда при динамической 
	//подгрузке (в противном случае на большинстве устройств прокрутка будет заметно тормозить)
	static IEnumerator addFrameAfterTimer(float seconds, ddeDiafilm.Slide slide)
	{
		yield return new WaitForSeconds(seconds);	
		main.StartCoroutine(addFrame(slide));
		RebuildFrameLine();
	}

	
	//Плавное проявление указанного канвасгруппа
	public static IEnumerator increaseAlpha(CanvasGroup target, float seconds)
	{
		target.alpha = 0; //Чисто на всякий случай
		
		while (target.alpha < 1)
		{
			if (target == null) yield break;
			yield return new WaitForSeconds(seconds); 
			target.alpha += 0.05f;
		}	
	}
	
	//Плавное исчезание указанного канвасгруппа
	public static IEnumerator decreaseAlpha(CanvasGroup target, float seconds)
	{
		target.alpha = 1; //Чисто на всякий случай
		
		while (target.alpha > 0)
		{
			yield return new WaitForSeconds(seconds); 
			target.alpha -= 0.05f;
		}	
	}
	
	//Перемещение указанного ректтрансформа к заданной точке на плоскости
	static IEnumerator moveRT (RectTransform rt, Vector2 destPoint, float delay)
	{
		Vector2 hrzDir = new Vector2();
		Vector2 vrtDir = new Vector2();

		//Направление горизонтального движения:
		if (rt.anchoredPosition.x < destPoint.x)
			hrzDir = Vector2.right;
		else
		if (rt.anchoredPosition.x > destPoint.x)
			hrzDir = Vector2.right * -1;
		else
			hrzDir = Vector2.zero;

		//Направление вертикального движения:
		if (rt.anchoredPosition.y < destPoint.y)
			vrtDir = Vector2.up;
		else
		if (rt.anchoredPosition.y > destPoint.y)
			vrtDir = Vector2.up * -1;
		else
			vrtDir = Vector2.zero;

		//Двигаем:
		while (rt.anchoredPosition != destPoint)
		{
			rt.transform.Translate(hrzDir + vrtDir);
			yield return new WaitForSeconds(delay);
		}
	}
	
	
	//Попытка уйти
	static IEnumerator tryToEscape(float timer)
	{
		while (timer > 0)
		{
			//if (!main.escapePressed) return false;
			yield return new WaitForSeconds(0.1f);
			timer -= 0.1f;
		}
		main.escapePressed = false;
	}


	//Демонстрация плашки про полную версию (только для демо-версий)
	private static void EndDemo ()
	{
		GameObject ogo = Resources.Load<GameObject>("demoGroup");
		GameObject go  = Instantiate(ogo) as GameObject;

		go.name = "demoGroup";
		go.transform.SetParent(demo.transform);
		go.transform.localScale = Vector3.one;
		go.GetComponent<RectTransform>().sizeDelta		  = Vector2.zero;
		go.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

		GameState = State.Menu;
		UnloadDiafilm ();
		main.StartCoroutine(increaseAlpha(cgDemo, 0.02f));
	}


	static IEnumerator showTitles (List<ddeDiafilm.clLabels> labels, List<CanvasGroup> cgs)
	{
		yield return new WaitForSeconds(0.5f);

		for (int i = 0; i < labels.Count; i++)
		{
			float seconds = 0.5f;

			Audio.clip = labels[i].audio;

			if (Audio.clip != null)
				seconds = Audio.clip.length;

			if (labels[i] == null) return false;

			main.StartCoroutine (increaseAlpha(cgs[i], 0.02f));
			Audio.Play ();
			yield return new WaitForSeconds(seconds + 0.5f);
		}
	}
	
}
