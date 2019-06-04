@echo off 
set vsync=1

..\bin\x264\avs4x26x.exe --x26x-binary "..\bin\x264\x264.exe" --bitrate 1000 --pass 1 --fps 24000/1001 --threads 0 --stats "1pass.stats" -o NUL "./%~nx1"
..\bin\x264\avs4x26x.exe --x26x-binary "..\bin\x264\x264.exe" --bitrate 1000 --pass 2 --fps 24000/1001 --threads 0 --stats "1pass.stats" --vf resize:width=1280,height=720,sar=1:1 -o "dd_temp.mp4" "./%~nx1"
..\bin\x264\mp4box -add "dd_temp.mp4" -add "./%~n1.mp4"#audio cfile.mp4

del 1pass.*,*.ffindex
pause
exit