# Warning!

As of version 1.2.2:
To use this App, you will need a (paid) authorization key for a third-party hashing service.
Such a key can be obtained on the following page: https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer
A 150 RPM key will be sufficient for this app.

Explanation: Every request that is sent to Niantic's game servers, must be signed by a hash value, calculated from various variables.
Before version 1.2.2, this hashing could be done by the app itself, but the algorithms were changed by Niantic and the crack is not publically available.
So far, the Pokefarmer team is the only one that can provide valid hashes. By using their hashing service - it needs an authorization key - the app is able to sign the requests correctly.

The Game is Working but with some small issues:
- The app is not 100% safe from bans. This means your Trainer account might be banned when you use it. 
I am trying to make a legit-like app, without cheats, but by using this app, you might be discovered and your account can be banned.

# Important information
You can find the installation file for PoGo in [Releases](https://github.com/mtaheij/PoGo-UWP/releases/). **Please read the whole release information before installing.**

# PoGo for Windows 10

Check [Wiki](https://github.com/mtaheij/PoGo-UWP/wiki) for information about the project, installing instructions and more in different language.

# Social (Not owned by me)

[Reddit](https://www.reddit.com/r/PoGoUWP/)

There is a reddit made to discuss about PoGo. Make sure to follow the subreddit rules and you're good to go.

There are 3 social chat groups to make sure everyone can use their favorite service to disscuss. Feel free to join any of them, or all at once. You're not limited to just one.

[Skype](https://join.skype.com/hOeCHq2oEyhA)

The skype group is used for Text/Voice chat and general disscussion, as well as support.

[Telegram](https://telegram.me/PoGoUWP)

For telegram users, there is also a Telegram superchat that is also for general disscussion, ideas and support.

[Discord](https://discord.gg/4GMbEWH)

The Discord is used mostly for live voice chat and is about the same subjects above groups are.

# Questions & Answers

Q: What is PoGo?

A: PoGo is an UWP (Universal Windows Platform) client for Niantic's Pokemon™ Go Android/iOS game. Being a client, this means that it gives you the ability to play in the same game-world as your friends that are playing with an Android or iOS device.

Q: Why PoGo?

A: Because there is no official client for Windows.

Q: Will this app feature 3D graphics and AR?

A: No, for both of them, it just takes too much work. If you feel that you could do this, clone the repo, add the changes and submit a pull request.

Q: Will this work on Windows Phone 8.1?

A: Not officialy. This is an open-source project, so people might fork it and port it to Windows Phone 8.1 later on.

Q: Can I play with the same account that I'm using on my Android/iOS device?

A: Yes, but **not at the same time or you may end up having a duplicated account** so, please, logout from the Android app before logging in PoGo.

Q: Will it run on low-end devices?

A: Yes, but the performance may not be perfect.

Q: Why is the Device portal returning error 0x80073cf6?

A: Change storage settings from SD card to phone storage. More infos [here](github.com/ST-Apps/PoGo-UWP/issues/11)
If you already had the app installed, probably a reboot is required.

Q: How can I logout?

A: Press the Pokeball, go to Settings in the top right corner, scroll all the way down in the settings page and hit the "LOGOUT" button in the bottom right corner.

# Changelog

**Changelog is available in [Releases](https://github.com/mtaheij/PoGo-UWP/releases/).**

# Download

Download the latest official release [here](https://github.com/mtaheij/PoGo-UWP/releases)
