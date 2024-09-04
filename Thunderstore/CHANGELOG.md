# Changelog

## 0.17.0 [Alpha] - 2024-08-
### Added
- InputUtils, change the keybinds for your intern how you want.
### Fixed
- Fix hard and soft dependencies completely forgotten in the plugin setup... hope compatibility fixes gets better for people now.
- Attempt to fix the 4th player or 5th player (with moreCompany) bug with intern taking control and all that. Really not sure it's fixed, need further testing.
- Fix some more movements for interns, they only teleport when no one is looking at them now.
- Complete rewrite of the behaviour of grabbable intern body, should work better, fixes issue 42, thanks @ShadowWolf90 on GitHub ([#42](https://github.com/Szumi57/LethalInternship/issues/42))
- Fix some more bug with lethalPhones et ReviveCompany.

- , thanks @ on Discord

## 0.16.0 [Alpha] - 2024-08-30
### Added
- Grab interns ! You can grab multiple interns with you but be carefull : their weight and their held item weight gets added to your weight. You may not be able to move after...
- Indicator of state in the name of the intern, an attempt at giving intern more personnality and information.
### Changed
- Removed "Following you" tooltip because of the new indicator in the billboard name above interns.
### Fixed
- Fix some v60 conflict, the maneater should be working with interns, for the mineshaft elevator, for now you can grab interns with you, I will look for a fix so they come with you in the elevator on next update
- Fix some movements and behaviours, the state of looking for players should be working, and the intern should less teleport when losing player.
- Fix some more conflicts with lethalphones, especially when an intern dies
- Fix major bug when a 4th player join lobby (taking control and other weird things), thanks @Dyz89 on Discord
- Fix menu "Lethal Internship" duplicating in the terminal help menu, thanks @Ryyye on GitHub, issue 29 ([#29](https://github.com/Szumi57/LethalInternship/issues/29))
- Fix compatibility with ReviveCompany, you can revive intern now with this mod
- Fix interns keeping items in hand for next round at the end of round, if items are not in the ship

## 0.15.2 [Alpha] - 2024-08-21
### Added
- Added config for internship program title in help menu, be careful, the title will become the command to type in the terminal to access the internship program
### Changed
- Some strings in the terminal, suggestion from @Ryyye on GitHub
### Fixed
- Fix doors not working, looked like a v62 compil problem with the mod, thanks @jakeisloud on Discord, @Ryyye on GitHub, issue 24 ([#24](https://github.com/Szumi57/LethalInternship/issues/24))

## 0.15.1 [Alpha] - 2024-08-20
### Fixed
- Fix softlock at end of game with a client (not host), thanks @Instaplayer on Discord

## 0.15.0 [Alpha] - 2024-08-19
### Added
- Added a landing option for intern, should the dropship land intern on moons or keep them for later ? Type 'land' or 'abort' to change the option.
- Configurable parameters for the internship program. You can now configure :
	- The maximum of interns available to buy, from 1 to 32, default to 16
	- The price of one intern, from 0 to 200, default to 19
	- The maximum health of an intern, from 1 to 200, default to 51
	- The size of an intern, from 85% to 100% of player size, default to 85%
	- The names of the interns, choose between de default names "Intern #(number)", a list of random names of my own (mostly reddit actually),
	or your own list of names.	
	</br></br>
    You can configure the behaviour of an intern too :
	- Should the intern just teleport when using ladders ? default to false
	- Should the intern grab items near entrances (main and fire exits) ? default to true
	- Should the intern grab bees nets ? default to false
	- Should the intern grab dead bodies ? default to false
	</br></br>
	All of those configs are synced with the host, that means that whatever config you have, if you are a client you will use the host's config.
	Not for the log debug stuff though, that's still client side.
### Changed
- Raised default hp to 51, thanks @Ogryn named Finger, on discord for the suggestion
- Spawning from the dropship should be smoother
### Fixed
- SpringManAI patch not working (v60 compatibility)
- Forest giant not targeting players, was actually a conflict with more company, thanks @jakeisloud discord, @doubletime32 on Github issue 17 ([#17](https://github.com/Szumi57/LethalInternship/issues/17))

## 0.14.8 [Alpha] - 2024-08-10
### Fixed
- Turret not damaging players. Thanks @Kimiko on Discord, Imiquel on Github, issue 21 ([#21](https://github.com/Szumi57/LethalInternship/issues/21))
- Conflicts with AdditionalNetworking and ShowCapacity, thanks @wwww on Discord

## 0.14.7 [Alpha] - 2024-08-09
### Changed
- Interns ignore bee nest and dead bodies, configurable on next update.
### Fixed
- Attempt to fix compatibility error type of "[Error  : Unity Log] NetworkPrefab ('prefabName') has a duplicate GlobalObjectIdHash source entry value of: 'value'!", thanks @Adrian on discord.
- Fix for Jester and ForestGiant (can not reproduce for ForestGiant) not targetting player, thanks @Adrian on discord. thanks @doubletime32 on Github, issue 17 ([#17](https://github.com/Szumi57/LethalInternship/issues/17))
- Fix for compatibility with mod FasterItemDropship, thanks me.
- Fix for old bird missiles pushing interns, also errors between old bird and interns (CheckSightForThreat stuff), thanks @Autumnis the Everchanging on Discord

## 0.14.6 [Alpha] - 2024-08-05
### Changed
- Interns are faster on ladders.
- Interns can use ladders even with a two handed item in hands.
### Hotfix
- Fix compatibility with LethalPhones, thanks @crump_laude on discord. Fixed but not functionnal for interns, no change (I hope) for the players. 
- Fix Masked unable to grab interns, thanks @Autumnis the Everchanging on discord.
- Fix bracken unable to grab interns, thanks @random_axolotl on discord.
- Fix terminal commands from base game pages, able to type 'c' to confirm, issue 12 ([#12](https://github.com/Szumi57/LethalInternship/issues/12))

## 0.14.5 [Alpha] - 2024-08-03
### Hotfix
- Fix compatibility with MoreEmotes and ModelReplacementAPI, thanks @Nysvaa on discord. Fixed but not functionnal for interns, no change (I hope) for the players. 

## 0.14.4 [Alpha] - 2024-08-02
### Hotfix
- Fix for "Typing Pro flashlight into the terminal forces you to the interns page preventing purchases of the pro", thanks @Loot Bug on discord.

## 0.14.3 [Alpha] - 2024-08-01
### Hotfix
- Fix for giving item to intern and it floats, issue 2 ([#4](https://github.com/Szumi57/LethalInternship/issues/4))

## 0.14.2 [Alpha] - 2024-08-01
### Hotfix
- Possible fix for compatibility with BepInExFasterLoadAssetBundlesPatcher ([#5](https://github.com/Szumi57/LethalInternship/issues/5))

## 0.14.1 [Alpha] - 2024-08-01
### Hotfix
- Fix compatibility with BetterEXP ([#2](https://github.com/Szumi57/LethalInternship/issues/2))

## 0.14.0 [Alpha] - 2024-07-31
- Initial release