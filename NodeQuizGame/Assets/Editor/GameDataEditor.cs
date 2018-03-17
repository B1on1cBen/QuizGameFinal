using UnityEngine;
using System.IO;
using UnityEditor;
using SocketIO;

public class GameDataEditor : EditorWindow
{
	static string gameDataFilePath = "/StreamingAssets/data.json";
	public GameData editorData;
	static SocketIOComponent socket;
	bool socketInit = false;
	Vector2 scrollPos;

	[MenuItem("Window/Game Data Editor")]
	static void Init()
	{
		GameDataEditor window = (GameDataEditor)GetWindow(typeof(GameDataEditor), true, "Game Editor");
		window.Show();
	}

	void OnGUI()
	{
		if (!socketInit)
		{
			socket = GameObject.Find("Canvas").GetComponent<SocketIOComponent>();
			socket.On("receiveServerData", ReceiveServerData);

			socketInit = true;
		}

		if (editorData != null)
		{
			EditorGUILayout.BeginVertical();
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			//display the data from json
			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty serializedProperty = serializedObject.FindProperty("editorData");
			EditorGUILayout.PropertyField(serializedProperty, true);
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.EndScrollView();

			if (GUILayout.Button("Save Local Data"))
			{
				SaveGameData();
			}

			if (GUILayout.Button("Send Data To Server"))
			{
				SendGameData();
			}

			EditorGUILayout.EndVertical();
		}

		if (GUILayout.Button("Load Server Data"))
		{
			RequestServerData();
		}

		if (GUILayout.Button("Load Local Data"))
		{
			LoadLocalData();
		}
	}

	void RequestServerData()
	{
		socket.Emit("load data");
	}

	void ReceiveServerData(SocketIOEvent e)
	{
		Debug.Log("Received data from server");
		editorData = JsonUtility.FromJson<GameData>(e.data.ToString());
		Repaint();
	}

	void LoadLocalData()
	{
		string filePath = Application.dataPath + gameDataFilePath;

		Debug.Log("Loading data from " + filePath);

		if (File.Exists(filePath))
		{
			string gameData = File.ReadAllText(filePath);
			editorData = JsonUtility.FromJson<GameData>(gameData);
			Debug.Log("Data loaded!");
		}
		else
		{
			Debug.Log("No data found. New data created.");
			editorData = new GameData();
		}
	}

	void SaveGameData()
	{
		string jsonObj = JsonUtility.ToJson (editorData);

		string filePath = Application.dataPath + gameDataFilePath;
		File.WriteAllText (filePath, jsonObj);
	}

	void SendGameData()
	{
		string jsonObj = JsonUtility.ToJson (editorData);
		socket.Emit ("send data", new JSONObject(jsonObj));
	}
}
