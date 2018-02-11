# Frequently Asked Questions

### What is Voxalia?

- Voxalia is a game about blocks and such! It'll be a lot of fun to play when it's complete!

### No, what is Voxalia *really*?

- A drunken misreading of the Minecraft Wiki.

### What can you do in Voxalia?

- When it's done, you'll be able to do a lot of things, such as:
	- Build things with blocks.
	- Destroy things made of blocks.
	- Make things with complex object types.
	- Play online with friends or random people.
	- Play through an interesting story line.
	- Mod the game freely through FreneticScript and C#.
	- And much more!

### What's the goal in Voxalia?

- Whatever you want it to be! You can go for:
	- The biggest / strongest base!
	- The coolest build!
	- The strongest character gear!
	- Completion of all the available story lines!
	- Or so many other possibilities...

### Can we mod it?

- Yup! You can use C# or FreneticScript to customize the game to any degree.
- You can even modify the game's base code, it's all open source!

### Did you say "story lines"?

- Yes! The game will have a primary story line to play through, and potentially more.
- But also, the game is open to modding - even the official storyline will be made in mod code to ensure the modding system is capable!

### How far along is the game?

- It's still in early dev! We haven't even reached what we'd consider alpha yet.

### Where can I get Voxalia?

- Talk to us! Check the contact page on the FreneticLLC official website, and join us on IRC or Discord to get involved!
- At this time, assets are being kept private to only people who've spoken directly with us about trying it out.
- In the future, you'll be able to get the game without having to go through people, we have it this way only in early development.

### How do I get started once I have the game?

- Once you have a valid FreneticLLC account and a copy of the game:
- Open the Launcher (bin/launcher.bat on Windows or bin/launcher\_linux.sh on Linux) and login.
- Next, hit the "Play" button.
- Almost there! Click "Singleplayer" in the main menu.
- Finally, click the "default" button (or any other game name).
- The game should load up now.
- There's not all the much to do yet. Those trying it should be familiar enough with the code to be able to decipher the keybindings from `ClientGame/UISystem/KeyHandler.cs`!

### Do I need to run the Voxalia server to play with friends?

- No. Your friends can join your singleplayer game as well. (There may be additional steps in the future to open a singleplayer game fully.)

### What ports do I need to forward to run a server?

- By default, Voxalia singleplayer and server open up on port 28010. You can edit this in your launch command options (first argument is always the port).
- Note that port forwarding is partially automated by the internals and it generally isn't required that you manually forward ports.

### How does Voxalia compare to similar games?

- We've got better graphics than most (optionally, there's also a less pretty 'fast' render mode).
- We use better/more-modern tech than most (OpenGL 4.3 for example, and C# as the primary language).
- We have more complex/fun survival situations than most:
	- You not only can build bases, you can fully interact with the terrain and build a base even where there was ground before (Similar to Minecraft).
	- You can do really cool and fancy things, like place automated powered turrets (Similar to ARK).
	- There's a lot of things to watch out for (enemies of various type, hunger, thirst, etc.) but difficulty sliders are available to make it fun for everyone!
- More moddable than most:
	- We have fully open source backing code!
	- We have a full native C# plugin engine and API.
	- We have a full script engine and API.
- More space than most:
	- Worlds are nearly unlimited in size!
	- Can have multiple worlds in a single game instance.
	- An entire galaxy's worth of servers will be available if plans go well.
- More user friendly than most:
	- Want to open up a server to play with friends? Just run the server program, it does everything for you - even portforwarding (or at least it tries to where available)!
	- Full control over most options, and scripting available where you need something more advanced!

### What is FreneticScript?

- A powerful and cleverly written command/tag syntax script engine, that can be used to mod Voxalia.

### No, what is FreneticScript *really*?

- A drunken misinterpretation of a beginner's tutorial to Visual Basic.

### What is FreneticDataSyntax

- A simple yet powerful method of storing data and configuration values in a human-readable format.

### No, what is FreneticDataSyntax *really*?

- A drunken mistranslation of a YAML specification written in a foreign language.

### What is FreneticLLC?

- Our company! It's a limited liability company owned and operated by our team.

### No, what is FreneticLLC *really*?

- A drunken misapplication of government guidelines.

### Why does the readme have all those badges on it?

- Why don't you?

### What do the little marks next to every commit on GitHub mean?

- Those are the status of the test builds. If it's not a green check mark, something went wrong!

### Can I open an issue or pull request?

- Yes! Just be sure to read the issue template ( https://github.com/FreneticLLC/Voxalia/blob/master/ISSUE_TEMPLATE ) and the contributing doc ( https://github.com/FreneticLLC/Voxalia/blob/master/CONTRIBUTING.md )!

### Can I resell the game or any large portion of it?

- No! That's not cool!

### Can I redistribute the files for the game?

- Only the code and binaries resultant directly from the code (*.exe, *.dll, *.xml, *.pdb) may be redistributed. Art assets (available separately) are licensed separately, and may not be redistributed!

### I want to know more about how the game is licensed!

- Check out the full terms of the license at https://github.com/FreneticLLC/Voxalia/blob/master/LICENSE.txt

### I want to sue you or otherwise engage you in court!

- Please don't. We are not liable for anything bad done with the project. This is just a game, and while we hope nobody does anything bad with it, we simply don't have the technology to prevent it entirely!
- If you have a problem with anything we do or think we can help a problem you have, please do CONTACT US as the first step: https://freneticllc.com/Home/Contact - we're open and friendly, don't worry!

### There's something dangerous or mislicensed or majorly problematic in this repo!

- Oh no! Please CONTACT US IMMEDIATELY at https://freneticllc.com/Home/Contact
- If you find a potentially important issue and need motivation to bother reporting it, feel free to ask for a bounty payment. We're happy to award one to the first finder, with an amount relative to the scale of the issue!
