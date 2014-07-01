# How to build C++/CX application on Windows 7 with Visual Studio 2013

Based on: http://blogs.microsoft.co.il/pavely/2012/09/29/using-ccx-in-desktop-apps/

1. Create Win32 Console application
  * Choose 'Console application' in Win32 Application Wizard
2. Open project property dialog
3. Open [Configuration Properties]-[C/C++]-[General]
  * Set `Consume Windows Runtime Extension` option `Yes (/ZW)`
  * Add the following directory entries as `Additional #using Directories`:
    * $(WindowsSDK_MetadataPath)
    * $(VCInstallDir_120)vcpackages
4. Open [Configuration Properties]-[C/C++]-[Code Generation]
  * Set `Enable Minimal Rebuild` option `No (/Gm-)`
5. Modify main entry point as follows:

```cpp
#include "stdafx.h"
#include <roapi.h>
#include <iostream>

using namespace std;
using namespace Windows::Globalization;
using namespace Platform;

int main(Array<String^>^ args)
{
  APTTYPE at;
  APTTYPEQUALIFIER atq;
  ::CoGetApartmentType(&at, &atq);

  auto calendar = ref new Calendar;
  calendar->SetToNow();
  wcout << "It's now " <<
    calendar->HourAsPaddedString(2)->Data() << L":" <<
    calendar->MinuteAsPaddedString(2)->Data() << L":" <<
    calendar->SecondAsPaddedString(2)->Data() << endl;

  return 0;
}
```

:warning: We can build this application successfully, but can't run on the Windows 7 environment because some WinRT runtime dlls are missing.
