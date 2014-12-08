@echo off
setlocal

call "%vs120comntools%vsvars32.bat"
ilasm MyAsm.il /OUTPUT=OddOrEven.exe
