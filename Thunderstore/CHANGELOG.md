# Changelog

## 0.18.3 [Alpha] - 2024-10-27
### Hotfixed
- Fix v66 joining bug "... while syncing objects! The file may be corrupted.", thanks @Timofey1337png on Github, (issue [#52](https://github.com/Szumi57/LethalInternship/issues/52)).
- Fix radar view on computer while a radar booster is active, thanks @Zeta on Discord.

## 0.18.2 [Alpha] - 2024-10-20
### Added
- Play sound when buying, or error page in the terminal for the internship program.
### Changed
- Interns teleport more to try to follow player. They teleport if they see the player, are too far from him and no players can see
the intern teleporting or the destination where the intern will teleport.
- Rework the teleporters, the interns will be inverse teleported randomly like players (unless they are grabbed by a player)
(issues [#39](https://github.com/Szumi57/LethalInternship/issues/39) and [#44](https://github.com/Szumi57/LethalInternship/issues/44)).
- Removed the config for making the interns follow or not the inverse teleported player. See above point.
- Added config for monitoring interns on the radar ship computer, default value to false. Can now teleport interns to the ship like players.
### Fixed
- Fix items invisible in interns hands after entering/leaving dungeon with Cullfactory.
- Fix interns not being able to see the player in ship if doors are closed.
- Fix tooltip for grenade like items showing to the player if an intern grab one.

## 0.18.1 [Alpha] - 2024-10-11
### Added
- Added config to make interns use random suits when spawning. Changed the config to be 3 choices : 0: Change manually | 1: Automatically change with the same suit as player | 2: Random available suit when the intern spawn.
### Changed
- Interns can emote while moving (if player moves while emoting with TooManyEmotes option)
### Fixed
- Fix item to be not grabbable when in shopping cart/wheelbarrow, thanks @AdamHarney on Github, (issue [#46](https://github.com/Szumi57/LethalInternship/issues/46)).
- Fix interns not being able to enter cruiser, thanks @jakeisloud on Discord.
- Small fixes for health and critical state calculation on interns.
- Fix Model replacement on interns not working with late joining mods (Late company, lobby control), thanks @AdamHarney on Github and @Wizardpie on Discord.
- Lot of small speculative fixes for 5th player bug losing control of camera when an intern spawn. Sorry I can not reproduce it, I will keep trying, so do not hesitate do send me logOuput AND modpack code, it helps a lot.

## 0.18.0 [Alpha] - 2024-10-04
### Added
- Compatibility with Model replacement API , More suits, change the suits of interns with X (default, configurable).
- Added a config to automatically change suits of intern when they get affected to you, default to false.
- Compatibility with TooManyEmotes, BetterEmotes (MoreEmotes too but mod deprecated).
- Interns mimic the emote you are doing when they are in a chill state (next to you not moving)
- Added a new order : use C (default, configurable) to make the interns look at what you are looking. Example of use with betterEmotes: You and the interns are applauding, and everyone is looking at a specific player.
- Added config for ignoring Wheelbarrow item (mod), ShoppingCart item (mod), default to true.
### Changed
- The minimum size of interns configurable is now 0.3 (from 0.85).
### Fixed
- Fix interns holding a item weirdly, now the item position should follow the hand accurately.
- Rework the code for giving item to interns, interns grabbing item, interns dropping item. Should work better when dropping items in ship.
- Fix input duplication when leaving the game to main menu and returning.
- Fix compatibility with QuickBuyMenu mod, thanks @Tye-wynd on Discord.
- Fix other player not owning interns unable to damage intern.
- Fix better compatibility with reserved item slot when giving/taking items from interns.

## 0.17.0 [Alpha] - 2024-09-05
### Added
- Lethal Company v64
- InputUtils, change the keybinds for interacting with interns how you want.
- Added config option for intern to grab or not the maneater as a baby, default to false, thanks @ShadowWolf90 on GitHub, (issue [#40](https://github.com/Szumi57/LethalInternship/issues/40)).
- Added config option to spectate or not interns.
- Inverse teleporters works with interns, if they are close to the teleporters or grabbed by player, request from @AtomicJuno on GitHub, (issue [#39](https://github.com/Szumi57/LethalInternship/issues/39)).
- Same with regular teleporters, only the grabbed interns will be bring back to the ship with the player.
### Changed
- Grab and release intern keybinds are now separate, default 'Q' for grab, 'R' for release.
- Debug config simpler with one debug option.
### Fixed
- Fix hard and soft dependencies completely forgotten in the plugin setup... hope compatibility fixes gets better for people now.
- Attempt to fix the 4th player or 5th player (with moreCompany) bug with intern taking control and all that. Really not sure it's fixed, need further testing.
- Fix some more movements for interns, they only teleport when no one is looking at them now.
- Complete rewrite of the behaviour of grabbable intern body, should work better, fixes issue 42, thanks @ShadowWolf90 on GitHub (issue [#42](https://github.com/Szumi57/LethalInternship/issues/42))
- Fix size of intern bodies when dead or grabbed by player.
- Fix dead interns appearing on the dead player spectator UI.
- Fix some more bug with lethalPhones et ReviveCompany.
- Fix picking up and release intern if their weight gets set to 0 with other mods, thanks @ShadowWolf90 on GitHub, (issue [#41](https://github.com/Szumi57/LethalInternship/issues/41))

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
- Fix menu "Lethal Internship" duplicating in the terminal help menu, thanks @Ryyye on GitHub, (issue [#29](https://github.com/Szumi57/LethalInternship/issues/29))
- Fix compatibility with ReviveCompany, you can revive intern now with this mod
- Fix interns keeping items in hand for next round at the end of round, if items are not in the ship

## 0.15.2 [Alpha] - 2024-08-21
### Added
- Added config for internship program title in help menu, be careful, the title will become the command to type in the terminal to access the internship program
### Changed
- Some strings in the terminal, suggestion from @Ryyye on GitHub
### Fixed
- Fix doors not working, looked like a v62 compil problem with the mod, thanks @jakeisloud on Discord, @Ryyye on GitHub, (issue [#24](https://github.com/Szumi57/LethalInternship/issues/24))

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
- Forest giant not targeting players, was actually a conflict with more company, thanks @jakeisloud discord, @doubletime32 on Github (issue [#17](https://github.com/Szumi57/LethalInternship/issues/17))

## 0.14.8 [Alpha] - 2024-08-10
### Fixed
- Turret not damaging players. Thanks @Kimiko on Discord, Imiquel on Github, (issue [#21](https://github.com/Szumi57/LethalInternship/issues/21))
- Conflicts with AdditionalNetworking and ShowCapacity, thanks @wwww on Discord

## 0.14.7 [Alpha] - 2024-08-09
### Changed
- Interns ignore bee nest and dead bodies, configurable on next update.
### Fixed
- Attempt to fix compatibility error type of "[Error  : Unity Log] NetworkPrefab ('prefabName') has a duplicate GlobalObjectIdHash source entry value of: 'value'!", thanks @Adrian on discord.
- Fix for Jester and ForestGiant (can not reproduce for ForestGiant) not targetting player, thanks @Adrian on discord. thanks @doubletime32 on Github, (issue [#17](https://github.com/Szumi57/LethalInternship/issues/17))
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
- Fix terminal commands from base game pages, able to type 'c' to confirm, (issue [#12](https://github.com/Szumi57/LethalInternship/issues/12))

## 0.14.5 [Alpha] - 2024-08-03
### Hotfix
- Fix compatibility with MoreEmotes and ModelReplacementAPI, thanks @Nysvaa on discord. Fixed but not functionnal for interns, no change (I hope) for the players. 

## 0.14.4 [Alpha] - 2024-08-02
### Hotfix
- Fix for "Typing Pro flashlight into the terminal forces you to the interns page preventing purchases of the pro", thanks @Loot Bug on discord.

## 0.14.3 [Alpha] - 2024-08-01
### Hotfix
- Fix for giving item to intern and it floats, (issue [#4](https://github.com/Szumi57/LethalInternship/issues/4))

## 0.14.2 [Alpha] - 2024-08-01
### Hotfix
- Possible fix for compatibility with BepInExFasterLoadAssetBundlesPatcher (issue [#5](https://github.com/Szumi57/LethalInternship/issues/5))

## 0.14.1 [Alpha] - 2024-08-01
### Hotfix
- Fix compatibility with BetterEXP (issue [#2](https://github.com/Szumi57/LethalInternship/issues/2))

## 0.14.0 [Alpha] - 2024-07-31
- Initial release