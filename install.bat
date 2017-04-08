@ECHO OFF

set assembly=Crop\bin\Debug\Crop.dll
set addins_dir=%appdata%\Autodesk\Revit\Addins\2017
set copy_fail=Something went wrong during copying

if exist %assembly% (
	echo Copying Crop.dll and Crop.addin
	copy %assembly% %addins_dir%
	copy Crop.addin %addins_dir%
	
	if exist %addins_dir%\Crop.dll (
		if exist %addins_dir%\Crop.addin (
			echo Succesfuly installed
		) else (
			echo %copy_fail%
		)
	) else (
		echo %copy_fail%
	)
) else (
	echo Project must be built at least once
)


:: Pause if executed from shell
for %%x in (%cmdcmdline%) do if /i "%%~x"=="/c" set DOUBLECLICKED=1
if defined DOUBLECLICKED pause