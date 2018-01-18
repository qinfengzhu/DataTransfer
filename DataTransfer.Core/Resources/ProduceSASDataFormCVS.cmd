@REM this bat's file will call stat/transfer to convert the type of cvs file to sas data,then zip it.    
@REM time:2018-01-17
@REM author:qinfeng.zhu

@REM parameters,1:cvs file and sas file directory,2:zip file directory,3:zipfile name,4:local st.exe path

@echo off

@REM get the paramters which's value will be seted to variable.
Set _DataSourcePath=%1
Set _ZipDirectory=%2
Set _ZipFileName=%3
Set _stPath=%4

@REM if the paramter isn't effective,then quit the process.
if "%_DataSourcePath%" == "" goto ExcuteOver
if "%_ZipDirectory%" == "" goto ExcuteOver
if not exist %_DataSourcePath% (goto ExcuteOver)
if not exist %_ZipDirectory% (goto ExcuteOver)
if not exist %_stPath% (goto normalExcute)
if exist %_stPath% (goto signExcute)
:normalExcute
pushd  %_DataSourcePath%
st "%~dp0%ST_Running_File_sasdata.stcmd"
st "%~dp0%ST_Running_File_xpt.stcmd"
goto zipExcute
:signExcute
pushd  %_DataSourcePath%
%_stPath% "%~dp0%ST_Running_File_sasdata.stcmd"
%_stPath% "%~dp0%ST_Running_File_xpt.stcmd"
goto zipExcute
:zipExcute
@REM zip the transfer data files to one zip file,zip is not window command,so the bussiness we place to csharp code
@REM zip -j %_ZipDirectory%\%_ZipFileName%.zip %_DataSourcePath%\*.sas7bdat %_DataSourcePath%\*.xpt
@REM if you want to delete *.cvs files,*.sas7bdat files,*.xpt files 
popd
@REM over the bat.
:ExcuteOver
Exit