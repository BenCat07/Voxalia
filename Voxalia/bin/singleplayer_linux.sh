#!/bin/sh
cd Release/
mono ./Voxalia.exe "wait 0.1; echo '^0^e^1PLEASE WAIT'; &startlocalserver 28010; connect 127.0.0.1 28010"
cd ../
