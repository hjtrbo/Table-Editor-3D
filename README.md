# Table-Editor-3D
A c# winforms user control 3d map editor designed to accompany tuning software that lack 3d graph support.

Includes an average tool that takes input from multiple data logs and averages an output weighted to the total number of sample points. 

Functions and features are similar to that of HP Tuners I.e. Paste special, Map smoothing, Map interpolation

2-way visual indication of cursor location. This can be turned on / off in the settings menu

3d graph supports multi point select (hold alt key) and drag (hold left mouse button).

3d graph can be viewed transposed. Default is EFI-Live orientation. 

Y axis supports text labels and there is an axis import tool to generate the Y axis points from PCMTEC.

As is the case with tools created for personal use there are limitations, quirks and bugs. 

![alt text](https://i.imgur.com/upWuZAM.png)

Utilises the following libraries:

Editor3d library from Elmue https://www.codeproject.com/Articles/5293980/Editor3D-A-Windows-Forms-Render-Control-with-inter

Microtimer library from ken.loveday https://www.codeproject.com/Articles/98346/Microsecond-and-Millisecond-NET-Timer

Licensing: For private use do what the fuck you want. Commercial use requires a dowry, virgins or $ to me, Elmue & ken
