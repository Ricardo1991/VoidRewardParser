# Void Reward Parser
Parses the Void rewards screen for Warframe and displays ducat Values

## To use:

Open in background with Warframe open.    
As long as Warframe is open it will scan the primary monitor for primed parts.
If detected it will read out any prime parts and display the rarity and Ducat value in a list.

**Requires Warframe to run in *Borderless Windowed* or *Windowed* mode, does not work fullscreen.**

## Requirements:

* Windows 10

* .Net Framework 4.6.1+

-----

## This fork:

* Improvements to the prime part recognition mechanism. 
    * Compared to the main repository, this one will only analize the area of the screen with warframe on it. 
    * This saves resources if you are playing on a **Windowed** instead of **Borderless Fullscreen**.
    * It also converts the captured screen to black and white, so the OCR has a better time detecting text.
    * Uses a custom spellcheck, optimized for warframe lingo, so that it can fix those times the OCR reads "BLUEPRTNT" instead of "BLUEPRINT", or "RECEWER" instead of "RECEIVER".

* It also wont check the screen if the warframe process is not on focus. 

This can be changed by changing   

    <add key="SkipIfNotFocus" value="true" />

to

    <add key="SkipIfNotFocus" value="false" />
    
on VoidRewardParser.exe.config.

-----

## Supported Languages (not being maintained by me):

English, Russian, Portuguese, German

To change languages open the VoidRewardParser.exe.config file in a text editor and change this line to the selected language.

    <add key="Language" value="English"/>

If your Windows default language does not match the language you use in the game, you may also need to change this line to include the language code you run the game as (such as "en", "pt", "ru", "de"). Note that this language must be installed in your Windows 10 region and laguage settings.

    <add key="LanguageCode" value="en"/>

If your language isn't currently supported, check out the [new localization readme](https://github.com/Xeio/VoidRewardParser/tree/master/VoidRewardParser/Localization), and send me the file so I can include it.
