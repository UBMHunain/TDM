using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Hadali;

public class DeathMatchManager : MonoBehaviour
{
    [Header("------------------------ PLAYER PROFILE ------------------------")]

    public string playerName;
    public Sprite playerFlag;
    public Transform[] playerSpawnPos;
    public GameObject Player;
    public static int totalKills_player;
    public static bool playerFired = false;

    [Header("------------------------ GAME PLAY ------------------------")]
    [Space(10)]
    public float alertTimeThreshold = 10f;
    public float timeRemaining = 10;

    //for InGame Calculations and Team Generations
    public int enemiesCount, playerAICount;

    //for Calculating Total Score of Teams
    public int terroristsScore, counterTerroristScore;
    public int weaponNumber;
    weaponselector inventory;
    public GameObject pickUpObject;

    public bool timerIsRunning = false;
    public bool isGameStarted, isGameCompleted, isPlayerDead;



    public Transform hittingObjectTransform;

    //store last enemy value in this
    public RaycastHit lastHitVal;


    [Header("------------------------ AI TEAMS ------------------------")]
    public AI_Teams[] playersAI;


    [Header("------------------------ UI CONTROLLER ------------------------")]
    public DeathMatch_UIController uIController;


    //Initialization of Game Code
    public void Awake()
    {
        SpawnPlayer();
    }

    void Start()
    {
        LoadGame();
        inventory = Player.GetComponent<weaponselector>();
    }

    private void Update()
    {
       if (isGameCompleted)
        {
            return;
        }

       if (isGameStarted)
        {
            TimeCalculations();
        }
        
       if(isGameStarted && isPlayerDead)
        {
            uIController.levelFailedPanel.SetActive(true);

            // Decrease the fillAmount gradually
            uIController.playerRespawnFill.fillAmount -= 0.2f * Time.deltaTime;

            if (uIController.playerRespawnFill.fillAmount <= 0)
            {
                ReSpawnPlayer();
            }
        }

        //CalculateDamageDirection();
    }



    #region UI - everything related UI


    void CalculateDamageDirection()
    {
        if (hittingObjectTransform != null)
        {
            // Calculate direction vector between player and hitting object
            Vector3 direction = hittingObjectTransform.position - transform.position;
            direction.Normalize();

            // Calculate angle between player's forward direction and the hitting object
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            uIController.directionIndicator.gameObject.SetActive(true);
            // Set the rotation of the UI image to the calculated angle
            uIController.directionIndicator.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);

            Invoke(nameof(DisableIndicator), 4);
        }
    }


    void DisableIndicator()
    {
        uIController.directionIndicator.gameObject.SetActive(false);
        //    // Hide or reset the direction indicator when there's no hitting object
        uIController.directionIndicator.rectTransform.rotation = Quaternion.identity;
    }


    public void PickUpItem()
    {
        inventory.HaveWeapons[weaponNumber] = true;
        inventory.AddWeapon(weaponNumber);
        inventory.playSwithSound();
        Destroy(pickUpObject);
    }

    void TimeCalculations()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                ShowMatchStats();
            }
        }
    }


    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        uIController.timeTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);


        //alert when time is less than 10 sec
        if (timeToDisplay <= alertTimeThreshold)
        {
            // Show the alert message
            uIController.alertText.gameObject.SetActive(true);

            int timeLeft = (int)timeToDisplay;
            uIController.alertText.text = timeLeft.ToString();
            uIController.alertText.color = Color.red;
        }
        else
        {
            uIController.alertText.gameObject.SetActive(false);
        }
    }



    public void UpdateKillsCount()
    {
        uIController.playerAICountTxt.text = counterTerroristScore.ToString();
        Debug.Log("PlayerCountTxt is : " + counterTerroristScore);


        uIController.enemyCountTxt.text = terroristsScore.ToString();
        Debug.Log("EnemyCountTxt is : " + terroristsScore);

        Debug.Log("EnemyCount is : " + enemiesCount);
        Debug.Log("PlayerCount is : " + playerAICount);



        if (playerAICount <= 1)
        {
            playerAICount = 3;

            //RespawnPlayersAI
            ReSpawnPlayers(0);
        }
        else if (enemiesCount <= 1)
        {
            enemiesCount = 4;

            //RespawnEnemiesAI
            ReSpawnPlayers(1);
        }
    }


    public void ShowMatchStats()
    {
        uIController.totalPlayersKilledTxt.text = counterTerroristScore.ToString();
        uIController.totalEnemiesKilledTxt.text = terroristsScore.ToString();

        if (counterTerroristScore > terroristsScore)
        {
            uIController.levelcompleteTxt.text = "Counter Terrorist Wins";
        }
        else
        {
            uIController.levelcompleteTxt.text = "Terrorist Wins";
            uIController.levelcompleteTxt.color = Color.red;
        }

        uIController.levelClearPanel.SetActive(true);
        Time.timeScale = 0;
        ShowTeamStats();

    }


    public void ShowTeamStats()
    {
        //game player data assingning
        Debug.Log("Total Kills by Player " + totalKills_player);
        uIController.gamePlayerNameTxt.text = playerName.ToString();
        uIController.gamePlayerFlag.sprite = playerFlag;

        //sending value to score calculations script
        uIController.scoreCalculations.gamePlayerKills = totalKills_player;
        uIController.scoreCalculations.totalKills_CT = counterTerroristScore;
        uIController.scoreCalculations.totalKills_T = terroristsScore;


        //AI player data assingning
        for (int k = 0; k < playersAI[0].TeamCount; k++)
        {
            uIController.team_CounterTerroristNames[k].text = playersAI[0].T_Names[k];
            uIController.team_CTerroristFlags[k].sprite = playersAI[0].T_playersFlags[k];
        }

        //Enemy player data assingning
        for (int k = 0; k < playersAI[1].TeamCount; k++)
        {
            uIController.team_TerroristNames[k].text = playersAI[1].T_Names[k];
            uIController.team_TerroristFlags[k].sprite = playersAI[1].T_playersFlags[k];
        }

        AdManager.Instance.ExecuteSequence_Interstitial(0);
    }


    public void OnButtonClick(string ClickName)
    {
        switch (ClickName)
        {
            case "Pause":
                uIController.LevelPausePanel.SetActive(true);
                uIController.Gamecontrols.SetActive(false);
                uIController.IngameControls.SetActive(false);

                Time.timeScale = 0;
                if (PlayerPrefs.GetInt("AdsRemove") == 0)
                {
                    if (AdManager.Instance)
                    {
                        //AdManager.Instance.ShowBannerRectAdmob(true);
                        AdManager.Instance.ExecuteSequence_Interstitial(0);
                    }
                }
                GameplaySoundController.instance.DisableAllRandomSounds();
                FirebaseAnalyticsController.CustomAnalyticEvent("TDM_Pause_Screen_Loaded");
                break;

            case "Restart":
                Time.timeScale = 1.0f;
                GameManager.timer = false;

                string currentScene = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(currentScene);

                uIController.LoadingPanel.SetActive(true);
                GameManager.PeopleKill = false;
                if (AdManager.Instance)
                {
                    AdManager.Instance.ShowBannerRectAdmob(false);
                }
                FirebaseAnalyticsController.CustomAnalyticEvent("TDM_Restarted");
                break;
            case "Resume":

                uIController.LevelPausePanel.SetActive(false);
                uIController.Gamecontrols.SetActive(true);
                uIController.IngameControls.SetActive(true);
                Time.timeScale = 1.0f;
                ControlFreak2.CFCursor.visible = true;
                GameManager.timer = false;

                if (AdManager.Instance)
                {
                    AdManager.Instance.ShowBannerRectAdmob(false);
                    AdManager.Instance.RequestInterstitial();
                }

                GameplaySoundController.instance.EnableAllRandomSounds();
                FirebaseAnalyticsController.CustomAnalyticEvent("TDM_Resumed");
                break;

            case "Home":
                Time.timeScale = 1.0f;

                if (AdManager.Instance)
                {
                    AdManager.Instance.ShowBannerRectAdmob(false);
                    AdManager.Instance.RequestInterstitial();
                }
                SceneManager.LoadScene("MainMenu");
                GameplaySoundController.instance.EnableAllRandomSounds();
                FirebaseAnalyticsController.CustomAnalyticEvent("TDM_GoingHome");
                break;

            default:
                break;
        }
    }

    #endregion

    #region GamePlay - everything related GamePlay

    void SpawnPlayer()
    {
        Debug.Log("Player pos change");
        if (Player != null)
        {
            int randPosNumber = Random.Range(0, playerSpawnPos.Length);
            Player.transform.SetPositionAndRotation(playerSpawnPos[randPosNumber].position, 
                playerSpawnPos[randPosNumber].rotation);
        }

        playerName = PlayerPrefs.GetString("PlayerName");
        playerFlag = uIController.randomFlags[PlayerPrefs.GetInt("FlagNo")];
    }


    public void ReSpawnPlayer()
    {
        Debug.Log("Player pos change");
        

        if (Player != null)
        {
            int randPosNumber = Random.Range(0, playerSpawnPos.Length);
            Player.transform.SetPositionAndRotation(playerSpawnPos[randPosNumber].position,
                playerSpawnPos[randPosNumber].rotation);
        }

        isPlayerDead = false;
        Player.GetComponent<playercontroller>().hitpoints = 100;
        uIController.playerRespawnFill.fillAmount = 1;
        uIController.levelFailedPanel.SetActive(false);

        Player.SetActive(true);
        Player.GetComponent<playercontroller>().DeathCam.transform.SetParent(Player.transform);
        Player.GetComponent<playercontroller>().DeathCam.SetActive(false);

        if (uIController.pickUpBtn.activeInHierarchy)
            uIController.pickUpBtn.SetActive(false);
    }

    void LoadGame()
    {
        //the code is getting all the players in AI_Teams Array (Spawning and Assigning their values respectively)
        //first loop getting values from Teams array

        for (int i = 0; i < playersAI.Length; i++)
        {
            Debug.Log("Player AI element number is " + i);

            int playersLength = playersAI[i].TeamCount;
            Debug.Log("Players length for element number " + i + " - " + playersLength);

            //this loop getting and assiging values from players array
            for (int j = 0; j < playersLength; j++)
            {
                Debug.Log("Loading Game Players of - " + j);
               int randPosNumber = Random.Range(0, playersAI[i].t_SpawnPos.Length);

                //instanting team players - all players from Teams Array
                GameObject playerAI = Instantiate(playersAI[i].t_Players,
                    playersAI[i].t_SpawnPos[randPosNumber].position,
                    playersAI[i].t_SpawnPos[randPosNumber].rotation);


                //adding random names to the list
                //doing this because we need these names throughout the map - so we are storing names in names array
                for (int k = 0; k < uIController.randomNames.Length; k++)
                {
                    playersAI[i].T_Names[j] = uIController.randomNames[Random.Range(0, uIController.randomNames.Length)];
                }


                //adding random flags to the list
                //doing this because we need these flag imgs throughout the map - so we are storing flag images in flags array
                for (int k = 0; k < uIController.randomFlags.Length; k++)
                {
                    playersAI[i].T_playersFlags[j] = uIController.randomFlags[Random.Range(0, uIController.randomFlags.Length)];
                }

                //assigning player values - values are being assigned to other script attached to each player seperately
                AI_PlayerMeta aI_PlayerStats = playerAI.GetComponentInChildren<AI_PlayerMeta>();
                if (aI_PlayerStats != null)
                {
                    aI_PlayerStats.playerName = playersAI[i].T_Names[j];
                    aI_PlayerStats.playerFlag.sprite = playersAI[i].T_playersFlags[Random.Range(0,
                        playersAI[i].T_playersFlags.Length)];
                }
            }
        }

        timerIsRunning = true;
        isGameStarted = true;
    }


    //this function calls the team with their team ID according to the requirement of team generation
    void ReSpawnPlayers(int teamID)
    {
        if (isGameCompleted || isPlayerDead)
        {
            return;
        }


        //this function works to identify teams for respawning players after death
        for (int i = teamID; i <= teamID; i++)
        {
            int playersLength = playersAI[i].TeamCount - 1;

            //this loop getting and assiging values from players array
            for (int j = 0; j < playersLength; j++)
            {
                int randPosNumber = Random.Range(0, playersAI[i].t_SpawnPos.Length);

                //instanting team players - all players from Teams Array
                GameObject playerAI = Instantiate(playersAI[i].t_Players,
                    playersAI[i].t_SpawnPos[randPosNumber].position,
                    playersAI[i].t_SpawnPos[randPosNumber].rotation);

                /*           //assigning names to the players list
                           //doing this because we need these names throughout the map - so we are storing names in names array
                           for (int k = 0; k < playersAI[i].T_Names.Length; k++)
                           {
                               playersAI[i].T_Names[j] = uIController.randomNames[Random.Range(0, playersAI[i].T_Names.Length)];
                           }

                           //adding random flags to the list
                           //doing this because we need these flag imgs throughout the map - so we are storing flag images in flags array
                           for (int k = 0; k < uIController.randomFlags.Length; k++)
                           {
                               playersAI[i].T_playersFlags[j] = uIController.randomFlags[Random.Range(0, uIController.randomFlags.Length)];
                           }*/

                //assigning player values - values are being assigned to other script attached to each player seperately
                AI_PlayerMeta aI_PlayerStats = playerAI.GetComponentInChildren<AI_PlayerMeta>();
                if (aI_PlayerStats != null)
                {
                    aI_PlayerStats.playerName = playersAI[i].T_Names[j];
                    aI_PlayerStats.playerFlag.sprite = playersAI[i].T_playersFlags[Random.Range(0, 
                        playersAI[i].T_playersFlags.Length)];
                }
            }
        }
    }

    #endregion
}

[System.Serializable]
public class AI_Teams
{
    [SerializeField]
    private string name;
    public int TeamCount;
    public GameObject t_Players;

    [Space(10)]
    public Transform[] t_SpawnPos;

    [Header("Fetching Names from UI Controller Random Names")]
    public string[] T_Names;
    [Space(10)]

    [Header("Fetching Flag Images from UI Controller Random Flags")]
    public Sprite[] T_playersFlags;
}

[System.Serializable]
public class DeathMatch_UIController
{
    [Header("G A M E __ O B J E C T S")]
    [Space(10)]

    public GameObject levelClearPanel;
    public GameObject levelFailedPanel, LevelPausePanel, Gamecontrols, IngameControls, LoadingPanel;
    public GameObject pickUpBtn;

    [Header("U_I __ T E X T S")]
    [Space(10)]

    public Text levelcompleteTxt;
    public Text gamePlayerNameTxt;
    [Space(10)]
    public Text timeTxt;
    public Text alertText;
    public Text playerAICountTxt,
        enemyCountTxt,
        playerHealthTxt,
        totalPlayersKilledTxt,
        totalEnemiesKilledTxt;

    public Text[] team_CounterTerroristNames;
    public Text[] team_TerroristNames;

    [Header("U_I __ I M A G E S")]
    [Space(10)]
    public Image gamePlayerFlag;
    public Image directionIndicator;
    public Image playerRespawnFill;
    
    [Space(10)]
    public Image[] team_CTerroristFlags;
    public Image[] team_TerroristFlags;
    public Sprite[] randomFlags;

    [Header("S T R I N G S")]
    [Space(10)]
    public string[] randomNames;

    [Header("S C R I P T __ R E F")]
    [Space(10)]
    public ScoreCalculations scoreCalculations;
}



