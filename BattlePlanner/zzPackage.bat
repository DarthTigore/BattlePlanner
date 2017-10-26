robocopy ./Working ./Package *.json
robocopy ./Working ./Package Default.xml
robocopy ./Working ./Package Presets.xml
robocopy ./bin/Release ./Package BattlePlanner.exe*
robocopy ./bin/Release ./Package *.dll

robocopy ./Working ./Test *.json
robocopy ./Working ./Test Default.xml
robocopy ./Working ./Test Presets.xml
robocopy ./bin/Release ./Test BattlePlanner.exe*
robocopy ./bin/Release ./Test *.dll
