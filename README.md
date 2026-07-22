
# ARtificial Intelligence: The AR version of GPTAvatars

## Introduction

This is an AR version of Dr. Max Fink's Web based application GPTAvatar inspired by SethRobinson's GPTAvatar:
https://github.com/SethRobinson/GPTAvatar
This version works on VR meta quest headsets or those that can run android apks.
Please find our lates release at our <a href="https://github.com/jmillman0128/GPTAvatarWebGL-main-New/releases"> releases tab </a>

License:  BSD style attribution, see [LICENSE.md](LICENSE.md)

This is a technology test that uses APIs from OpenAI, ElevenLabs, and Google to allow a 3D AI character to converse with using a microphone.

It includes a number of "scenarios" including:

 * Japanese teacher - Atsuko sensei can teach any level Japanese.  She can create quizes or roleplay situations, like working at a store or whatever.
 * Seth - It's me!  You can talk to me.  Do not trust anything I say.  Unfortunately I can't share the custom voice I trained using ElevenLabs, so it's using a default one.
 * Big Burger - Order your food from the rudest fast food employee in the universe


<a href="https://www.youtube.com/watch?v=2sriENjy-x8"><img align="top" src="Misc/teacher_thumb.png" width=300></a>
<a href="https://www.youtube.com/watch?v=J3aGM1yA6O4"><img align="top" src="Misc/seth_thumb.png" width=300></a>



## Running it
To run this app you will need to have a few things
   <ol>
      <li>API keys for: <a href="https://platform.openai.com/home">OpenAI</a>, <a href="https://console.cloud.google.com">Google Cloud TTS</a>, and <a href="https://elevenlabs.io/app/speech-synthesis/text-to-speech">ElevenLabs</a>.</li>
      <li>A pc/laptop with <a href= "https://sidequestvr.com/download">sidequest</a> installed.</li>
   </ol>

 Once you have these items, you have nearly everything necessary to get the app running. Just follow the steps below:

 <ol>
    <li>Download our release to the device that has the sidequest app and connect your headset.</li>
    <li>Open the application, you will be met with a prompt to visit our <a href="https://jmillman0128.github.io/ARtificial-Intelligence-Landing-Page/">website</a>, go to the activate tab and enter the six digit code into the form.</li>
    <li>Below your code, enter your API keys in the order requested on the form.</li>
    <li>Submit the form, it may take a few attempts or seconds for the server to wake up and send the request.</li>
    <li>Enter your user ID provided by the researcher, or just "00000" if not available.</li>
    <li>Select an experience from <a href="https://ai-avatars.net/avatarconfigurations/">the avatar library</a>, click on the details link, and enter the id number that appears at the end of the url.</li>
 </ol>

 WARNING: These APIs cost real money to use, so watch out.  The ElevenLabs voices are probably the most pricey thing of all (but damn they sound real!), so consider switching to using Google's TTS instead to save money, just edit the config.txt for that character. The "teacher" is already set to use Google as Elevenlabs can't do Japanese.


---

Credits and links
- Modified from Dr. Max Fink's GPTAvatars: <a "href="https://ai-avatars.net/">project website</a>.

- Written by Seth A. Robinson (seth@rtsoft.com) twitter: @rtsoft - [Codedojo](https://www.codedojo.com), Seth's blog

Note:  The license only applies to my source code, for sound/graphics, uh, it might be complicated so don't count on being able to use any of that in a real product.

Engine: Unity

Listening: Whisper (via OpenAI's API)

Thinking: ChatGTP (via OpenAI's API)

Talking: ElevenLabs' Voicelab (trained on my voice)

3D Model: Reallusion's Headshot (model faces created using my picture and AI)

Lipsync: SALSA Lipsync Suite
