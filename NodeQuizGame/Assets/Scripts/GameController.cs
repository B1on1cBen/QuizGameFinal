using UnityEngine;
using UnityEngine.UI;
using SocketIO;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(SocketIOComponent))]
public class GameController : MonoBehaviour
{
	[SerializeField] AudioClip navigateSound;
	[SerializeField] AudioClip confirmSound;
	[SerializeField] AudioClip music;
	[SerializeField] AudioClip drumRollSound;
	[SerializeField] AudioClip tadaSound;
	[SerializeField] AudioClip supaSuccessSound;
	[SerializeField] AudioClip fartSound;
	[SerializeField] AudioClip hmmSound;
	[Space]
	[SerializeField] GameObject[] unselectedButtons;
	[SerializeField] GameObject[] selectedButtons;
	[Space]
	[SerializeField] GameTimer timer;
	[SerializeField] Text questionHeaderText;
	[SerializeField] Text questionText;
	[SerializeField] GameObject grade;
	[SerializeField] Text gradeText;
	[SerializeField] GameObject dimmer;
	[SerializeField] GameObject playAgain;
	[Space]
	[SerializeField] GameObject nameHeader;
	[SerializeField] GameObject nameBox;
	[SerializeField] InputField nameInput;
	[SerializeField] GameObject highScoreHeader;
	[SerializeField] GameObject[] highScoreBoxes;
	[SerializeField] GameObject nextButton;
	[SerializeField] GameObject tutorial1;
	[SerializeField] GameObject tutorial2;
	[SerializeField] GameObject connectionDisplay;
	[SerializeField] Sprite connection;

	[HideInInspector] public QandA[] questionsAndAnswers;
	[HideInInspector] public HighScore[] highScores;

	int score = 0;

	int currentButtonIndex;
	int prevButtonIndex;
	int questionIndex = -1;
	int newHighScoreIndex = 20;
	bool gameStarted = false;
	bool dataRecieved = false;
	bool scoresRecieved = false;
	bool connectedToServer = false;
	bool dead = false;
	bool gameOver = false;
	bool passedNavTut = false;
	bool nameEntered = false;
	bool highScoreViewed = false;
	bool confirmPressedThisFrame = false;

	int connectTries = 3;
	int dataReqTries = 0;
	int scoreReqTries = 0;

	AudioSource soundSource;
	AudioSource musicSource;

	static string gameDataFilePath = "/StreamingAssets/data.json";
	static string highScoreFilePath = "/StreamingAssets/scores.json";
	static SocketIOComponent socket;

	void Awake()
	{
		soundSource = gameObject.AddComponent<AudioSource>();
		musicSource = gameObject.AddComponent<AudioSource>();
	}

	void Start()
	{
		socket = GetComponent<SocketIOComponent>();
		socket.On("receiveServerData", ReceiveServerData);
		socket.On("receiveHighscores", ReceiveHighscores);
		socket.On("connect", OnConnect);

		//nameInput.OnSubmit(

		SelectButton(0, false);

		musicSource.playOnAwake = false;
		musicSource.loop = true;
		musicSource.clip = music;
		musicSource.volume = 0.208f;

		highScores = new HighScore[10];
	}

	public void StartGame()
	{
		musicSource.PlayDelayed(2);
		timer.StartTimer();

		NextQuestion();
	}

	void Update()
	{
		if (!dead)
		{
			if (!connectedToServer && connectTries > 0)
			{
				socket.Emit("ping");
				connectTries--;
			}

			if (!dataRecieved && !connectedToServer && connectTries <= 0)
			{
				Debug.LogWarning("Cannot connect to server.");
				LoadLocalGameData();
				LoadLocalHighScores();
			}

			if (connectedToServer && !dataRecieved)
			{
				connectionDisplay.GetComponent<Image>().sprite = connection;
				RequestServerData();
			}

			if (dataRecieved && !scoresRecieved)
				RequestHighScores();

			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				if (currentButtonIndex - 1 < 0)
					SelectButton(selectedButtons.Length - 1, true);
				else
					SelectButton(currentButtonIndex - 1, true);
			}

			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				if (currentButtonIndex + 1 > selectedButtons.Length - 1)
					SelectButton(0, true);
				else
					SelectButton(currentButtonIndex + 1, true);
			}

			if (Input.GetKeyDown(KeyCode.Return))
			{
				if (!gameStarted && dataRecieved)
				{
					StartGame();

					if (soundSource)
						soundSource.PlayOneShot(confirmSound);
					SelectButton(0, false);
					gameStarted = true;

					passedNavTut = true;
					tutorial1.gameObject.SetActive(false);
					tutorial2.gameObject.SetActive(false);
				}
				else
					ConfirmSelection();
			}

			if (timer.Time < 0)
			{
				SelectButton(0, false);
				timer.ResetTimer();
				NextQuestion();
			}
		}

		if (dead && Input.GetKeyDown(KeyCode.Return) && highScoreViewed && !confirmPressedThisFrame)
		{
			PlayConfirmSound();
			ShowEndPrompt();
			confirmPressedThisFrame = true;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
			Application.Quit();

		confirmPressedThisFrame = false;
	}

	public void SelectButton(int index)
	{
		SelectButton(index, true);
	}

	public void SelectButton(int index, bool playSound)
	{
		if (!passedNavTut && playSound)
		{
			passedNavTut = true;
			tutorial1.gameObject.SetActive(false);
			tutorial2.gameObject.SetActive(true);
		}

		prevButtonIndex = currentButtonIndex;
		currentButtonIndex = index;

		selectedButtons[prevButtonIndex].SetActive(false);
		selectedButtons[currentButtonIndex].SetActive(true);

		unselectedButtons[prevButtonIndex].SetActive(true);
		unselectedButtons[currentButtonIndex].SetActive(false);

		if (soundSource && playSound)
			soundSource.PlayOneShot(navigateSound);
	}

	public void ConfirmSelection()
	{
		confirmPressedThisFrame = true;
		if (gameOver)
		{
			if (currentButtonIndex == 0)
				SceneManager.LoadScene(0);
			else if (currentButtonIndex == 1)
				Application.Quit();
		}
		else if (soundSource)
			soundSource.PlayOneShot(confirmSound);

		if (nameEntered && !highScoreViewed)
		{
			GoToHighScores();
		}
		else if (questionIndex < questionsAndAnswers.Length)
		{
			if (currentButtonIndex == questionsAndAnswers[questionIndex].correctIndex)
				score++;

			SelectButton(0, false);
			timer.ResetTimer();
			NextQuestion();
		}
	}

	public void PlayConfirmSound()
	{
		if (soundSource)
			soundSource.PlayOneShot(confirmSound);
	}

	public void NextQuestion()
	{
		questionIndex++;
		if (questionIndex < questionsAndAnswers.Length)
		{
			questionHeaderText.text = "Question " + (questionIndex + 1) + " of " + questionsAndAnswers.Length;
			questionText.text = questionsAndAnswers[questionIndex].question;

			for (int i = 0; i < 4; i++)
			{
				unselectedButtons[i].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = questionsAndAnswers[questionIndex].answers[i];
				selectedButtons[i].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = questionsAndAnswers[questionIndex].answers[i];
			}
		}
		else
			EndGame();
	}

	void EndGame() {
		dead = true;
		musicSource.Stop();
		Debug.Log("Score: " + ((float)score / (float)questionsAndAnswers.Length));
		timer.gameObject.SetActive(false);

		questionHeaderText.text = "We're done!";
		questionText.text = "And your score is...";

		for (int i = 0; i < 4; i++)
		{
			unselectedButtons[i].SetActive(false);
			selectedButtons[i].SetActive(false);
		}

		soundSource.PlayOneShot(drumRollSound);
		Invoke("ShowScore", 4.5f);
	}

	void ShowScore()
	{
		questionHeaderText.text = "We're done!";

		string punctuation = "";

		float percent = ((float)score / (float)questionsAndAnswers.Length);
		float tempScore = percent * 100;
		score = (int)tempScore;

		float endDelay = 2.5f;

		grade.SetActive(true);

		if (percent < .6f)
		{
			punctuation = "%...";
			gradeText.text = "F";
			soundSource.PlayOneShot(fartSound);
		}
		else if (percent >= .6f && percent <= .7f)
		{
			gradeText.text = "D";
			punctuation = "%.";
			soundSource.PlayOneShot(hmmSound);
		}
		else if (percent > .7f && percent <= .8f)
		{
			gradeText.text = "C";
			punctuation = "%.";
			soundSource.PlayOneShot(hmmSound);
		}
		else if (percent > .8f && percent <= .9f)
		{
			gradeText.text = "B";
			punctuation = "%!";
			soundSource.PlayOneShot(tadaSound);
		}
		else if (percent == 1)
		{
			gradeText.text = "A";
			punctuation = "%!!! Congratulations!";
			endDelay = 4.5f;
			soundSource.PlayOneShot(supaSuccessSound);
		}

		questionText.text = (percent * 100) + punctuation;

		Invoke("ShowName", endDelay);
	}

	public void ShowEndPrompt()
	{
		Debug.Log("End");

		gameOver = true;
		dead = false;

		playAgain.SetActive(true);
		dimmer.SetActive(true);

		GameObject[] endUnselectedButtons = new GameObject[2];
		GameObject[] endSelectedButtons = new GameObject[2];
		int counter = 0;

		for (int i = 2; i < 4; i++)
		{
			endUnselectedButtons[counter] = unselectedButtons[i];
			unselectedButtons[i].SetActive(true);
			endSelectedButtons[counter] = selectedButtons[i];
			counter++;
		}

		unselectedButtons = endUnselectedButtons;
		selectedButtons = endSelectedButtons;

		unselectedButtons[0].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Yes";
		selectedButtons[0].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Yes";
		unselectedButtons[1].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "No";
		selectedButtons[1].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "No";

		SelectButton(0, false);
	}

    public void ShowName()
    {
        dimmer.SetActive(true);
        nameHeader.SetActive(true);
        nameBox.SetActive(true);
        GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(nameInput.gameObject);

		nameEntered = true;
    }

    public void GoToHighScores()
    {
		highScoreViewed = true;
		confirmPressedThisFrame = true;

		dimmer.SetActive(false);
        nameHeader.SetActive(false);
        grade.SetActive(false);
        questionHeaderText.transform.parent.parent.gameObject.SetActive(false);
        questionText.transform.parent.parent.gameObject.SetActive(false);
        nextButton.SetActive(true);
		GameObject.Find("EventSystem").GetComponent<EventSystem>().SetSelectedGameObject(null);
		nameBox.SetActive(false);
		highScoreHeader.SetActive(true);

		if (nameInput.text != "")
			UpdateHighScores();

        for (int i = 0; i < highScoreBoxes.Length; i++){
            highScoreBoxes[i].SetActive(true);

			if (i < highScores.Length)
			{
				if (highScores[i].name != "")
					highScoreBoxes[i].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = highScores[i].name + ": " + highScores[i].score + "%";
				else
					highScoreBoxes[i].transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "---";

				if (i == newHighScoreIndex)
				{
					highScoreBoxes[i].GetComponent<Image>().color = new Color(.75f, 0, 0);

					Transform front = highScoreBoxes[i].transform.GetChild(0);

					front.GetComponent<Image>().color = Color.white;
					front.GetChild(0).GetComponent<Text>().color = Color.white;

					Transform place = highScoreBoxes[i].transform.FindChild("Place");

					place.GetComponent<Image>().color = Color.black;
					place.GetChild(0).GetComponent<Text>().color = Color.white;
				}
			}
        }
    }

    void UpdateHighScores()
    {
		List<HighScore> scoreList = new List<HighScore>(highScores);

		newHighScoreIndex = 20;
		for (int i = scoreList.Count - 1; i >= 0; i--)
		{
			if (score >= scoreList[i].score)
				newHighScoreIndex = i;
		}

		if (newHighScoreIndex <= 9)
		{
			scoreList.Insert(newHighScoreIndex, new HighScore(nameInput.text, score));

			while (scoreList.Count > 10)
				scoreList.RemoveAt(10);

			highScores = scoreList.ToArray();
		}

		SaveHighScores(new HighScores(highScores));

		if(connectedToServer)
			SendHighScores();
	}

    void RequestServerData()
	{
        if (!dataRecieved)
        {
            if (dataReqTries < 3)
            {
                dataReqTries++;
                Debug.Log("Requesting data from server... " + dataReqTries + " tries.");
                socket.Emit("load data");
            }
            else
            {
                Debug.LogWarning("Cannot get data from server. Switching to local backup...");
                LoadLocalGameData();
            }
        }
	}

	void RequestHighScores()
	{
		if (!scoresRecieved)
		{
			if (scoreReqTries < 3)
			{
				scoreReqTries++;
				Debug.Log("Requesting highscores from server... " + scoreReqTries + " tries.");
				socket.Emit("load highscores");
			}
			else
			{
				Debug.LogWarning("Cannot get highscores from server. Switching to local backup...");
				LoadLocalHighScores();
			}
		}
	}

	void ReceiveServerData(SocketIOEvent e)
	{
		if (!dataRecieved)
		{
			Debug.Log("Got response from server!");
			GameData gameData;
			if (e.data != null)
			{
				gameData = JsonUtility.FromJson<GameData>(e.data.ToString());

				if (gameData.questionsAndAnswers.Length == 0)
				{
					Debug.LogWarning("Data from server is empty.");
					LoadLocalGameData();
					return;
				}

				questionsAndAnswers = gameData.questionsAndAnswers;

				// Save data from server as a local backup.
				SaveGameData(gameData);

				Debug.Log("Received data from server.");
				dataRecieved = true;
			}
			else
			{
				Debug.LogWarning("Data from server is null.");
				LoadLocalGameData();
			}
		}
	}

	void ReceiveHighscores(SocketIOEvent e)
	{
		if (!scoresRecieved)
		{
			Debug.Log("Got response from server!");
			HighScores scores;
			if (e.data != null)
			{
				scores = JsonUtility.FromJson<HighScores>(e.data.ToString());

				if (scores.highScores.Length == 0)
				{
					Debug.LogWarning("Highscores from server are empty.");
					LoadLocalHighScores();
					return;
				}

				highScores = scores.highScores;

				// Save data from server as a local backup.
				SaveHighScores(scores);

				Debug.Log("Received highscores from server.");
				scoresRecieved = true;
			}
			else
			{
				Debug.LogWarning("Highscores from server are null.");
				LoadLocalHighScores();
			}
		}
	}


	void LoadLocalGameData()
	{
		string filePath = Application.dataPath + gameDataFilePath;

		Debug.Log("Loading local game data backup from " + filePath);

		if (File.Exists(filePath))
		{
			string data = File.ReadAllText(filePath);
			GameData gameData = JsonUtility.FromJson<GameData>(data);
			questionsAndAnswers = gameData.questionsAndAnswers;
			Debug.Log("Local game data backup loaded!");

			dataRecieved = true;
		}
		else
		{
			Debug.LogError("No game data found on server or local backup.");

			questionHeaderText.text = "ERROR";
			questionText.text = "No game data found on server or local backup.";

			dead = true;
		}
	}

	void LoadLocalHighScores()
	{
		string filePath = Application.dataPath + highScoreFilePath;

		Debug.Log("Loading local highscore backup from " + filePath);

		if (File.Exists(filePath))
		{
			string data = File.ReadAllText(filePath);
			HighScores scores = JsonUtility.FromJson<HighScores>(data);
			highScores = scores.highScores;
			Debug.Log("Local highscore backup loaded!");
		}
		else
		{
			Debug.LogWarning("No highscore data found on server or local backup. Creating new score data");
			highScores = new HighScore[10];
			highScores[0] = new HighScore("", 0);
			highScores[1] = new HighScore("", 0);
			highScores[2] = new HighScore("", 0);
			highScores[3] = new HighScore("", 0);
			highScores[4] = new HighScore("", 0);
			highScores[5] = new HighScore("", 0);
			highScores[6] = new HighScore("", 0);
			highScores[7] = new HighScore("", 0);
			highScores[8] = new HighScore("", 0);
			highScores[9] = new HighScore("", 0);
		}

		scoresRecieved = true;
	}


	void SaveGameData(GameData data)
	{
		string jsonObj = JsonUtility.ToJson(data);

		string filePath = Application.dataPath + gameDataFilePath;
		File.WriteAllText(filePath, jsonObj);
	}

	void SaveHighScores(HighScores data)
	{
		string jsonObj = JsonUtility.ToJson(data);

		string filePath = Application.dataPath + highScoreFilePath;
		File.WriteAllText(filePath, jsonObj);
	}

	void OnConnect(SocketIOEvent e)
	{
		if (!connectedToServer)
		{
			Debug.Log("Connected to server!");
			connectedToServer = true;
			socket.Off("ping", OnConnect);
		}
	}

	void SendHighScores()
	{
		string jsonObj = JsonUtility.ToJson(new HighScores(highScores));
		socket.Emit("send highscores", new JSONObject(jsonObj));
	}
}
