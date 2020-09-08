<a href="https://www.linkedin.com/in/egilsandfeld/"><img src="https://avatars2.githubusercontent.com/u/5029330?s=400&u=4614d06f7a8ebcd31858b0d529649ea99b91d2e9&v=4" title="Egil Sandfeld Code Showcase" alt="Egil Sandfeld Code Showcase"></a>

# Egil Sandfeld Code Showcase





**In this repo**

- Code examples of my previous work
- Sweet gifs to visually show what's up 🌈
- You will discover "everyday" code here from time to time, that have been exposed to intensive, last-minute changes and hacky approaches, due to time/resource contraints.
- Feel free to reach out to me via <a href="https://www.linkedin.com/in/egilsandfeld/" target="_blank">LinkedIn</a> for more answers

&nbsp;
&nbsp;
&nbsp;

**Go to**
- [Coding style](#My-coding-style)
- [Coding competencies](#Coding-competencies)
- [Unity knowledge](#Unity-skills)
- [Unity editor work](#Unity-editor-work)
- [Stargazer](#Stargazer)
- [Somnia](#Somnia)
- [Contact](#Contact)

&nbsp;
&nbsp;
&nbsp;

# Coding style

- I generally avoid commenting code to prevent redundant information. I do tend to explain code parts that are complicated (while I know those parts should be broken up)
- I do like making whitespace and regions in code
- I do like making human-readable debug logs in code for internal use, so the team members not familiar with the code can easier report issues
- I'm very interested in utilizing <a href="https://adamramberg.github.io/unity-atoms/" target="_blank">Unity Atoms</a> further, that utilizes scriptable objects for complete modularity, but I haven't had the chance yet
- I'm continuously interested in improving myself and optimizing code for modularity

&nbsp;
&nbsp;
&nbsp;

# Coding competencies

- C# .NET & Unity ⭐⭐⭐⭐⭐
- WPF ⭐⭐
- Javascript ⭐⭐
- HTML, CSS ⭐⭐
- Java for native Android apps ⭐
- Objective C for iOS ⭐
- Firebase functions and database rules ⭐
- Wwise: 5+ years
- VS Studio + Code

&nbsp;
&nbsp;
&nbsp;

# Unity skills

5+ years Unity experience

Extensive experience with Unity Packages:

- Addressables
- Advertisements
- Analytics
- Android Logcat
- AR Foundation
- AR Core XR Plugin
- ARKit XR Plugin
- Audio Mixer
- Device Simulator
- GameTune
- Google VR
- In App Purchasing
- Memory Profiler
- Mobile Notifications
- Oculus VR
- ProBuilder
- Profiler
- Recorder
- Scriptable Build Pipeline
- Shader Graph
- TextMeshPro
- Timeline
- Video

&nbsp;
&nbsp;
&nbsp;

# Unity editor work
I Have extended Unity editor several times with custom inspectors, scripts, tools:

**DLC <-> CDN Upload Manager from Unity to Firebase Storage**<br>
![DLC CDN Editor](https://github.com/EgilSandfeld/development-showcase/blob/master/unity/Somnia-DLC-Editor.gif?raw=true)



&nbsp;

**Sound editor to create new sounds for Somnia**<br>
![Sound Editor](https://github.com/EgilSandfeld/development-showcase/blob/master/unity/Somnia-Aubit-Editor.gif?raw=true)


&nbsp;

**Sound Live Config Editor for Somnia for tweaking sounds to sound natural**<br>
![Sound Live Config Editor](https://github.com/EgilSandfeld/development-showcase/blob/master/unity/Somnia-Aubit-Live-Config.gif?raw=true)


&nbsp;

**Firebase database app version editor, to warn users of new app versions when starting the app**<br>
![Version Editor](https://github.com/EgilSandfeld/development-showcase/blob/master/unity/Somnia-Version-Editor.gif?raw=true)




&nbsp;
&nbsp;
&nbsp;




---




# Stargazer
Beat Saber meets the neck - A VR neck training game

[![Stargazer](https://github.com/EgilSandfeld/development-showcase/blob/master/stargazer/stargazer-short.gif?raw=true)](https://www.youtube.com/watch?v=MsluAHI6XTY)
<a href="https://www.youtube.com/watch?v=MsluAHI6XTY" target="_blank">See the demo video for Stargazer</a>

&nbsp;

Responsibilities: Concepts, design, development frontend & backend, testing

Stargazer is a music VR neck-training tool to alleviate mild neck strains and pain. Paint and draw star constellations on the VR night sky in rhythm to the music. The system makes sure to help to alleviate the pain in certain neck stretches. Get feedback on your exercise while playing and afterwards with statistics. The project reached the prototype stages culminating in a showcase tour to Tallinn as well as requests from physiotherapists to use the tool.

<a href="https://github.com/EgilSandfeld/development-showcase/tree/master/stargazer" target="_blank">The code examples</a> show parts of the music integration with Wwise as well as VR input handling


&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/stargazer/MusicSystem.cs" target="_blank">MusicSystem.cs</a>**
    - Creates a mutual relationship with Wwise music engine, keeping track of music time and various temporal triggers following the music. Music is arranged in segments that have different and unique rhythm patterns for the player to connect stars in sync with. The MusicSystem enables visual and auditive triggers based on the music. Handles de-sync between Wwise and Unity code: Takes desyncing into account and attempts to counterbalance the measured beat desync (delay) of when each beat was supposed to happen

&nbsp;
&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/stargazer/InputSystem.cs" target="_blank">InputSystem.cs</a>**
    - VR, controller inputs handling. Captures all inputs and forward them using Action. Code based on 2018 Oculus Go VR integration with Unity

&nbsp;
&nbsp;






# Somnia
ASMR for your sleep

[![Stargazer](https://github.com/EgilSandfeld/development-showcase/blob/master/somnia/somnia-trailer.gif?raw=true)](https://somnia.app/)


&nbsp;

I developed <a href="https://somnia.app/" target="_blank">the app</a> released for iOS and Android

Fall asleep faster with customizable ASMR sounds. Mobile app that plays no-looping, high-quality ASMR sounds. Somnia uses Spatial 3D Audio for dynamic binaural audio for an intimate and relaxing experience. The 3D sounds evolve over time like small stories, creating a believable and immersive experience that makes users escape reality.

<a href="https://github.com/EgilSandfeld/development-showcase/tree/master/somnia" target="_blank">The code examples</a> show parts of the audio integration with Wwise, scriptable object handling and optimizing physics


&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/somnia/WwiseBankController.cs" target="_blank">WwiseBankController.cs</a>**
    - Loads and unloads Wwise soundbanks, optimizes loading when multiple sound objects request load of a soundbank. It's Possible to quickly "Log" what's happening from Unity editors

&nbsp;
&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/somnia/AubitCollisionController.cs" target="_blank">AubitCollisionController.cs</a>**
    - Class to handle collision checks between aubits on the 3D Space and react. Registers objects that need to be checked for colliding. Does a check on normalized screen distance between objects, and trigger a collision when two objects are close. This script removes the needs for 2D physics module in Unity, thus saving build size from stripping the module

&nbsp;
&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/somnia/Matches.cs" target="_blank">Matches.cs</a>**
    - This script holds and manipulates data of user profile matching with app content like sounds and audio trigger types. The scriptable objects made from this are fully serializable using JSON, and are loaded/unloaded using the BaseSO class. The script utilizes <see href="https://github.com/dbrizov/NaughtyAttributes">NaughtyAttributes</see> for quick Unity inspector prettyness. The class is flexible to changes in content, and allows for refreshed to the match values when requested by other classes. The scriptable object makes sure to only hold data and do internal data changes fed with information from outside the class

&nbsp;
&nbsp;

- **<a href="https://github.com/EgilSandfeld/development-showcase/blob/master/somnia/BaseSO.cs" target="_blank">BaseSO.cs</a>**
    - Foundational scriptable object serialization system that takes care of serializing to and from persistent data folder. Persistence may be disabled using !Persist for Unity authored files only. Class is abstract to require extensibility

&nbsp;
&nbsp;


# Contact

[![Check my LinkedIn](https://media.giphy.com/media/Is1O1TWV0LEJi/giphy.gif)](https://www.linkedin.com/in/egilsandfeld/)<br>
Thanks for reading 💪
Feel free to reach out to me on <a href="https://www.linkedin.com/in/egilsandfeld/" target="_blank">LinkedIn</a>