using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ddeMain : MonoBehaviour {

	private static ddeMain       main; 
	private static GameObject    player;
	private static GameObject    projector;
	private static GameObject    menu;
	private static GameObject    exam;
	private static RectTransform rtPrj;
	
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
	
	private static bool menuCreated = false;
	
	//Случайная последовательность кадров в экзамене
	private static List<int> examSequence = new List<int>();
	
	//Множители ручного и автоматического движений слайдов:
	const float MOVE_SPEED  = 2500f;
	const float SWIPE_SPEED = 300f;
	

	void Awake () 
	{
		main      = this;
		player    = main.transform.gameObject;
		menu	  = player.transform.GetChild(0).gameObject;
		projector = player.transform.GetChild(1).gameObject;
		exam	  = player.transform.GetChild(2).gameObject;
		rtPrj     = projector.GetComponent<RectTransform>();
		diafilms  = FindObjectsOfType(typeof(ddeDiafilm)) as ddeDiafilm[];
	}
	
	
	void Start ()
	{
		cgMenu = menu.GetComponent<CanvasGroup>();
		cgProj = projector.GetComponent<CanvasGroup>();
		cgExam = exam.GetComponent<CanvasGroup>();
		
		//Рисуем фон
		Image back = player.GetComponent<Image>();
		
		Sprite imageSpr = Resources.Load<Sprite> ("background");
		Sprite backSpr  = Instantiate(imageSpr) as Sprite;
		
		back.sprite = crop (rtPrj, backSpr);		

		//Первичная демонстрация меню
		ShowMenu ();
	}


	void Update () 
	{
		//Считываем касания в зависимости от фазы игры
		switch (GameState)
		{
			case State.Menu:
				if (Input.GetKeyDown (KeyCode.Escape)) 
					Application.Quit ();
				/*else
					touchWhenMenu ();*/
			break;
				
			case State.Diafilm:
				if (allowTouches)
				{
					if (Input.GetKeyDown (KeyCode.Escape)) 
					{
						UnloadDiafilm();
						ShowMenu();
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
				ShowMenu();
			else
			//Последний кадр уехал влево,переходим к тесту
			if (index >= frames.Count)
				StartExam();
			//Фаза диафильма не прекращена, озвучиваем текущий кадр
			else 
				playFrameAudio(index);
		}
	}
	
	
	private static void playFrameAudio (int id)
	{
		AudioSource audio = player.GetComponent<AudioSource>();
		
		audio.clip = film[id].RegularSound;
		audio.Play();
	}
	
	
	//Подготовка и запуск диафильма. Не использовать в Awake!
	public static void LoadDiafilm (GameObject source)
	{
		int   i      = 0;
		float frameX = 0.0f;
		
		index = 0;
		
		cgMenu.alpha = 0;
		
		film = source.GetComponent<ddeDiafilm>().Diafilm;
		
		for (i = 0; i < film.Count; i++)
			addFrame (film[i]);
		
		for (i = 0; i < frames.Count; i++)
		{
			RectTransform rt = frames[i].GetComponent <RectTransform>();
			
			rt.anchoredPosition = new Vector2(frameX, 0);
			frameX += rt.rect.width;
		}
		
		GameState = State.Diafilm;
		
		rtPrj.anchoredPosition = new Vector2 (0.0f, 0.0f);
		
		main.StartCoroutine(increaseAlpha(cgProj, 0.01f));
		
		playFrameAudio(0);
	}
	
	
	//Очистка списка кадров
	private static void UnloadDiafilm ()
	{
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
	private static void ShowMenu ()
	{
		//Создаём пункты меню, числом не более четырёх, если не создавали ранее
		if (menuCreated == false)
		{
			for (int i = 0; i < diafilms.Length; i++)
			{
				Button image = Resources.Load<Button> ("menuItem_");
				Button frame = Instantiate(image) as Button;
				
				RectTransform frameRT = frame.GetComponent<RectTransform>();
				
				Sprite picture = diafilms[i].Diafilm[0].Pictures[0];
				
				frame.name = "ddeMenuItem_0" + i.ToString();
				frame.transform.SetParent(GameObject.Find ("ddeMainMenu").transform, false);
				
				frame.transform.localScale = Vector3.one;
				frameRT.sizeDelta		   = Vector2.zero;
				frameRT.anchoredPosition   = new Vector2(0, 0);
				
				switch (i)
				{
					case 0:
						frameRT.anchorMin = new Vector2(0.05f, 0.55f);
						frameRT.anchorMax = new Vector2(0.45f, 0.95f);
					break;
					
					case 1:
						frameRT.anchorMin = new Vector2(0.55f, 0.55f);
						frameRT.anchorMax = new Vector2(0.95f, 0.95f);
					break;
					
					case 2:
						frameRT.anchorMin = new Vector2(0.05f, 0.05f);
						frameRT.anchorMax = new Vector2(0.45f, 0.45f);
					break;
					
					case 3:
						frameRT.anchorMin = new Vector2(0.55f, 0.05f);
						frameRT.anchorMax = new Vector2(0.95f, 0.45f);
					break;
				}
				
				//Нумеруем пункты меню:
				frame.GetComponent<ddeMenuItem>().index = i;
				
				//Создаём предпросмотр: первый кадр диафильма
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
	private static Sprite crop (RectTransform frame, Sprite source)
	{
		//Размеры источника
		int sourceWidth  = source.texture.width;
		int sourceHeight = source.texture.height;
		
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
		return Sprite.Create (source.texture, rect, new Vector2(0, 0));
	}
	
	
	private static void addFrame(ddeDiafilm.Slide slide)
	{
		int   count = frames.Count;
		
		//Генерация нового кадра из префаба:
		Image image = Resources.Load<Image> ("slideHolder_");
		Image frame = Instantiate(image) as Image;
		
		RectTransform frameRT = frame.GetComponent<RectTransform>();
		
		//Случайным образом выбирается картинка для демонстрации (в слайде диафильма их может быть несколько)
		int spriteNumber = UnityEngine.Random.Range(0, slide.Pictures.Count-1);
		
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
	}

	
	//Плавное проявление указанного канвасгруппа
	static IEnumerator increaseAlpha(CanvasGroup target, float seconds)
	{
		target.alpha = 0; //Чисто на всякий случай
		
		while (target.alpha < 1)
		{
			yield return new WaitForSeconds(seconds); 
			target.alpha += 0.05f;
		}	
	}
	
	//Плавное исчезание указанного канвасгруппа
	static IEnumerator decreaseAlpha(CanvasGroup target, float seconds)
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
	
}
