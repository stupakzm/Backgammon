# Backgammon Game Project


This repository contains the code for my upcoming Nardi (Backgammon) game.


#Project Objective
The primary aim of this project is to offer a versatile Nardi gaming experience that can be enjoyed anytime and anywhere.

#Features
- The game offers possibility to play against botand three modes for play on one device (the functioning of which can be viewed in the game itself, it is implemented using Coroutine and has a short description):
  1. **FreeAspect:** This mode is designed for players who trust each other completely. In this mode, all positions are open, and you can use either your own dice or generate them within the game.
  
  2. **Fixed 6:** Similar to the Trust Mode, but limited to moves of up to 6 positions forward.

  3. **Set Cubes:** This mode follows the official rules for dice setting, allowing players to throw their own dice. It's included for those who prefer manual dice rolling over machine-generated ones, although there's also an option for automated dice generation.

- The game has background music and sounds of chip movement and dice rolling. 
There are also settings with reset functions, the main menu, music volume control, turning off the generator for Fixed6 and FreeAspect modes, and automatic generation for Set Cubes mode and also shows how many steps are left to win for both players.
In the game it is also possible to undo the move if the player accidentally clicked on the wrong position.

#Game Rules - can be viewed both from the main menu and from the settings in the game.

• "A player cannot move a checker to a point that is already occupied by opponent’s checkers."

• "A player cannot move a checker to an invalid point. For example, if a player rolls a 5 and a 3, they cannot move one checker 8 points. Instead, they must move checker 5 points and checker 3 points."

• "A player can only move one checker from the 'head' (starting position) per turn."

• "If a player rolls a pair of dice, for example, 3:3, they need to move 3 points four times."

• "Exception for a 6:6 pair at the first move: they need to move 2 checkers from the 'head' to 6 positions."

• "You can move the checkers 'home' (final section) only after all the checkers are in positions from which you can move 'home' in one move."

• "If a player can play a roll, they must do so even if it puts them at a disadvantage."

• "If a player cannot make a move due to position blocking, the move goes to the opponent."

• "You can't block 6 positions in a row if there are no opponent's checkers after them."

• "A player wins when they are the first to move all the checkers 'home'."

![Home Board Movement Example](https://github.com/stupakzm/Backgammon/blob/main/readme/homeMovement.gif)

- The game takes place within a single scene. After one player wins, you have the option to choose the next game mode to play.

![Win Screen](https://github.com/stupakzm/Backgammon/blob/main/readme/winScreen.gif)


#Future Plans
- Future enhancements include adding correct scaling for about modes view for every device and ability to play with someone on 2 devices via bluetooth or WiFi.
