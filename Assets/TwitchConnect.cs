using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using System.IO;

public class TwitchConnect : MonoBehaviour
{
    TcpClient Twitch;
    StreamReader Reader;
    StreamWriter Writer;

    const string DEFAULT_URL = "irc.chat.twitch.tv";
    const int DEFAULT_PORT = 6667;

    const string DEFAULT_USER = "attwitchchat";
    const string DEFAULT_OAUTH = "";
    const string DEFAULT_CHANNEL = "attwitchchat";

    string User = DEFAULT_USER;
    string OAuth = DEFAULT_OAUTH;
    string Channel = DEFAULT_CHANNEL;

    public HitTheGriddy hitTheGriddy;


    public GameObject loginPanel; 
    public Button connectButton;
    public TMP_Text statusText; 
    public TMP_InputField userInputField; 
    public TMP_InputField oauthInputField; 
    public TMP_InputField channelInputField; 
    public Toggle useDefaultToggle; 

    private Queue<string> commandQueue = new Queue<string>();
    private bool isProcessingCommand = false;
    private bool isConnected = false;
    private bool gameStarted = false;

    private void Start()
    {
       
        if (hitTheGriddy == null)
        {
            hitTheGriddy = FindObjectOfType<HitTheGriddy>();
            if (hitTheGriddy == null)
            {
                Debug.LogError("HitTheGriddy not found");
            }
        }

       
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonPressed);
        }
        else
        {
            Debug.LogError("Connect button not assigned.");
        }
    }

    private void OnConnectButtonPressed()
    {
        if (!isConnected)
        {
            if (useDefaultToggle != null && useDefaultToggle.isOn)
            {
                UseDefaultCredentials();
            }
            else
            {
                if (!ValidateCustomCredentials())
                {
                    UpdateStatus("Missing or incomplete credentials. Please provide username, OAuth token, and channel.");
                    return;
                }
                ReadCustomCredentials();
            }

            try
            {
                ConnectToTwitch();
                if (ValidateConnection())
                {
                    isConnected = true;
                    hitTheGriddy.SetChannelOwner(User); 
                    UpdateStatus("Success! Starting Game...");
                    StartCoroutine(HideLoginPanel());
                }
                else
                {
                    UpdateStatus("Connection failed. Check credentials.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to connect to Twitch: " + ex.Message);
                UpdateStatus("Connection failed. Check credentials or logs.");
            }
        }
        else
        {
            UpdateStatus("Already connected.");
        }
    }

    private void UseDefaultCredentials()
    {
        User = DEFAULT_USER;
        OAuth = DEFAULT_OAUTH;
        Channel = DEFAULT_CHANNEL;
        UpdateStatus("Using default credentials.");
    }

    private void ReadCustomCredentials()
    {
        if (userInputField != null)
        {
            User = userInputField.text;
        }
        if (oauthInputField != null)
        {
            OAuth = oauthInputField.text;
        }
        if (channelInputField != null)
        {
            Channel = channelInputField.text;
        }

        UpdateStatus("Using custom credentials.");
    }

    private bool ValidateCustomCredentials()
    {
        return !(string.IsNullOrWhiteSpace(userInputField?.text) ||
                 string.IsNullOrWhiteSpace(oauthInputField?.text) ||
                 string.IsNullOrWhiteSpace(channelInputField?.text));
    }

    private void ConnectToTwitch()
    {
        Twitch = new TcpClient(DEFAULT_URL, DEFAULT_PORT);
        Reader = new StreamReader(Twitch.GetStream());
        Writer = new StreamWriter(Twitch.GetStream());

        Writer.WriteLine("PASS " + OAuth);
        Writer.WriteLine("NICK " + User.ToLower());
        Writer.WriteLine("JOIN #" + Channel.ToLower());
        Writer.Flush();
    }

    private bool ValidateConnection()
    {
        try
        {
          
            for (int i = 0; i < 5; i++) 
            {
                if (Twitch.Available > 0)
                {
                    string response = Reader.ReadLine();
                    if (response.Contains("Welcome") || response.Contains(":tmi.twitch.tv 376"))
                    {
                        return true; 
                    }
                }
                System.Threading.Thread.Sleep(100); 
            }
            return false; 
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error validating connection: " + ex.Message);
            return false;
        }
    }

    private IEnumerator HideLoginPanel()
    {
        yield return new WaitForSeconds(3); 
        if (loginPanel != null)
        {
            loginPanel.SetActive(false); 
        }
        else
        {
            Debug.LogError("Login panel not assigned.");
        }
    }

    void Update()
    {

        if (isConnected && Twitch.Available > 0)
        {
            string twitchMessage = Reader.ReadLine();

            if (!string.IsNullOrEmpty(twitchMessage) && twitchMessage.Contains("PRIVMSG"))
            {
                commandQueue.Enqueue(twitchMessage);
            }
        }

        if (!isProcessingCommand && commandQueue.Count > 0)
        {
            isProcessingCommand = true;
            string commandToProcess = commandQueue.Dequeue();
            ParseChatCommand(commandToProcess);
            isProcessingCommand = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key pressed. Closing game...");
            Application.Quit();
        }
    }




    void ParseChatCommand(string twitchMessage)
    {
        int messageIndex = twitchMessage.IndexOf(":", 1);
        if (messageIndex >= 0)
        {
            string commandText = twitchMessage.Substring(messageIndex + 1).Trim();
            Debug.Log($"Parsed command text: {commandText}");

            string[] splitMessage = commandText.Split(' ');

            string username = twitchMessage.Substring(1, twitchMessage.IndexOf('!') - 1);

            if (splitMessage.Length >= 1)
            {
                string command = splitMessage[0].ToLower();

                if (command == "!start" && username.ToLower() == User.ToLower())
                {
                    gameStarted = true; 
                    Debug.Log("Game has started!");
                    hitTheGriddy.StartGame();
                    return; 
                }

               
                if (command == "!join" && gameStarted)
                {

                    return;
                }

            
                if (!gameStarted && command != "!join")
                {

                    return;
                }


                if (command == "!join")
                {
                    // Testing - !join <FakeName>

                    if (splitMessage.Length > 1 && username.ToLower() == User.ToLower())
                    {
                        
                        string fakeUsername = splitMessage[1].Trim();
                        hitTheGriddy.JoinTeam(fakeUsername, username);
                    }
                    else
                    {
                       
                        hitTheGriddy.JoinTeam(username, username);
                    }
                }
                else if (command == "!attack" && splitMessage.Length == 4)
                {
                    string fromRegion = splitMessage[1];
                    string toRegion = splitMessage[2];
                    string colorName = splitMessage[3].ToLower();

                    if (hitTheGriddy.CanExecuteCommand(username, colorName, User))
                    {
                        Color color = colorName switch
                        {
                            "red" => Color.red,
                            "green" => Color.green,
                            "blue" => Color.blue,
                            "yellow" => Color.yellow,
                            _ => throw new System.Exception($"Unrecognized color: {colorName}")
                        };

                        hitTheGriddy.Attack(fromRegion, toRegion, color);
                    }
                }
                else if (command == "!endturn")
                {
                    if (hitTheGriddy.CanExecuteCommand(username, hitTheGriddy.currentTurn, User))
                    {
                        hitTheGriddy.EndTurn();
                    }
                }
                else if (command == "!place" && splitMessage.Length == 4)
                {
                    string region = splitMessage[1];
                    if (int.TryParse(splitMessage[2], out int numTroops))
                    {
                        string colorName = splitMessage[3].ToLower();
                        if (hitTheGriddy.CanExecuteCommand(username, colorName, User))
                        {
                            hitTheGriddy.PlaceTroop(region, numTroops, colorName);
                        }
                    }
                }

                else if (command == "!move" && splitMessage.Length == 5)
                {
                    string fromRegion = splitMessage[1].ToLower();
                    string toRegion = splitMessage[2].ToLower();
                    if (int.TryParse(splitMessage[3], out int numTroops))
                    {
                        string colorName = splitMessage[4].ToLower();
                        if (hitTheGriddy.CanExecuteCommand(username, colorName, User))
                        {
                            hitTheGriddy.MoveTroops(fromRegion, toRegion, numTroops, colorName);
                        }
                    }
                }

                else
                {
                    Debug.LogWarning("Bad command format.");
                }
            }
        }
    }





    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }
}
