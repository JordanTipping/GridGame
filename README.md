# GridGame
A twitch-integrated grid game inspired by Risk. 



IMPORTANT - When entering your access token, PREFIX IT with oauth:

This is because the original third party service that was used to generate the token automatically used the prefix, but was taken down days prior to submission.
 
A final product would need to register with Twitch. If the new method is also taken down, there are untested alternatives such as twitchscopes.com or antiscuff.com/oauth. Only attempt to use these if the tested token generation method detailed below is taken down. 

If you have typed a command correctly, and there is no change on the game board, type another correct command and the board will update, processing the previous command. 



Example login:
user: 	 MyUserName
channel: MyChannelName
oauth:   oauth:MyAccessToken


TOKEN GENERATION:

1. Log in to your Twitch account 

2. Visit https://twitchtokengenerator.com/ (a third party service for prototyping and testing)

3. You will be prompted - click "I am here to get a Bot Chat Token". 

4. You will be redirected. If you are not already logged in - log in and authorise. If logged in, just authorise. 

5. Confirm you're not a robot. 

6. Scroll down to ACCESS TOKEN. Copy it to your clipboard. 


LOGGING IN:


1. Run GridGame.exe 

2. You will be shown a login panel - untick "Use Default" (this was a testing feature that used my own credentials for auto-connect)

3. Enter your Twitch Username and Enter your Twitch channel name(the same thing).

4. Enter oauth:youraccesstoken (You must use the prefix oauth:) followed by the access token you copied. 

5. Click connect - the status message should say "starting game". 

6. Regions and capitals have been randomly assigned to teams. 



HOW TO PLAY:

(note - when working with two windows, game and twitch chat - you need to click into the game, making it your active window, for commands to be processed.)

(You, the channel owner, can type commands for any team in twitch chat)

The goal of grid game is to attempt to take your opponents capitals. If you control more than one capital for three consecutive turns, you win. 


Joining phase:

1. Type !join into twitch chat to join a team 

2. Any other audience member in chat can type !join and they will be added to a team. (test adding fake players by typing !join FakeUsername)

3. Type !start to start the game and enter the placement phase.

 

Placing phase: 

1. The command format for placing troops is !place <regionname> <numberoftroops> <team> (e.g. !place devon 21 red)

3. Place your troops strategically to defend your capital, or place them in striking range of an opponents. 

2. Once a team places all their troops, the game will go to the next team. (for testing, place all troops for teams yourself, remember to type the team name at the end of command) !place devon 21 red

Next turn will be called when all troops are placed, and once finished, the game will automatically start the main loop. 

Core loop:

(Teams can End their turn at any time during the loop with the !endturn command)

1. To attack, choose a target adjacent to one of your owned cells 

2. Type the command !attack <fromRegion> <toRegion> <team> (e.g. !attack dorset devon red)

3. The game will roll dice to simulate battles between troops. 

4. On a victory, one of your troops will move into the defeated territory. 

5. To move troops manually during your turn, type !move <fromRegion> <toRegion> <team> (e.g. !move dorset Hampshire red)

6. If a cell is empty and owned by enemy, ownership will automatically transfer. If it's not, you need to attack, not move. 

Winning: 

1. To win, take over an enemy capital and defend it, and your own, for 3 turns. 

2. The scrolling UI will let you know how many turns you have controlled more than one capital. 

(control count regrettably does nothing in this prototype, as victory conditions had to be simplified.)




That's it, have fun!





 




