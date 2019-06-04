@echo off 
set bitrate=1200000
set threads=0
::set fps=30000/1001
set fps=24000/1001
set vsync=1
set aacquality=0.50
set /a bufsize=%bitrate% * 2

if %~x1==.txt (set concat=-f concat) else (set concat=)

:: nerroaac
:: ..\bin\ffmpeg\ffmpeg.exe %concat% -i "./%~nx1" -f wav - | .\bin\ffmpeg\neroAacEnc.exe -q %aacquality% -if - -ignorelength -of ".\%~n1-0.m4a"

:: one pass
..\bin\ffmpeg\ffmpeg.exe %concat% -i "./%~nx1" -an -pass 1 -vcodec libx264 -vsync %vsync% -r %fps% -threads %threads% -b:v %bitrate% -maxrate %bitrate% -bufsize %bufsize% -y -f rawvideo NUL

:: two pass
:: ..\bin\ffmpeg\ffmpeg.exe %concat% -i "./%~nx1" -i "./%~n1-0.m4a" -pass 2 -vcodec libx264 -vsync %vsync% -r %fps% -threads %threads% -b:v %bitrate% -maxrate %bitrate% -bufsize %bufsize% -map_chapters -1 ".\%~n1-0.mp4"
..\bin\ffmpeg\ffmpeg.exe %concat% -i "./%~nx1" -pass 2 -vcodec libx264 -vsync %vsync% -r %fps% -threads %threads% -b:v %bitrate% -maxrate %bitrate% -bufsize %bufsize% -map_chapters -1 -y ".\%~n1-0.mp4"

del ffmpeg2pass-0.*, *.m4a
pause
exit