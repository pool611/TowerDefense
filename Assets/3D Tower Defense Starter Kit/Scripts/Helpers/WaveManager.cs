/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//WaveManager.cs handles spawning of waves and wave properties
[System.Serializable]
public class WaveManager : MonoBehaviour
{
    //store wave properties, this is a list of an own class (see below)
    [HideInInspector]
    public List<WaveOptions> options = new List<WaveOptions>();

    //wave mode to handle the behavior of wave spawning
    public enum WaveMode
    {
        normal,         //top to bottom
        endless,        //top to bottom in a loop
        randomEndless   //select random wave after each wave
    }
    //default wave mode variable
    public WaveMode waveMode = WaveMode.normal;

    //wave start option to handle the behaviour and time of new waves
    public enum WaveStartOption
    {
        waveCleared, 	//wait till current wave is cleared
        interval,		//wait defined interval
        userInput,		//wait for the player to start next wave
        roundBased		//wait till current wave is over and player input
    }
    //default wave start option variable
    public WaveStartOption waveStartOption = WaveStartOption.waveCleared;

    //internal wave index. on waveMode = normal, this is equal to the current wave.
    //on endless modes, this variable defines the actual wave index (entry) in the
    //WaveSettings widget, not the wave count played during this session
    private int waveIndex = 0;
    //delay between two waves - break / time to relax few seconds
    public int secBetweenWaves;
	//increasing seconds between waves on wave start option 'interval'
    public int secIncrement;
    //auto start waves at scene launch
    public bool autoStart;
    //auto clean up unused inactive instances above preload amount
    public bool autoCleanup;

    //options for WaveModes other than normal (endless)
    public EndlessOptions endlessOptions = new EndlessOptions();
    //please scroll down to the actual classes
    public WaveAnimations anims = new WaveAnimations();
    public WaveSounds sounds = new WaveSounds();

    //seconds till next wave starts, (between waves)
    //used on wave start option 'interval' and 'waveCleared'
    //accessed from GameInfo.cs to display the wave timer
    [HideInInspector]
    public float secTillWave = 0;

    //boolean value for allowing the player to start the next wave
    //only used on wave start option 'userInput', this gets toggled after all enemies
    //of the current wave are spawned so the player is able to start the next wave immediately
    //via our start button in GameInfo.cs 
    [HideInInspector]
    public bool userInput = true;


    void Start()
    {
        //set total wave count of GameHandler.cs so that GameInfo.cs can display it within our GUI
        //display actual waves on waveMode normal, otherwise GameInfo.cs will use its static value
        if (waveMode == WaveMode.normal)
            GameHandler.waveCount = options.Count.ToString();

        //play background music as soon as the game starts
        AudioManager.Play(sounds.backgroundMusic);

        //start waves immediately on game launch ( no extra button )
        if(autoStart)
            StartWaves();
        
    }


    //method to launch waves, called by GameInfo.cs (start button)
    public void StartWaves()
    {
        //start first wave
        StartCoroutine(LaunchWave());
    }


    //here we check the state of our game and perform different actions per state
    void CheckStatus()
    {
        //don't continue if we lost the game
        if (GameHandler.gameHealth <= 0)
        {
            CancelInvoke("CheckStatus");
            return;
        }

        //there are waves left, continue
        if (waveIndex < options.Count)
        {
            //not all enemies are dead/removed, do nothing and return
            if (GameHandler.enemiesAlive > 0)
                return;

			//handle different wave start options
			//(we only implemented 'waveCleared')
            switch (waveStartOption)
            {
            	//all enemies are dead and chosen option is 'wait till wave is cleared'
            	//this part gets executed with its specific properties
                case WaveStartOption.waveCleared:
					//increase seconds between waves (if secIncrement is greater than 0),
					//starting at the 2nd wave. in endless modes only skip first wave
                    if (GameHandler.wave > 0)
                        secBetweenWaves += secIncrement;

					//set and reduce wave timer with current value
					//(here wave timer starts if the current wave is over)
                    StartCoroutine("WaveTimer", secBetweenWaves);

                    //start next wave in defined seconds
                    Invoke("StartWaves", secBetweenWaves);
                    break;

				//we skip other options, they don't have specific properties
                    /*
                case WaveStartOption.interval:
                case WaveStartOption.userInput:
                case WaveStartOption.roundBased:
                    break;
                    */
            }

            Debug.Log("Wave Defeated");
         
			//play background music between waves
            AudioManager.Play(sounds.backgroundMusic);
            //play "wave end" sound one time
            AudioManager.Play2D(sounds.waveEndSound);
        }
        else
        {
            //to the given time - at the last enemy of the last wave (in WaveMode normal)
            //this step repeats checking if all enemies are dead and therefore the game is over
            //if so, it sets the gameOver flag to true
            //GameHandler.cs then looks up our health points and determines win or lose
            if (GameHandler.enemiesAlive > 0)
                return;

			//play background music on game end
            AudioManager.Play(sounds.backgroundMusic);

			//play "wave end" sound one time
            AudioManager.Play2D(sounds.waveEndSound);

			//toggle gameover flag
            GameHandler.gameOver = true;
        }

        //cancel repeating CheckStatus() calls after the part above hasn't returned
        //this means we executed a complete CheckStatus() call and either started a new
        //wave or ended the game
        CancelInvoke("CheckStatus");
    }


    //this method launches a new / next wave
    IEnumerator LaunchWave()
    {
        Debug.Log("Wave " + waveIndex + " launched! StartUp GameTime: " + Time.time);

        //if toggled, destroy inactive instances for every pool
        //useful on endless modes to not flood the memory
        if (autoCleanup)
            PoolManager.DestroyAllInactive(true);

        //handle specific wave mode setting after the first wave
        if(GameHandler.wave > 0)
            IncreaseSettings();

        //play battle music while fighting
        AudioManager.Play(sounds.battleMusic);

        //play wave start sound
        AudioManager.Play2D(sounds.waveStartSound);

        //play spawn start animation
        if (anims.spawnStart)
        {
            anims.objectToAnimate.animation.Play(anims.spawnStart.name);
            //wait until spawn animation ended before spawning enemies
            yield return new WaitForSeconds(anims.spawnStart.length);
        }

        //spawn independent coroutines for every enemy type (row) defined in the current wave,
        //or in other words, go through the wave and start a coroutine for each row in this wave
        for (int i = 0; i < options[waveIndex].enemyPrefab.Count; i++)
        {
            StartCoroutine(SpawnEnemyWave(i));
        }

        //only invoke the wave status check ("CheckStatus()") when we need it
        //CheckStatus() checks if all enemies in this wave are defeated or it's the last wave etc.
        //this method gets called when all enemies are successfully spawned of this wave
		//here we get the seconds at what time this happens
        float lastSpawn = GetLastSpawnTime(waveIndex);

		//cancel running CheckStatus calls, below we start them again
        if (IsInvoking("CheckStatus"))
            CancelInvoke("CheckStatus");

		//handle different wave start option
        switch (waveStartOption)
        {
        	//'waveCleared' and 'roundBased' don't have specific properties, skip them
        	/*
            case WaveStartOption.waveCleared:
            case WaveStartOption.roundBased:
                break;
			*/
			
			//on 'interval' we start the next wave immediately in 'secBetweenWaves' seconds
			//ignoring the current wave status and enemies
            case WaveStartOption.interval:
            	//increase seconds between the following waves
                if (GameHandler.wave > 0)
                    secBetweenWaves += secIncrement;

				//on WaveMode normal, only invoke new waves until the last wave
                //ignore on all other WaveModes and repeat invoking new waves
                if (waveMode != WaveMode.normal || waveIndex + 1 < options.Count)
                {
                	//set and reduce wave timer with current value
					//(here the wave timer starts automatically with every new wave)
                    StartCoroutine("WaveTimer", secBetweenWaves);
                    //start new wave in 'secBetweenWaves' seconds
                    Invoke("StartWaves", secBetweenWaves);
                }
                break;
                
            //on option 'userInput', at wave start the boolean 'userInput' is set to false
            //so the player isn't able to start new waves anymore until all enemies are spawned
            //we call "ToggleInput" at the last spawned enemy time which toggles userInput again
            case WaveStartOption.userInput:
                userInput = false;
                Invoke("ToggleInput", lastSpawn);
                break;
        }

		//repeat CheckStatus calls every 2 seconds after the last enemy of this wave
        InvokeRepeating("CheckStatus", lastSpawn, 2f);
        
        //invoke spawn end animation at the given time
        Invoke("PlaySpawnEndAnimation", lastSpawn);

        //increase wave index (UI)
        GameHandler.wave++;

        //set waveIndex value for spawning desired wave based on WaveMode value
        switch (waveMode)
        {
            case WaveMode.normal:
                //normally increase the wave index
                waveIndex++;
                break;
            case WaveMode.endless:
                //reset if we reached the last wave (loop)
                waveIndex++;
                if (waveIndex == options.Count)
                    waveIndex = 0;
                break;
            case WaveMode.randomEndless:
                //randomize spawning wave each time
                int rnd = waveIndex;
                while(rnd == waveIndex)
                    waveIndex = Random.Range(0, options.Count);
                break;
        }
    }


    //play spawn end animation, invoked by CheckStatus() on the last enemy
    void PlaySpawnEndAnimation()
    {
        if (anims.spawnEnd)
        {
            anims.objectToAnimate.animation.Play(anims.spawnEnd.name);
        }
    }

	
	//toggle userInput after the last enemy of the current wave
	//(the player is able to start a new wave on wave start option 'userInput')
    void ToggleInput()
    {
        userInput = true;
    }


	//this method reduces the seconds variable till the next wave,
	//then this value gets displayed on the screen by GameInfo.cs
    IEnumerator WaveTimer(float seconds)
    {
    	//store passed in seconds value and add current playtime
    	//to get the targeted playtime value
        float timer = Time.time + seconds;

		//while the playtime hasn't reached the desired playtime
        while (Time.time < timer)
        {
        	//get actual seconds till next wave by subtracting calculated and current playtime
        	//this value gets rounded to two decimals
            secTillWave = Mathf.Round((timer - Time.time) * 100f) / 100f;
            yield return true;
        }

		//when the time is up we set the calculated value exactly back to zero
        secTillWave = 0f;
    }


    //this method spawns all enemies for one wave
    //the given parameter defines the position in our wave editor lists
    //-(the enemy row of our current wave)
    IEnumerator SpawnEnemyWave(int index)
    {
        //store row index because it could change over the next time delay
        int waveNo = waveIndex;
        //delay spawn at start (seconds defined in List "startDelay")
        yield return new WaitForSeconds(Random.Range(options[waveNo].startDelayMin[index], options[waveNo].startDelayMax[index]));

        //Debug.Log("Spawning " + EnemyCount[index] + " " + EnemyPref[index].name + " on Path: " + Path[index]);

        //instantiate the entered count of enemies of this row
        for (int j = 0; j < options[waveNo].enemyCount[index]; j++)
        {
            //print error if prefab is not set and abort
            if (options[waveNo].enemyPrefab[index] == null)
            {
                Debug.LogWarning("Enemy Prefab not set in Wave Editor!");
                yield return null;
            }

            //if this row has no path Container assigned (enemies don't get a path), debug a warning and return
            if (options[waveNo].path[index] == null)
            {
                Debug.LogWarning(options[waveNo].enemyPrefab[index].name + " has no path! Please set Path Container.");
                break;
            }

            //all checks were passed, spawn this enemy
            SpawnEnemy(waveNo, index);
                
            //delay the spawn time between enemies (seconds defined in List "delayBetween")
            yield return new WaitForSeconds(Random.Range(options[waveNo].delayBetweenMin[index],options[waveNo].delayBetweenMax[index]));
        }
    }


    void SpawnEnemy(int waveNo, int index)
    {
        //get prefab and first waypoint position of this enemy and its path
        GameObject prefab = options[waveNo].enemyPrefab[index];
        Vector3 position = options[waveNo].path[index].waypoints[0].position;

        //instantiate/spawn enemy from PoolManager class with its configs
        GameObject enemy = PoolManager.Pools["Enemies"].Spawn(prefab, position, Quaternion.identity);
        //don't continue in case PoolManager couldn't get an instance (due to limited instances)
        if (!enemy) return;
        //get the TweenMove component so we can set the corresponding path container to follow
        enemy.GetComponentInChildren<TweenMove>().pathContainer = options[waveNo].path[index];
        //get the enemy Properties component
        Properties prop = enemy.GetComponentInChildren<Properties>();
        //overwrite health and/or shield values if they are not left on the default value 'zero'
        if (options[waveNo].enemyHP[index] > 0)
            prop.health = options[waveNo].enemyHP[index];
        if (options[waveNo].enemySH[index] > 0)
            prop.shield.value = options[waveNo].enemySH[index];

        //increase alive enemy count by one
        GameHandler.enemiesAlive++;
    }


    //this method calculates the maximum time for spawning an enemy per wave and returns it
    //so we don't call CheckStatus() until we need to
    float GetLastSpawnTime(int wave)
    {
        //time result variable to return
        float lastSpawn = 1;

        //loop through all enemy rows in this active wave and calculate spawn time
        //store the highest spawn time
        for (int i = 0; i < options[wave].enemyCount.Count; i++)
        {
            //last enemy spawn time output, comment out to see details
            /*
            Debug.Log("Overall spawn time for enemy: " + i + " in wave " + wave + " === " + "delay " + (Delay[indx + i] 
                        + " multiply " + (EnemyCount[indx+i] - 1) * DelayBetw[indx + i]) + " === "
                        + (Delay[indx + i] + (EnemyCount[indx + i] - 1) * DelayBetw[indx + i]) + " seconds.");
            */

            //add each possible delay, to calculate final spawn time for this row
            //the final time consists of the delay at start and time between all enemies
            float result = options[wave].startDelayMax[i] + (options[wave].enemyCount[i] - 1) * options[wave].delayBetweenMax[i];

            //save result if higher spawn time was found
            if (result > lastSpawn)
            {
                //add a quarter second at the end so CheckStatus() doesn't get called at the same frame
                lastSpawn = result + 0.25f;
            }
        }

        //give result back
        return lastSpawn;
    }


    //on endless WaveModes, increase wave settings based on endlessOptions.
    //on endless, this gets called after all waves went through.
    //on randomEndless, this gets called after every single wave.
    void IncreaseSettings()
    {
        switch (waveMode)
        {
            //do nothing on normal mode
            case WaveMode.normal:
                return;
            //on endless mode,
            //only continue if all waves went through
            case WaveMode.endless:
                if (GameHandler.wave % options.Count != 0)
                    return;
                break;
        }
        
        //loop through each wave
        for (int i = 0; i < options.Count; i++)
        {
            //get current wave option
            WaveOptions option = options[i];

            //loop through each row of this wave
            for (int j = 0; j < option.enemyPrefab.Count; j++)
            {
                //if enemy amount increase is enabled
                if (endlessOptions.increaseAmount.enabled)
                {
                    //overwrite enemy amount based on fix or percentual value
                    if (endlessOptions.increaseAmount.type == TDValue.fix)
                        option.enemyCount[j] += (int)endlessOptions.increaseAmount.value;
                    else
                        option.enemyCount[j] = Mathf.CeilToInt(option.enemyCount[j] * endlessOptions.increaseAmount.value);
                }

                //if enemy amount increase is enabled
                if (endlessOptions.increaseHP.enabled)
                {
                    //if we haven't set a default overwriting value,
                    //get the enemy health points and set it as default value here
                    //(this way we can increase that value the next time) 
                    if (option.enemyHP[j] == 0)
                        option.enemyHP[j] = option.enemyPrefab[j].GetComponent<Properties>().health;
                    //overwrite enemy health points based on fix or percentual value
                    if (endlessOptions.increaseHP.type == TDValue.fix)
                        option.enemyHP[j] += endlessOptions.increaseHP.value;
                    else
                        option.enemyHP[j] = Mathf.Round(option.enemyHP[j] * endlessOptions.increaseHP.value
                                            * 100f) / 100f;
                }

                //if enemy amount increase is enabled
                if (endlessOptions.increaseSH.enabled)
                {
                    //if we haven't set a default overwriting value,
                    if (option.enemySH[j] == 0)
                    {
                        //get the component of this enemy
                        Properties prop = option.enemyPrefab[j].GetComponent<Properties>();
                        //check if the enemy has its shield enabled, then insert this value here
                        if (prop.shield.enabled)
                            option.enemySH[j] = prop.shield.value;
                        else
                            //shield isn't enabled for this enemy, skip to next
                            continue;
                    }
                    //overwrite enemy shield value based on fix or percentual value
                    if (endlessOptions.increaseSH.type == TDValue.fix)
                        option.enemySH[j] += endlessOptions.increaseSH.value;
                    else
                        option.enemySH[j] = Mathf.Round(option.enemySH[j] * endlessOptions.increaseSH.value
                                            * 100f) / 100f;
                }
            }
        }
    }
}


//value enum for various settings,
//not limited to this script
public enum TDValue
{
    fix,
    percentual
}


//Options class for endless WaveModes
[System.Serializable]
public class EndlessOptions
{
    //increase health points setting
    public Setting increaseHP = new Setting();
    //increase shield value setting
    public Setting increaseSH = new Setting();
    //increase enemy amount setting
    public Setting increaseAmount = new Setting();

    //each setting has a toggle,
    //fix/percentual type and corresponding value
    [System.Serializable]
    public class Setting
    {
        public bool enabled;
        public TDValue type;
        public float value;
    }
}

[System.Serializable]
public class WaveAnimations
{
    public GameObject objectToAnimate;
    //animation to play on wave start
    public AnimationClip spawnStart;
    //animation to play when spawns ended
    public AnimationClip spawnEnd;
}


[System.Serializable]
public class WaveSounds
{
    //sound to play on wave start
    public AudioClip waveStartSound;
    //sound to play during battle
    public AudioClip battleMusic;
    //sound to play on wave end
    public AudioClip waveEndSound;
    //sound to play during breaks
    public AudioClip backgroundMusic;
}


//Wave Options class - per wave
[System.Serializable]
public class WaveOptions
{
    //enemy prefab to instantiate, multiple enemies per wave possible
    public List<GameObject> enemyPrefab = new List<GameObject>();
    //overwrite enemy hp value (optional)
    public List<float> enemyHP = new List<float>();
    //overwrite enemy shield value (optional)
    public List<float> enemySH = new List<float>();
    //how many enemies to spawn, per type
    public List<int> enemyCount = new List<int>();
    //spawn delay measured from start, per type
    public List<float> startDelayMin = new List<float>();
    public List<float> startDelayMax = new List<float>();
    //spawn delay between each enemy, per type
    public List<float> delayBetweenMin = new List<float>();
    public List<float> delayBetweenMax = new List<float>();
    //which path each enemy has to follow, per type
    public List<PathManager> path = new List<PathManager>();
}