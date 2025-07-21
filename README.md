# Virtual Volume Ray Caster
The Virtual Ray Tracer (VRT) is an educational tool to provide users with an interactive environment for understanding ray tracing concepts. Extending VRT, we created Virtual Volume Raycaster (VVRT), an interactive and gamified application that allows to view and explore the volume raycasting process in real-time. The goal is to help the users—students of Scientific Visualization and the general public—to better understand the steps of volume raycasting and their characteristics, for example the effect of early ray termination. VVRT shows a scene containing a camera casting rays which interact with a volume in the scene. Learners are able to modify and explore different settings, e.g., concerning the transfer function or ray sampling step size. Our educational tool is built with the cross-platform engine Unity, and we make it fully available to be extended and/or adjusted to fit the requirements of courses at other institutions, educational tutorials, or of enthusiasts from the general public. Two user studies demonstrate the effectiveness of VVRT in supporting the understanding and teaching of volume raycasting.

## Download
To try out a ready version of the VVRT download the zip folder with [this dropbox link](https://www.dropbox.com/scl/fo/1manjxd07j5n2zjqg18ld/AMEdYlu-3ltPDNdRuyGZD6c?rlkey=a19gma37iuq2a3o3jhl0akpyo&st=vnu13trr&dl=0) which contians a build version for Windows. Extract the zip folder, open it and launch `VVRTBuild > Virtual Ray Tracer`. The Virtual Volume Ray Caster can be found in the levels menu under the title "Ray Casting"

## Building the Application

As a prerequisite, you need a [Unity 2021.3.12f1 LTS](https://unity3d.com/unity/qa/lts-releases) release. 

- open the `UnityProject` folder with Unity [Unity 2021.3.12f1 LTS](https://unity3d.com/unity/qa/lts-releases)
- naviate to `CreateVoxelGrid > 3DTexture` and click the 3DTexture button to generate the 3D textures
- navigate to `File > Build Settings`, select your desired platform and press 'build'. The application has been tested on Windows
For more information on building Unity applications see the [Unity Manual page](https://docs.unity3d.com/Manual/BuildSettings.html).

## Future

The application is under active development and we hope that you will contribute to Virtual Ray Tracer, too; read on.

## License

The application is released under the MIT license. Therefore, you may use and modify the code as you see fit. If you use or build on the application we would appreciate it if you cited this repository and the Eurographics 2022 paper. As we are still working on the application ourselves, we would also like to hear about any improvements you may have made.

## Contact

Any questions, bug reports or suggestions can be created as an issue on this repository. Alternatively, please contact [Jiri Kosinka](http://www.cs.rug.nl/svcg/People/JiriKosinka).
