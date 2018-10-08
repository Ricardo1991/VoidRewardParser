# Void Reward Parser
Reads the Void Rewards screen of Warframe and displays Ducat values

## How to use:

Open in background with Warframe open.
As long as Warframe is running it will scan the primary monitor for primed parts.
If detected it will read out any prime parts and display the rarity and Ducat value in a list.


## Requirements:

* .Net Framework 4.6.2+

* C++ Redistributable

* Warframe in *Borderless Windowed* or *Windowed* mode

-----

## This fork:

* Improvements to the prime part recognition mechanism. 
    * Compared to the main repository, this one will only analize the area of the screen with warframe on it, saving a bit of work.
    * Converts the captured screen to black and white, so the OCR has a better time detecting text. Might or might not actually improve it.
    * Uses a custom spellcheck, optimized for warframe, so that it can fix those times the OCR reads "BLUEPRTNT" instead of "BLUEPRINT" for example.
    * It wont check the screen if the warframe process is not on focus. This can be disable to make it check always.
    * Finished the compatibility for non-Windows 10 versions
    * Added an optional in-game overlay so you dont have to tab to this program to check ducat and plat values.
    * Many other bug fixes and added options

-----

## Supported Languages (not being maintained or tested by me, might be broken now):

English, Russian, Portuguese, German

To change languages open the VoidRewardParser.exe.config file in a text editor and change this line to the selected language.

    <add key="Language" value="English"/>

If your Windows default language does not match the language you use in the game, you may also need to change this line to include the language code you run the game as (such as "en", "pt", "ru", "de"). Note that this language must be installed in your Windows 10 region and laguage settings.

    <add key="LanguageCode" value="en"/>

If your language isn't currently supported, check out the [new localization readme](https://github.com/Ricardo1991/VoidRewardParser/tree/master/VoidRewardParser/Localization), and send me the file so I can include it.
