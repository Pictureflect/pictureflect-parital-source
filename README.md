# Pictureflect Partial Source
Some of the more generic source code from Pictureflect Photo Viewer.

## Introduction and disclaimer
The purpose of this repository is to share some of the more reusable code from Pictureflect Photo Viewer. It is provided as a Visual Studio solution to make it easy to experiment with, and it also demonstrates the use of some custom controls.
This is not intended to be used for development, and the quality of code is variable. In general code has been copied directly from the Pictureflect Photo Viewer source with a namespace change, and sometimes some omissions.
However, you are still welcome to create issues if you spot and bugs, have questions about usage, or have suggestions for other code to share. The code is released under an MIT license, so may be freely used elsewhere.

There are generally very few comments in the source code, and it may hard to follow in places, but there is some basic documentation of the classes/controls below.

## Documentation of classes/controls
This is a work in progress...

### CustomShadowControl
This was originally based on the [DropShadowPanel](https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/dropshadowpanel) control in the Windows Community Toolkit. However, it adds an important new property - SpreadRadius.
In particular, using SpreadRadius with zero BlurRadius allows you to add a stroke to text. See MainPage.xaml for example usage. The stroke will be slightly blurred for large radiuses because the spread is achieved using a blur effect on an alpha mask then a color matrix effect to sharpen the blur. 
