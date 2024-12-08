using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScrollingChat : MonoBehaviour
{
    public TextMeshProUGUI chatText;
    private Queue<string> chatMessages = new Queue<string>(); 
    private int maxMessages = 30;

    void Start()
    {
        if (chatText == null)
        {
            Debug.LogError("Chat text not found");
        }
        else
        {
            Debug.Log("Chat text loaded");
        }

        AddMessage("Welcome! Historic Updates Appear Here!");



    }

    public void AddMessage(string newChatMessage)
    {
      
        newChatMessage = FormatTeamColor(newChatMessage);

        if (chatMessages.Count >= maxMessages)
        {
            chatMessages.Dequeue(); 
        }
        chatMessages.Enqueue(newChatMessage); 
        UpdateChatText();
    }

    private string FormatTeamColor(string message)
    {
 
        Dictionary<string, string> teamColors = new Dictionary<string, string>
    {
        { "red", "#FF0000" },
        { "green", "#00FF00" },
        { "blue", "#0000FF" },
        { "yellow", "#FFFF00" }
    };



        // bug fixed - ensures that words like captuRED dont have the "red" part highlighted red. 
        string[] words = message.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            foreach (var team in teamColors)
            {
               
                if (string.Equals(words[i], team.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                   
                    words[i] = $"<color={teamColors[team.Key]}>{words[i]}</color>";
                }
            }
        }

       
        return string.Join(" ", words);
    }


    private void UpdateChatText()
    {
        if (chatText != null)
        {
            chatText.text = string.Join("\n", chatMessages.ToArray());
        }
    }

    public void ClearChat()
    {
        chatMessages.Clear();
        UpdateChatText();
    }
}

