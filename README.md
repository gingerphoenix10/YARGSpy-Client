# YARGSpyClient
A BepInEx 5 plugin for [Yet Another Rhythm Game (Stable)](https://github.com/YARC-Official/YARG) to communicate with the unofficial [YARGSpy leaderboards](https://github.com/raphaelgoulart/YARGSpy).

> [!WARNING]
>
> This mod is still in testing and may contain bugs and missing / unfinished features. Don't expect a perfect experience

# Installation
- Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/latest) onto your install of YARG Stable
  - Download the ZIP file for your system (`BepInEx_win_x64_x.x.xx.x.zip` for windows), and extract it's contents to your YARG folder. (On my system, the directory is `D:\YARC\YARG Installs\fde5dfa1-c9d3-436f-9aa9-1e931c7cbdc2\installation`. Your path should be similar)
  - The `BepInEx` folder, as well as files such as `doorstop_config.ini` and `winhttp.dll` should be next to the YARG.exe executable
  - More setup may be required on Linux / MacOS. See the [BepInEx docs](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for more info
- Launch YARG once to generate BepInEx's files. Close the game once you get to the main menu
- Download `YARGSpy.dll` (and optionally `YARGSpy.pdb` if you'd like to help with debugging) from the [YARG Discord Server](https://discord.gg/sqpu4R552r)'s `#leaderboard-dev` channel. GitHub releases will be made in the future, however in testing all versions are posted on DIscord
- In YARG's install folder, navigate to the `BepInEx\Plugins` directory which should've been generated after the first launch.
- Move the `YARGSpy.dll` and `YARGSpy.pdb` (if downloaded) files into the Plugins folder
- When you next launch YARG, you should see the logo replaced with the YARGSpy logo. That means the mod sucessfully installed. Log into your YARGSpy account in settings, and you're ready to play