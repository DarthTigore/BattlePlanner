robocopy ./bin/Release ./Package *.json
robocopy ./bin/Release ./Package Changes.xml
robocopy ./bin/Release ./Package Default.xml
robocopy ./bin/Release ./Package Presets.xml
robocopy ./bin/Release ./Package Units*x*.xml
robocopy ./bin/Release ./Package BattlePlanner.exe*
robocopy ./bin/Release ./Package *.dll

robocopy ./bin/Release ./Test *.json
robocopy ./bin/Release ./Test Changes.xml
robocopy ./bin/Release ./Test Default.xml
robocopy ./bin/Release ./Test Presets.xml
robocopy ./bin/Release ./Test Units*x*.xml
robocopy ./bin/Release ./Test BattlePlanner.exe*
robocopy ./bin/Release ./Test *.dll
