# ShAI_U
Militant Shooter AI (Slightly Advanced) For Unity Alpha v0.76

 ## I'm Still Working On It

 > Before Starting, I'm a noob, so if you find any mistakes or anything odd in the code, kindly let me know, I'm still learning :)
Infact this is my first repositary.


This is a work in progress project, for designing a slightly advanced AI that can be used for any fps game.
Everything is in one single script, as I want this to be easy to use and extremely dynamic.
Right now this rep. only contains this script, but in future I might add everything, so all one has to do is to import the prefab into the scene, adjust some variables
accordingly, and VOILLA!
But if you want, you can use this script yourself, even right now, I've tried to keep everything dynamic.
## Current Features:
 - Wanders Around according to a given array of points.
 - If Surrounded by multiple enemies, It will attack someone closest (Might add some more complexity later).
 - While attacking the enemy, It looks for a suitable cover, and if found proceeds to it.
 - A suitable cover is, which is near the friendly AIs and farther from Enemies, if their are no any other ai's left from it's faction, it will proceed to a cover which is more close to him than the enemy.
 - A seperate gun script is created for the weapon which can also be used by the player.
