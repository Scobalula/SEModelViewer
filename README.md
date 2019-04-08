# SEModelViewer

![SEModelViewer](https://i.imgur.com/DnFk6CT.png)

[![Donate](https://img.shields.io/badge/Donate-PayPal-yellowgreen.svg)](https://www.paypal.me/scobalula) ![Downloads](https://img.shields.io/github/downloads/Scobalula/SEModelViewer/total.svg) [![license](https://img.shields.io/github/license/Scobalula/SEModelViewer.svg)]()

SEModelViewer is a tool that allows you to view SEModel files, it was primarly made as a way to easily view models exported by [Wraith](http://aviacreations.com/wraith/) but can work on any valid SEModel files.

## How to Use 

Using SEModelViewer is very easy, to use SEModelViewer you can either load a folder of models by clicking the folder or load a single model/specific models by clicking the folder with a sheet of paper, the folder load method will parse all subdirectories and load any *.semodel files found. If loading a folder that was exported from Wraith, Greyhound, or Legion (folder name contains "models"), it will . While models are loading you're free to view other models, but some parts of the application may be disabled while models are loading.

With models loaded you can click a model and use the arrow keys on your keyboard to cycle through models, you can also disable texture loading to increase model load speeds.

If you'd like to stop the current task (loading models, loading bone counts, etc.) then you can click the X button or press CTRL+X, pressing CTRL+SHIFT+X or double clicking the X button will clear loaded models. 

Pressing CTRL+C will copy the selected model's name, pressing CTRL+SHIFT+C will copy its file.

## Requirements

* Windows 10 64Bit (Windows 7/8/8.1 should work, but untested)
* Microsoft .NET Framework 4.7.2
* Microsoft MSVC++ 2019 Runtime

## License / Disclaimers

SEModelViewer is licensed under the GPL license and it and its source code is free to use and modify under the . SEModelViewer comes with NO warranty, any damages caused are solely the responsibility of the user. See the LICENSE file for more information.

**SEModelViewer is currently in alpha, and with that in mind, bugs, errors, you know, the bad stuff.**

## Download

The latest version can be found on the [Releases Page](https://github.com/Scobalula/SEModelViewer/releases).

## Credits

* DTZxPorter ([SELibDotNet](https://github.com/dtzxporter/SELibDotNet)) ([PyCod](https://github.com/SE2Dev/PyCoD) (for XMODEL_BIN structure))
* SE2Dev ([PyCod](https://github.com/SE2Dev/PyCoD) (for XMODEL_BIN structure))
* Helix Toolkit ([https://github.com/helix-toolkit/helix-toolkit](https://github.com/helix-toolkit/helix-toolkit))
* Amazing people of Stackoverflow

## Support Me

If you use SEModelViewer in any of your projects, it would be appreciated if you provide credit for its use, a lot of time and work went into developing it and a simple credit isn't too much to ask for.

If you'd like to support me even more, consider donating, I develop a lot of apps including SEModelViewer and majority are available free of charge with source code included:

[![Donate](https://img.shields.io/badge/Donate-PayPal-yellowgreen.svg)](https://www.paypal.me/scobalula)
