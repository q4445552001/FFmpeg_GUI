@echo off 

..\bin\ffmpeg\ffprobe.exe -of json -show_streams -show_format -v quiet ".\%~nx1" > ".\%~n1-0.txt" 