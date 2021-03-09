@ECHO OFF

pushd %~dp0

REM Command file for Sphinx documentation

if "%SPHINXBUILD%" == "" (
	set SPHINXBUILD=sphinx-build
)
set SOURCEDIR=.
set BUILDDIR=_build

if "%1" == "" goto help
if "%1" == "serve" goto serve

%SPHINXBUILD% >NUL 2>NUL
if errorlevel 9009 (
	echo.
	echo.The 'sphinx-build' command was not found. Make sure you have Sphinx
	echo.installed, then set the SPHINXBUILD environment variable to point
	echo.to the full path of the 'sphinx-build' executable. Alternatively you
	echo.may add the Sphinx directory to PATH.
	echo.
	echo.If you don't have Sphinx installed, grab it from
	echo.http://sphinx-doc.org/
	exit /b 1
)

if "%1" == "pdf-hdrp" goto pdf-hdrp
if "%1" == "pdf-urp" goto pdf-urp
if "%1" == "pdf-birp" goto pdf-birp
if "%1" == "pdf" goto pdf

%SPHINXBUILD% -M %1 %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%
goto end

:pdf

:pdf-birp
%SPHINXBUILD% -M latexpdf %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% -E -a -t birp -t no-tabs %O%
MOVE /Y _build\latex\crest.pdf _build\crest-birp.pdf
if NOT "%1" == "pdf" goto end

:pdf-urp
%SPHINXBUILD% -M latexpdf %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% -E -a -t urp -t no-tabs %O%
MOVE /Y _build\latex\crest.pdf _build\crest-urp.pdf
if NOT "%1" == "pdf" goto end

:pdf-hdrp
%SPHINXBUILD% -M latexpdf %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% -E -a -t hdrp -t no-tabs %O%
MOVE /Y _build\latex\crest.pdf _build\crest-hdrp.pdf
goto end

:serve
py -m http.server -d _build\html
goto end

:help
%SPHINXBUILD% -M help %SOURCEDIR% %BUILDDIR% %SPHINXOPTS% %O%

:end
popd
