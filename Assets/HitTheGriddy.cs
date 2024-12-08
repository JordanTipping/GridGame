using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class HitTheGriddy : MonoBehaviour
{
    // Grid settings
    public int rows = 5;
    public int columns = 5;
    public float cellSize = 1.0f;
    private float cellSpacing = 0.05f;
    public GameObject cellPrefab;

    // Team colors
    private Color team1Color = Color.red;
    private Color team2Color = Color.green;
    private Color team3Color = Color.blue;
    private Color team4Color = Color.yellow;

    // Team Management 
    private Dictionary<string, string> playerTeams = new Dictionary<string, string>();
    private string channelOwner;

    public Transform teamListPanel; 
    public GameObject usernamePrefab; 

    // Game state and turn management
    public string currentTurn = "red";
    private bool troopPlacementPhase = true;  
    private bool isAttackInProgress = false;

    // Troop management
    private int initialTroopCount = 21; 
    private Dictionary<string, int> troopsToPlace;

    // Control counts
    private int redCount = 0;
    private int greenCount = 0;
    private int blueCount = 0;
    private int yellowCount = 0;

    // Grid and troops
    private GameObject[,] gridArray;
    private int[,] troops;
    private Dictionary<string, Vector2Int> regionCoordinates = new Dictionary<string, Vector2Int>();

    // Capitals
    private Dictionary<Color, Vector2Int> teamCapitals = new Dictionary<Color, Vector2Int>();
    private Dictionary<Color, int> multipleCapitalsTurns = new Dictionary<Color, int>();

    // UI
    public TextMeshProUGUI turnTMP;
    private TextMeshProUGUI redControlTMP, greenControlTMP, blueControlTMP, yellowControlTMP;
    private ScrollingChat scrollingChat;

    void Start()
    {
        scrollingChat = FindObjectOfType<ScrollingChat>();
        if (scrollingChat == null)
        {
            Debug.LogError("ScrollingChat notn assigned.");
        }

        GameObject mainCanvas = GameObject.Find("Canvas");
        if (mainCanvas != null)
        {
            redControlTMP = mainCanvas.transform.Find("redControlTMP").GetComponent<TextMeshProUGUI>();
            greenControlTMP = mainCanvas.transform.Find("greenControlTMP").GetComponent<TextMeshProUGUI>();
            blueControlTMP = mainCanvas.transform.Find("blueControlTMP").GetComponent<TextMeshProUGUI>();
            yellowControlTMP = mainCanvas.transform.Find("yellowControlTMP").GetComponent<TextMeshProUGUI>();
        }




        UpdateTurnText();
        InitializeGrid();
        UpdateControlCountsUI();

        
    }

    public void SetChannelOwner(string owner)
    {
        channelOwner = owner.ToLower();
    }

    public void StartGame()
    {
        scrollingChat.AddMessage("Game Started!");
        StartTroopPlacementPhase();
    }

    private int currentTeamIndex = 0;
    private int totalPlayers = 0; 
    private const int maxPlayers = 4; 

    public void JoinTeam(string input, string commandUser)
    {
        string[] teams = { "red", "green", "blue", "yellow" }; 

        if (totalPlayers >= maxPlayers) 
        {
            scrollingChat.AddMessage("All teams are full.");
            return;
        }

        if (input.Contains(" ")) 
        {
            string[] splitInput = input.Split(' ', 2);
            if (splitInput.Length > 1 && commandUser.ToLower() == channelOwner.ToLower())
            {
                string fakeUsername = splitInput[1].Trim();

                if (playerTeams.ContainsKey(fakeUsername))
                {
                    scrollingChat.AddMessage($"{fakeUsername} is already in the {playerTeams[fakeUsername]} team.");
                    return;
                }

                
                string assignedTeam = teams[currentTeamIndex];
                currentTeamIndex = (currentTeamIndex + 1) % teams.Length;
                playerTeams[fakeUsername] = assignedTeam;
                totalPlayers++; 

                scrollingChat.AddMessage($"{fakeUsername} joined the {assignedTeam} team.");
                UpdateTeamList();
            }
        }
        else 
        {
            string username = input;

            if (playerTeams.ContainsKey(username))
            {
                scrollingChat.AddMessage($"{username} is already in the {playerTeams[username]} team.");
                return;
            }

            
            string assignedTeam = teams[currentTeamIndex];
            currentTeamIndex = (currentTeamIndex + 1) % teams.Length; 
            playerTeams[username] = assignedTeam;
            totalPlayers++; 

            scrollingChat.AddMessage($"{username} joined the {assignedTeam} team.");
            UpdateTeamList(); 
        }
    }


    private void UpdateTeamList()
    {
       
        foreach (Transform child in teamListPanel)
        {
            Destroy(child.gameObject);
        }

       
        foreach (var player in playerTeams)
        {
            string username = player.Key;
            string team = player.Value;

          
            GameObject usernameObj = Instantiate(usernamePrefab, teamListPanel);
            TextMeshProUGUI usernameText = usernameObj.GetComponent<TextMeshProUGUI>();


            usernameText.text = username;
            usernameText.color = team switch
            {
                "red" => Color.red,
                "green" => Color.green,
                "blue" => Color.blue,
                "yellow" => Color.yellow,
                _ => Color.white
            };
        }
    }




    public bool CanExecuteCommand(string username, string commandTeam, string ownerUsername)
    {
        if (username.ToLower() == ownerUsername.ToLower())
            return true; 

        if (playerTeams.TryGetValue(username, out string userTeam))
        {
            if (userTeam == commandTeam)
                return true;

           
        }
        return false;
    }

    private void AssignRandomOwnership()
    {
        List<Color> teamColors = new List<Color> { team1Color, team2Color, team3Color, team4Color };
        List<Vector2Int> availableCells = new List<Vector2Int>();
        List<Vector2Int> ownedCells = new List<Vector2Int>();


        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                availableCells.Add(new Vector2Int(row, col));
            }
        }

  
        availableCells = ShuffleList(availableCells);


        int territoriesPerTeam = 4;
        foreach (Color teamColor in teamColors)
        {
            for (int i = 0; i < territoriesPerTeam && availableCells.Count > 0; i++)
            {
                Vector2Int randomCell = availableCells[0];
                availableCells.RemoveAt(0);

                gridArray[randomCell.x, randomCell.y].GetComponent<SpriteRenderer>().color = teamColor;
                ownedCells.Add(randomCell);
                IncrementControlCount(teamColor);
            }
        }

     
        while (availableCells.Count > 0)
        {
            Vector2Int cellToAssign = availableCells[0];
            availableCells.RemoveAt(0);

            Color assignedColor = teamColors[Random.Range(0, teamColors.Count)];
            gridArray[cellToAssign.x, cellToAssign.y].GetComponent<SpriteRenderer>().color = assignedColor;
            ownedCells.Add(cellToAssign);
            IncrementControlCount(assignedColor);
        }

        AssignCapitals(ownedCells, teamColors);
    }


    // Fisher-Yates shuffle
    private List<Vector2Int> ShuffleList(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            Vector2Int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }

    private void AssignCapitals(List<Vector2Int> ownedCells, List<Color> teamColors)
    {
        HashSet<Vector2Int> capitalCells = new HashSet<Vector2Int>();

        foreach (Color teamColor in teamColors)
        {
            List<Vector2Int> potentialCapitals = new List<Vector2Int>();

            foreach (Vector2Int cell in ownedCells)
            {
                if (gridArray[cell.x, cell.y].GetComponent<SpriteRenderer>().color == teamColor)
                {
                    List<Vector2Int> adjacentOwned = GetAdjacentOwnedCells(cell, ownedCells);
                    if (adjacentOwned.Count > 0 && !IsAdjacentToCapital(cell, capitalCells)) 
                    {
                        potentialCapitals.Add(cell);
                    }
                }
            }

            if (potentialCapitals.Count > 0)
            {
                Vector2Int chosenCapital = potentialCapitals[Random.Range(0, potentialCapitals.Count)];
                capitalCells.Add(chosenCapital);
                teamCapitals[teamColor] = chosenCapital; 


                GameObject canvas = gridArray[chosenCapital.x, chosenCapital.y].transform.Find("Canvas2").gameObject;
                TextMeshProUGUI text = canvas.transform.Find("CoordPrefab").GetComponent<TextMeshProUGUI>();
                text.fontStyle = FontStyles.Bold;
                string regionName = text.text.ToLower();
                text.text = $"{regionName} \n(CAPITAL)";

                string teamColorName = GetTeamColorName(teamColor);
                scrollingChat.AddMessage($"{teamColorName} team, your capital is {regionName}.");
                
            }
        }

        scrollingChat.AddMessage("Type !join to join a team!\nChannel Owner -  Type !start when ready to begin!");
    }

    private void CheckForCapitalControl()
    {
        Dictionary<Color, int> capitalCount = new Dictionary<Color, int>();

        foreach (KeyValuePair<Color, Vector2Int> entry in teamCapitals)
        {
            Vector2Int capitalCoords = entry.Value;
            Color occupyingColor = gridArray[capitalCoords.x, capitalCoords.y].GetComponent<SpriteRenderer>().color;

            if (!capitalCount.ContainsKey(occupyingColor))
            {
                capitalCount[occupyingColor] = 0;
            }

            capitalCount[occupyingColor]++;
        }

        
        foreach (KeyValuePair<Color, int> entry in capitalCount)
        {
            Color teamColor = entry.Key;
            int count = entry.Value;

            if (count >= 2)
            {
                if (!multipleCapitalsTurns.ContainsKey(teamColor))
                {
                    multipleCapitalsTurns[teamColor] = 0;
                }

                multipleCapitalsTurns[teamColor]++;
                scrollingChat.AddMessage($"{GetTeamColorName(teamColor)} team controls {count} capitals for {multipleCapitalsTurns[teamColor]} turn(s).");

                if (multipleCapitalsTurns[teamColor] >= 3)
                {
                    scrollingChat.AddMessage($"{GetTeamColorName(teamColor)} team controls two capitals for three consecutive turns. They win!");
                    EndGame(teamColor);
                }
            }
            else
            {
                if (multipleCapitalsTurns.ContainsKey(teamColor))
                {
                    multipleCapitalsTurns[teamColor] = 0;
                }
            }
        }
    }

    private void EndGame(Color winningTeam)
    {
        scrollingChat.AddMessage($"{GetTeamColorName(winningTeam)} team is victorious! Game over.");
    }


    private bool IsAdjacentToCapital(Vector2Int cell, HashSet<Vector2Int> capitalCells)
    {
        int[,] directions = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int newRow = cell.x + directions[i, 0];
            int newCol = cell.y + directions[i, 1];

          
            if (newRow >= 0 && newRow < rows && newCol >= 0 && newCol < columns)
            {
                Vector2Int adjacentCell = new Vector2Int(newRow, newCol);

                if (capitalCells.Contains(adjacentCell))
                {
                    return true;
                }
            }
        }

      
        return false;
    }




    private List<Vector2Int> GetAdjacentOwnedCells(Vector2Int cell, List<Vector2Int> ownedCells)
    {
        List<Vector2Int> adjacentCells = new List<Vector2Int>();

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int adjRow = cell.x + i;
                int adjCol = cell.y + j;

                if (adjRow >= 0 && adjRow < rows && adjCol >= 0 && adjCol < columns)
                {
                    Vector2Int adjacentCell = new Vector2Int(adjRow, adjCol);
                    if (ownedCells.Contains(adjacentCell))
                    {
                        adjacentCells.Add(adjacentCell);
                    }
                }
            }
        }

        return adjacentCells;
    }

    private void IncrementControlCount(Color teamColor)
    {
        if (teamColor == team1Color) redCount++;
        else if (teamColor == team2Color) greenCount++;
        else if (teamColor == team3Color) blueCount++;
        else if (teamColor == team4Color) yellowCount++;
    }




    private void InitializeGrid()
    {
        gridArray = new GameObject[rows, columns];
        troops = new int[rows, columns];

        List<string> regionNames = new List<string>
    {
        "Cornwall", "Devon", "Dorset", "Hampshire", "Kent",
        "Somerset", "Wiltshire", "Oxfordshire", "Surrey", "Essex",
        "Gloucestershire", "Warwickshire", "Leicestershire", "Cambridgeshire", "Suffolk",
        "Shropshire", "Staffordshire", "Derbyshire", "Nottinghamshire", "Norfolk",
        "Lancashire", "Cheshire", "Yorkshire", "Durham", "Northumberland"
    };

        int regionIndex = 0;
        float gridWidth = columns * cellSize;
        float gridHeight = rows * cellSize;
        Vector2 gridOffset = new Vector2(gridWidth / 2f, gridHeight / 2f);

      
        float xOffset = -0.6f; 
        float yOffset = 0.1f; 

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 position = new Vector2(
                    (col * (cellSize + cellSpacing)) - gridOffset.x + xOffset,
                    (row * (cellSize + cellSpacing)) - gridOffset.y + yOffset
                );
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);
                string regionName = regionNames[regionIndex++ % regionNames.Count].ToLower();
                regionCoordinates[regionName] = new Vector2Int(row, col);
                cell.name = regionName;
                gridArray[row, col] = cell;

                GameObject canvas = cell.transform.Find("Canvas2").gameObject;
                TextMeshProUGUI text = canvas.transform.Find("CoordPrefab").GetComponent<TextMeshProUGUI>();
                text.text = regionName;

                troops[row, col] = 0; 
                TextMeshProUGUI troopText = canvas.transform.Find("TroopCount").GetComponent<TextMeshProUGUI>();
                troopText.text = troops[row, col].ToString();
            }
        }

        AssignRandomOwnership(); 
    }


   
    void UpdateControlCountsUI()
    {
        if (redControlTMP != null) redControlTMP.text = $"Red Controls: {redCount}";
        if (greenControlTMP != null) greenControlTMP.text = $"Green Controls: {greenCount}";
        if (blueControlTMP != null) blueControlTMP.text = $"Blue Controls: {blueCount}";
        if (yellowControlTMP != null) yellowControlTMP.text = $"Yellow Controls: {yellowCount}";
    }



    private bool IsAdjacent(Vector2Int fromCoords, Vector2Int toCoords)
    {
        int rowDiff = Mathf.Abs(fromCoords.x - toCoords.x); 
        int colDiff = Mathf.Abs(fromCoords.y - toCoords.y); 
        return
       (rowDiff <= 1 && colDiff <= 1) 
       && (rowDiff + colDiff > 0); 



    }

    public void Attack(string fromRegion, string toRegion, Color color)
    {
        if (isAttackInProgress) return;

        isAttackInProgress = true;

        fromRegion = fromRegion.ToLower();
        toRegion = toRegion.ToLower();

        if (regionCoordinates.TryGetValue(fromRegion, out Vector2Int fromCoords) &&
            regionCoordinates.TryGetValue(toRegion, out Vector2Int toCoords))
        {
            if (IsAdjacent(fromCoords, toCoords) && IsTeamColorValid(color) && IsAdjacentCellOwned(fromCoords, color))
            {
                HandleAttack(fromCoords, toCoords, color);
            }
            else
            {
                scrollingChat.AddMessage("Invalid move. Check team and adjacency.");
            }
        }
        else
        {
            scrollingChat.AddMessage("Invalid region names.");
        }

        isAttackInProgress = false;
    }

    private void HandleAttack(Vector2Int fromCoords, Vector2Int toCoords, Color color)
    {
        var targetCellRenderer = gridArray[toCoords.x, toCoords.y]?.GetComponent<SpriteRenderer>();
        if (targetCellRenderer == null)
        {
            Debug.LogError("SpriteRenderer missing");
            return;
        }

        int attackerTroops = troops[fromCoords.x, fromCoords.y];
        int defenderTroops = troops[toCoords.x, toCoords.y];

        if (attackerTroops <= 1)
        {
            scrollingChat.AddMessage("Not enough troops to attack!");
            return;
        }

        scrollingChat.AddMessage($"Rolling dice...");
        int attackerDice = Mathf.Min(attackerTroops - 1, 3);
        int defenderDice = Mathf.Min(defenderTroops, 2);

        List<int> attackerRolls = RollDice(attackerDice);
        List<int> defenderRolls = RollDice(defenderDice);

        scrollingChat.AddMessage($"Attacker rolls: {string.Join(", ", attackerRolls)}");
        scrollingChat.AddMessage($"Defender rolls: {string.Join(", ", defenderRolls)}");

        int attackerLosses = 0, defenderLosses = 0;
        for (int i = 0; i < Mathf.Min(attackerDice, defenderDice); i++)
        {
            if (attackerRolls[i] > defenderRolls[i])
                defenderLosses++;
            else attackerLosses++;
        }

        troops[fromCoords.x, fromCoords.y] -= attackerLosses;
        troops[toCoords.x, toCoords.y] -= defenderLosses;

        scrollingChat.AddMessage($"Attacker loses {attackerLosses} troops.");
        scrollingChat.AddMessage($"Defender loses {defenderLosses} troops.");

        UpdateTroopText(fromCoords);
        UpdateTroopText(toCoords);

        if (troops[toCoords.x, toCoords.y] <= 0)
        {
            CaptureRegion(fromCoords, toCoords, color, targetCellRenderer);
        }

        UpdateControlCountsUI();
    }

    private void CaptureRegion(Vector2Int fromCoords, Vector2Int toCoords, Color color, SpriteRenderer targetCellRenderer)
    {
        scrollingChat.AddMessage("Defender defeated! Territory captured.");
        Color previousColor = targetCellRenderer.color;
        targetCellRenderer.color = color;

        troops[toCoords.x, toCoords.y] = 1;
        troops[fromCoords.x, fromCoords.y] -= 1;

        scrollingChat.AddMessage($"1 troop moved from {fromCoords} to {toCoords}.");
        UpdateTroopText(fromCoords);
        UpdateTroopText(toCoords);

        UpdateControlCounts(color, previousColor);
    }


     
    private bool HasAvailableAttacks(Color teamColor)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (gridArray[row, col].GetComponent<SpriteRenderer>().color == teamColor && troops[row, col] > 1)
                {
                    //look for adjacet enemies. 
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0) continue;
                            int adjRow = row + i;
                            int adjCol = col + j;

                            if (adjRow >= 0 && adjRow < rows && adjCol >= 0 && adjCol < columns)
                            {
                                Color adjColor = gridArray[adjRow, adjCol].GetComponent<SpriteRenderer>().color;
                                if (adjColor != teamColor && adjColor != Color.clear) return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }


    private List<int> RollDice(int count)
    {
        List<int> rolls = new List<int>();
        for (int i = 0; i < count; i++)
        {
            rolls.Add(Random.Range(1, 7));
        }
        rolls.Sort((a, b) => b.CompareTo(a));
        return rolls;
    }

    private void UpdateTroopText(Vector2Int coords)
    {
        gridArray[coords.x, coords.y].transform.Find("Canvas2/TroopCount").GetComponent<TextMeshProUGUI>().text = troops[coords.x, coords.y].ToString(); 
    }



    private void UpdateControlCounts(Color newOwner, Color previousOwner)
    {
        if (previousOwner == team1Color) redCount--;
        else if (previousOwner == team2Color) greenCount--;
        else if (previousOwner == team3Color) blueCount--;
        else if (previousOwner == team4Color) yellowCount--;

        if (newOwner == team1Color) redCount++;
        else if (newOwner == team2Color) greenCount++;
        else if (newOwner == team3Color) blueCount++;
        else if (newOwner == team4Color) yellowCount++;
    }

    private void StartTroopPlacementPhase()
    {
        troopsToPlace = new Dictionary<string, int>
    {
        { "red", initialTroopCount },
        { "green", initialTroopCount },
        { "blue", initialTroopCount },
        { "yellow", initialTroopCount }
    };
        troopPlacementPhase = true;

        scrollingChat.AddMessage("Troop placement phase. Each player has 21 troops to place.");
        scrollingChat.AddMessage("To place troops: !place <region> <number> <team color> (!place dorset 3 red).");
    }



    public void PlaceTroop(string region, int numTroops, string teamColor)
    {


        if (teamColor.ToLower() != currentTurn)
        {
            scrollingChat.AddMessage($"It is not {teamColor}'s turn to place troops.");
            return;
        }

        region = region.ToLower();
        teamColor = teamColor.ToLower();

        if (regionCoordinates.TryGetValue(region, out Vector2Int coords))
        {
            Color cellColor = gridArray[coords.x, coords.y].GetComponent<SpriteRenderer>().color;

            if (IsTeamColorValid(cellColor, teamColor))
            {
                if (troopsToPlace[teamColor] >= numTroops)
                {
                    
                    troops[coords.x, coords.y] += numTroops;
                    troopsToPlace[teamColor] -= numTroops;

                  
                    gridArray[coords.x, coords.y].transform.Find("Canvas2/TroopCount").GetComponent<TextMeshProUGUI>().text = troops[coords.x, coords.y].ToString();
                    scrollingChat.AddMessage($"{teamColor} placed {numTroops} troops in {region}. Troops left to place: {troopsToPlace[teamColor]}");

                 
                    if (troopsToPlace[teamColor] == 0)
                    {
                        scrollingChat.AddMessage($"{teamColor} has placed all their troops.");
                        NextTurn(); 
                        CheckTroopPlacementPhaseEnd();
                    }
                }
                else
                {
                    scrollingChat.AddMessage($"{teamColor} does not have enough troops left to place {numTroops}.");
                }
            }
            else
            {
                scrollingChat.AddMessage($"{teamColor} cannot place troops in a region they do not control.");
            }
        }
        else
        {
            scrollingChat.AddMessage("Invalid region name.");
        }
    }


    public void MoveTroops(string fromRegion, string toRegion, int numTroops, string teamColor)
    {
      
        if (!regionCoordinates.TryGetValue(fromRegion, out Vector2Int fromCoords) ||
            !regionCoordinates.TryGetValue(toRegion, out Vector2Int toCoords))
        {
            scrollingChat.AddMessage("Invalid region name.");
            return;
        }

       
        if (!IsAdjacent(fromCoords, toCoords))
        {
            scrollingChat.AddMessage("Regions must be adjacent.");
            return;
        }
      
        Color fromCellColor = gridArray[fromCoords.x, fromCoords.y].GetComponent<SpriteRenderer>().color;
        Color toCellColor = gridArray[toCoords.x, toCoords.y].GetComponent<SpriteRenderer>().color;
        int toCellTroops = troops[toCoords.x, toCoords.y];

        
        if (!IsTeamColorValid(fromCellColor, teamColor))
        {
            scrollingChat.AddMessage("You can only move troops from a region you own.");
            return;
        }

        if (troops[fromCoords.x, fromCoords.y] < numTroops)
        {
            scrollingChat.AddMessage("Not enough troops to move.");
            return;
        }

        if (toCellColor != fromCellColor && toCellTroops > 0)
        {
            scrollingChat.AddMessage("You cannot move troops into an enemy region that has troops. Attack instead.");
            return;
        }

   
        troops[fromCoords.x, fromCoords.y] -= numTroops;
        troops[toCoords.x, toCoords.y] += numTroops;

        UpdateTroopText(fromCoords);
        UpdateTroopText(toCoords);

 
        if (toCellTroops == 0 && toCellColor != fromCellColor)
        {
            gridArray[toCoords.x, toCoords.y].GetComponent<SpriteRenderer>().color = fromCellColor;
            scrollingChat.AddMessage($"{teamColor} has taken control of {toRegion} by moving troops.");
            UpdateControlCounts(fromCellColor, toCellColor);
        }
        else
        {
            scrollingChat.AddMessage($"{teamColor} moved {numTroops} troops from {fromRegion} to {toRegion}.");
        }

        UpdateControlCountsUI();
    }



    private void CheckTroopPlacementPhaseEnd()
    {
        foreach (var troopsLeft in troopsToPlace.Values)
        {
            if (troopsLeft > 0)
            {
                return;
            }
        }

        scrollingChat.AddMessage("Troop placement phase is over!");
        troopPlacementPhase = false;


        scrollingChat.AddMessage("Available commands:");
        scrollingChat.AddMessage("!endturn");
        scrollingChat.AddMessage("!attack <from where> <to where> <team color>");
        scrollingChat.AddMessage("!move <from where> <to where> <number> <team color>");
    }

    bool IsTeamColorValid(Color color)
    {
        switch (currentTurn)
        {
            case "red": return color == team1Color;
            case "green": return color == team2Color;
            case "blue": return color == team3Color;
            case "yellow": return color == team4Color;
            default: return false;
        }
    }

    private bool IsTeamColorValid(Color cellColor, string teamColor)
    {
        return (teamColor == "red" && cellColor == team1Color) ||
               (teamColor == "green" && cellColor == team2Color) ||
               (teamColor == "blue" && cellColor == team3Color) ||
               (teamColor == "yellow" && cellColor == team4Color);
    }

    private string GetTeamColorName(Color color)
    {
        if (color == team1Color) return "Red";
        if (color == team2Color) return "Green";
        if (color == team3Color) return "Blue";
        if (color == team4Color) return "Yellow";
        return "Unknown";
    }

    bool IsAdjacentCellOwned(Vector2Int coords, Color teamColor)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                int adjRow = coords.x + i;
                int adjCol = coords.y + j;

                if (adjRow >= 0 && adjRow < rows && adjCol >= 0 && adjCol < columns)
                {
                    if (gridArray[adjRow, adjCol].GetComponent<SpriteRenderer>().color == teamColor) return true;
                }
            }
        }
        return false;
    }

    Color GetCellColor(int row, int col)
    {
        int halfRows = rows / 2;
        int halfCols = columns / 2;
        if (row < halfRows && col < halfCols) return team1Color;
        else if (row < halfRows && col >= halfCols) return team2Color;
        else if (row >= halfRows && col < halfCols) return team3Color;
        else return team4Color;
    }

    void UpdateTurnText()
    {
        turnTMP.text = "Current Turn: " + currentTurn;
    }


    public void EndTurn()
    {
        if (troopPlacementPhase)
        {
            scrollingChat.AddMessage("You cannot end your turn during the troop placement phase. Place all your troops first.");
            return;
        }

        scrollingChat.AddMessage($"{currentTurn} ends their turn.");
        NextTurn();
    }


    private void NextTurn()
    {
        
        switch (currentTurn)
        {
            case "red":
                currentTurn = "green";
                break;
            case "green":
                currentTurn = "blue";
                break;
            case "blue":
                currentTurn = "yellow";
                break;
            case "yellow":
                currentTurn = "red";
                break;
        }

        UpdateTurnText();
        scrollingChat.AddMessage($"It's now {currentTurn} turn.");

        CheckForCapitalControl();
        scrollingChat.AddMessage("Available commands:");

        if (troopPlacementPhase)
        {
            scrollingChat.AddMessage("!place");
        }

        if (!troopPlacementPhase)
        {
            scrollingChat.AddMessage("!endturn");
            scrollingChat.AddMessage("!attack <from where> <to where> <team color>");
            scrollingChat.AddMessage("!move <from where> <to where> <number> <team color>");
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}