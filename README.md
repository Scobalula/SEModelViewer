# SEModelViewer

![Downloads](https://img.shields.io/github/downloads/Scobalula/SEModelViewer/total.svg) [![license](https://img.shields.io/github/license/Scobalula/SEModelViewer.svg)]()

SEModelViewer is a tool that allows you to view SEModel files, it provides a quick way to cycle through large amounts of models to find exactly what you want.

## How to Use 

Using SEModelViewer is very easy, to use SEModelViewer you can either load a folder of models by clicking the folder or load a single model/specific models by clicking the folder with a sheet of paper, the folder load method will parse all subdirectories and load any *.semodel files found. If loading a folder that was exported from Wraith, Greyhound, or Legion (folder name contains "models"), it will provide a prompt to ask if you'd like to use a faster loading method that makes assumptions about model names based off the folder names. While models are loading you're free to view other models, but some parts of the application may be disabled while models are loading.

With models loaded you can click a model and use the arrow keys on your keyboard to cycle through models, you can also disable texture loading to increase model load speeds.

If you'd like to stop the current task (loading models, loading bone counts, etc.) then you can click the X button or press *CTRL+X*, pressing *CTRL+SHIFT+X* or double clicking the X button will clear loaded models. 

Pressing *CTRL+C* will copy the selected model's name, pressing CTRL+SHIFT+C will copy its file.

## Requirements

* Windows 10 64Bit (Windows 7/8/8.1 should work, but untested)
* Microsoft .NET Framework 4.8

## License / Disclaimers

SEModelViewer is licensed under the MIT license and it and its source code is free to use and modify under the MIT. SEModelViewer comes with NO warranty, any damages caused are solely the responsibility of the user. See the LICENSE file for more information.

For the most part the tool is stable and works well, it's unlikely to receive new features but if any issues occur then feel free to open an issue with as much info as possible!

## Download

The latest version can be found on the [Releases Page](https://github.com/Scobalula/SEModelViewer/releases).

## Credits

* DTZxPorter ([SELibDotNet](https://github.com/dtzxporter/SELibDotNet))
* Helix Toolkit ([https://github.com/helix-toolkit/helix-toolkit](https://github.com/helix-toolkit/helix-toolkit))
* Amazing people of Stackoverflow

## Support Me

If you use SEModelViewer in any of your projects, it would be appreciated if you provide credit for its use, a lot of time and work went into developing it and a simple credit isn't too much to ask for.

If you'd like to support me even more, consider donating, I develop a lot of apps including SEModelViewer and majority are available free of charge with source code included:

<a href='https://ko-fi.com/Y8Y4CO22U' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=3' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>